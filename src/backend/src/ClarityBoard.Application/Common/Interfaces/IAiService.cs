namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// AI service abstraction for document extraction, booking suggestions, and KPI analysis.
/// </summary>
public interface IAiService
{
    Task<DocumentExtractionResult> ExtractDocumentFieldsAsync(
        string documentText, string? mimeType, CancellationToken ct);

    Task<BookingSuggestionResult> SuggestBookingAsync(
        DocumentExtractionResult extraction, Guid entityId,
        string chartOfAccounts, IReadOnlyList<AccountInfo> accounts,
        string? companyContext,
        CancellationToken ct);

    Task<string> AnalyzeKpiAsync(
        string kpiId, decimal value, decimal? previousValue, string? context, CancellationToken ct);
}

// ── Result Records ────────────────────────────────────────────────────────

public record DocumentExtractionResult
{
    public string? VendorName { get; init; }
    public string? VendorTaxId { get; init; }
    public string? VendorStreet { get; init; }
    public string? VendorCity { get; init; }
    public string? VendorPostalCode { get; init; }
    public string? VendorCountry { get; init; }
    public string? VendorIban { get; init; }
    public string? VendorBic { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientTaxId { get; init; }
    public string? RecipientVatId { get; init; }
    public string? RecipientStreet { get; init; }
    public string? RecipientCity { get; init; }
    public string? RecipientPostalCode { get; init; }
    public string? RecipientCountry { get; init; }
    public string? DocumentDirection { get; init; } // "incoming" or "outgoing"
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
    // Existing fields
    public string? DebitAccountNumber { get; init; }
    public string? CreditAccountNumber { get; init; }
    public decimal Amount { get; init; }
    public string? VatCode { get; init; }
    public string? Description { get; init; }
    public decimal Confidence { get; init; }
    public string? Reasoning { get; init; }

    // Classification fields from enriched prompt
    public string? InvoiceType { get; init; }
    public string? TaxKey { get; init; }
    public VatTreatmentResult? VatTreatment { get; init; }
    public BookingFlagsResult? Flags { get; init; }
    public IReadOnlyList<ClassifiedLineItemResult> ClassifiedLineItems { get; init; } = [];
    public IReadOnlyList<BookingEntryResult> BookingEntries { get; init; } = [];
    public string? AssignedEntity { get; init; }
    public string? Notes { get; init; }
}

public record VatTreatmentResult
{
    public string Type { get; init; } = "standard_19";
    public string? Explanation { get; init; }
    public string? InputTaxAccount { get; init; }
    public bool InputTaxDeductible { get; init; } = true;
    public string? OutputTaxAccount { get; init; }
    public string? LegalBasis { get; init; }
}

public record BookingFlagsResult
{
    public bool NeedsManualReview { get; init; }
    public IReadOnlyList<string> ReviewReasons { get; init; } = [];
    public bool IsRecurring { get; init; }
    public bool GwgRelevant { get; init; }
    public bool ActivationRequired { get; init; }
    public bool ReverseCharge { get; init; }
    public bool IntraCommunity { get; init; }
    public bool EntertainmentExpense { get; init; }
}

public record ClassifiedLineItemResult
{
    public string? Description { get; init; }
    public decimal NetAmount { get; init; }
    public decimal VatRate { get; init; }
    public decimal VatAmount { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountName { get; init; }
    public string? CostCenter { get; init; }
}

public record BookingEntryResult
{
    public string? DebitAccount { get; init; }
    public string? DebitAccountName { get; init; }
    public string? CreditAccount { get; init; }
    public string? CreditAccountName { get; init; }
    public decimal Amount { get; init; }
    public string? TaxKey { get; init; }
    public string? Description { get; init; }
}

public record AccountInfo(string AccountNumber, string Name, string AccountType, string? VatDefault);
