using System.Diagnostics;
using Azure;
using Azure.AI.DocumentIntelligence;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

/// <summary>
/// Azure Document Intelligence service for OCR + structured field extraction.
/// Uses prebuilt-invoice/receipt for structured data, prebuilt-layout as fallback.
/// </summary>
public sealed class AzureDocIntelligenceService : IAzureDocIntelligenceService
{
    private readonly IServiceProvider _sp;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<AzureDocIntelligenceService> _logger;

    public AzureDocIntelligenceService(
        IServiceProvider sp,
        IEncryptionService encryption,
        ILogger<AzureDocIntelligenceService> logger)
    {
        _sp = sp;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<AzureDocIntelligenceResult?> AnalyzeDocumentAsync(
        Stream documentStream, string documentType, Guid documentId, CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        if (config is null)
        {
            _logger.LogDebug("Azure Document Intelligence not configured, skipping");
            return null;
        }

        var modelId = ResolveModel(documentType);
        var client = CreateClient(config);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting Azure Document Intelligence analysis for document {DocumentId} with model {Model}",
            documentId, modelId);

        try
        {
            var content = await BinaryData.FromStreamAsync(documentStream, ct);

            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                content,
                cancellationToken: ct);

            var result = operation.Value;
            sw.Stop();

            var ocrText = result.Content ?? string.Empty;
            var pageCount = result.Pages?.Count ?? 0;
            var documentCount = result.Documents?.Count ?? 0;

            _logger.LogInformation(
                "Azure Document Intelligence completed for document {DocumentId} in {DurationMs}ms with {PageCount} pages and {DocumentCount} documents",
                documentId, sw.ElapsedMilliseconds, pageCount, documentCount);

            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(ocrText))
                warnings.Add("azure_empty_text");

            // Extract structured fields from prebuilt model results
            var extraction = MapToExtractionResult(result, modelId, warnings);

            return new AzureDocIntelligenceResult
            {
                Extraction = extraction,
                OcrText = ocrText,
                Confidence = extraction.Confidence,
                ModelUsed = modelId,
                Warnings = warnings,
            };
        }
        catch (RequestFailedException ex) when (ex.Status is 401 or 403)
        {
            sw.Stop();
            _logger.LogError(ex, "Azure Document Intelligence authentication failed for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Azure Document Intelligence timed out for document {DocumentId} after {DurationMs}ms",
                documentId, sw.ElapsedMilliseconds);
            throw new TimeoutException($"Azure Document Intelligence timed out after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Azure Document Intelligence failed for document {DocumentId} after {DurationMs}ms",
                documentId, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        if (config is null) return false;

        try
        {
            var client = CreateClient(config);
            var testPdf = Convert.FromBase64String(
                "JVBERi0xLjAKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgPj4KZW5kb2" +
                "JqCjIgMCBvYmoKPDwgL1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5k" +
                "b2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAgUiAvTWVkaWFCb3ggWzAgMCA2" +
                "MTIgNzkyXSA+PgplbmRvYmoKeHJlZgowIDQKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDA5" +
                "IDAwMDAwIG4gCjAwMDAwMDAwNzQgMDAwMDAgbiAKMDAwMDAwMDE0MyAwMDAwMCBuIAp0cmFpbGVy" +
                "Cjw8IC9TaXplIDQgL1Jvb3QgMSAwIFIgPj4Kc3RhcnR4cmVmCjIzNgolJUVPRg==");

            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                BinaryData.FromBytes(testPdf),
                cancellationToken: ct);

            return operation.HasCompleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Document Intelligence health check failed");
            return false;
        }
    }

    // ── Model Routing ────────────────────────────────────────────────────

    private static string ResolveModel(string documentType)
        => documentType.ToLowerInvariant() switch
        {
            "invoice" => "prebuilt-invoice",
            "receipt" => "prebuilt-receipt",
            _ => "prebuilt-layout",
        };

    // ── Field Mapping ────────────────────────────────────────────────────

    private DocumentExtractionResult MapToExtractionResult(
        AnalyzeResult result, string modelId, List<string> warnings)
    {
        // For prebuilt-invoice/receipt, extract structured fields from documents
        if (modelId is "prebuilt-invoice" or "prebuilt-receipt"
            && result.Documents is { Count: > 0 })
        {
            var doc = result.Documents[0];
            var fields = doc.Fields;

            if (fields is null || fields.Count == 0)
            {
                warnings.Add("azure_no_structured_fields");
                return CreateOcrOnlyResult(result);
            }

            return MapInvoiceOrReceiptResult(fields, doc, modelId);
        }

        // For prebuilt-layout or when no documents found, return OCR-only result
        return CreateOcrOnlyResult(result);
    }

    private static DocumentExtractionResult MapInvoiceOrReceiptResult(
        IReadOnlyDictionary<string, DocumentField> fields,
        AnalyzedDocument doc,
        string modelId)
    {
        // Vendor address
        string? vendorStreet = null, vendorCity = null, vendorPostalCode = null, vendorCountry = null;
        if (fields.TryGetValue("VendorAddress", out var addrField)
            && addrField.FieldType == DocumentFieldType.Address
            && addrField.ValueAddress is { } addr)
        {
            vendorStreet = addr.Road;
            vendorCity = addr.City;
            vendorPostalCode = addr.PostalCode;
            vendorCountry = addr.CountryRegion;
        }

        // Customer/recipient address
        string? recipientStreet = null, recipientCity = null, recipientPostalCode = null, recipientCountry = null;
        if (fields.TryGetValue("CustomerAddress", out var custAddrField)
            && custAddrField.FieldType == DocumentFieldType.Address
            && custAddrField.ValueAddress is { } custAddr)
        {
            recipientStreet = custAddr.Road;
            recipientCity = custAddr.City;
            recipientPostalCode = custAddr.PostalCode;
            recipientCountry = custAddr.CountryRegion;
        }

        // Currency from InvoiceTotal or SubTotal
        string? currency = null;
        if (fields.TryGetValue("InvoiceTotal", out var totalField)
            && totalField.FieldType == DocumentFieldType.Currency
            && totalField.ValueCurrency is { } totalCurrency)
        {
            currency = totalCurrency.CurrencyCode;
        }
        else if (fields.TryGetValue("SubTotal", out var subField)
                 && subField.FieldType == DocumentFieldType.Currency
                 && subField.ValueCurrency is { } subCurrency)
        {
            currency = subCurrency.CurrencyCode;
        }

        // Line items
        var lineItems = new List<LineItemResult>();
        if (fields.TryGetValue("Items", out var itemsField)
            && itemsField.FieldType == DocumentFieldType.List
            && itemsField.ValueList is { } itemList)
        {
            foreach (var item in itemList)
            {
                if (item.FieldType != DocumentFieldType.Dictionary
                    || item.ValueDictionary is not { } itemFields)
                    continue;

                lineItems.Add(new LineItemResult
                {
                    Description = GetStringValue(itemFields, "Description"),
                    Quantity = GetDecimalValue(itemFields, "Quantity"),
                    UnitPrice = GetCurrencyAmount(itemFields, "UnitPrice"),
                    TotalPrice = GetCurrencyAmount(itemFields, "Amount"),
                });
            }
        }

        // Tax: TotalTax amount → we store as raw field, derive rate if possible
        var totalAmount = GetCurrencyAmount(fields, "InvoiceTotal");
        var subTotal = GetCurrencyAmount(fields, "SubTotal");
        var totalTax = GetCurrencyAmount(fields, "TotalTax");
        decimal? taxRate = null;
        if (totalTax.HasValue && subTotal.HasValue && subTotal.Value != 0)
            taxRate = Math.Round(totalTax.Value / subTotal.Value * 100, 2);

        // Raw fields for anything extra
        var rawFields = new Dictionary<string, string>();
        if (totalTax.HasValue)
            rawFields["TotalTax"] = totalTax.Value.ToString("F2");
        if (subTotal.HasValue)
            rawFields["SubTotal"] = subTotal.Value.ToString("F2");
        rawFields["AzureModel"] = modelId;

        // Confidence: average of all field confidences
        var fieldConfidences = fields.Values
            .Where(f => f.Confidence.HasValue && float.IsFinite(f.Confidence.Value))
            .Select(f => (decimal)f.Confidence!.Value)
            .ToList();

        var confidence = fieldConfidences.Count > 0
            ? fieldConfidences.Average()
            : SafeConfidence(doc.Confidence);

        return new DocumentExtractionResult
        {
            VendorName = GetStringValue(fields, "VendorName"),
            VendorTaxId = GetStringValue(fields, "VendorTaxId"),
            VendorStreet = vendorStreet,
            VendorCity = vendorCity,
            VendorPostalCode = vendorPostalCode,
            VendorCountry = vendorCountry,
            VendorIban = null, // Not extracted by Azure prebuilt models
            VendorBic = null,
            RecipientName = GetStringValue(fields, "CustomerName"),
            RecipientTaxId = GetStringValue(fields, "CustomerTaxId"),
            RecipientVatId = GetStringValue(fields, "CustomerTaxId"), // Azure uses same field for both
            RecipientStreet = recipientStreet,
            RecipientCity = recipientCity,
            RecipientPostalCode = recipientPostalCode,
            RecipientCountry = recipientCountry,
            DocumentDirection = !string.IsNullOrWhiteSpace(GetStringValue(fields, "VendorName")) ? "incoming"
                : !string.IsNullOrWhiteSpace(GetStringValue(fields, "CustomerName")) ? "outgoing"
                : null,
            InvoiceNumber = GetStringValue(fields, "InvoiceId"),
            InvoiceDate = GetDateValue(fields, "InvoiceDate"),
            TotalAmount = totalAmount,
            GrossAmount = totalAmount,      // InvoiceTotal = Brutto
            NetAmount = subTotal,            // SubTotal = Netto
            TaxAmount = totalTax,            // TotalTax = USt
            Currency = currency,
            TaxRate = taxRate,
            LineItems = lineItems,
            RawFields = rawFields,
            Confidence = confidence,
        };
    }

    private static DocumentExtractionResult CreateOcrOnlyResult(AnalyzeResult result)
    {
        var wordConfidence = CalculateWordConfidence(result);
        return new DocumentExtractionResult
        {
            Confidence = wordConfidence,
            RawFields = new Dictionary<string, string> { ["AzureModel"] = "prebuilt-layout" },
        };
    }

    // ── Safe Value Accessors ─────────────────────────────────────────────

    private static string? GetStringValue(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        return field.FieldType == DocumentFieldType.String ? field.ValueString : field.Content;
    }

    private static decimal? GetCurrencyAmount(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.Currency && field.ValueCurrency is { } c)
            return (decimal)c.Amount;
        if (field.FieldType == DocumentFieldType.Double && field.ValueDouble is { } d)
            return (decimal)d;
        return null;
    }

    private static DateOnly? GetDateValue(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.Date && field.ValueDate is { } date)
            return DateOnly.FromDateTime(date.DateTime);
        return null;
    }

    private static decimal? GetDecimalValue(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.Double && field.ValueDouble is { } d)
            return (decimal)d;
        if (field.FieldType == DocumentFieldType.Currency && field.ValueCurrency is { } c)
            return (decimal)c.Amount;
        return null;
    }

    /// <summary>DocumentField.Confidence is float? — safe cast with NaN/Infinity guard.</summary>
    private static decimal SafeConfidence(float? confidence)
        => confidence.HasValue && float.IsFinite(confidence.Value)
            ? (decimal)confidence.Value
            : 0.5m;

    /// <summary>AnalyzedDocument.Confidence is float — only NaN/Infinity guard needed.</summary>
    private static decimal SafeConfidence(float confidence)
        => float.IsFinite(confidence) ? (decimal)confidence : 0.5m;

    private static decimal CalculateWordConfidence(AnalyzeResult result)
    {
        if (result.Pages is null or { Count: 0 }) return 0.5m;

        var allConfidences = result.Pages
            .SelectMany(p => p.Words ?? Enumerable.Empty<DocumentWord>())
            .Where(w => float.IsFinite(w.Confidence))
            .Select(w => (decimal)w.Confidence)
            .ToList();

        return allConfidences.Count > 0 ? allConfidences.Average() : 0.5m;
    }

    // ── Infrastructure ───────────────────────────────────────────────────

    private sealed record ProviderConfig(string ApiKey, string Endpoint);

    private async Task<ProviderConfig?> GetConfigAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var config = await db.AiProviderConfigs
            .FirstOrDefaultAsync(c => c.Provider == AiProvider.AzureDocIntelligence && c.IsActive, ct);

        if (config is null) return null;

        var endpoint = config.BaseUrl;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogWarning("Azure Document Intelligence is configured but has no endpoint URL (BaseUrl)");
            return null;
        }

        return new ProviderConfig(
            _encryption.Decrypt(config.EncryptedApiKey),
            endpoint);
    }

    private static DocumentIntelligenceClient CreateClient(ProviderConfig config)
    {
        return new DocumentIntelligenceClient(
            new Uri(config.Endpoint),
            new AzureKeyCredential(config.ApiKey));
    }
}
