namespace ClarityBoard.Domain.Entities.Accounting;

public class RevenueScheduleEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid? BookingSuggestionId { get; private set; }
    public int? LineItemIndex { get; private set; }
    public DateOnly PeriodDate { get; private set; }
    public decimal Amount { get; private set; }
    public string RevenueAccountNumber { get; private set; } = default!;
    public Guid? RevenueAccountId { get; private set; }
    public string Status { get; private set; } = "planned"; // planned, booked, cancelled
    public Guid? JournalEntryId { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RevenueScheduleEntry() { } // EF Core

    public static RevenueScheduleEntry Create(
        Guid entityId,
        Guid documentId,
        DateOnly periodDate,
        decimal amount,
        string revenueAccountNumber,
        Guid? revenueAccountId = null,
        Guid? bookingSuggestionId = null,
        int? lineItemIndex = null)
    {
        return new RevenueScheduleEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            DocumentId = documentId,
            BookingSuggestionId = bookingSuggestionId,
            LineItemIndex = lineItemIndex,
            PeriodDate = periodDate,
            Amount = amount,
            RevenueAccountNumber = revenueAccountNumber,
            RevenueAccountId = revenueAccountId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void MarkBooked(Guid journalEntryId)
    {
        if (Status != "planned")
            throw new InvalidOperationException($"Cannot book entry with status '{Status}'.");
        Status = "booked";
        JournalEntryId = journalEntryId;
        PostedAt = DateTime.UtcNow;
    }

    public void ClearJournalEntry()
    {
        JournalEntryId = null;
        if (Status == "booked")
        {
            Status = "planned";
            PostedAt = null;
        }
    }

    public void Cancel()
    {
        if (Status != "planned")
            throw new InvalidOperationException($"Cannot cancel entry with status '{Status}'.");
        Status = "cancelled";
    }
}
