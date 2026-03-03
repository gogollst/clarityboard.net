namespace ClarityBoard.Domain.Entities.Hr;

public class LeaveBalance
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public int Year { get; private set; }
    public decimal EntitlementDays { get; private set; }
    public decimal UsedDays { get; private set; }
    public decimal PendingDays { get; private set; }
    public decimal CarryOverDays { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LeaveBalance() { }

    public static LeaveBalance Create(Guid employeeId, Guid leaveTypeId, int year,
        decimal entitlementDays, decimal carryOverDays = 0)
    => new()
    {
        Id             = Guid.NewGuid(),
        EmployeeId     = employeeId,
        LeaveTypeId    = leaveTypeId,
        Year           = year,
        EntitlementDays = entitlementDays,
        CarryOverDays  = carryOverDays,
        UpdatedAt      = DateTime.UtcNow,
    };

    public decimal RemainingDays => EntitlementDays + CarryOverDays - UsedDays - PendingDays;

    public void AddPending(decimal days)    { PendingDays += days; UpdatedAt = DateTime.UtcNow; }
    public void ApprovePending(decimal days) { PendingDays -= days; UsedDays += days; UpdatedAt = DateTime.UtcNow; }
    public void RejectPending(decimal days)  { PendingDays -= days; UpdatedAt = DateTime.UtcNow; }
}
