using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Document;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class DocumentProcessingConsumer : IConsumer<ProcessDocument>
{
    private readonly ILogger<DocumentProcessingConsumer> _logger;
    private readonly IAppDbContext _db;
    private readonly IAiService _aiService;
    private readonly IDocumentStorage _documentStorage;

    public DocumentProcessingConsumer(
        ILogger<DocumentProcessingConsumer> logger,
        IAppDbContext db,
        IAiService aiService,
        IDocumentStorage documentStorage)
    {
        _logger = logger;
        _db = db;
        _aiService = aiService;
        _documentStorage = documentStorage;
    }

    public async Task Consume(ConsumeContext<ProcessDocument> context)
    {
        var documentId = context.Message.DocumentId;
        var entityId = context.Message.EntityId;
        var ct = context.CancellationToken;

        _logger.LogInformation("Processing document {DocumentId} for entity {EntityId}", documentId, entityId);

        // 1. Load Document from DB
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityId == entityId, ct);

        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} not found for entity {EntityId}", documentId, entityId);
            return;
        }

        try
        {
            document.MarkProcessing();
            await _db.SaveChangesAsync(ct);

            // 2. Download file from MinIO
            using var fileStream = await _documentStorage.DownloadAsync(entityId, document.StoragePath, ct);

            // 3. Extract text from document
            var documentText = await ExtractTextAsync(fileStream, document.ContentType, ct);

            if (string.IsNullOrWhiteSpace(documentText))
            {
                _logger.LogWarning("No text extracted from document {DocumentId}", documentId);
                documentText = "[No text could be extracted from this document]";
            }

            // 4. Send extracted text to AI for field extraction
            var extraction = await _aiService.ExtractDocumentFieldsAsync(
                documentText, document.ContentType, ct);

            // 5. Store DocumentFields from extraction result
            StoreDocumentFields(document, extraction);

            // 6. Update Document entity with extracted metadata
            var extractedJson = JsonSerializer.Serialize(extraction);
            document.SetExtraction(
                ocrText: documentText,
                extractedData: extractedJson,
                confidence: extraction.Confidence,
                vendorName: extraction.VendorName,
                invoiceNumber: extraction.InvoiceNumber,
                invoiceDate: extraction.InvoiceDate,
                totalAmount: extraction.TotalAmount,
                currency: extraction.Currency);

            // 7. Call AI for booking suggestion
            var bookingSuggestion = await _aiService.SuggestBookingAsync(extraction, entityId, ct);

            // 8. Create BookingSuggestion entity
            await CreateBookingSuggestionAsync(document, entityId, bookingSuggestion, ct);

            // 9. Status is already set to "extracted" by SetExtraction
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Successfully processed document {DocumentId}: vendor={Vendor}, amount={Amount}, confidence={Confidence}",
                documentId, extraction.VendorName, extraction.TotalAmount, extraction.Confidence);
        }
        catch (Exception ex)
        {
            // 10. On error, set status to "failed"
            _logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
            document.MarkFailed();
            await _db.SaveChangesAsync(ct);
            throw; // Re-throw so MassTransit retry policy can handle it
        }
    }

    /// <summary>
    /// Simple text extraction. For PDFs this is a placeholder for a proper PDF library.
    /// For images, we pass a placeholder -- in production this would use OCR.
    /// </summary>
    private static async Task<string> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken ct)
    {
        if (contentType == "application/pdf")
        {
            // Simple text extraction for PDF -- reads raw text content.
            // In production, replace with a proper PDF text extraction library (e.g., PdfPig, iText).
            using var reader = new StreamReader(fileStream);
            var rawContent = await reader.ReadToEndAsync(ct);

            // Basic PDF text extraction: strip binary content, keep readable text
            var textChars = rawContent
                .Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
                .ToArray();
            return new string(textChars);
        }

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            // For images, OCR would be needed. This is a placeholder.
            return "[Image document -- OCR extraction pending. Please implement OCR provider.]";
        }

        // Fallback: attempt to read as text
        using var fallbackReader = new StreamReader(fileStream);
        return await fallbackReader.ReadToEndAsync(ct);
    }

    private static void StoreDocumentFields(Document document, DocumentExtractionResult extraction)
    {
        if (extraction.VendorName is not null)
            document.AddField(DocumentField.Create(document.Id, "vendor_name", extraction.VendorName, extraction.Confidence));

        if (extraction.InvoiceNumber is not null)
            document.AddField(DocumentField.Create(document.Id, "invoice_number", extraction.InvoiceNumber, extraction.Confidence));

        if (extraction.InvoiceDate.HasValue)
            document.AddField(DocumentField.Create(document.Id, "invoice_date", extraction.InvoiceDate.Value.ToString("O"), extraction.Confidence));

        if (extraction.TotalAmount.HasValue)
            document.AddField(DocumentField.Create(document.Id, "total_amount", extraction.TotalAmount.Value.ToString("F2"), extraction.Confidence));

        if (extraction.Currency is not null)
            document.AddField(DocumentField.Create(document.Id, "currency", extraction.Currency, extraction.Confidence));

        if (extraction.TaxRate.HasValue)
            document.AddField(DocumentField.Create(document.Id, "tax_rate", extraction.TaxRate.Value.ToString("F2"), extraction.Confidence));

        // Store line items as individual fields
        for (var i = 0; i < extraction.LineItems.Count; i++)
        {
            var item = extraction.LineItems[i];
            if (item.Description is not null)
                document.AddField(DocumentField.Create(document.Id, $"line_item_{i}_description", item.Description, extraction.Confidence));
            if (item.TotalPrice.HasValue)
                document.AddField(DocumentField.Create(document.Id, $"line_item_{i}_total", item.TotalPrice.Value.ToString("F2"), extraction.Confidence));
        }

        // Store raw fields
        foreach (var (key, value) in extraction.RawFields)
        {
            document.AddField(DocumentField.Create(document.Id, $"raw_{key}", value, extraction.Confidence));
        }
    }

    private async Task CreateBookingSuggestionAsync(
        Document document, Guid entityId,
        BookingSuggestionResult suggestion, CancellationToken ct)
    {
        // Resolve account numbers to account IDs
        var debitAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId
                                      && a.AccountNumber == suggestion.DebitAccountNumber, ct);

        var creditAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId
                                      && a.AccountNumber == suggestion.CreditAccountNumber, ct);

        if (debitAccount is null || creditAccount is null)
        {
            _logger.LogWarning(
                "Could not resolve accounts for booking suggestion: debit={Debit}, credit={Credit}",
                suggestion.DebitAccountNumber, suggestion.CreditAccountNumber);
            return;
        }

        var bookingSuggestion = BookingSuggestion.Create(
            documentId: document.Id,
            entityId: entityId,
            debitAccountId: debitAccount.Id,
            creditAccountId: creditAccount.Id,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: null, // Could be calculated from amount + vatCode
            description: suggestion.Description,
            confidence: suggestion.Confidence,
            aiReasoning: suggestion.Reasoning);

        _db.BookingSuggestions.Add(bookingSuggestion);
    }
}
