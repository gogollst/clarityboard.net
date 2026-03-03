namespace ClarityBoard.Application.Common.Interfaces;

public interface IHrHubNotifier
{
    Task NotifySalaryUpdatedAsync(Guid entityId, Guid employeeId, CancellationToken ct);
    Task NotifyLeaveRequestUpdatedAsync(Guid entityId, Guid requestId, string status, CancellationToken ct);
    Task NotifyEmployeeStatusChangedAsync(Guid entityId, Guid employeeId, string newStatus, CancellationToken ct);
    Task NotifyTravelExpenseUpdatedAsync(Guid entityId, Guid reportId, string status, CancellationToken ct);
}
