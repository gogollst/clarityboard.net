namespace ClarityBoard.Domain.Entities.CashFlow;

public class CashFlowEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public string Category { get; private set; } = default!; // operating_inflow, operating_outflow, investing, financing
    public string Subcategory { get; private set; } = default!; // e.g. customer_receipts, payroll
    public decimal Amount { get; private set; } // Positive = inflow, negative = outflow
    public string Currency { get; private set; } = "EUR";
    public decimal BaseAmount { get; private set; } // Amount in EUR
    public string? SourceType { get; private set; } // journal_entry, forecast, manual
    public Guid? SourceRef { get; private set; }
    public string? Description { get; private set; }
    public bool IsRecurring { get; private set; }
    public string Certainty { get; private set; } = "confirmed"; // confirmed, probable, possible
    public int? PaymentTermsDays { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CashFlowEntry() { }

    public static CashFlowEntry Create(
        Guid entityId, DateOnly entryDate, string category, string subcategory,
        decimal amount, string? description = null, string? sourceType = null,
        Guid? sourceRef = null, string currency = "EUR", decimal exchangeRate = 1.0m)
    {
        return new CashFlowEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntryDate = entryDate,
            Category = category,
            Subcategory = subcategory,
            Amount = amount,
            Currency = currency,
            BaseAmount = amount * exchangeRate,
            SourceType = sourceType,
            SourceRef = sourceRef,
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
