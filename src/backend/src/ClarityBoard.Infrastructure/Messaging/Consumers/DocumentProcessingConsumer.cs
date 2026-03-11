using System.Diagnostics;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Infrastructure.Services.Documents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class DocumentProcessingConsumer : IConsumer<ProcessDocument>
{
    private const decimal LowConfidenceThreshold = 0.70m;

    private readonly ILogger<DocumentProcessingConsumer> _logger;
    private readonly IAppDbContext _db;
    private readonly IAiService _aiService;
    private readonly IDocumentStorage _documentStorage;
    private readonly IDocumentTextAcquisitionService _textAcquisitionService;
    private readonly DocumentStatusChangeNotifier _documentStatusChangeNotifier;

    public DocumentProcessingConsumer(
        ILogger<DocumentProcessingConsumer> logger,
        IAppDbContext db,
        IAiService aiService,
        IDocumentStorage documentStorage,
        IDocumentTextAcquisitionService textAcquisitionService,
        DocumentStatusChangeNotifier documentStatusChangeNotifier)
    {
        _logger = logger;
        _db = db;
        _aiService = aiService;
        _documentStorage = documentStorage;
        _textAcquisitionService = textAcquisitionService;
        _documentStatusChangeNotifier = documentStatusChangeNotifier;
    }

    public async Task Consume(ConsumeContext<ProcessDocument> context)
    {
        var documentId = context.Message.DocumentId;
        var entityId = context.Message.EntityId;
        var ct = context.CancellationToken;
        var overallStopwatch = Stopwatch.StartNew();
        var currentStage = "load_document";
        var reviewReasons = new List<string>();

        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["DocumentId"] = documentId,
            ["EntityId"] = entityId,
        });

        _logger.LogInformation("Starting document processing pipeline");

        // 1. Load Document from DB
        var loadDocumentStopwatch = Stopwatch.StartNew();
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityId == entityId, ct);
        loadDocumentStopwatch.Stop();

        if (document is null)
        {
            LogStageWarning(currentStage, loadDocumentStopwatch.ElapsedMilliseconds, "not_found");
            return;
        }

        LogStageInformation(currentStage, loadDocumentStopwatch.ElapsedMilliseconds, "loaded",
            "status {Status} contentType {ContentType}", document.Status, document.ContentType);

        try
        {
            currentStage = "mark_processing";
            var markProcessingStopwatch = Stopwatch.StartNew();
            document.MarkProcessing();
            await _db.SaveChangesAsync(ct);
            await _documentStatusChangeNotifier.NotifyAsync(entityId, document.Id, document.Status, ct);
            markProcessingStopwatch.Stop();
            LogStageInformation(currentStage, markProcessingStopwatch.ElapsedMilliseconds, document.Status);

            // 2. Download file from MinIO
            currentStage = "download_document";
            var downloadStopwatch = Stopwatch.StartNew();
            using var fileStream = await _documentStorage.DownloadAsync(entityId, document.StoragePath, ct);
            downloadStopwatch.Stop();
            LogStageInformation(currentStage, downloadStopwatch.ElapsedMilliseconds, "downloaded",
                "contentType {ContentType} fileSizeBytes {FileSizeBytes}", document.ContentType, document.FileSize);

            // 3. Acquire text (native extraction → quality check → vision OCR fallback)
            currentStage = "acquire_text";
            var acquireTextStopwatch = Stopwatch.StartNew();
            var textResult = await _textAcquisitionService.AcquireTextAsync(
                fileStream, document.ContentType, documentId, entityId, document.FileName, ct);
            acquireTextStopwatch.Stop();

            var documentText = textResult.Text;
            reviewReasons.AddRange(textResult.ReviewReasons);

            if (string.IsNullOrWhiteSpace(documentText))
            {
                LogStageWarning(currentStage, acquireTextStopwatch.ElapsedMilliseconds, "empty_text",
                    "source {Source} usedVision {UsedVision} contentType {ContentType}",
                    textResult.Source, textResult.UsedVision, document.ContentType);
                documentText = "[No text could be extracted from this document]";
            }
            else
            {
                LogStageInformation(currentStage, acquireTextStopwatch.ElapsedMilliseconds, "text_acquired",
                    "source {Source} usedVision {UsedVision} confidence {Confidence} nativeLen {NativeLen} visionLen {VisionLen}",
                    textResult.Source, textResult.UsedVision, textResult.Confidence,
                    textResult.NativeTextLength, textResult.VisionTextLength);
            }

            // 4. Send extracted text to AI for field extraction
            currentStage = "extract_fields";
            var extractFieldsStopwatch = Stopwatch.StartNew();
            var extraction = await _aiService.ExtractDocumentFieldsAsync(
                documentText, document.ContentType, ct);
            extractFieldsStopwatch.Stop();
            LogConfidenceStage(currentStage, extractFieldsStopwatch.ElapsedMilliseconds, extraction.Confidence,
                "fieldsDetected {FieldsDetected} lineItems {LineItemCount} rawFieldCount {RawFieldCount}",
                CountDetectedFields(extraction), extraction.LineItems.Count, extraction.RawFields.Count);
            if (extraction.Confidence < LowConfidenceThreshold)
                reviewReasons.Add("low_extraction_confidence");

            // 5. Store DocumentFields from extraction result
            currentStage = "persist_extraction";
            var persistExtractionStopwatch = Stopwatch.StartNew();
            StoreDocumentFields(document, extraction);

            // 6. Update Document entity with extracted metadata
            var extractedJson = DocumentExtractedDataSerializer.Serialize(extraction, reviewReasons, textResult);
            document.SetExtraction(
                ocrText: documentText,
                extractedData: extractedJson,
                confidence: extraction.Confidence,
                vendorName: extraction.VendorName,
                invoiceNumber: extraction.InvoiceNumber,
                invoiceDate: extraction.InvoiceDate,
                totalAmount: extraction.TotalAmount,
                currency: extraction.Currency);
            persistExtractionStopwatch.Stop();
            LogStageInformation(currentStage, persistExtractionStopwatch.ElapsedMilliseconds, document.Status,
                "storedFieldCount {StoredFieldCount}", document.Fields.Count);

            // 7. Call AI for booking suggestion
            currentStage = "suggest_booking";
            var suggestBookingStopwatch = Stopwatch.StartNew();
            var bookingSuggestion = await _aiService.SuggestBookingAsync(extraction, entityId, ct);
            suggestBookingStopwatch.Stop();
            LogConfidenceStage(currentStage, suggestBookingStopwatch.ElapsedMilliseconds, bookingSuggestion.Confidence,
                "amount {Amount} debitAccount {DebitAccount} creditAccount {CreditAccount}",
                bookingSuggestion.Amount, bookingSuggestion.DebitAccountNumber, bookingSuggestion.CreditAccountNumber);
            if (bookingSuggestion.Confidence < LowConfidenceThreshold)
                reviewReasons.Add("low_booking_confidence");

            // 8. Create BookingSuggestion entity
            currentStage = "persist_results";
            var persistResultsStopwatch = Stopwatch.StartNew();
            var bookingSuggestionCreated = await CreateBookingSuggestionAsync(document, entityId, bookingSuggestion, ct);
            if (!bookingSuggestionCreated)
                reviewReasons.Add("booking_suggestion_unresolved_accounts");

            document.UpdateExtractedData(DocumentExtractedDataSerializer.Serialize(extraction, reviewReasons, textResult));

            if (reviewReasons.Count > 0)
                document.MarkReview();

            // 9. Status is already set to "extracted" by SetExtraction
            await _db.SaveChangesAsync(ct);
            persistResultsStopwatch.Stop();
            LogStageInformation(currentStage, persistResultsStopwatch.ElapsedMilliseconds,
                bookingSuggestionCreated ? "saved" : "saved_without_booking_suggestion",
                "documentStatus {DocumentStatus} storedFieldCount {StoredFieldCount}", document.Status, document.Fields.Count);

            if (reviewReasons.Count > 0)
            {
                _logger.LogWarning(
                    "Document marked for manual review with reasons {ReviewReasons}",
                    string.Join(",", reviewReasons));
            }

            await _documentStatusChangeNotifier.NotifyAsync(entityId, document.Id, document.Status, ct);

            overallStopwatch.Stop();

            _logger.LogInformation(
                "Document processing completed in {DurationMs}ms with result {Result} status {Status} extractionConfidence {ExtractionConfidence} bookingConfidence {BookingConfidence} reviewReasonCount {ReviewReasonCount}",
                overallStopwatch.ElapsedMilliseconds, "success", document.Status, extraction.Confidence, bookingSuggestion.Confidence, reviewReasons.Count);
        }
        catch (Exception ex)
        {
            // 10. On error, set status to "failed"
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Document processing failed in stage {Stage} after {DurationMs}ms with currentStatus {Status}",
                currentStage, overallStopwatch.ElapsedMilliseconds, document.Status);
            document.MarkFailed();
            await _db.SaveChangesAsync(ct);
            await _documentStatusChangeNotifier.NotifyAsync(entityId, document.Id, document.Status, ct);
            throw; // Re-throw so MassTransit retry policy can handle it
        }
    }

    private void LogStageInformation(string stage, long durationMs, string result,
        string? detailsTemplate = null, params object?[] details)
    {
        if (detailsTemplate is null)
        {
            _logger.LogInformation(
                "Document processing stage {Stage} completed in {DurationMs}ms with result {Result}",
                stage, durationMs, result);
            return;
        }

        _logger.LogInformation(
            $"Document processing stage {{Stage}} completed in {{DurationMs}}ms with result {{Result}} and {detailsTemplate}",
            CreateLogArguments(stage, durationMs, result, details));
    }

    private void LogStageWarning(string stage, long durationMs, string result,
        string? detailsTemplate = null, params object?[] details)
    {
        if (detailsTemplate is null)
        {
            _logger.LogWarning(
                "Document processing stage {Stage} completed in {DurationMs}ms with result {Result}",
                stage, durationMs, result);
            return;
        }

        _logger.LogWarning(
            $"Document processing stage {{Stage}} completed in {{DurationMs}}ms with result {{Result}} and {detailsTemplate}",
            CreateLogArguments(stage, durationMs, result, details));
    }

    private void LogConfidenceStage(string stage, long durationMs, decimal confidence,
        string detailsTemplate, params object?[] details)
    {
        var result = confidence < LowConfidenceThreshold ? "low_confidence" : "completed";
        var message = $"Document processing stage {{Stage}} completed in {{DurationMs}}ms with result {{Result}} and confidence {{Confidence}} and {detailsTemplate}";
        var arguments = CreateLogArguments(stage, durationMs, result, confidence, details);

        if (confidence < LowConfidenceThreshold)
        {
            _logger.LogWarning(message, arguments);
            return;
        }

        _logger.LogInformation(message, arguments);
    }

    private static object?[] CreateLogArguments(object? first, object? second, object? third, params object?[] remaining)
    {
        var arguments = new object?[3 + remaining.Length];
        arguments[0] = first;
        arguments[1] = second;
        arguments[2] = third;
        Array.Copy(remaining, 0, arguments, 3, remaining.Length);
        return arguments;
    }

    private static object?[] CreateLogArguments(object? first, object? second, object? third, object? fourth, params object?[] remaining)
    {
        var arguments = new object?[4 + remaining.Length];
        arguments[0] = first;
        arguments[1] = second;
        arguments[2] = third;
        arguments[3] = fourth;
        Array.Copy(remaining, 0, arguments, 4, remaining.Length);
        return arguments;
    }

    private static int CountDetectedFields(DocumentExtractionResult extraction)
    {
        var count = 0;

        if (!string.IsNullOrWhiteSpace(extraction.VendorName)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.InvoiceNumber)) count++;
        if (extraction.InvoiceDate.HasValue) count++;
        if (extraction.TotalAmount.HasValue) count++;
        if (!string.IsNullOrWhiteSpace(extraction.Currency)) count++;
        if (extraction.TaxRate.HasValue) count++;

        return count + extraction.LineItems.Count + extraction.RawFields.Count;
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

    private async Task<bool> CreateBookingSuggestionAsync(
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
                "Document processing stage {Stage} completed with result {Result}: debitAccount {Debit} creditAccount {Credit}",
                "persist_results", "account_resolution_failed",
                suggestion.DebitAccountNumber, suggestion.CreditAccountNumber);
            return false;
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
        return true;
    }
}
