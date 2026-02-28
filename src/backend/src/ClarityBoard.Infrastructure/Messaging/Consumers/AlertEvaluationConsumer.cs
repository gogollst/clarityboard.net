using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.KPI;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class AlertEvaluationConsumer : IConsumer<EvaluateAlerts>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAlertNotificationService _alertNotification;
    private readonly ILogger<AlertEvaluationConsumer> _logger;

    // Epsilon for decimal equality comparison
    private const decimal DecimalEpsilon = 0.0001m;

    public AlertEvaluationConsumer(
        IAppDbContext dbContext,
        IAlertNotificationService alertNotification,
        ILogger<AlertEvaluationConsumer> logger)
    {
        _dbContext = dbContext;
        _alertNotification = alertNotification;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EvaluateAlerts> context)
    {
        var entityId = context.Message.EntityId;
        var kpiId = context.Message.KpiId;
        var value = context.Message.Value;
        var previousValue = context.Message.PreviousValue;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "Evaluating alerts for entity {EntityId}, KPI {KpiId} (value={Value}, previous={PreviousValue})",
            entityId, kpiId, value, previousValue);

        // Query active alerts for this entity + KPI
        var alerts = await _dbContext.KpiAlerts
            .Where(a => a.EntityId == entityId
                     && a.KpiId == kpiId
                     && a.IsActive)
            .ToListAsync(ct);

        if (alerts.Count == 0)
        {
            _logger.LogDebug("No active alerts configured for entity {EntityId}, KPI {KpiId}", entityId, kpiId);
            return;
        }

        var triggeredCount = 0;

        foreach (var alert in alerts)
        {
            try
            {
                var isTriggered = EvaluateCondition(
                    alert.Condition,
                    value,
                    previousValue,
                    alert.ThresholdValue);

                if (!isTriggered)
                {
                    _logger.LogDebug(
                        "Alert {AlertId} ({AlertName}) not triggered: condition '{Condition}' threshold={Threshold}, value={Value}",
                        alert.Id, alert.Name, alert.Condition, alert.ThresholdValue, value);
                    continue;
                }

                // Build descriptive title and message
                var title = $"Alert: {alert.Name}";
                var message = BuildAlertMessage(alert, value, previousValue);

                // Create alert event
                var alertEvent = KpiAlertEvent.Create(
                    alertId: alert.Id,
                    entityId: entityId,
                    kpiId: kpiId,
                    currentValue: value,
                    thresholdValue: alert.ThresholdValue,
                    severity: alert.Severity,
                    title: title,
                    message: message);

                await _dbContext.KpiAlertEvents.AddAsync(alertEvent, ct);
                triggeredCount++;

                _logger.LogInformation(
                    "Alert triggered: {AlertName} ({Severity}) for entity {EntityId}, KPI {KpiId} - {Message}",
                    alert.Name, alert.Severity, entityId, kpiId, message);

                // Push real-time notification via SignalR
                try
                {
                    await _alertNotification.SendAlertAsync(entityId, new
                    {
                        alertEvent.Id,
                        alertEvent.AlertId,
                        alertEvent.EntityId,
                        alertEvent.KpiId,
                        alertEvent.CurrentValue,
                        alertEvent.ThresholdValue,
                        alertEvent.Severity,
                        alertEvent.Title,
                        alertEvent.Message,
                        alertEvent.Status,
                        alertEvent.CreatedAt,
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send SignalR notification for alert {AlertId}, continuing",
                        alert.Id);
                    // Non-critical: the event is still persisted
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to evaluate alert {AlertId} ({AlertName}) for entity {EntityId}",
                    alert.Id, alert.Name, entityId);
                // Continue evaluating remaining alerts
            }
        }

        // Persist all alert events in a single save
        if (triggeredCount > 0)
        {
            try
            {
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Persisted {Count} alert events for entity {EntityId}, KPI {KpiId}",
                    triggeredCount, entityId, kpiId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to persist alert events for entity {EntityId}, KPI {KpiId}",
                    entityId, kpiId);
                throw; // Let MassTransit retry
            }
        }
        else
        {
            _logger.LogDebug(
                "No alerts triggered for entity {EntityId}, KPI {KpiId}",
                entityId, kpiId);
        }
    }

    /// <summary>
    /// Evaluates a single alert condition against the current value.
    /// </summary>
    internal static bool EvaluateCondition(
        string condition,
        decimal value,
        decimal? previousValue,
        decimal threshold)
    {
        return condition.ToLowerInvariant() switch
        {
            "lt" => value < threshold,
            "gt" => value > threshold,
            "lte" => value <= threshold,
            "gte" => value >= threshold,
            "eq" => Math.Abs(value - threshold) < DecimalEpsilon,
            "change_pct_gt" => EvaluateChangePct(value, previousValue, threshold),
            _ => false,
        };
    }

    private static bool EvaluateChangePct(
        decimal value,
        decimal? previousValue,
        decimal threshold)
    {
        if (!previousValue.HasValue || previousValue.Value == 0)
            return false;

        var changePct = Math.Abs(((value - previousValue.Value) / Math.Abs(previousValue.Value)) * 100);
        return changePct > threshold;
    }

    private static string BuildAlertMessage(
        KpiAlert alert,
        decimal value,
        decimal? previousValue)
    {
        var conditionDescription = alert.Condition.ToLowerInvariant() switch
        {
            "lt" => $"Value {value:N2} is below threshold {alert.ThresholdValue:N2}",
            "gt" => $"Value {value:N2} exceeds threshold {alert.ThresholdValue:N2}",
            "lte" => $"Value {value:N2} is at or below threshold {alert.ThresholdValue:N2}",
            "gte" => $"Value {value:N2} is at or above threshold {alert.ThresholdValue:N2}",
            "eq" => $"Value {value:N2} equals threshold {alert.ThresholdValue:N2}",
            "change_pct_gt" => FormatChangePctMessage(value, previousValue, alert.ThresholdValue),
            _ => $"Condition '{alert.Condition}' met for value {value:N2}",
        };

        return $"{alert.Name}: {conditionDescription}";
    }

    private static string FormatChangePctMessage(
        decimal value,
        decimal? previousValue,
        decimal threshold)
    {
        if (!previousValue.HasValue || previousValue.Value == 0)
            return $"Value {value:N2} changed (no previous value available)";

        var changePct = ((value - previousValue.Value) / Math.Abs(previousValue.Value)) * 100;
        return $"Value changed by {changePct:N2}% (from {previousValue.Value:N2} to {value:N2}), exceeding threshold of {threshold:N2}%";
    }
}
