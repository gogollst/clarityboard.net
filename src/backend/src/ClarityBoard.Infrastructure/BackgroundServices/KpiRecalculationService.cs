using ClarityBoard.Application.Common.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClarityBoard.Infrastructure.Persistence;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs daily at 02:00 UTC.
/// For each active legal entity, publishes a RecalculateKpis message
/// so that the KPI calculation pipeline executes asynchronously.
/// </summary>
public sealed class KpiRecalculationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KpiRecalculationService> _logger;

    public KpiRecalculationService(
        IServiceScopeFactory scopeFactory,
        ILogger<KpiRecalculationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KpiRecalculationService started");

        // Calculate initial delay to next 02:00 UTC
        var initialDelay = CalculateDelayUntilNext0200Utc();
        _logger.LogInformation(
            "KpiRecalculationService will first run at {NextRun} (in {Delay})",
            DateTime.UtcNow.Add(initialDelay),
            initialDelay);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KpiRecalculationService stopped before first run");
            return;
        }

        // Use a 24-hour periodic timer after the initial delay
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        // Execute immediately after initial delay, then on each tick
        do
        {
            await PublishRecalculationMessagesAsync(stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));

        _logger.LogInformation("KpiRecalculationService stopped");
    }

    /// <summary>
    /// Calculates the delay from now until the next 02:00 UTC.
    /// If the current time is exactly 02:00 (within the first minute), returns zero.
    /// </summary>
    internal static TimeSpan CalculateDelayUntilNext0200Utc()
    {
        var now = DateTime.UtcNow;

        // If we are at 02:00 within the first minute, run immediately
        if (now.Hour == 2 && now.Minute == 0)
            return TimeSpan.Zero;

        // Calculate next 02:00 UTC
        var todayAt0200 = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc);
        var next0200 = now < todayAt0200 ? todayAt0200 : todayAt0200.AddDays(1);

        return next0200 - now;
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    internal async Task PublishRecalculationMessagesAsync(CancellationToken ct)
    {
        // The snapshot covers yesterday (the day the calculation covers)
        var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _logger.LogInformation(
            "KpiRecalculationService publishing recalculation messages for snapshot date {SnapshotDate}",
            snapshotDate);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        List<(Guid Id, string Name)> entities;
        try
        {
            entities = await context.LegalEntities
                .Where(e => e.IsActive)
                .Select(e => new { e.Id, e.Name })
                .ToListAsync(ct)
                .ContinueWith(t => t.Result.Select(e => (e.Id, e.Name)).ToList(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query active legal entities");
            return;
        }

        if (entities.Count == 0)
        {
            _logger.LogWarning("No active legal entities found, skipping KPI recalculation");
            return;
        }

        _logger.LogInformation("Publishing KPI recalculation for {Count} entities", entities.Count);

        var publishedCount = 0;
        var failedCount = 0;

        foreach (var (entityId, entityName) in entities)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                await publishEndpoint.Publish(
                    new RecalculateKpis(entityId, snapshotDate),
                    ct);
                publishedCount++;

                _logger.LogDebug(
                    "Published RecalculateKpis for entity {EntityId} ({EntityName})",
                    entityId, entityName);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "Failed to publish RecalculateKpis for entity {EntityId} ({EntityName})",
                    entityId, entityName);
            }
        }

        _logger.LogInformation(
            "KpiRecalculationService completed: {Published} published, {Failed} failed",
            publishedCount, failedCount);
    }
}
