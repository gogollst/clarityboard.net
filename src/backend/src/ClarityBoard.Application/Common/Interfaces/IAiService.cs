namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// AI service abstraction for document extraction, booking suggestions, and KPI analysis.
/// </summary>
public interface IAiService
{
    Task<DocumentExtractionResult> ExtractDocumentFieldsAsync(
        string documentText, string? mimeType, CancellationToken ct);

    Task<BookingSuggestionResult> SuggestBookingAsync(
        DocumentExtractionResult extraction, Guid entityId, CancellationToken ct);

    Task<string> AnalyzeKpiAsync(
        string kpiId, decimal value, decimal? previousValue, string? context, CancellationToken ct);
}

// ── Result Records ────────────────────────────────────────────────────────

public record DocumentExtractionResult
{
    public string? VendorName { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Currency { get; init; }
    public decimal? TaxRate { get; init; }
    public IReadOnlyList<LineItemResult> LineItems { get; init; } = [];
    public IReadOnlyDictionary<string, string> RawFields { get; init; } =
        new Dictionary<string, string>();
    public decimal Confidence { get; init; }
}

public record LineItemResult
{
    public string? Description { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? TotalPrice { get; init; }
    public string? TaxCode { get; init; }
}

public record BookingSuggestionResult
{
    public string? DebitAccountNumber { get; init; }
    public string? CreditAccountNumber { get; init; }
    public decimal Amount { get; init; }
    public string? VatCode { get; init; }
    public string? Description { get; init; }
    public decimal Confidence { get; init; }
    public string? Reasoning { get; init; }
}
