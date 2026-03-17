using System.Diagnostics;
using System.Text.Json;
using ClarityBoard.Application.Common.Helpers;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Infrastructure.Services.Documents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class DocumentProcessingConsumer : IConsumer<ProcessDocument>
{
    private const decimal LowConfidenceThreshold = 0.70m;
    private const decimal AutoBookConfidenceThreshold = 0.85m;
    private const decimal AzureMinConfidenceThreshold = 0.70m;

    private readonly ILogger<DocumentProcessingConsumer> _logger;
    private readonly IAppDbContext _db;
    private readonly IAiService _aiService;
    private readonly IDocumentStorage _documentStorage;
    private readonly IDocumentTextAcquisitionService _textAcquisitionService;
    private readonly IAzureDocIntelligenceService _azureDocIntelligence;
    private readonly IBusinessPartnerMatchingService _partnerMatchingService;
    private readonly DocumentStatusChangeNotifier _documentStatusChangeNotifier;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IDocumentClassifier _documentClassifier;
    private readonly IRevenueScheduleService _revenueScheduleService;

    public DocumentProcessingConsumer(
        ILogger<DocumentProcessingConsumer> logger,
        IAppDbContext db,
        IAiService aiService,
        IDocumentStorage documentStorage,
        IDocumentTextAcquisitionService textAcquisitionService,
        IAzureDocIntelligenceService azureDocIntelligence,
        IBusinessPartnerMatchingService partnerMatchingService,
        DocumentStatusChangeNotifier documentStatusChangeNotifier,
        IAccountingRepository accountingRepo,
        IDocumentClassifier documentClassifier,
        IRevenueScheduleService revenueScheduleService)
    {
        _logger = logger;
        _db = db;
        _aiService = aiService;
        _documentStorage = documentStorage;
        _textAcquisitionService = textAcquisitionService;
        _azureDocIntelligence = azureDocIntelligence;
        _partnerMatchingService = partnerMatchingService;
        _documentStatusChangeNotifier = documentStatusChangeNotifier;
        _accountingRepo = accountingRepo;
        _documentClassifier = documentClassifier;
        _revenueScheduleService = revenueScheduleService;
    }

    public async Task Consume(ConsumeContext<ProcessDocument> context)
    {
        var documentId = context.Message.DocumentId;
        var entityId = context.Message.EntityId;
        var ct = context.CancellationToken;
        var overallStopwatch = Stopwatch.StartNew();
        var currentStage = "load_document";
        var reviewReasons = new List<ReviewReason>();

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

            // 3–4. Try Azure Document Intelligence first, then fall back to standard pipeline
            var (documentText, extraction, textResult) = await AcquireTextAndExtractAsync(
                fileStream, document, documentId, entityId, reviewReasons, ct);

            // 4b. Classify document direction (incoming vs outgoing)
            currentStage = "classify_direction";
            var classifyStopwatch = Stopwatch.StartNew();
            var activeEntities = await _db.LegalEntities
                .Where(e => e.IsActive)
                .ToListAsync(ct);
            var classification = await _documentClassifier.ClassifyAsync(documentText, activeEntities, ct);
            document.SetClassification(classification.Direction, classification.Confidence);
            classifyStopwatch.Stop();
            LogStageInformation(currentStage, classifyStopwatch.ElapsedMilliseconds, classification.Direction,
                "score {Score} confidence {Confidence} rules {Rules}",
                classification.Score, classification.Confidence,
                string.Join(",", classification.MatchedRules));

            var isOutgoing = classification.Direction == "outgoing";

            // Override AI-extracted direction with classifier's decision (rules-based is more reliable)
            if (extraction.DocumentDirection != classification.Direction)
            {
                _logger.LogInformation(
                    "Overriding AI-extracted direction '{AiDirection}' with classifier direction '{ClassifierDirection}' (score={Score}, confidence={Confidence})",
                    extraction.DocumentDirection, classification.Direction, classification.Score, classification.Confidence);
                extraction = extraction with { DocumentDirection = classification.Direction };
            }

            // 5. Store DocumentFields from extraction result
            currentStage = "persist_extraction";
            var persistExtractionStopwatch = Stopwatch.StartNew();
            var storedFieldCount = StoreDocumentFields(document, extraction);

            // 6. Update Document entity with extracted metadata
            var extractedJson = DocumentExtractedDataSerializer.Serialize(extraction, reviewReasons, textResult);
            // Resolve gross/net/tax amounts with plausibility checks
            var reconciled = AmountReconciler.Reconcile(
                extraction.GrossAmount ?? extraction.TotalAmount,
                extraction.NetAmount,
                extraction.TaxAmount);
            var grossAmount = reconciled.GrossAmount;
            var netAmount = reconciled.NetAmount;
            var taxAmount = reconciled.TaxAmount;

            if (reconciled.PlausibilityMismatch)
            {
                reviewReasons.Add(new ReviewReason
                {
                    Key = "amount_plausibility_mismatch",
                    Detail = reconciled.MismatchDetail
                });
            }

            document.SetExtraction(
                ocrText: documentText,
                extractedData: extractedJson,
                confidence: extraction.Confidence,
                vendorName: extraction.VendorName,
                invoiceNumber: extraction.InvoiceNumber,
                invoiceDate: extraction.InvoiceDate,
                totalAmount: grossAmount ?? extraction.TotalAmount,
                currency: extraction.Currency,
                netAmount: netAmount,
                taxAmount: taxAmount);

            // Set additional extracted fields on document
            document.SetDueDate(extraction.DueDate);
            document.SetOrderNumber(extraction.OrderNumber);
            document.SetReverseCharge(extraction.ReverseCharge);

            persistExtractionStopwatch.Stop();
            LogStageInformation(currentStage, persistExtractionStopwatch.ElapsedMilliseconds, document.Status,
                "storedFieldCount {StoredFieldCount}", storedFieldCount);

            // 6b. Match or create business partner (direction-aware)
            currentStage = "match_partner";
            var matchPartnerStopwatch = Stopwatch.StartNew();

            // For outgoing invoices, the partner is the recipient (customer/debtor)
            // For incoming invoices, the partner is the vendor (creditor)
            var partnerName = isOutgoing ? extraction.RecipientName : extraction.VendorName;
            var partnerTaxId = isOutgoing ? extraction.RecipientTaxId : extraction.VendorTaxId;
            var partnerIban = isOutgoing ? extraction.RecipientIban : extraction.VendorIban;

            if (!string.IsNullOrWhiteSpace(partnerName))
            {
                var matchResult = await _partnerMatchingService.MatchPartnerAsync(
                    entityId, partnerName, partnerTaxId, partnerIban, ct);

                switch (matchResult.MatchType)
                {
                    case PartnerMatchType.Exact:
                        document.AssignBusinessPartner(matchResult.MatchedPartner!.Id);
                        // Enrich existing partner with any newly extracted fields
                        var enriched = EnrichPartnerFromExtraction(matchResult.MatchedPartner, extraction, isOutgoing);
                        LogStageInformation(currentStage, matchPartnerStopwatch.ElapsedMilliseconds, "exact_match",
                            "partnerId {PartnerId} partnerName {PartnerName} enriched {Enriched}",
                            matchResult.MatchedPartner.Id, matchResult.MatchedPartner.Name, enriched);
                        break;

                    case PartnerMatchType.Fuzzy:
                        var firstSuggestion = matchResult.SuggestedPartners[0];
                        document.SuggestBusinessPartner(firstSuggestion.Id);
                        reviewReasons.Add(new ReviewReason { Key = "partner_fuzzy_match" });
                        LogStageInformation(currentStage, matchPartnerStopwatch.ElapsedMilliseconds, "fuzzy_match",
                            "suggestedCount {SuggestedCount} firstSuggestion {FirstSuggestion}",
                            matchResult.SuggestedPartners.Count, firstSuggestion.Name);
                        break;

                    case PartnerMatchType.None:
                        // For outgoing: use recipient fields. For incoming: use vendor fields.
                        var partnerCountryRaw = isOutgoing ? extraction.RecipientCountry : extraction.VendorCountry;
                        var partnerCountry = partnerCountryRaw is { Length: 2 }
                            ? partnerCountryRaw.ToUpperInvariant()
                            : null;

                        var newPartner = BusinessPartner.Create(
                            entityId: entityId,
                            name: partnerName,
                            isCreditor: !isOutgoing,   // incoming → creditor
                            isDebtor: isOutgoing,      // outgoing → debtor
                            taxId: partnerTaxId,
                            vatNumber: isOutgoing ? extraction.RecipientVatId : null,
                            street: isOutgoing ? extraction.RecipientStreet : extraction.VendorStreet,
                            city: isOutgoing ? extraction.RecipientCity : extraction.VendorCity,
                            postalCode: isOutgoing ? extraction.RecipientPostalCode : extraction.VendorPostalCode,
                            country: partnerCountry,
                            email: isOutgoing ? extraction.RecipientEmail : extraction.VendorEmail,
                            phone: isOutgoing ? extraction.RecipientPhone : extraction.VendorPhone,
                            bankName: isOutgoing ? extraction.RecipientBankName : extraction.VendorBankName,
                            iban: isOutgoing ? extraction.RecipientIban : extraction.VendorIban,
                            bic: isOutgoing ? extraction.RecipientBic : extraction.VendorBic);

                        // Generate partner number: D- for debtors, K- for creditors
                        var prefix = isOutgoing ? "D-" : "K-";
                        var lastNumber = await _db.BusinessPartners
                            .Where(bp => bp.EntityId == entityId && bp.PartnerNumber.StartsWith(prefix))
                            .OrderByDescending(bp => bp.PartnerNumber)
                            .Select(bp => bp.PartnerNumber)
                            .FirstOrDefaultAsync(ct);

                        var nextSeq = 1;
                        if (lastNumber is not null && int.TryParse(lastNumber[2..], out var parsed))
                            nextSeq = parsed + 1;

                        newPartner.SetPartnerNumber($"{prefix}{nextSeq:D5}");
                        _db.BusinessPartners.Add(newPartner);
                        document.AssignBusinessPartner(newPartner.Id);

                        LogStageInformation(currentStage, matchPartnerStopwatch.ElapsedMilliseconds, "auto_created",
                            "partnerNumber {PartnerNumber} partnerName {PartnerName} direction {Direction}",
                            newPartner.PartnerNumber, newPartner.Name, classification.Direction);
                        break;
                }
            }
            else
            {
                LogStageInformation(currentStage, matchPartnerStopwatch.ElapsedMilliseconds, "skipped_no_partner_name");
            }
            matchPartnerStopwatch.Stop();

            // 6c. Entity recognition — match against ALL entities to find the best target
            currentStage = "entity_recognition";
            var entityRecognitionStopwatch = Stopwatch.StartNew();
            Guid? suggestedEntityId = null;
            // Reuse activeEntities loaded during classification step
            var allEntities = activeEntities;

            var legalEntity = allEntities.FirstOrDefault(e => e.Id == entityId);

            if (allEntities.Count > 0)
            {
                var bestMatch = allEntities
                    .Select(e => (Entity: e, Match: MatchEntityFields(e, extraction)))
                    .OrderByDescending(x => x.Match.Confidence)
                    .First();

                if (bestMatch.Match.Confidence >= 0.5m)
                {
                    suggestedEntityId = bestMatch.Entity.Id;
                    LogStageInformation(currentStage, entityRecognitionStopwatch.ElapsedMilliseconds, "matched",
                        "suggestedEntity {SuggestedEntity} confidence {Confidence} reason {Reason}",
                        bestMatch.Entity.Name, bestMatch.Match.Confidence, bestMatch.Match.Reason);

                    if (bestMatch.Entity.Id != entityId)
                    {
                        _logger.LogInformation(
                            "Entity recognition suggests different entity: {SuggestedEntity} (uploaded as {UploadedEntity})",
                            bestMatch.Entity.Name, legalEntity?.Name ?? entityId.ToString());
                    }
                }
                else
                {
                    // No strong match — fall back to uploaded entity
                    suggestedEntityId = entityId;
                    reviewReasons.Add(new ReviewReason { Key = "entity_mismatch_suspected" });
                    _logger.LogWarning(
                        "Document processing stage {Stage} completed in {DurationMs}ms with result {Result} — best match: {BestEntity} ({Confidence})",
                        currentStage, entityRecognitionStopwatch.ElapsedMilliseconds, "low_match",
                        bestMatch.Entity.Name, bestMatch.Match.Confidence);
                }
            }
            entityRecognitionStopwatch.Stop();

            // 7. Build company context for AI prompt
            currentStage = "build_company_context";
            string? companyContext = null;
            try
            {
                var holdingEntity = allEntities.FirstOrDefault(e => e.ParentEntityId == null);
                var holdingName = holdingEntity?.Name ?? legalEntity?.Name ?? "Unknown";

                var entitiesJson = string.Join("\n", allEntities.Select(e =>
                    $"- {e.Name} ({e.LegalForm}){(e.VatId is not null ? $", USt-IdNr: {e.VatId}" : "")}"));

                companyContext = $"Holding: {holdingName}\n\nGesellschaften:\n{entitiesJson}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build company context for booking suggestion — proceeding without");
            }

            // 7b. Call AI for booking suggestion — load entity's accounts
            currentStage = "suggest_booking";
            var suggestBookingStopwatch = Stopwatch.StartNew();
            BookingSuggestionResult? bookingSuggestion = null;
            var bookingSuggestionCreated = false;
            try
            {
                var chartOfAccounts = legalEntity?.ChartOfAccounts ?? "SKR03";
                var accounts = await _db.Accounts
                    .Where(a => a.EntityId == entityId && a.IsActive)
                    .OrderBy(a => a.AccountNumber)
                    .Select(a => new AccountInfo(a.AccountNumber, a.Name, a.AccountType, a.VatDefault))
                    .ToListAsync(ct);

                bookingSuggestion = await _aiService.SuggestBookingAsync(
                    extraction, entityId, chartOfAccounts, accounts, companyContext, ct);
                suggestBookingStopwatch.Stop();
                LogConfidenceStage(currentStage, suggestBookingStopwatch.ElapsedMilliseconds, bookingSuggestion.Confidence,
                    "amount {Amount} debitAccount {DebitAccount} creditAccount {CreditAccount}",
                    bookingSuggestion.Amount, bookingSuggestion.DebitAccountNumber, bookingSuggestion.CreditAccountNumber);
                if (bookingSuggestion.Confidence < LowConfidenceThreshold)
                    reviewReasons.Add(new ReviewReason { Key = "low_booking_confidence" });

                // Evaluate AI classification flags
                if (bookingSuggestion.Flags is not null)
                {
                    if (bookingSuggestion.Flags.NeedsManualReview)
                        reviewReasons.Add(new ReviewReason { Key = "ai_needs_manual_review" });
                    if (bookingSuggestion.Flags.ReverseCharge)
                        reviewReasons.Add(new ReviewReason { Key = "reverse_charge_detected" });
                    if (bookingSuggestion.Flags.ActivationRequired)
                        reviewReasons.Add(new ReviewReason { Key = "activation_required" });
                    if (bookingSuggestion.Flags.EntertainmentExpense)
                        reviewReasons.Add(new ReviewReason { Key = "entertainment_expense_70_30" });
                    if (bookingSuggestion.Flags.IntraCommunity)
                        reviewReasons.Add(new ReviewReason { Key = "intra_community_acquisition" });

                    foreach (var reason in bookingSuggestion.Flags.ReviewReasons)
                    {
                        if (!reviewReasons.Any(r => r.Key == reason.Key))
                            reviewReasons.Add(reason);
                    }
                }
            }
            catch (Exception ex)
            {
                suggestBookingStopwatch.Stop();
                _logger.LogWarning(ex,
                    "Document processing stage {Stage} failed in {DurationMs}ms — document will be marked for review",
                    currentStage, suggestBookingStopwatch.ElapsedMilliseconds);
                reviewReasons.Add(new ReviewReason { Key = "booking_suggestion_failed" });
            }

            // 8. Create BookingSuggestion entity
            currentStage = "persist_results";
            var persistResultsStopwatch = Stopwatch.StartNew();
            if (bookingSuggestion is not null)
            {
                bookingSuggestionCreated = await CreateBookingSuggestionAsync(document, entityId, bookingSuggestion, suggestedEntityId, ct);
                if (!bookingSuggestionCreated)
                    reviewReasons.Add(new ReviewReason { Key = "booking_suggestion_unresolved_accounts" });
            }

            // 8b. Outgoing invoice: validation + revenue schedule + cashflow entry
            if (isOutgoing && bookingSuggestionCreated && bookingSuggestion is not null)
            {
                // Run outgoing invoice validation (V-01 to V-10)
                var validator = new Application.Features.Documents.Services.OutgoingInvoiceValidationService();
                var validationResults = validator.Validate(extraction);
                foreach (var v in validationResults)
                {
                    reviewReasons.Add(new ReviewReason { Key = v.ReviewReasonKey, Detail = v.Detail });
                }

                // Create revenue schedule if service period > 1 month
                if (bookingSuggestion.ServicePeriodStart.HasValue && bookingSuggestion.ServicePeriodEnd.HasValue)
                {
                    var periodMonths = ((bookingSuggestion.ServicePeriodEnd.Value.Year - bookingSuggestion.ServicePeriodStart.Value.Year) * 12)
                                       + bookingSuggestion.ServicePeriodEnd.Value.Month - bookingSuggestion.ServicePeriodStart.Value.Month;

                    if (periodMonths > 1)
                    {
                        var latestSuggestion = await _db.BookingSuggestions
                            .Where(bs => bs.DocumentId == document.Id)
                            .OrderByDescending(bs => bs.CreatedAt)
                            .FirstOrDefaultAsync(ct);

                        if (latestSuggestion is not null)
                        {
                            var scheduleEntries = await _revenueScheduleService.CreateScheduleAsync(
                                document, latestSuggestion, bookingSuggestion, ct);
                            _logger.LogInformation(
                                "Created {Count} revenue schedule entries for document {DocumentId}",
                                scheduleEntries.Count, document.Id);

                            if (scheduleEntries.Count > 0)
                                reviewReasons.Add(new ReviewReason { Key = "deferred_revenue_required",
                                    Detail = $"{scheduleEntries.Count} deferred revenue entries created" });
                        }
                    }
                }

                // Create cashflow entry (expected inflow)
                await _revenueScheduleService.CreateCashflowEntryAsync(document, "inflow", ct);
            }
            else if (!isOutgoing && bookingSuggestionCreated)
            {
                // Incoming invoice: create cashflow outflow entry
                await _revenueScheduleService.CreateCashflowEntryAsync(document, "outflow", ct);
            }

            document.UpdateExtractedData(DocumentExtractedDataSerializer.Serialize(extraction, reviewReasons, textResult));

            // 9. Check auto-booking eligibility
            var autoBooked = false;
            if (bookingSuggestionCreated)
            {
                autoBooked = await TryAutoBookAsync(document, entityId, bookingSuggestion!, ct);
            }

            if (!autoBooked)
            {
                if (reviewReasons.Count > 0)
                    document.MarkReview();
            }

            await _db.SaveChangesAsync(ct);
            persistResultsStopwatch.Stop();
            LogStageInformation(currentStage, persistResultsStopwatch.ElapsedMilliseconds,
                autoBooked ? "auto_booked" : bookingSuggestionCreated ? "saved" : "saved_without_booking_suggestion",
                "documentStatus {DocumentStatus} storedFieldCount {StoredFieldCount}", document.Status, storedFieldCount);

            if (reviewReasons.Count > 0 && !autoBooked)
            {
                _logger.LogWarning(
                    "Document marked for manual review with reasons {ReviewReasons}",
                    string.Join(",", reviewReasons.Select(r => r.Key)));
            }

            await _documentStatusChangeNotifier.NotifyAsync(entityId, document.Id, document.Status, ct);

            overallStopwatch.Stop();

            _logger.LogInformation(
                "Document processing completed in {DurationMs}ms with result {Result} status {Status} extractionConfidence {ExtractionConfidence} bookingConfidence {BookingConfidence} reviewReasonCount {ReviewReasonCount}",
                overallStopwatch.ElapsedMilliseconds, autoBooked ? "auto_booked" : "success", document.Status, extraction.Confidence, bookingSuggestion?.Confidence, reviewReasons.Count);
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

    private async Task<bool> TryAutoBookAsync(
        Document document, Guid entityId,
        BookingSuggestionResult suggestion, CancellationToken ct)
    {
        // Only auto-book if document has a confirmed business partner (not just suggested)
        if (!document.BusinessPartnerId.HasValue)
            return false;

        var vendorName = document.VendorName;
        if (string.IsNullOrWhiteSpace(vendorName))
            return false;

        // Find matching recurring pattern with auto-book enabled (direction-aware)
        var direction = document.DocumentDirection;
        var pattern = await _db.RecurringPatterns
            .FirstOrDefaultAsync(p =>
                p.EntityId == entityId
                && p.AutoBookEnabled
                && p.IsActive
                && p.DocumentDirection == direction
                && p.VendorName.ToLower() == vendorName.ToLower(), ct);

        if (pattern is null)
            return false;

        // Verify AI suggestion matches the pattern accounts
        var debitAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId && a.AccountNumber == suggestion.DebitAccountNumber, ct);
        var creditAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId && a.AccountNumber == suggestion.CreditAccountNumber, ct);

        if (debitAccount is null || creditAccount is null)
            return false;

        if (debitAccount.Id != pattern.DebitAccountId || creditAccount.Id != pattern.CreditAccountId)
            return false;

        if (suggestion.Confidence < AutoBookConfidenceThreshold)
            return false;

        // All conditions met — auto-book
        var invoiceDate = document.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == entityId
                && fp.StartDate <= invoiceDate
                && fp.EndDate >= invoiceDate, ct);

        if (fiscalPeriod is null)
            return false; // No fiscal period — fall back to manual review

        var entryNumber = await _accountingRepo.GetNextEntryNumberAsync(entityId, ct);

        var journalEntry = JournalEntry.Create(
            entityId: entityId,
            entryNumber: entryNumber,
            entryDate: invoiceDate,
            description: suggestion.Description ?? $"Invoice: {document.VendorName} {document.InvoiceNumber}",
            fiscalPeriodId: fiscalPeriod.Id,
            createdBy: Guid.Empty, // system
            sourceType: "ai-auto-book",
            sourceRef: document.Id.ToString(),
            documentId: document.Id);

        var debitLine = JournalEntryLine.CreateDebit(
            lineNumber: 1,
            accountId: debitAccount.Id,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: document.TaxAmount ?? 0,
            description: suggestion.Description,
            hrEmployeeId: pattern.HrEmployeeId);
        journalEntry.AddLine(debitLine);

        var creditLine = JournalEntryLine.CreateCredit(
            lineNumber: 2,
            accountId: creditAccount.Id,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: document.TaxAmount ?? 0,
            description: suggestion.Description,
            hrEmployeeId: pattern.HrEmployeeId);
        journalEntry.AddLine(creditLine);

        await _accountingRepo.AddJournalEntryAsync(journalEntry, ct);

        // Update booking suggestion
        var bookingSuggestionEntity = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == document.Id && bs.Status == "suggested")
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (bookingSuggestionEntity is not null)
        {
            bookingSuggestionEntity.Accept(Guid.Empty);
            bookingSuggestionEntity.MarkAutoBooked();
        }

        document.MarkBooked(journalEntry.Id);
        pattern.IncrementMatch();

        _logger.LogInformation(
            "Document auto-booked via recurring pattern {PatternId} with confidence {Confidence} matchCount {MatchCount}",
            pattern.Id, suggestion.Confidence, pattern.MatchCount);

        return true;
    }

    /// <summary>
    /// Hybrid extraction pipeline:
    /// 1. Try Azure Document Intelligence (OCR + structured field extraction)
    ///    → Success + confidence ≥ threshold: use Azure result directly (no AI needed)
    ///    → Success + low confidence: use Azure result but flag for review
    /// 2. Fallback: native/vision OCR → AI field extraction (Anthropic/OpenAI)
    /// </summary>
    private async Task<(string DocumentText, DocumentExtractionResult Extraction, DocumentTextAcquisitionResult? TextResult)>
        AcquireTextAndExtractAsync(
            Stream fileStream, Document document, Guid documentId, Guid entityId,
            List<ReviewReason> reviewReasons, CancellationToken ct)
    {
        // Buffer stream for potential reuse
        using var bufferedStream = new MemoryStream();
        await fileStream.CopyToAsync(bufferedStream, ct);
        bufferedStream.Position = 0;

        // ── Try Azure Document Intelligence (OCR + extraction) ──
        var azureResult = await TryAzureAnalyzeAsync(
            bufferedStream, document.DocumentType, documentId, reviewReasons, ct);

        if (azureResult is not null && !string.IsNullOrWhiteSpace(azureResult.OcrText))
        {
            // Azure succeeded — use its extraction directly, skip AI
            var extraction = azureResult.Extraction;

            LogConfidenceStage("azure_extraction", 0, extraction.Confidence,
                "model {Model} fieldsDetected {FieldsDetected} lineItems {LineItemCount}",
                azureResult.ModelUsed, CountDetectedFields(extraction), extraction.LineItems.Count);

            if (extraction.Confidence < AzureMinConfidenceThreshold)
                reviewReasons.Add(new ReviewReason { Key = "azure_doc_intelligence_low_confidence" });
            if (extraction.Confidence < LowConfidenceThreshold)
                reviewReasons.Add(new ReviewReason { Key = "low_extraction_confidence" });

            return (azureResult.OcrText, extraction, null);
        }

        // ── Fallback: standard pipeline (native/vision OCR → AI extraction) ──
        bufferedStream.Position = 0;
        var acquireTextStopwatch = Stopwatch.StartNew();
        var textResult = await _textAcquisitionService.AcquireTextAsync(
            bufferedStream, document.ContentType, documentId, entityId, document.FileName, ct);
        acquireTextStopwatch.Stop();

        var documentText = textResult.Text;
        reviewReasons.AddRange(textResult.ReviewReasons.Select(r => new ReviewReason { Key = r }));

        if (string.IsNullOrWhiteSpace(documentText))
        {
            LogStageWarning("acquire_text", acquireTextStopwatch.ElapsedMilliseconds, "empty_text",
                "source {Source} usedVision {UsedVision} contentType {ContentType}",
                textResult.Source, textResult.UsedVision, document.ContentType);
            documentText = "[No text could be extracted from this document]";
        }
        else
        {
            LogStageInformation("acquire_text", acquireTextStopwatch.ElapsedMilliseconds, "text_acquired",
                "source {Source} usedVision {UsedVision} confidence {Confidence} nativeLen {NativeLen} visionLen {VisionLen}",
                textResult.Source, textResult.UsedVision, textResult.Confidence,
                textResult.NativeTextLength, textResult.VisionTextLength);
        }

        // AI field extraction (only when Azure wasn't available)
        var extractFieldsStopwatch = Stopwatch.StartNew();
        var aiExtraction = await _aiService.ExtractDocumentFieldsAsync(
            documentText, document.ContentType, ct);
        extractFieldsStopwatch.Stop();
        LogConfidenceStage("extract_fields", extractFieldsStopwatch.ElapsedMilliseconds, aiExtraction.Confidence,
            "fieldsDetected {FieldsDetected} lineItems {LineItemCount} rawFieldCount {RawFieldCount}",
            CountDetectedFields(aiExtraction), aiExtraction.LineItems.Count, aiExtraction.RawFields.Count);
        if (aiExtraction.Confidence < LowConfidenceThreshold)
            reviewReasons.Add(new ReviewReason { Key = "low_extraction_confidence" });

        return (documentText, aiExtraction, textResult);
    }

    /// <summary>
    /// Try Azure Document Intelligence for OCR + structured field extraction.
    /// Returns the full result, or null if Azure is not configured or fails.
    /// </summary>
    private async Task<AzureDocIntelligenceResult?> TryAzureAnalyzeAsync(
        Stream stream, string documentType, Guid documentId,
        List<ReviewReason> reviewReasons, CancellationToken ct)
    {
        var azureStopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _azureDocIntelligence.AnalyzeDocumentAsync(
                stream, documentType, documentId, ct);
            azureStopwatch.Stop();

            if (result is null)
            {
                _logger.LogDebug("Azure Document Intelligence not configured, using standard pipeline");
                return null;
            }

            if (string.IsNullOrWhiteSpace(result.OcrText))
            {
                _logger.LogWarning(
                    "Document processing stage {Stage} completed in {DurationMs}ms with result {Result} — empty text, falling back to standard pipeline",
                    "azure_analyze", azureStopwatch.ElapsedMilliseconds, "empty_text");
                reviewReasons.Add(new ReviewReason { Key = "azure_doc_intelligence_empty_text" });
                return null;
            }

            _logger.LogInformation(
                "Document processing stage {Stage} completed in {DurationMs}ms with result {Result} — model {Model}, confidence {Confidence}, {TextLength} chars",
                "azure_analyze", azureStopwatch.ElapsedMilliseconds, "success",
                result.ModelUsed, result.Confidence, result.OcrText.Length);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            azureStopwatch.Stop();
            _logger.LogWarning(ex,
                "Document processing stage {Stage} failed in {DurationMs}ms — falling back to standard pipeline",
                "azure_analyze", azureStopwatch.ElapsedMilliseconds);
            reviewReasons.Add(new ReviewReason { Key = "azure_doc_intelligence_failed" });
            return null;
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
        if (!string.IsNullOrWhiteSpace(extraction.VendorTaxId)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.VendorIban)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.VendorStreet)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.VendorCity)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.RecipientName)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.RecipientTaxId)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.DocumentDirection)) count++;
        if (!string.IsNullOrWhiteSpace(extraction.InvoiceNumber)) count++;
        if (extraction.InvoiceDate.HasValue) count++;
        if (extraction.TotalAmount.HasValue) count++;
        if (extraction.GrossAmount.HasValue) count++;
        if (extraction.NetAmount.HasValue) count++;
        if (extraction.TaxAmount.HasValue) count++;
        if (!string.IsNullOrWhiteSpace(extraction.Currency)) count++;
        if (extraction.TaxRate.HasValue) count++;

        return count + extraction.LineItems.Count + extraction.RawFields.Count;
    }

    private static bool EnrichPartnerFromExtraction(
        BusinessPartner partner, DocumentExtractionResult extraction, bool isOutgoing)
    {
        var countryRaw = isOutgoing ? extraction.RecipientCountry : extraction.VendorCountry;
        var country = countryRaw is { Length: 2 } ? countryRaw.ToUpperInvariant() : null;

        return partner.EnrichMissingFields(
            taxId: isOutgoing ? extraction.RecipientTaxId : extraction.VendorTaxId,
            vatNumber: isOutgoing ? extraction.RecipientVatId : null,
            street: isOutgoing ? extraction.RecipientStreet : extraction.VendorStreet,
            city: isOutgoing ? extraction.RecipientCity : extraction.VendorCity,
            postalCode: isOutgoing ? extraction.RecipientPostalCode : extraction.VendorPostalCode,
            country: country,
            email: isOutgoing ? extraction.RecipientEmail : extraction.VendorEmail,
            phone: isOutgoing ? extraction.RecipientPhone : extraction.VendorPhone,
            bankName: isOutgoing ? extraction.RecipientBankName : extraction.VendorBankName,
            iban: isOutgoing ? extraction.RecipientIban : extraction.VendorIban,
            bic: isOutgoing ? extraction.RecipientBic : extraction.VendorBic);
    }

    private int StoreDocumentFields(Document document, DocumentExtractionResult extraction)
    {
        var fields = new List<DocumentField>();

        void Add(string name, string? value)
        {
            if (value is not null)
                fields.Add(DocumentField.Create(document.Id, name, value, extraction.Confidence));
        }

        Add("vendor_name", extraction.VendorName);
        Add("vendor_tax_id", extraction.VendorTaxId);
        Add("vendor_iban", extraction.VendorIban);
        Add("vendor_bic", extraction.VendorBic);
        Add("vendor_email", extraction.VendorEmail);
        Add("vendor_phone", extraction.VendorPhone);
        Add("vendor_bank_name", extraction.VendorBankName);
        Add("vendor_street", extraction.VendorStreet);
        Add("vendor_city", extraction.VendorCity);
        Add("vendor_postal_code", extraction.VendorPostalCode);
        Add("vendor_country", extraction.VendorCountry);
        Add("recipient_name", extraction.RecipientName);
        Add("recipient_tax_id", extraction.RecipientTaxId);
        Add("recipient_vat_id", extraction.RecipientVatId);
        Add("recipient_iban", extraction.RecipientIban);
        Add("recipient_bic", extraction.RecipientBic);
        Add("recipient_email", extraction.RecipientEmail);
        Add("recipient_phone", extraction.RecipientPhone);
        Add("recipient_bank_name", extraction.RecipientBankName);
        Add("recipient_street", extraction.RecipientStreet);
        Add("recipient_city", extraction.RecipientCity);
        Add("recipient_postal_code", extraction.RecipientPostalCode);
        Add("recipient_country", extraction.RecipientCountry);
        Add("document_direction", extraction.DocumentDirection);
        Add("invoice_number", extraction.InvoiceNumber);

        if (extraction.InvoiceDate.HasValue)
            fields.Add(DocumentField.Create(document.Id, "invoice_date", extraction.InvoiceDate.Value.ToString("O"), extraction.Confidence));
        if (extraction.TotalAmount.HasValue)
            fields.Add(DocumentField.Create(document.Id, "total_amount", extraction.TotalAmount.Value.ToString("F2"), extraction.Confidence));
        if (extraction.GrossAmount.HasValue)
            fields.Add(DocumentField.Create(document.Id, "gross_amount", extraction.GrossAmount.Value.ToString("F2"), extraction.Confidence));
        if (extraction.NetAmount.HasValue)
            fields.Add(DocumentField.Create(document.Id, "net_amount", extraction.NetAmount.Value.ToString("F2"), extraction.Confidence));
        if (extraction.TaxAmount.HasValue)
            fields.Add(DocumentField.Create(document.Id, "tax_amount", extraction.TaxAmount.Value.ToString("F2"), extraction.Confidence));

        Add("currency", extraction.Currency);

        if (extraction.TaxRate.HasValue)
            fields.Add(DocumentField.Create(document.Id, "tax_rate", extraction.TaxRate.Value.ToString("F2"), extraction.Confidence));

        // Sprint 2: Service period, recurring revenue, and extended fields
        if (extraction.DueDate.HasValue)
            fields.Add(DocumentField.Create(document.Id, "due_date", extraction.DueDate.Value.ToString("O"), extraction.Confidence));
        Add("order_number", extraction.OrderNumber);
        if (extraction.ReverseCharge)
            fields.Add(DocumentField.Create(document.Id, "reverse_charge", "true", extraction.Confidence));
        if (extraction.ServicePeriodStart.HasValue)
            fields.Add(DocumentField.Create(document.Id, "service_period_start", extraction.ServicePeriodStart.Value.ToString("O"), extraction.Confidence));
        if (extraction.ServicePeriodEnd.HasValue)
            fields.Add(DocumentField.Create(document.Id, "service_period_end", extraction.ServicePeriodEnd.Value.ToString("O"), extraction.Confidence));
        if (extraction.IsRecurringRevenue)
            fields.Add(DocumentField.Create(document.Id, "is_recurring_revenue", "true", extraction.Confidence));
        Add("recurring_interval", extraction.RecurringInterval);

        for (var i = 0; i < extraction.LineItems.Count; i++)
        {
            var item = extraction.LineItems[i];
            if (item.Description is not null)
                fields.Add(DocumentField.Create(document.Id, $"line_item_{i}_description", item.Description, extraction.Confidence));
            if (item.TotalPrice.HasValue)
                fields.Add(DocumentField.Create(document.Id, $"line_item_{i}_total", item.TotalPrice.Value.ToString("F2"), extraction.Confidence));
            Add($"line_item_{i}_product_category", item.ProductCategory);
            if (item.ServicePeriodStart.HasValue)
                fields.Add(DocumentField.Create(document.Id, $"line_item_{i}_service_period_start", item.ServicePeriodStart.Value.ToString("O"), extraction.Confidence));
            if (item.ServicePeriodEnd.HasValue)
                fields.Add(DocumentField.Create(document.Id, $"line_item_{i}_service_period_end", item.ServicePeriodEnd.Value.ToString("O"), extraction.Confidence));
            Add($"line_item_{i}_billing_interval", item.BillingInterval);
        }

        foreach (var (key, value) in extraction.RawFields)
            fields.Add(DocumentField.Create(document.Id, $"raw_{key}", value, extraction.Confidence));

        // Add directly to DbContext to ensure EF tracks them as Added
        // Do NOT also add to document.Fields — EF would try to insert them twice
        _db.DocumentFields.AddRange(fields);
        return fields.Count;
    }

    private async Task<bool> CreateBookingSuggestionAsync(
        Document document, Guid entityId,
        BookingSuggestionResult suggestion, Guid? suggestedEntityId, CancellationToken ct)
    {
        // Resolve account numbers to account IDs (use suggested entity for account lookup)
        var targetEntityId = suggestedEntityId ?? entityId;
        var debitAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == targetEntityId
                                      && a.AccountNumber == suggestion.DebitAccountNumber, ct);

        var creditAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == targetEntityId
                                      && a.AccountNumber == suggestion.CreditAccountNumber, ct);

        if (debitAccount is null || creditAccount is null)
        {
            _logger.LogWarning(
                "Document processing stage {Stage} completed with result {Result}: debitAccount {Debit} creditAccount {Credit}",
                "persist_results", "account_resolution_failed",
                suggestion.DebitAccountNumber, suggestion.CreditAccountNumber);
            return false;
        }

        // Serialize full AI response (flags, vat treatment, line items) as JSON for AiReasoning
        var aiReasoningJson = JsonSerializer.Serialize(new
        {
            suggestion.Reasoning,
            suggestion.InvoiceType,
            suggestion.TaxKey,
            suggestion.VatTreatment,
            suggestion.Flags,
            suggestion.ClassifiedLineItems,
            suggestion.BookingEntries,
            suggestion.AssignedEntity,
            suggestion.Notes,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false });

        var bookingSuggestion = BookingSuggestion.Create(
            documentId: document.Id,
            entityId: entityId,
            debitAccountId: debitAccount.Id,
            creditAccountId: creditAccount.Id,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: document.TaxAmount,
            description: suggestion.Description,
            confidence: suggestion.Confidence,
            aiReasoning: aiReasoningJson,
            invoiceType: suggestion.InvoiceType,
            taxKey: suggestion.TaxKey,
            vatTreatmentType: suggestion.VatTreatment?.Type,
            suggestedEntityId: suggestedEntityId);

        _db.BookingSuggestions.Add(bookingSuggestion);
        return true;
    }

    // ── Entity Matching ──────────────────────────────────────────────────

    private static (decimal Confidence, string Reason) MatchEntityFields(
        LegalEntity entity, DocumentExtractionResult extraction)
    {
        // For incoming documents: recipient is us (the entity)
        // For outgoing documents: vendor/issuer is us
        var isIncoming = extraction.DocumentDirection != "outgoing"; // default assumption

        var nameToMatch = isIncoming ? extraction.RecipientName : extraction.VendorName;
        var taxIdToMatch = isIncoming ? extraction.RecipientTaxId : extraction.VendorTaxId;
        var vatIdToMatch = isIncoming ? extraction.RecipientVatId : null;

        var reasons = new List<string>();
        var totalScore = 0m;
        var checks = 0;

        // Tax ID match (strongest signal)
        if (!string.IsNullOrWhiteSpace(taxIdToMatch) && !string.IsNullOrWhiteSpace(entity.TaxId))
        {
            checks++;
            if (NormalizeTaxId(taxIdToMatch) == NormalizeTaxId(entity.TaxId))
            {
                totalScore += 1.0m;
                reasons.Add("tax_id_match");
            }
        }

        // VAT ID match
        if (!string.IsNullOrWhiteSpace(vatIdToMatch) && !string.IsNullOrWhiteSpace(entity.VatId))
        {
            checks++;
            if (NormalizeVatId(vatIdToMatch) == NormalizeVatId(entity.VatId))
            {
                totalScore += 1.0m;
                reasons.Add("vat_id_match");
            }
        }

        // Name match (fuzzy — company names vary)
        if (!string.IsNullOrWhiteSpace(nameToMatch) && !string.IsNullOrWhiteSpace(entity.Name))
        {
            checks++;
            var normalizedMatch = NormalizeCompanyName(nameToMatch);
            var normalizedEntity = NormalizeCompanyName(entity.Name);
            if (normalizedMatch.Contains(normalizedEntity, StringComparison.OrdinalIgnoreCase)
                || normalizedEntity.Contains(normalizedMatch, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 0.8m;
                reasons.Add("name_match");
            }
        }

        if (checks == 0)
            return (0.5m, "no_fields_to_match"); // Can't determine — neutral

        var confidence = totalScore / checks;
        return (confidence, reasons.Count > 0 ? string.Join(",", reasons) : "no_match");
    }

    private static string NormalizeTaxId(string id)
        => id.Replace("/", "").Replace(" ", "").Replace("-", "").Trim();

    private static string NormalizeVatId(string id)
        => id.Replace(" ", "").Trim().ToUpperInvariant();

    private static string NormalizeCompanyName(string name)
        => name.ToLowerInvariant()
            .Replace("gmbh", "").Replace(" ag ", " ").Replace("ug", "")
            .Replace("e.k.", "").Replace("ohg", "").Replace(" kg ", " ")
            .Replace("&", "").Replace("co.", "")
            .Trim();
}
