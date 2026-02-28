namespace ClarityBoard.Domain.Services;

/// <summary>
/// Detects recurring invoice patterns for a given entity by scanning booked documents.
/// Vendors with 3+ invoices of similar amounts (within 10% tolerance) are flagged as recurring.
/// </summary>
public interface IRecurringPatternDetector
{
    Task<IReadOnlyList<DetectedPattern>> DetectPatternsAsync(Guid entityId, CancellationToken ct = default);
}

public record DetectedPattern
{
    public required string VendorName { get; init; }
    public decimal AverageAmount { get; init; }
    public int InvoiceCount { get; init; }
    public Guid? SuggestedDebitAccountId { get; init; }
    public Guid? SuggestedCreditAccountId { get; init; }
    public string? SuggestedVatCode { get; init; }
    public decimal Confidence { get; init; }
}
