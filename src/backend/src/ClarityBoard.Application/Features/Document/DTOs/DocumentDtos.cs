namespace ClarityBoard.Application.Features.Document.DTOs;

public record DocumentListDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long FileSize { get; init; }
    public string DocumentType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? VendorName { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Currency { get; init; }
    public decimal? Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}

public record DocumentDetailDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long FileSize { get; init; }
    public string DocumentType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? VendorName { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Currency { get; init; }
    public decimal? Confidence { get; init; }
    public Guid? BookedJournalEntryId { get; init; }
    public Guid? BusinessPartnerId { get; init; }
    public string? BusinessPartnerName { get; init; }
    public string? BusinessPartnerNumber { get; init; }
    public Guid? SuggestedBusinessPartnerId { get; init; }
    public string? SuggestedBusinessPartnerName { get; init; }
    public string? SuggestedBusinessPartnerNumber { get; init; }
    public string? OcrText { get; init; }
    public OcrMetadataDto? OcrMetadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public IReadOnlyList<string> ReviewReasons { get; init; } = [];
    public IReadOnlyList<DocumentFieldDto> Fields { get; init; } = [];
    public BookingSuggestionDto? BookingSuggestion { get; init; }
}

public record OcrMetadataDto
{
    public string? Source { get; init; }
    public decimal? Confidence { get; init; }
    public bool UsedVision { get; init; }
    public string? UsedProvider { get; init; }
    public string[]? Warnings { get; init; }
    public int? NativeTextLength { get; init; }
    public int? VisionTextLength { get; init; }
}

public record DocumentFieldDto
{
    public Guid Id { get; init; }
    public string FieldName { get; init; } = default!;
    public string? FieldValue { get; init; }
    public decimal Confidence { get; init; }
    public bool IsVerified { get; init; }
    public string? CorrectedValue { get; init; }
}

public record BookingSuggestionDto
{
    public Guid Id { get; init; }
    public Guid DebitAccountId { get; init; }
    public string? DebitAccountNumber { get; init; }
    public string? DebitAccountName { get; init; }
    public Guid CreditAccountId { get; init; }
    public string? CreditAccountNumber { get; init; }
    public string? CreditAccountName { get; init; }
    public decimal Amount { get; init; }
    public string? VatCode { get; init; }
    public decimal? VatAmount { get; init; }
    public string? Description { get; init; }
    public decimal Confidence { get; init; }
    public string Status { get; init; } = default!;
    public string? AiReasoning { get; init; }
    public Guid? HrEmployeeId { get; init; }
    public string? HrEmployeeName { get; init; }
    public bool IsAutoBooked { get; init; }
    public string? RejectionReason { get; init; }
    public string? InvoiceType { get; init; }
    public string? TaxKey { get; init; }
    public string? VatTreatmentType { get; init; }
}

public record DocumentUploadResult
{
    public Guid DocumentId { get; init; }
}

public record PresignedDownloadUrl
{
    public string Url { get; init; } = default!;
}
