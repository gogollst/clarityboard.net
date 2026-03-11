namespace ClarityBoard.Domain.Entities.Accounting;

public class FiscalPeriod
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public short Year { get; private set; }
    public short Month { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Status { get; private set; } = "open"; // open, soft_closed, hard_closed
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTime? ExportedAt { get; private set; }
    public Guid? ExportedBy { get; private set; }
    public int ExportCount { get; private set; } = 0;

    private FiscalPeriod() { }

    public static FiscalPeriod Create(Guid entityId, short year, short month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Year = year,
            Month = month,
            StartDate = start,
            EndDate = end,
        };
    }

    public void SoftClose(Guid closedBy)
    {
        Status = "soft_closed";
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
    }

    public void HardClose(Guid closedBy)
    {
        Status = "hard_closed";
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
    }

    public void Reopen()
    {
        if (Status == "hard_closed")
            throw new InvalidOperationException("Cannot reopen a hard-closed period.");
        Status = "open";
        ClosedAt = null;
        ClosedBy = null;
    }

    public void MarkExported(Guid exportedBy)
    {
        Status = "exported";
        ExportedAt = DateTime.UtcNow;
        ExportedBy = exportedBy;
    }

    public void IncrementExportCount() => ExportCount++;
}
