namespace ClarityBoard.Domain.Entities.Hr;

public class DeletionRequest
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime ScheduledDeletionAt { get; private set; }
    public DeletionRequestStatus Status { get; private set; }
    public string? BlockReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private DeletionRequest() { }

    public static DeletionRequest Create(Guid employeeId, Guid requestedBy, DateTime scheduledDeletionAt)
    => new()
    {
        Id                  = Guid.NewGuid(),
        EmployeeId          = employeeId,
        RequestedBy         = requestedBy,
        RequestedAt         = DateTime.UtcNow,
        ScheduledDeletionAt = scheduledDeletionAt,
        Status              = DeletionRequestStatus.Pending,
    };

    public void Block(string reason)
    {
        Status      = DeletionRequestStatus.Blocked;
        BlockReason = reason;
    }

    public void Complete()
    {
        Status      = DeletionRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}
