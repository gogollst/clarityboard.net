namespace ClarityBoard.Domain.Entities.Document;

public class Document
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!; // application/pdf, image/jpeg, etc.
    public long FileSize { get; private set; }
    public string StoragePath { get; private set; } = default!; // MinIO object key
    public string DocumentType { get; private set; } = default!; // invoice, receipt, bank_statement, contract
    public string Status { get; private set; } = "uploaded"; // uploaded, processing, extracted, review, booked, archived, failed
    public string? OcrText { get; private set; }
    public string? ExtractedData { get; private set; } // JSON: structured fields from AI extraction
    public decimal? Confidence { get; private set; } // AI extraction confidence 0-1
    public Guid? BookedJournalEntryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? VendorName { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public decimal? TotalAmount { get; private set; }
    public decimal? NetAmount { get; private set; }
    public decimal? TaxAmount { get; private set; }
    public string? Currency { get; private set; }
    public Guid? BusinessPartnerId { get; private set; }
    public Guid? SuggestedBusinessPartnerId { get; private set; }
    public string DocumentDirection { get; private set; } = "incoming"; // incoming or outgoing
    public decimal? ClassificationConfidence { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string? OrderNumber { get; private set; }
    public bool ReverseCharge { get; private set; }

    private readonly List<DocumentField> _fields = new();
    public IReadOnlyCollection<DocumentField> Fields => _fields.AsReadOnly();

    private Document() { }

    public static Document Create(
        Guid entityId, string fileName, string contentType, long fileSize,
        string storagePath, string documentType, Guid createdBy)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            DocumentType = documentType,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void MarkProcessing() => Status = "processing";

    public void SetExtraction(string ocrText, string extractedData, decimal confidence,
        string? vendorName = null, string? invoiceNumber = null, DateOnly? invoiceDate = null,
        decimal? totalAmount = null, string? currency = null,
        decimal? netAmount = null, decimal? taxAmount = null)
    {
        OcrText = ocrText;
        ExtractedData = extractedData;
        Confidence = confidence;
        VendorName = vendorName;
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        TotalAmount = totalAmount;
        NetAmount = netAmount;
        TaxAmount = taxAmount;
        Currency = currency;
        Status = "extracted";
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkBooked(Guid journalEntryId)
    {
        BookedJournalEntryId = journalEntryId;
        Status = "booked";
    }

    public void MarkReview() => Status = "review";

    public void MarkFailed() => Status = "failed";

    public void UpdateExtractedData(string extractedData) => ExtractedData = extractedData;

    public void AddField(DocumentField field) => _fields.Add(field);

    public void AssignBusinessPartner(Guid? businessPartnerId)
    {
        BusinessPartnerId = businessPartnerId;
        if (businessPartnerId.HasValue)
            SuggestedBusinessPartnerId = null;
    }

    public void SuggestBusinessPartner(Guid suggestedPartnerId)
    {
        SuggestedBusinessPartnerId = suggestedPartnerId;
    }

    public void SetClassification(string direction, decimal confidence)
    {
        DocumentDirection = direction;
        ClassificationConfidence = confidence;
    }

    public void ClearJournalEntry()
    {
        BookedJournalEntryId = null;
        if (Status == "booked")
            Status = "extracted";
    }

    public void SetDueDate(DateOnly? dueDate) => DueDate = dueDate;
    public void SetOrderNumber(string? orderNumber) => OrderNumber = orderNumber;
    public void SetReverseCharge(bool reverseCharge) => ReverseCharge = reverseCharge;
}
