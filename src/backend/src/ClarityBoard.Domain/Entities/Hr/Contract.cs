namespace ClarityBoard.Domain.Entities.Hr;

public class Contract
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public ContractType ContractType { get; private set; }
    public decimal WeeklyHours { get; private set; }
    public int WorkdaysPerWeek { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? ProbationEndDate { get; private set; }
    public int NoticeWeeks { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;

    private Contract() { }

    public static Contract Create(Guid employeeId, ContractType type, decimal weeklyHours,
        int workdaysPerWeek, DateOnly startDate, int noticeWeeks, Guid createdBy, string changeReason,
        DateOnly? endDate = null, DateOnly? probationEndDate = null)
    => new()
    {
        Id               = Guid.NewGuid(),
        EmployeeId       = employeeId,
        ContractType     = type,
        WeeklyHours      = weeklyHours,
        WorkdaysPerWeek  = workdaysPerWeek,
        StartDate        = startDate,
        EndDate          = endDate,
        ProbationEndDate = probationEndDate,
        NoticeWeeks      = noticeWeeks,
        ValidFrom        = DateTime.UtcNow,
        CreatedBy        = createdBy,
        ChangeReason     = changeReason,
    };

    public void Close(DateTime validTo)
    {
        if (validTo <= ValidFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom.", nameof(validTo));
        ValidTo = validTo;
    }
}
