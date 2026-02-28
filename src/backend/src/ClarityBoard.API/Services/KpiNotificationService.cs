using ClarityBoard.API.Hubs;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Services;

/// <summary>
/// Pushes real-time KPI and alert updates to connected SignalR clients.
/// Uses KpiHub for KPI value changes and AlertHub for alert events.
/// </summary>
public class KpiNotificationService : IKpiNotificationService
{
    private readonly IHubContext<KpiHub> _kpiHub;
    private readonly IHubContext<AlertHub> _alertHub;
    private readonly ILogger<KpiNotificationService> _logger;

    public KpiNotificationService(
        IHubContext<KpiHub> kpiHub,
        IHubContext<AlertHub> alertHub,
        ILogger<KpiNotificationService> logger)
    {
        _kpiHub = kpiHub;
        _alertHub = alertHub;
        _logger = logger;
    }

    public async Task NotifyKpiUpdateAsync(
        Guid entityId, KpiSummaryDto kpi, CancellationToken ct = default)
    {
        var groupName = $"entity:{entityId}";

        _logger.LogInformation(
            "Sending KpiUpdated to group {Group} for KPI {KpiId}",
            groupName, kpi.KpiId);

        await _kpiHub.Clients
            .Group(groupName)
            .SendAsync("KpiUpdated", kpi, ct);
    }

    public async Task NotifyAlertAsync(
        Guid entityId, AlertEventDto alert, CancellationToken ct = default)
    {
        var groupName = $"alerts:{entityId}";

        _logger.LogInformation(
            "Sending AlertTriggered to group {Group} for alert {AlertId}",
            groupName, alert.Id);

        await _alertHub.Clients
            .Group(groupName)
            .SendAsync("AlertTriggered", alert, ct);
    }
}
