namespace ClarityBoard.Domain.Entities.Asset;

public class DepreciationSchedule
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public DateOnly PeriodDate { get; private set; } // Month start
    public decimal DepreciationAmount { get; private set; }
    public decimal AccumulatedAmount { get; private set; }
    public decimal BookValueAfter { get; private set; }
    public bool IsPosted { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private DepreciationSchedule() { }

    public static DepreciationSchedule Create(
        Guid assetId, DateOnly periodDate, decimal depreciationAmount,
        decimal accumulatedAmount, decimal bookValueAfter)
    {
        return new DepreciationSchedule
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            PeriodDate = periodDate,
            DepreciationAmount = depreciationAmount,
            AccumulatedAmount = accumulatedAmount,
            BookValueAfter = bookValueAfter,
        };
    }

    public void MarkPosted(Guid journalEntryId)
    {
        IsPosted = true;
        JournalEntryId = journalEntryId;
        PostedAt = DateTime.UtcNow;
    }
}
