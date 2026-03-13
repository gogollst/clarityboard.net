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
/// Azure Document Intelligence service for structured document extraction.
/// Takes binary documents and returns structured fields — NOT a chat-completions provider.
/// Supports prebuilt models: prebuilt-invoice, prebuilt-receipt, prebuilt-layout.
/// </summary>
public sealed class AzureDocIntelligenceService : IAzureDocIntelligenceService
{
    private readonly IServiceProvider _sp;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<AzureDocIntelligenceService> _logger;

    private static readonly Dictionary<string, string> DocumentTypeToModel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["invoice"] = "prebuilt-invoice",
        ["receipt"] = "prebuilt-receipt",
    };

    private const string FallbackModel = "prebuilt-layout";

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
        Stream documentStream, string contentType, string documentType,
        Guid documentId, CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        if (config is null)
        {
            _logger.LogDebug("Azure Document Intelligence not configured, skipping");
            return null;
        }

        var modelId = ResolveModel(documentType, config.ModelDefault);
        var client = CreateClient(config);
        var sw = Stopwatch.StartNew();
        var warnings = new List<string>();

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

            _logger.LogInformation(
                "Azure Document Intelligence completed for document {DocumentId} in {DurationMs}ms with {PageCount} pages and {DocCount} documents",
                documentId, sw.ElapsedMilliseconds, result.Pages?.Count ?? 0, result.Documents?.Count ?? 0);

            var extraction = MapToExtractionResult(result, modelId, warnings);
            var ocrText = result.Content ?? string.Empty;

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
            // Minimal 1-page blank PDF to test connectivity
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

    // ── Private Helpers ───────────────────────────────────────────────────

    private sealed record ProviderConfig(string ApiKey, string Endpoint, string? ModelDefault);

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
            endpoint,
            config.ModelDefault);
    }

    private static DocumentIntelligenceClient CreateClient(ProviderConfig config)
    {
        return new DocumentIntelligenceClient(
            new Uri(config.Endpoint),
            new AzureKeyCredential(config.ApiKey));
    }

    private static string ResolveModel(string documentType, string? configuredDefault)
    {
        if (DocumentTypeToModel.TryGetValue(documentType, out var model))
            return model;

        return configuredDefault ?? FallbackModel;
    }

    // ── Result Mapping ───────────────────────────────────────────────────

    private static DocumentExtractionResult MapToExtractionResult(
        AnalyzeResult result, string modelId, List<string> warnings)
    {
        // For prebuilt-invoice and prebuilt-receipt, extract structured fields
        if (modelId is "prebuilt-invoice" or "prebuilt-receipt")
            return MapInvoiceOrReceiptResult(result, warnings);

        // For prebuilt-layout: only OCR text, no structured fields
        return new DocumentExtractionResult
        {
            Confidence = CalculateWordConfidence(result),
        };
    }

    private static DocumentExtractionResult MapInvoiceOrReceiptResult(
        AnalyzeResult result, List<string> warnings)
    {
        var doc = result.Documents?.FirstOrDefault();
        if (doc is null)
        {
            warnings.Add("azure_no_document_detected");
            return new DocumentExtractionResult
            {
                Confidence = CalculateWordConfidence(result),
            };
        }

        if (doc.Fields is not { } fields)
        {
            warnings.Add("azure_no_fields_detected");
            return new DocumentExtractionResult
            {
                Confidence = float.IsFinite(doc.Confidence) ? (decimal)doc.Confidence : 0.5m,
            };
        }

        // Extract line items
        var lineItems = new List<LineItemResult>();
        if (fields.TryGetValue("Items", out var itemsField)
            && itemsField.FieldType == DocumentFieldType.List
            && itemsField.ValueList is { } itemList)
        {
            foreach (var item in itemList)
            {
                if (item.FieldType != DocumentFieldType.Dictionary || item.ValueDictionary is not { } itemFields)
                    continue;

                lineItems.Add(new LineItemResult
                {
                    Description = GetStringValue(itemFields, "Description"),
                    Quantity = GetDoubleValue(itemFields, "Quantity") is { } qty ? (decimal)qty : null,
                    UnitPrice = GetCurrencyAmount(itemFields, "UnitPrice"),
                    TotalPrice = GetCurrencyAmount(itemFields, "Amount"),
                });
            }
        }

        // Calculate confidence from field confidences (Confidence is float, not nullable)
        // Filter out NaN/Infinity which would throw OverflowException on decimal cast
        var confidences = fields.Values
            .Select(f => f.Confidence)
            .Where(c => c.HasValue && float.IsFinite(c.Value))
            .Select(c => (decimal)c!.Value)
            .ToList();
        var fieldConfidence = confidences.Count > 0 ? confidences.Average() : 0.5m;
        var docConfidence = float.IsFinite(doc.Confidence) ? (decimal)doc.Confidence : 0.5m;
        var overallConfidence = Math.Min(docConfidence, fieldConfidence);

        // Build raw fields from all Azure fields for transparency
        var rawFields = new Dictionary<string, string>();
        foreach (var kvp in fields)
        {
            if (kvp.Value.Content is not null)
                rawFields[$"azure_{kvp.Key}"] = kvp.Value.Content;
        }

        // Extract vendor address components
        AddressValue? vendorAddress = null;
        if (fields.TryGetValue("VendorAddress", out var vendorAddrField)
            && vendorAddrField.FieldType == DocumentFieldType.Address
            && vendorAddrField.ValueAddress is { } addr)
        {
            vendorAddress = addr;
        }

        // Calculate tax rate from TotalTax / SubTotal
        var totalTax = GetCurrencyAmount(fields, "TotalTax");
        var subTotal = GetCurrencyAmount(fields, "SubTotal");
        decimal? taxRate = totalTax.HasValue && subTotal is > 0
            ? Math.Round(totalTax.Value / subTotal.Value * 100, 2)
            : null;

        return new DocumentExtractionResult
        {
            VendorName = GetStringValue(fields, "VendorName"),
            VendorTaxId = GetStringValue(fields, "VendorTaxId"),
            VendorStreet = vendorAddress?.Road,
            VendorCity = vendorAddress?.City,
            VendorPostalCode = vendorAddress?.PostalCode,
            VendorCountry = vendorAddress?.CountryRegion,
            InvoiceNumber = GetStringValue(fields, "InvoiceId") ?? GetStringValue(fields, "InvoiceNumber"),
            InvoiceDate = GetDateValue(fields, "InvoiceDate") ?? GetDateValue(fields, "TransactionDate"),
            TotalAmount = GetCurrencyAmount(fields, "InvoiceTotal")
                          ?? GetCurrencyAmount(fields, "Total")
                          ?? GetCurrencyAmount(fields, "SubTotal"),
            Currency = GetStringValue(fields, "CurrencyCode"),
            TaxRate = taxRate,
            LineItems = lineItems,
            RawFields = rawFields,
            Confidence = overallConfidence,
        };
    }

    // ── Field Value Extractors ───────────────────────────────────────────

    private static string? GetStringValue(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.String)
            return field.ValueString;
        return field.Content;
    }

    private static decimal? GetCurrencyAmount(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.Currency && field.ValueCurrency is { } currency)
            return (decimal)currency.Amount;
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

    private static double? GetDoubleValue(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        if (field.FieldType == DocumentFieldType.Double)
            return field.ValueDouble;
        return null;
    }

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
}
