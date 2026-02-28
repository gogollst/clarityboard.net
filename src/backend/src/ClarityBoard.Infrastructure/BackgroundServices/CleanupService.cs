using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service running daily at 04:00 UTC.
/// Performs housekeeping tasks:
///   1. Delete expired RefreshTokens (ExpiresAt &lt; now)
///   2. Delete processed WebhookEvents older than 90 days
///   3. Log the count of deleted records
/// </summary>
public sealed class CleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupService> _logger;

    private const int WebhookRetentionDays = 90;

    public CleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNext0400Utc();
            _logger.LogInformation(
                "CleanupService sleeping until {NextRun} ({Delay})",
                DateTime.UtcNow.Add(delay),
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunCleanupAsync(stoppingToken);
        }

        _logger.LogInformation("CleanupService stopped");
    }

    /// <summary>
    /// Calculates the delay from now until the next 04:00 UTC.
    /// If it is currently 04:00 UTC (within the first minute), runs immediately.
    /// </summary>
    internal static TimeSpan CalculateDelayUntilNext0400Utc()
    {
        var now = DateTime.UtcNow;

        // If we are at 04:00 within the first minute, run immediately
        if (now.Hour == 4 && now.Minute == 0)
            return TimeSpan.Zero;

        // Calculate next 04:00 UTC
        var today0400 = new DateTime(now.Year, now.Month, now.Day, 4, 0, 0, DateTimeKind.Utc);
        var next0400 = now < today0400 ? today0400 : today0400.AddDays(1);

        return next0400 - now;
    }

    internal async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("CleanupService running cleanup at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();

        var expiredTokensDeleted = await CleanupExpiredRefreshTokensAsync(context, ct);
        var webhookEventsDeleted = await CleanupOldWebhookEventsAsync(context, ct);

        _logger.LogInformation(
            "CleanupService completed: {ExpiredTokens} expired refresh tokens deleted, " +
            "{WebhookEvents} processed webhook events deleted",
            expiredTokensDeleted,
            webhookEventsDeleted);
    }

    private async Task<int> CleanupExpiredRefreshTokensAsync(ClarityBoardContext context, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            var deletedCount = await context.RefreshTokens
                .Where(t => t.ExpiresAt < now)
                .ExecuteDeleteAsync(ct);

            if (deletedCount > 0)
                _logger.LogInformation("Deleted {Count} expired refresh tokens", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired refresh tokens");
            return 0;
        }
    }

    private async Task<int> CleanupOldWebhookEventsAsync(ClarityBoardContext context, CancellationToken ct)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-WebhookRetentionDays);
            var deletedCount = await context.WebhookEvents
                .Where(w => w.Status == "completed" && w.ProcessedAt != null && w.ProcessedAt < cutoffDate)
                .ExecuteDeleteAsync(ct);

            if (deletedCount > 0)
                _logger.LogInformation("Deleted {Count} processed webhook events older than {Days} days", deletedCount, WebhookRetentionDays);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old webhook events");
            return 0;
        }
    }
}
