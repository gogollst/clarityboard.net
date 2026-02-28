using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.KPI;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Domain.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class KpiRecalculationConsumer : IConsumer<RecalculateKpis>
{
    private readonly IAppDbContext _dbContext;
    private readonly IKpiRepository _kpiRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<KpiRecalculationConsumer> _logger;

    public KpiRecalculationConsumer(
        IAppDbContext dbContext,
        IKpiRepository kpiRepository,
        IServiceProvider serviceProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<KpiRecalculationConsumer> logger)
    {
        _dbContext = dbContext;
        _kpiRepository = kpiRepository;
        _serviceProvider = serviceProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RecalculateKpis> context)
    {
        var entityId = context.Message.EntityId;
        var snapshotDate = context.Message.SnapshotDate;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "Recalculating KPIs for entity {EntityId} on {SnapshotDate}",
            entityId, snapshotDate);

        // Get all active KPI definitions
        var definitions = await _kpiRepository.GetDefinitionsAsync(ct: ct);
        if (definitions.Count == 0)
        {
            _logger.LogWarning("No active KPI definitions found, skipping recalculation");
            return;
        }

        // Group definitions by their CalculationClass so we call each calculator once
        var definitionsByCalculator = definitions
            .Where(d => !string.IsNullOrEmpty(d.CalculationClass))
            .GroupBy(d => d.CalculationClass!)
            .ToList();

        // Resolve all registered IKpiCalculationService implementations
        var calculators = _serviceProvider
            .GetServices<IKpiCalculationService>()
            .ToDictionary(c => c.CalculatorName, c => c);

        var snapshots = new List<KpiSnapshot>();
        var alertMessages = new List<EvaluateAlerts>();
        var calculatedCount = 0;
        var skippedCount = 0;

        foreach (var group in definitionsByCalculator)
        {
            var calculatorName = group.Key;

            if (!calculators.TryGetValue(calculatorName, out var calculator))
            {
                _logger.LogWarning(
                    "No calculator registered for '{CalculatorName}', skipping {Count} KPIs",
                    calculatorName, group.Count());
                skippedCount += group.Count();
                continue;
            }

            Dictionary<string, decimal?> results;
            try
            {
                results = await calculator.CalculateAsync(entityId, snapshotDate, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Calculator '{CalculatorName}' failed for entity {EntityId} on {SnapshotDate}",
                    calculatorName, entityId, snapshotDate);
                skippedCount += group.Count();
                continue;
            }

            foreach (var definition in group)
            {
                // Check if the calculator returned a result for this KPI
                if (!results.TryGetValue(definition.Id, out var value))
                {
                    _logger.LogDebug(
                        "Calculator '{CalculatorName}' did not return a value for KPI {KpiId}",
                        calculatorName, definition.Id);
                    skippedCount++;
                    continue;
                }

                // Get the previous snapshot for change percentage calculation
                KpiSnapshot? previousSnapshot = null;
                try
                {
                    previousSnapshot = await _kpiRepository.GetLatestSnapshotAsync(
                        entityId, definition.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to retrieve previous snapshot for KPI {KpiId}, proceeding without it",
                        definition.Id);
                }

                var previousValue = previousSnapshot?.Value;

                // Create the snapshot
                var snapshot = KpiSnapshot.Create(
                    entityId: entityId,
                    kpiId: definition.Id,
                    snapshotDate: snapshotDate,
                    value: value,
                    previousValue: previousValue,
                    targetValue: null,
                    components: null);

                snapshots.Add(snapshot);
                calculatedCount++;

                // Queue alert evaluation for KPIs with a non-null value
                if (value.HasValue)
                {
                    alertMessages.Add(new EvaluateAlerts(
                        entityId,
                        definition.Id,
                        value.Value,
                        previousValue));
                }
            }
        }

        // Batch persist all snapshots
        if (snapshots.Count > 0)
        {
            try
            {
                await _kpiRepository.AddSnapshotsAsync(snapshots, ct);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Persisted {Count} KPI snapshots for entity {EntityId} on {SnapshotDate}",
                    snapshots.Count, entityId, snapshotDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to persist KPI snapshots for entity {EntityId} on {SnapshotDate}",
                    entityId, snapshotDate);
                throw; // Let MassTransit retry
            }
        }

        // Publish alert evaluation messages
        foreach (var alertMsg in alertMessages)
        {
            try
            {
                await _publishEndpoint.Publish(alertMsg, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish EvaluateAlerts for entity {EntityId}, KPI {KpiId}",
                    alertMsg.EntityId, alertMsg.KpiId);
                // Non-critical: continue with remaining alerts
            }
        }

        _logger.LogInformation(
            "KPI recalculation completed for entity {EntityId}: {Calculated} calculated, {Skipped} skipped, {Alerts} alert evaluations queued",
            entityId, calculatedCount, skippedCount, alertMessages.Count);
    }
}
