using System.Text;
using System.Threading.RateLimiting;
using ClarityBoard.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using ClarityBoard.API.Middleware;
using ClarityBoard.API.Services;
using ClarityBoard.Application;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================
// Logging
// ============================
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ClarityBoard.API")
        .WriteTo.Console();
});

// ============================
// Services
// ============================

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Current user context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Real-time notifications (SignalR bridge for Infrastructure consumers)
builder.Services.AddScoped<IAlertNotificationService, AlertNotificationService>();
builder.Services.AddScoped<IKpiNotificationService, KpiNotificationService>();

// Controllers + API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ClarityBoard API", Version = "v1" });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token (from POST /api/Auth/login)",
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = new List<string>(),
    });
});

// CORS
builder.Services.AddCors(options =>
{
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                     ?? ["http://localhost:3000"];
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("authenticated", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 300;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 20;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Try again later.",
        }, ct);
    };
});

// Authentication (JWT)
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "DEVELOPMENT_SECRET_KEY_CHANGE_IN_PRODUCTION_MIN_32_CHARS!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClarityBoard",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ClarityBoard.Client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // SignalR token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// Health Checks
builder.Services.AddHealthChecks();

// SignalR
builder.Services.AddSignalR();

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// ============================
// Database Migration
// ============================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClarityBoard.Infrastructure.Persistence.ClarityBoardContext>();
    await db.Database.MigrateAsync();
}

// ============================
// Seed Data
// ============================
using (var scope = app.Services.CreateScope())
{
    await ClarityBoard.Infrastructure.Persistence.Seed.SeedData.InitializeAsync(app.Services);
}

// ============================
// Middleware Pipeline
// ============================
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditLoggingMiddleware>();

// Controllers
app.MapControllers();

// SignalR Hubs
app.MapHub<KpiHub>("/hubs/kpi");
app.MapHub<AlertHub>("/hubs/alerts");

// Health checks
app.MapHealthChecks("/health");

app.Run();
