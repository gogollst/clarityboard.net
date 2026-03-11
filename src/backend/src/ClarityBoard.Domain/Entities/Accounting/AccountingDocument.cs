namespace ClarityBoard.Domain.Entities.Accounting;

public class AccountingDocument
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string DocumentType { get; private set; } = default!; // "IncomingInvoice", "OutgoingInvoice", "Receipt", "Other"
    public string? DocumentNumber { get; private set; }
    public DateOnly? DocumentDate { get; private set; }
    public string? VendorName { get; private set; }
    public long? TotalAmountCents { get; private set; }
    public string? CurrencyCode { get; private set; } = "EUR";
    public string? StoragePath { get; private set; }   // MinIO path
    public string? MimeType { get; private set; }
    public long? FileSizeBytes { get; private set; }
    public string Status { get; private set; } = "pending"; // "pending", "processed", "linked"
    public Guid? JournalEntryId { get; private set; }
    public string? AiExtractedData { get; private set; } // JSON
    public string? AiSuggestedBooking { get; private set; } // JSON
    public DateTime CreatedAt { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateOnly RetentionUntil { get; private set; }

    private AccountingDocument() { }

    public static AccountingDocument Create(
        Guid entityId, string documentType, Guid uploadedBy,
        string? storagePath = null, string? mimeType = null, long? fileSizeBytes = null)
    {
        return new AccountingDocument
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            DocumentType = documentType,
            Status = "pending",
            StoragePath = storagePath,
            MimeType = mimeType,
            FileSizeBytes = fileSizeBytes,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            RetentionUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)),
        };
    }

    public void LinkToJournalEntry(Guid journalEntryId)
    {
        JournalEntryId = journalEntryId;
        Status = "linked";
    }

    public void SetAiExtraction(string extractedDataJson, string? suggestedBookingJson)
    {
        AiExtractedData = extractedDataJson;
        AiSuggestedBooking = suggestedBookingJson;
        Status = "processed";
    }

    public void SetDocumentDetails(string? documentNumber, DateOnly? documentDate,
        string? vendorName, long? totalAmountCents, string? currencyCode)
    {
        DocumentNumber = documentNumber;
        DocumentDate = documentDate;
        VendorName = vendorName;
        TotalAmountCents = totalAmountCents;
        CurrencyCode = currencyCode ?? "EUR";
    }
}
