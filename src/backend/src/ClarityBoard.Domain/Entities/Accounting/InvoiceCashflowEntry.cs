namespace ClarityBoard.Domain.Entities.Accounting;

public class InvoiceCashflowEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateOnly ExpectedDate { get; private set; }
    public DateOnly? ActualDate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public string Direction { get; private set; } = "inflow"; // inflow, outflow
    public string Status { get; private set; } = "open"; // open, received, overdue, written_off
    public Guid? JournalEntryId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InvoiceCashflowEntry() { } // EF Core

    public static InvoiceCashflowEntry Create(
        Guid entityId,
        Guid documentId,
        DateOnly expectedDate,
        decimal grossAmount,
        string direction,
        string currency = "EUR")
    {
        return new InvoiceCashflowEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            DocumentId = documentId,
            ExpectedDate = expectedDate,
            GrossAmount = grossAmount,
            Direction = direction,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void MarkReceived(DateOnly actualDate, Guid? journalEntryId = null)
    {
        Status = "received";
        ActualDate = actualDate;
        JournalEntryId = journalEntryId;
    }

    public void MarkOverdue()
    {
        if (Status == "open")
            Status = "overdue";
    }

    public void ClearJournalEntry()
    {
        JournalEntryId = null;
    }

    public void WriteOff()
    {
        Status = "written_off";
    }
}
