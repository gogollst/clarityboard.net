namespace ClarityBoard.Domain.Entities.Hr;

public class WorkTimeEntry
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public int BreakMinutes { get; private set; }
    public int TotalMinutes { get; private set; }
    public EntryType EntryType { get; private set; }
    public string? ProjectCode { get; private set; }
    public string? Notes { get; private set; }
    public WorkTimeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private WorkTimeEntry() { }

    public static WorkTimeEntry Create(Guid employeeId, DateOnly date, int totalMinutes, Guid createdBy,
        TimeOnly? startTime = null, TimeOnly? endTime = null, int breakMinutes = 0,
        EntryType type = EntryType.Work, string? projectCode = null, string? notes = null)
    => new()
    {
        Id           = Guid.NewGuid(),
        EmployeeId   = employeeId,
        Date         = date,
        StartTime    = startTime,
        EndTime      = endTime,
        BreakMinutes = breakMinutes,
        TotalMinutes = totalMinutes,
        EntryType    = type,
        ProjectCode  = projectCode,
        Notes        = notes,
        Status       = WorkTimeStatus.Open,
        CreatedAt    = DateTime.UtcNow,
        UpdatedBy    = createdBy,
    };

    public void Lock() => Status = WorkTimeStatus.Locked;
}
