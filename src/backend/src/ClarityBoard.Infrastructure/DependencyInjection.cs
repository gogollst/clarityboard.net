using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.CashFlow.Services;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Domain.Services;
using ClarityBoard.Infrastructure.BackgroundServices;
using ClarityBoard.Infrastructure.Messaging.Consumers;
using ClarityBoard.Infrastructure.Persistence;
using ClarityBoard.Infrastructure.Services;
using ClarityBoard.Infrastructure.Services.AI;
using ClarityBoard.Infrastructure.Services.Cache;
using ClarityBoard.Infrastructure.Services.Datev;
using ClarityBoard.Infrastructure.Services.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Minio;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClarityBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        services.AddDbContext<ClarityBoardContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), npgsql =>
            {
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsql.CommandTimeout(30);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        // Register IAppDbContext
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<ClarityBoardContext>());

        // Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "clarityboard:";
        });

        // Redis ConnectionMultiplexer for ICacheService
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // MassTransit + RabbitMQ
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddConsumer<WebhookProcessorConsumer>();
            bus.AddConsumer<DocumentProcessingConsumer>();
            bus.AddConsumer<KpiRecalculationConsumer>();
            bus.AddConsumer<AlertEvaluationConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration.GetConnectionString("RabbitMQ")
                    ?? "rabbitmq://guest:guest@localhost:5672";
                cfg.Host(new Uri(rabbitHost));

                cfg.ConfigureEndpoints(context);

                cfg.UseMessageRetry(r => r.Exponential(
                    3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(5)));
            });
        });

        // Repositories
        services.AddScoped<IAccountingRepository, Repositories.AccountingRepository>();
        services.AddScoped<IKpiRepository, Repositories.KpiRepository>();
        services.AddScoped<IEntityRepository, Repositories.EntityRepository>();

        // Services
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IWorkingCapitalService, WorkingCapitalService>();
        services.AddScoped<IMessagePublisher, MassTransitMessagePublisher>();

        // KPI Calculators (registered as IKpiCalculationService for resolution by CalculatorName)
        services.AddScoped<IKpiCalculationService, ClarityBoard.Application.Services.FinancialKpiCalculator>();
        services.AddScoped<IKpiCalculationService, SalesKpiCalculator>();
        services.AddScoped<IKpiCalculationService, MarketingKpiCalculator>();
        services.AddScoped<IKpiCalculationService, HrKpiCalculator>();
        services.AddScoped<IKpiCalculationService, GeneralKpiCalculator>();

        // Cash Flow Forecast
        services.AddScoped<ICashFlowForecastService, CashFlowForecastService>();

        // Scenario Engine
        services.AddScoped<IScenarioEngine, ScenarioEngine>();

        // Depreciation Service
        services.AddScoped<IDepreciationService, DepreciationService>();

        // DATEV Export
        services.AddScoped<DatevExportService>();

        // Background Services
        services.AddHostedService<RecurringEntryService>();
        services.AddHostedService<BackgroundServices.KpiRecalculationService>();
        services.AddHostedService<CleanupService>();
        services.AddHostedService<DepreciationBackgroundService>();

        // Exchange Rate Service (background service + queryable service)
        services.AddHttpClient("ecb");
        services.AddSingleton<BackgroundServices.ExchangeRateService>();
        services.AddSingleton<IExchangeRateService>(sp => sp.GetRequiredService<BackgroundServices.ExchangeRateService>());
        services.AddHostedService(sp => sp.GetRequiredService<BackgroundServices.ExchangeRateService>());

        // Legacy AI Service (Claude) – used by DocumentProcessingConsumer
        services.AddHttpClient<IAiService, ClaudeAiProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Encryption service for API keys (AES-256-GCM)
        services.AddSingleton<IEncryptionService, ClarityBoard.Infrastructure.Services.AI.AesEncryptionService>();

        // Mail service (SMTP with retry + DB logging)
        services.AddScoped<IEmailService, ClarityBoard.Infrastructure.Services.Mail.SmtpEmailService>();

        // Named HttpClient for the prompt AI service (30s timeout, no auth headers – each call sets its own)
        services.AddHttpClient("ai_prompt", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // Prompt-based AI service (new, centralised engine)
        services.AddScoped<IPromptAiService, ClarityBoard.Infrastructure.Services.AI.PromptAiService>();

        // Background health check for AI providers (daily at 03:00 UTC)
        services.AddHostedService<ClarityBoard.Infrastructure.BackgroundServices.AiHealthCheckService>();

        // MinIO Document Storage
        services.AddSingleton<IMinioClient>(sp =>
        {
            var endpoint = configuration["Storage:MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = configuration["Storage:MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = configuration["Storage:MinIO:SecretKey"] ?? "minioadmin";

            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        });
        services.AddScoped<IDocumentStorage, MinioDocumentStorage>();

        // Recurring Pattern Detector
        services.AddScoped<IRecurringPatternDetector, RecurringPatternDetectorService>();

        return services;
    }

    /// <summary>
    /// Polly circuit breaker: opens after 3 failures within 30 seconds,
    /// stays half-open for 60 seconds before testing again.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(60),
                onBreak: (_, duration) => { },
                onReset: () => { });
    }
}
