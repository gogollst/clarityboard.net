using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Domain.Entities.Integration;
using ClarityBoard.Domain.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

/// <summary>
/// Specialized consumer for Zoho Books invoice webhooks.
/// Creates Document (outgoing) + BookingSuggestion instead of a simple 2-line JournalEntry,
/// so that the existing review/approve workflow can be used.
/// </summary>
public class ZohoInvoiceProcessorConsumer : IConsumer<ProcessWebhookEvent>
{
    private const string SourceType = "zoho_books";
    private const decimal ZohoConfidence = 0.95m; // Structured data, no OCR needed

    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IBusinessPartnerMatchingService _partnerMatchingService;
    private readonly ILogger<ZohoInvoiceProcessorConsumer> _logger;

    public ZohoInvoiceProcessorConsumer(
        IAppDbContext db,
        IAccountingRepository accountingRepo,
        IBusinessPartnerMatchingService partnerMatchingService,
        ILogger<ZohoInvoiceProcessorConsumer> logger)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _partnerMatchingService = partnerMatchingService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessWebhookEvent> context)
    {
        var message = context.Message;

        // Only handle zoho_books events — other sources are handled by WebhookProcessorConsumer
        if (!string.Equals(message.SourceType, SourceType, StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogInformation(
            "Processing Zoho Books webhook event {EventId} type {EventType}",
            message.WebhookEventId, message.EventType);

        var ct = context.CancellationToken;
        var sw = Stopwatch.StartNew();

        var webhookEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(e => e.Id == message.WebhookEventId, ct);

        if (webhookEvent is null)
        {
            _logger.LogWarning("Webhook event {EventId} not found", message.WebhookEventId);
            return;
        }

        webhookEvent.MarkProcessing();
        await _db.SaveChangesAsync(ct);

        try
        {
            var invoice = ParseZohoInvoice(webhookEvent.Payload);
            if (invoice is null)
            {
                webhookEvent.MarkFailed("Could not parse Zoho invoice payload.");
                await SaveDuration(webhookEvent, sw, ct);
                return;
            }

            // Duplicate check: same invoice number + entity
            var duplicate = await _db.Documents
                .AnyAsync(d =>
                    d.EntityId == message.EntityId
                    && d.InvoiceNumber == invoice.InvoiceNumber
                    && d.DocumentDirection == "outgoing"
                    && d.Status != "failed", ct);

            if (duplicate)
            {
                _logger.LogInformation(
                    "Duplicate Zoho invoice {InvoiceNumber} for entity {EntityId}, skipping",
                    invoice.InvoiceNumber, message.EntityId);
                webhookEvent.MarkCompleted();
                await SaveDuration(webhookEvent, sw, ct);
                return;
            }

            // Create Document entity (outgoing invoice, no file upload — webhook-sourced)
            var document = Document.Create(
                entityId: message.EntityId,
                fileName: $"zoho-{invoice.InvoiceNumber}.json",
                contentType: "application/json",
                fileSize: webhookEvent.Payload.Length,
                storagePath: $"zoho-webhooks/{message.WebhookEventId}",
                documentType: "invoice",
                createdBy: Guid.Empty); // System-generated

            document.SetClassification("outgoing", 1.0m);
            document.SetExtraction(
                ocrText: string.Empty,
                extractedData: webhookEvent.Payload,
                confidence: ZohoConfidence,
                vendorName: invoice.CustomerName,
                invoiceNumber: invoice.InvoiceNumber,
                invoiceDate: invoice.InvoiceDate,
                totalAmount: invoice.Total,
                currency: invoice.CurrencyCode ?? "EUR",
                netAmount: invoice.SubTotal,
                taxAmount: invoice.TaxTotal);

            if (invoice.DueDate.HasValue)
                document.SetDueDate(invoice.DueDate.Value);

            if (!string.IsNullOrWhiteSpace(invoice.ReferenceNumber))
                document.SetOrderNumber(invoice.ReferenceNumber);

            document.SetReverseCharge(invoice.IsReverseChargeApplied);

            // Business partner matching
            if (!string.IsNullOrWhiteSpace(invoice.CustomerName))
            {
                var matchResult = await _partnerMatchingService.MatchPartnerAsync(
                    message.EntityId, invoice.CustomerName, taxId: null, iban: null, ct);

                if (matchResult.MatchType == PartnerMatchType.Exact && matchResult.MatchedPartner is not null)
                    document.AssignBusinessPartner(matchResult.MatchedPartner.Id);
                else if (matchResult.MatchType == PartnerMatchType.Fuzzy && matchResult.MatchedPartner is not null)
                    document.SuggestBusinessPartner(matchResult.MatchedPartner.Id);
            }

            _db.Documents.Add(document);

            // Resolve accounts for the booking suggestion
            var receivablesAccount = await _accountingRepo.GetAccountAsync(message.EntityId, "1400", ct);
            var revenueAccount = await ResolveRevenueAccountAsync(message.EntityId, ct);

            if (receivablesAccount is null || revenueAccount is null)
            {
                webhookEvent.MarkFailed(
                    $"Required accounts not found: 1400={receivablesAccount is not null}, revenue={revenueAccount is not null}");
                await SaveDuration(webhookEvent, sw, ct);
                return;
            }

            // Determine VAT code from tax percentage
            var vatCode = DetermineVatCode(invoice);
            var vatTreatmentType = invoice.IsReverseChargeApplied ? "reverse_charge" : "standard";

            var suggestion = BookingSuggestion.Create(
                documentId: document.Id,
                entityId: message.EntityId,
                debitAccountId: receivablesAccount.Id,   // 1400 Forderungen
                creditAccountId: revenueAccount.Id,       // 4110/4400 Erlöse
                amount: invoice.Total,                    // Gross amount
                vatCode: vatCode,
                vatAmount: invoice.TaxTotal,
                description: $"AR: {invoice.CustomerName} {invoice.InvoiceNumber}",
                confidence: ZohoConfidence,
                aiReasoning: JsonSerializer.Serialize(new
                {
                    source = "zoho_books",
                    eventType = message.EventType,
                    zohoInvoiceId = invoice.InvoiceId,
                    reason = "Structured invoice data from Zoho Books webhook — no OCR needed"
                }),
                invoiceType: "outgoing",
                taxKey: vatCode,
                vatTreatmentType: vatTreatmentType);

            _db.BookingSuggestions.Add(suggestion);

            // Set document to review status (manual approval required in Phase 1)
            document.MarkReview();

            webhookEvent.MarkCompleted();
            await SaveDuration(webhookEvent, sw, ct);

            _logger.LogInformation(
                "Zoho invoice {InvoiceNumber} processed: Document {DocumentId}, Suggestion {SuggestionId} in {Duration}ms",
                invoice.InvoiceNumber, document.Id, suggestion.Id, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error processing Zoho webhook event {EventId}: {Error}",
                message.WebhookEventId, ex.Message);

            webhookEvent.MarkFailed(ex.Message);
            webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);

            if (webhookEvent.RetryCount >= 3)
            {
                _logger.LogWarning("Zoho webhook event {EventId} exceeded max retries, moving to dead_letter",
                    message.WebhookEventId);
                webhookEvent.MarkDeadLetter();
            }

            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private ZohoInvoicePayload? ParseZohoInvoice(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Zoho webhook payload has invoice data either at root.invoice or directly at root
            JsonElement invoiceElement;
            if (root.TryGetProperty("invoice", out var inv))
                invoiceElement = inv;
            else
                invoiceElement = root;

            return new ZohoInvoicePayload
            {
                InvoiceId = GetStringProperty(invoiceElement, "invoice_id"),
                InvoiceNumber = GetStringProperty(invoiceElement, "invoice_number"),
                InvoiceDate = GetDateProperty(invoiceElement, "date"),
                DueDate = GetDateProperty(invoiceElement, "due_date"),
                CustomerName = GetStringProperty(invoiceElement, "customer_name"),
                CustomerId = GetStringProperty(invoiceElement, "customer_id"),
                CurrencyCode = GetStringProperty(invoiceElement, "currency_code"),
                SubTotal = GetDecimalProperty(invoiceElement, "sub_total") ?? 0m,
                TaxTotal = GetDecimalProperty(invoiceElement, "tax_total") ?? 0m,
                Total = GetDecimalProperty(invoiceElement, "total") ?? 0m,
                Balance = GetDecimalProperty(invoiceElement, "balance"),
                Status = GetStringProperty(invoiceElement, "status"),
                ReferenceNumber = GetStringProperty(invoiceElement, "reference_number"),
                IsReverseChargeApplied = GetBoolProperty(invoiceElement, "is_reverse_charge_applied"),
                TaxTreatment = GetStringProperty(invoiceElement, "tax_treatment"),
                LineItems = ParseLineItems(invoiceElement),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Zoho invoice payload");
            return null;
        }
    }

    private List<ZohoLineItem> ParseLineItems(JsonElement invoiceElement)
    {
        var items = new List<ZohoLineItem>();
        if (!invoiceElement.TryGetProperty("line_items", out var lineItemsArray)
            || lineItemsArray.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var item in lineItemsArray.EnumerateArray())
        {
            items.Add(new ZohoLineItem
            {
                Name = GetStringProperty(item, "name"),
                Description = GetStringProperty(item, "description"),
                Quantity = GetDecimalProperty(item, "quantity") ?? 0m,
                Rate = GetDecimalProperty(item, "rate") ?? 0m,
                TaxPercentage = GetDecimalProperty(item, "tax_percentage"),
                ItemTotal = GetDecimalProperty(item, "item_total") ?? 0m,
            });
        }

        return items;
    }

    private static string DetermineVatCode(ZohoInvoicePayload invoice)
    {
        if (invoice.IsReverseChargeApplied)
            return "RC";

        // Check line items for tax percentage
        var taxPercentage = invoice.LineItems.FirstOrDefault()?.TaxPercentage;
        if (taxPercentage.HasValue)
        {
            return taxPercentage.Value switch
            {
                19m => "19",
                7m => "7",
                0m => "0",
                _ => taxPercentage.Value.ToString(CultureInfo.InvariantCulture),
            };
        }

        // Fallback: infer from amounts
        if (invoice.SubTotal > 0 && invoice.TaxTotal > 0)
        {
            var rate = invoice.TaxTotal / invoice.SubTotal;
            return rate < 0.12m ? "7" : "19";
        }

        return "0";
    }

    private async Task<Account?> ResolveRevenueAccountAsync(Guid entityId, CancellationToken ct)
    {
        // Try standard revenue accounts in priority order
        var revenueAccountNumbers = new[] { "4110", "4400", "4000" };
        foreach (var accountNumber in revenueAccountNumbers)
        {
            var account = await _accountingRepo.GetAccountAsync(entityId, accountNumber, ct);
            if (account is not null)
                return account;
        }

        return null;
    }

    private async Task SaveDuration(WebhookEvent webhookEvent, Stopwatch sw, CancellationToken ct)
    {
        sw.Stop();
        webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);
        await _db.SaveChangesAsync(ct);
    }

    #region JSON helpers

    private static string? GetStringProperty(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static decimal? GetDecimalProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };
    }

    private static DateOnly? GetDateProperty(JsonElement element, string name)
    {
        var str = GetStringProperty(element, name);
        if (str is null) return null;
        return DateOnly.TryParse(str, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static bool GetBoolProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var prop))
            return false;

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(prop.GetString(), out var b) && b,
            _ => false,
        };
    }

    #endregion

    #region Payload DTOs

    private sealed class ZohoInvoicePayload
    {
        public string? InvoiceId { get; init; }
        public string? InvoiceNumber { get; init; }
        public DateOnly? InvoiceDate { get; init; }
        public DateOnly? DueDate { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerId { get; init; }
        public string? CurrencyCode { get; init; }
        public decimal SubTotal { get; init; }
        public decimal TaxTotal { get; init; }
        public decimal Total { get; init; }
        public decimal? Balance { get; init; }
        public string? Status { get; init; }
        public string? ReferenceNumber { get; init; }
        public bool IsReverseChargeApplied { get; init; }
        public string? TaxTreatment { get; init; }
        public List<ZohoLineItem> LineItems { get; init; } = [];
    }

    private sealed class ZohoLineItem
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal Quantity { get; init; }
        public decimal Rate { get; init; }
        public decimal? TaxPercentage { get; init; }
        public decimal ItemTotal { get; init; }
    }

    #endregion
}
