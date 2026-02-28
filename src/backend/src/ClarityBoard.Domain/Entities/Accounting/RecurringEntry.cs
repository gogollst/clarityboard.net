namespace ClarityBoard.Domain.Entities.Accounting;

public class RecurringEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Frequency { get; private set; } = default!; // monthly, quarterly, yearly
    public int DayOfMonth { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string TemplateLines { get; private set; } = "[]"; // JSON array of line templates
    public bool IsActive { get; private set; } = true;
    public DateOnly? LastGeneratedDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private RecurringEntry() { }

    /// <summary>
    /// Determines whether this recurring entry is due for generation on the given reference date.
    /// For monthly: due every month. For quarterly: due Jan/Apr/Jul/Oct. For yearly: due in January.
    /// Skips if already generated for this period, or if outside the start/end date range.
    /// </summary>
    public bool IsDueForGeneration(DateOnly referenceDate)
    {
        if (!IsActive)
            return false;

        if (referenceDate < StartDate)
            return false;

        if (EndDate.HasValue && referenceDate > EndDate.Value)
            return false;

        // Check frequency alignment
        var isDueMonth = Frequency.ToLowerInvariant() switch
        {
            "monthly" => true,
            "quarterly" => referenceDate.Month is 1 or 4 or 7 or 10,
            "yearly" => referenceDate.Month == 1,
            _ => false,
        };

        if (!isDueMonth)
            return false;

        // Skip if already generated for this month
        if (LastGeneratedDate.HasValue
            && LastGeneratedDate.Value.Year == referenceDate.Year
            && LastGeneratedDate.Value.Month == referenceDate.Month)
            return false;

        return true;
    }

    /// <summary>
    /// Marks this recurring entry as generated for the given date.
    /// </summary>
    public void MarkGenerated(DateOnly generatedDate)
    {
        LastGeneratedDate = generatedDate;
    }
}

/// <summary>
/// Represents a single line template within a RecurringEntry's TemplateLines JSON array.
/// </summary>
public record RecurringEntryLineTemplate
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Side { get; init; } = default!; // "debit" or "credit"
    public string? VatCode { get; init; }
    public decimal VatAmount { get; init; }
    public string? CostCenter { get; init; }
    public string? Description { get; init; }
    public string Currency { get; init; } = "EUR";
    public decimal ExchangeRate { get; init; } = 1.0m;
}
