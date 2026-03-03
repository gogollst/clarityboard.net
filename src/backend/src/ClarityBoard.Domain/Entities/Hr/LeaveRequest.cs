namespace ClarityBoard.Domain.Entities.Hr;

public class LeaveRequest
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal WorkingDays { get; private set; }
    public bool HalfDay { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? Notes { get; private set; }

    private LeaveRequest() { }

    public static LeaveRequest Create(Guid employeeId, Guid leaveTypeId, DateOnly startDate, DateOnly endDate,
        decimal workingDays, bool halfDay = false, string? notes = null)
    => new()
    {
        Id          = Guid.NewGuid(),
        EmployeeId  = employeeId,
        LeaveTypeId = leaveTypeId,
        StartDate   = startDate,
        EndDate     = endDate,
        WorkingDays = workingDays,
        HalfDay     = halfDay,
        Status      = LeaveRequestStatus.Pending,
        RequestedAt = DateTime.UtcNow,
        Notes       = notes,
    };

    public void Approve(Guid approvedBy)
    {
        Status     = LeaveRequestStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status          = LeaveRequestStatus.Rejected;
        RejectionReason = reason;
    }

    public void Cancel() => Status = LeaveRequestStatus.Cancelled;
}

