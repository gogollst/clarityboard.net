using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Runs daily at 03:00 UTC to verify all active AI provider API keys are reachable.
/// Updates IsHealthy and LastTestedAt on each AiProviderConfig.
/// </summary>
public sealed class AiHealthCheckService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AiHealthCheckService> _logger;

    public AiHealthCheckService(IServiceProvider sp, ILogger<AiHealthCheckService> logger)
    {
        _sp     = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until 03:00 UTC the first time
        var delay = CalculateDelayUntilNext0300Utc();
        _logger.LogInformation("AiHealthCheckService: first run in {Delay}", delay);
        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        do
        {
            try
            {
                await RunHealthChecksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during AI provider health check");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunHealthChecksAsync(CancellationToken ct)
    {
        using var scope   = _sp.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();
        var aiService     = scope.ServiceProvider.GetRequiredService<IPromptAiService>();

        var configs = await db.AiProviderConfigs
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        _logger.LogInformation("Running health checks for {Count} AI providers", configs.Count);

        foreach (var config in configs)
        {
            try
            {
                var isHealthy = await aiService.TestProviderAsync(config.Provider, ct);
                config.SetHealthStatus(isHealthy);

                _logger.LogInformation(
                    "AI provider {Provider}: {Status}",
                    config.Provider,
                    isHealthy ? "healthy" : "unhealthy");
            }
            catch (Exception ex)
            {
                config.SetHealthStatus(false);
                _logger.LogWarning(ex, "Health check exception for provider {Provider}", config.Provider);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    internal static TimeSpan CalculateDelayUntilNext0300Utc()
    {
        var now         = DateTime.UtcNow;
        var todayAt0300 = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
        var next0300    = now < todayAt0300 ? todayAt0300 : todayAt0300.AddDays(1);
        return next0300 - now;
    }
}

