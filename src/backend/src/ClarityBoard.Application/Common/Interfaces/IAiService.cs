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
    public string? VendorEmail { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorBankName { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientTaxId { get; init; }
    public string? RecipientVatId { get; init; }
    public string? RecipientStreet { get; init; }
    public string? RecipientCity { get; init; }
    public string? RecipientPostalCode { get; init; }
    public string? RecipientCountry { get; init; }
    public string? RecipientIban { get; init; }
    public string? RecipientBic { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
    public string? RecipientBankName { get; init; }
    public string? DocumentDirection { get; init; } // "incoming" or "outgoing"
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? GrossAmount { get; init; }
    public decimal? NetAmount { get; init; }
    public decimal? TaxAmount { get; init; }
    public string? Currency { get; init; }
    public decimal? TaxRate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? OrderNumber { get; init; }
    public bool ReverseCharge { get; init; }
    public DateOnly? ServicePeriodStart { get; init; }
    public DateOnly? ServicePeriodEnd { get; init; }
    public bool IsRecurringRevenue { get; init; }
    public string? RecurringInterval { get; init; } // monthly, quarterly, annually
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
    public string? ProductCategory { get; init; } // SAAS_LICENSE, ON_PREM_LICENSE, HOSTING, MAINTENANCE, ONE_TIME_SERVICE, DISCOUNT
    public DateOnly? ServicePeriodStart { get; init; }
    public DateOnly? ServicePeriodEnd { get; init; }
    public string? BillingInterval { get; init; } // monthly, quarterly, annually
    public bool IsRecurring { get; init; }
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
    // Revenue schedule for outgoing invoices with deferred revenue
    public IReadOnlyList<RevenueScheduleItemResult> RevenueSchedule { get; init; } = [];
    public string? DeferredRevenueAccount { get; init; } // e.g. "3900" for PRA
    public DateOnly? ServicePeriodStart { get; init; }
    public DateOnly? ServicePeriodEnd { get; init; }
}

public record RevenueScheduleItemResult
{
    public DateOnly PeriodDate { get; init; }
    public decimal Amount { get; init; }
    public string? RevenueAccount { get; init; }
    public bool IsImmediate { get; init; } // true for first month (direct revenue)
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
    public IReadOnlyList<ReviewReason> ReviewReasons { get; init; } = [];
    public bool IsRecurring { get; init; }
    public bool GwgRelevant { get; init; }
    public bool ActivationRequired { get; init; }
    public bool ReverseCharge { get; init; }
    public bool IntraCommunity { get; init; }
    public bool EntertainmentExpense { get; init; }
}

public record ReviewReason
{
    public string Key { get; init; } = "";
    public string Detail { get; init; } = "";
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
