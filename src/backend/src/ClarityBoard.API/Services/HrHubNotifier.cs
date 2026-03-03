using ClarityBoard.API.Hubs;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Services;

/// <summary>
/// Pushes real-time HR events to connected clients via SignalR HrHub.
/// </summary>
public class HrHubNotifier : IHrHubNotifier
{
    private readonly IHubContext<HrHub> _hub;

    public HrHubNotifier(IHubContext<HrHub> hub) => _hub = hub;

    public Task NotifySalaryUpdatedAsync(Guid entityId, Guid employeeId, CancellationToken ct)
        => _hub.Clients.Group($"hr:{entityId}").SendAsync("SalaryUpdated", new { EmployeeId = employeeId }, ct);

    public Task NotifyLeaveRequestUpdatedAsync(Guid entityId, Guid requestId, string status, CancellationToken ct)
        => _hub.Clients.Group($"hr:{entityId}").SendAsync("LeaveRequestUpdated", new { RequestId = requestId, Status = status }, ct);

    public Task NotifyEmployeeStatusChangedAsync(Guid entityId, Guid employeeId, string newStatus, CancellationToken ct)
        => _hub.Clients.Group($"hr:{entityId}").SendAsync("EmployeeStatusChanged", new { EmployeeId = employeeId, Status = newStatus }, ct);

    public Task NotifyTravelExpenseUpdatedAsync(Guid entityId, Guid reportId, string status, CancellationToken ct)
        => _hub.Clients.Group($"hr:{entityId}").SendAsync("TravelExpenseUpdated", new { ReportId = reportId, Status = status }, ct);
}
