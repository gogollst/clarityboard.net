using ClarityBoard.Application.Features.KPI.DTOs;

namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Pushes real-time KPI and alert updates to connected SignalR clients.
/// </summary>
public interface IKpiNotificationService
{
    /// <summary>
    /// Notifies all clients subscribed to the entity's KPI group
    /// that a KPI value has been updated.
    /// </summary>
    Task NotifyKpiUpdateAsync(Guid entityId, KpiSummaryDto kpi, CancellationToken ct = default);

    /// <summary>
    /// Notifies all clients subscribed to the entity's alert group
    /// that a new alert event has been triggered.
    /// </summary>
    Task NotifyAlertAsync(Guid entityId, AlertEventDto alert, CancellationToken ct = default);
}
