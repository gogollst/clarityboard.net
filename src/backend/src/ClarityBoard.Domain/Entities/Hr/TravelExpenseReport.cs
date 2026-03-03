namespace ClarityBoard.Domain.Entities.Hr;

public class TravelExpenseReport
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly TripStartDate { get; private set; }
    public DateOnly TripEndDate { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public string BusinessPurpose { get; private set; } = string.Empty;
    public TravelExpenseStatus Status { get; private set; }
    public int TotalAmountCents { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ReimbursedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<TravelExpenseItem> Items { get; private set; } = [];

    private TravelExpenseReport() { }

    public static TravelExpenseReport Create(Guid employeeId, string title,
        DateOnly tripStart, DateOnly tripEnd, string destination, string businessPurpose)
    => new()
    {
        Id              = Guid.NewGuid(),
        EmployeeId      = employeeId,
        Title           = title,
        TripStartDate   = tripStart,
        TripEndDate     = tripEnd,
        Destination     = destination,
        BusinessPurpose = businessPurpose,
        Status          = TravelExpenseStatus.Draft,
        CurrencyCode    = "EUR",
        CreatedAt       = DateTime.UtcNow,
    };

    public void UpdateTotal(int totalAmountCents) => TotalAmountCents = totalAmountCents;

    public void Submit()
    {
        if (Status != TravelExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft reports can be submitted.");
        Status = TravelExpenseStatus.Submitted;
    }

    public void Approve(Guid approvedBy)
    {
        Status     = TravelExpenseStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject() => Status = TravelExpenseStatus.Rejected;

    public void MarkReimbursed()
    {
        Status        = TravelExpenseStatus.Reimbursed;
        ReimbursedAt  = DateTime.UtcNow;
    }
}
