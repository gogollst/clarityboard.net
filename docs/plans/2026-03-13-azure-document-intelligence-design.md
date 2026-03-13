# Azure Document Intelligence Integration — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Integrate Azure Document Intelligence as a new document processing provider with hybrid fallback to the existing OCR+AI pipeline.

**Architecture:** Azure Doc Intelligence replaces Stages 3-5 (text acquisition + field extraction) in the document processing pipeline when configured and active. On failure or low confidence, the system falls back to the existing native/vision OCR + Claude extraction pipeline. Azure is registered as `AiProvider.AzureDocIntelligence = 8` for credential management via the existing admin UI, but is NOT a chat-completions provider — it has its own dedicated service (`IAzureDocIntelligenceService`).

**Tech Stack:** Azure.AI.DocumentIntelligence NuGet SDK, .NET 10, EF Core, React 19 + TypeScript, TanStack Query, i18next

---

## Task 1: Add AiProvider Enum Value

**Files:**
- Modify: `src/backend/src/ClarityBoard.Domain/Entities/AI/AiProvider.cs`

**Step 1: Add enum value**

```csharp
public enum AiProvider
{
    Anthropic = 1,
    OpenAI    = 2,
    Grok      = 3,
    Gemini    = 4,
    ZAI       = 5,
    Manus     = 6,
    DeepL     = 7,
    AzureDocIntelligence = 8,
}
```

**Step 2: Update PromptAiService default model + skip**

Modify `src/backend/src/ClarityBoard.Infrastructure/Services/AI/PromptAiService.cs`:

In `GetDefaultModel`:
```csharp
AiProvider.AzureDocIntelligence => "prebuilt-invoice",
```

In `CallProviderAsync` switch, add before the default case:
```csharp
AiProvider.AzureDocIntelligence => throw new NotSupportedException("Azure Document Intelligence does not support chat completions. Use IAzureDocIntelligenceService instead."),
AiProvider.DeepL => throw new NotSupportedException("DeepL does not support chat completions. Use ITranslationService instead."),
```

**Step 3: Commit**

```
feat: add AzureDocIntelligence to AiProvider enum
```

---

## Task 2: Add NuGet Package

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/ClarityBoard.Infrastructure.csproj`

**Step 1: Add package reference**

Run from `src/backend/`:
```bash
dotnet add src/ClarityBoard.Infrastructure/ClarityBoard.Infrastructure.csproj package Azure.AI.DocumentIntelligence
```

**Step 2: Commit**

```
chore: add Azure.AI.DocumentIntelligence NuGet package
```

---

## Task 3: Create IAzureDocIntelligenceService Interface

**Files:**
- Create: `src/backend/src/ClarityBoard.Application/Common/Interfaces/IAzureDocIntelligenceService.cs`

**Step 1: Write interface**

```csharp
namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Azure Document Intelligence service for structured document extraction.
/// Unlike chat-completion providers, this takes binary documents and returns structured fields.
/// </summary>
public interface IAzureDocIntelligenceService
{
    /// <summary>
    /// Analyze a document using Azure Document Intelligence.
    /// Returns structured extraction result or null if the provider is not configured.
    /// </summary>
    Task<AzureDocIntelligenceResult?> AnalyzeDocumentAsync(
        Stream documentStream,
        string contentType,
        string documentType,
        Guid documentId,
        CancellationToken ct);

    /// <summary>
    /// Test connectivity to Azure Document Intelligence.
    /// </summary>
    Task<bool> TestConnectivityAsync(CancellationToken ct);
}

public record AzureDocIntelligenceResult
{
    public required DocumentExtractionResult Extraction { get; init; }
    public required string OcrText { get; init; }
    public required decimal Confidence { get; init; }
    public required string ModelUsed { get; init; }
    public List<string> Warnings { get; init; } = [];
}
```

**Step 2: Commit**

```
feat: add IAzureDocIntelligenceService interface
```

---

## Task 4: Implement AzureDocIntelligenceService

**Files:**
- Create: `src/backend/src/ClarityBoard.Infrastructure/Services/Documents/AzureDocIntelligenceService.cs`

**Step 1: Write implementation**

```csharp
using System.Diagnostics;
using Azure;
using Azure.AI.DocumentIntelligence;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

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
                contentType: contentType,
                cancellationToken: ct);

            var result = operation.Value;
            sw.Stop();

            _logger.LogInformation(
                "Azure Document Intelligence completed for document {DocumentId} in {DurationMs}ms with {PageCount} pages",
                documentId, sw.ElapsedMilliseconds, result.Pages?.Count ?? 0);

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
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            sw.Stop();
            _logger.LogError(ex, "Azure Document Intelligence authentication failed for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Azure Document Intelligence timed out for document {DocumentId} after {DurationMs}ms", documentId, sw.ElapsedMilliseconds);
            throw new TimeoutException($"Azure Document Intelligence timed out after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Azure Document Intelligence failed for document {DocumentId} after {DurationMs}ms", documentId, sw.ElapsedMilliseconds);
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
            // Use a minimal PDF to test connectivity
            var testPdf = Convert.FromBase64String(
                "JVBERi0xLjAKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAgUiAvTWVkaWFCb3ggWzAgMCA2MTIgNzkyXSA+PgplbmRvYmoKeHJlZgowIDQKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDA5IDAwMDAwIG4gCjAwMDAwMDAwNzQgMDAwMDAgbiAKMDAwMDAwMDE0MyAwMDAwMCBuIAp0cmFpbGVyCjw8IC9TaXplIDQgL1Jvb3QgMSAwIFIgPj4Kc3RhcnR4cmVmCjIzNgolJUVPRg==");

            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                BinaryData.FromBytes(testPdf),
                contentType: "application/pdf",
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

    private record ProviderConfig(string ApiKey, string Endpoint, string? ModelDefault);

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

    private static DocumentExtractionResult MapToExtractionResult(
        AnalyzeResult result, string modelId, List<string> warnings)
    {
        // For prebuilt-invoice and prebuilt-receipt, extract structured fields
        if (modelId is "prebuilt-invoice" or "prebuilt-receipt")
            return MapInvoiceOrReceiptResult(result, warnings);

        // For prebuilt-layout: only OCR text, no structured fields
        return new DocumentExtractionResult
        {
            Confidence = CalculateOverallConfidence(result),
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
                Confidence = CalculateOverallConfidence(result),
            };
        }

        var fields = doc.Fields ?? new Dictionary<string, DocumentField>();

        string? GetString(string key)
        {
            if (!fields.TryGetValue(key, out var field)) return null;
            return field.Content ?? field.ValueString;
        }

        decimal? GetDecimal(string key)
        {
            if (!fields.TryGetValue(key, out var field)) return null;
            if (field.ValueCurrency is not null) return (decimal)field.ValueCurrency.Amount;
            if (field.ValueNumber is not null) return (decimal)field.ValueNumber.Value;
            return null;
        }

        DateOnly? GetDate(string key)
        {
            if (!fields.TryGetValue(key, out var field)) return null;
            if (field.ValueDate is not null) return DateOnly.FromDateTime(field.ValueDate.Value);
            return null;
        }

        // Extract address components
        var vendorAddress = fields.TryGetValue("VendorAddress", out var addrField)
            ? addrField.ValueAddress : null;
        var vendorAddressContent = fields.TryGetValue("VendorAddressRecipient", out var addrRecipient)
            ? addrRecipient.Content : null;

        // Extract line items
        var lineItems = new List<LineItemResult>();
        if (fields.TryGetValue("Items", out var itemsField) && itemsField.ValueList is not null)
        {
            foreach (var item in itemsField.ValueList)
            {
                var itemFields = item.ValueObject ?? new Dictionary<string, DocumentField>();
                lineItems.Add(new LineItemResult
                {
                    Description = itemFields.TryGetValue("Description", out var desc) ? desc.Content : null,
                    Quantity = itemFields.TryGetValue("Quantity", out var qty) && qty.ValueNumber.HasValue
                        ? (decimal)qty.ValueNumber.Value : null,
                    UnitPrice = itemFields.TryGetValue("UnitPrice", out var up) && up.ValueCurrency is not null
                        ? (decimal)up.ValueCurrency.Amount : null,
                    TotalPrice = itemFields.TryGetValue("Amount", out var amt) && amt.ValueCurrency is not null
                        ? (decimal)amt.ValueCurrency.Amount : null,
                });
            }
        }

        // Calculate confidence from field confidences
        var confidences = fields.Values
            .Where(f => f.Confidence.HasValue)
            .Select(f => (decimal)f.Confidence!.Value)
            .ToList();
        var fieldConfidence = confidences.Count > 0 ? confidences.Average() : 0.5m;
        var docConfidence = doc.Confidence.HasValue ? (decimal)doc.Confidence.Value : fieldConfidence;
        var overallConfidence = Math.Min(docConfidence, fieldConfidence);

        // Build raw fields from all Azure fields for transparency
        var rawFields = new Dictionary<string, string>();
        foreach (var (key, field) in fields)
        {
            if (field.Content is not null)
                rawFields[$"azure_{key}"] = field.Content;
        }

        return new DocumentExtractionResult
        {
            VendorName = GetString("VendorName"),
            VendorTaxId = GetString("VendorTaxId"),
            VendorStreet = vendorAddress?.Road,
            VendorCity = vendorAddress?.City,
            VendorPostalCode = vendorAddress?.PostalCode,
            VendorCountry = vendorAddress?.CountryRegion,
            InvoiceNumber = GetString("InvoiceId") ?? GetString("InvoiceNumber"),
            InvoiceDate = GetDate("InvoiceDate") ?? GetDate("TransactionDate"),
            TotalAmount = GetDecimal("InvoiceTotal") ?? GetDecimal("Total") ?? GetDecimal("SubTotal"),
            Currency = GetString("CurrencyCode"),
            TaxRate = GetDecimal("TotalTax") is { } tax && GetDecimal("SubTotal") is { } sub and > 0
                ? Math.Round(tax / sub * 100, 2) : null,
            LineItems = lineItems,
            RawFields = rawFields,
            Confidence = overallConfidence,
        };
    }

    private static decimal CalculateOverallConfidence(AnalyzeResult result)
    {
        if (result.Pages is null or { Count: 0 }) return 0.5m;

        // Average word confidence across all pages
        var allConfidences = result.Pages
            .SelectMany(p => p.Words ?? Enumerable.Empty<DocumentWord>())
            .Where(w => w.Confidence.HasValue)
            .Select(w => (decimal)w.Confidence!.Value)
            .ToList();

        return allConfidences.Count > 0 ? allConfidences.Average() : 0.5m;
    }
}
```

**Step 2: Commit**

```
feat: implement AzureDocIntelligenceService with model mapping and fallback
```

---

## Task 5: Register Service + Health Check Special Case

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/DependencyInjection.cs`
- Modify: `src/backend/src/ClarityBoard.Application/Features/AI/Commands/UpsertAiProviderCommand.cs`

**Step 1: Register in DI**

Add after the `IDocumentTextAcquisitionService` registration:
```csharp
services.AddScoped<IAzureDocIntelligenceService, AzureDocIntelligenceService>();
```

**Step 2: Add health check in UpsertAiProviderCommand**

In the handler, add Azure case alongside the DeepL special case:
```csharp
if (request.Provider == AiProvider.DeepL)
{
    var result = await _translationService.TranslateAsync("Test", "en", ["de"], cancellationToken);
    isHealthy = result.Count > 0;
}
else if (request.Provider == AiProvider.AzureDocIntelligence)
{
    var azureService = _db is DbContext dbCtx
        ? dbCtx.GetService<IAzureDocIntelligenceService>()
        : throw new InvalidOperationException("Cannot resolve IAzureDocIntelligenceService");
    isHealthy = await azureService.TestConnectivityAsync(cancellationToken);
}
else
{
    isHealthy = await _aiService.TestProviderAsync(request.Provider, cancellationToken);
}
```

Actually, the better approach is to inject `IAzureDocIntelligenceService` into the handler. Add it to the constructor.

**Step 3: Commit**

```
feat: register AzureDocIntelligenceService and add health check
```

---

## Task 6: Integrate into DocumentProcessingConsumer

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs`

**Step 1: Inject the service**

Add to constructor: `IAzureDocIntelligenceService azureDocIntelligenceService`

**Step 2: Add Azure-first stage after file download**

After Stage 2 (download from MinIO), before Stage 3 (acquire text):

```csharp
// 2b. Try Azure Document Intelligence (replaces stages 3-4 if successful)
currentStage = "azure_doc_intelligence";
var azureStopwatch = Stopwatch.StartNew();
AzureDocIntelligenceResult? azureResult = null;
try
{
    fileStream.Position = 0;
    azureResult = await _azureDocIntelligenceService.AnalyzeDocumentAsync(
        fileStream, document.ContentType, document.DocumentType,
        document.Id, ct);
    azureStopwatch.Stop();

    if (azureResult is not null)
    {
        if (azureResult.Confidence < LowConfidenceThreshold)
        {
            LogStageWarning(currentStage, azureStopwatch.ElapsedMilliseconds, "low_confidence",
                "confidence {Confidence} model {Model}", azureResult.Confidence, azureResult.ModelUsed);
            reviewReasons.Add("azure_doc_intelligence_low_confidence");
            azureResult = null; // Fall back to standard pipeline
        }
        else
        {
            LogStageInformation(currentStage, azureStopwatch.ElapsedMilliseconds, "completed",
                "confidence {Confidence} model {Model}", azureResult.Confidence, azureResult.ModelUsed);
        }
    }
    else
    {
        LogStageInformation(currentStage, azureStopwatch.ElapsedMilliseconds, "skipped_not_configured");
    }
}
catch (TimeoutException ex)
{
    azureStopwatch.Stop();
    _logger.LogWarning(ex, "Azure Document Intelligence timed out after {DurationMs}ms, falling back to standard pipeline", azureStopwatch.ElapsedMilliseconds);
    reviewReasons.Add("azure_doc_intelligence_timeout");
}
catch (Exception ex)
{
    azureStopwatch.Stop();
    _logger.LogWarning(ex, "Azure Document Intelligence failed after {DurationMs}ms, falling back to standard pipeline", azureStopwatch.ElapsedMilliseconds);
    reviewReasons.Add("azure_doc_intelligence_failed");
}
```

**Step 3: Conditional pipeline**

Wrap existing stages 3-4 (acquire text + extract fields) in an `if (azureResult is null)` block. When Azure succeeds, use its results directly:

```csharp
DocumentExtractionResult extraction;
string documentText;

if (azureResult is not null)
{
    // Azure provided both OCR text and structured extraction
    extraction = azureResult.Extraction;
    documentText = azureResult.OcrText;
    reviewReasons.AddRange(azureResult.Warnings);
}
else
{
    // Standard pipeline: acquire text + AI extraction
    // ... existing stages 3-4 code ...
}
```

**Step 4: Update OcrMetadata serialization**

The `DocumentTextAcquisitionResult` is used in `DocumentExtractedDataSerializer`. When Azure is used, we need to pass metadata differently. Create a synthetic `DocumentTextAcquisitionResult`:

```csharp
var textResult = azureResult is not null
    ? new DocumentTextAcquisitionResult
    {
        Text = documentText,
        Source = "azure_doc_intelligence",
        Confidence = azureResult.Confidence,
        UsedVision = false,
        UsedProvider = $"AzureDocIntelligence/{azureResult.ModelUsed}",
        NativeTextLength = 0,
        VisionTextLength = documentText.Length,
        Warnings = azureResult.Warnings,
        ReviewReasons = [],
    }
    : existingTextResult; // from standard pipeline
```

**Step 5: Commit**

```
feat: integrate Azure Document Intelligence into document processing pipeline with fallback
```

---

## Task 7: Seed Provider Models

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs`

**Step 1: Add Azure models to the seeder**

In the seeding method, after existing model seeds, add:
```csharp
await SeedModelIfMissing(db, AiProvider.AzureDocIntelligence, "prebuilt-invoice", "Prebuilt Invoice", 1, "Structured extraction for invoices", ct);
await SeedModelIfMissing(db, AiProvider.AzureDocIntelligence, "prebuilt-receipt", "Prebuilt Receipt", 2, "Structured extraction for receipts", ct);
await SeedModelIfMissing(db, AiProvider.AzureDocIntelligence, "prebuilt-layout", "Prebuilt Layout", 3, "Generic OCR + layout analysis", ct);
await SeedModelIfMissing(db, AiProvider.AzureDocIntelligence, "prebuilt-read", "Prebuilt Read", 4, "OCR-optimized text extraction", ct);
```

If there's no `SeedModelIfMissing` method, create one or use the existing pattern.

**Step 2: Commit**

```
feat: seed Azure Document Intelligence provider models
```

---

## Task 8: Frontend — Add Provider to Types and UI

**Files:**
- Modify: `src/frontend/src/types/ai.ts`
- Modify: `src/frontend/src/features/admin/AiProviders.tsx`

**Step 1: Update type union and array**

```typescript
export type AiProvider = 'Anthropic' | 'OpenAI' | 'Grok' | 'Gemini' | 'ZAI' | 'Manus' | 'DeepL' | 'AzureDocIntelligence';

export const AI_PROVIDERS: AiProvider[] = ['Anthropic', 'OpenAI', 'Grok', 'Gemini', 'ZAI', 'Manus', 'DeepL', 'AzureDocIntelligence'];
```

**Step 2: Add color scheme**

In `PROVIDER_COLORS`:
```typescript
AzureDocIntelligence: 'bg-cyan-50 border-cyan-200 dark:bg-cyan-950 dark:border-cyan-800',
```

**Step 3: Commit**

```
feat: add AzureDocIntelligence to frontend provider types and UI
```

---

## Task 9: Translations

**Files:**
- Modify: `src/frontend/src/locales/en/ai.json`
- Modify: `src/frontend/src/locales/de/ai.json`
- Modify: `src/frontend/src/locales/ru/ai.json`

No provider-specific display name keys are needed — the existing UI shows `provider.toString()` which yields `"AzureDocIntelligence"`. If display names exist in the ai.json, add them. Otherwise, the provider card grid uses the enum name directly.

Check if a provider name mapping exists. If so, add:
- EN: `"AzureDocIntelligence": "Azure Document Intelligence"`
- DE: `"AzureDocIntelligence": "Azure Dokumentenintelligenz"`
- RU: `"AzureDocIntelligence": "Azure Анализ документов"`

Also add review reason translations to `src/frontend/src/locales/{lang}/documents.json`:
- `"azureDocIntelligenceFailed"` / `"azureDocIntelligenceFailedHint"`
- `"azureDocIntelligenceLowConfidence"` / `"azureDocIntelligenceLowConfidenceHint"`
- `"azureDocIntelligenceTimeout"` / `"azureDocIntelligenceTimeoutHint"`

And update the `REVIEW_REASON_MAP` in `DocumentDetail.tsx`.

**Step 1: Commit**

```
feat: add Azure Document Intelligence translations (en/de/ru)
```

---

## Task 10: Verify Build

**Step 1: Backend build**
```bash
cd src/backend && dotnet build
```

**Step 2: Frontend type check**
```bash
cd src/frontend && npx tsc --noEmit -p tsconfig.app.json
```

**Step 3: Fix any issues, commit**

```
fix: resolve build issues for Azure Document Intelligence integration
```

---

## Verification Checklist

1. Backend compiles without errors
2. Frontend TypeScript compiles without errors
3. Azure provider appears in admin UI provider grid
4. Azure models appear in model management page
5. When Azure is not configured: standard pipeline works unchanged
6. When Azure is configured: documents are processed through Azure first
7. When Azure fails: falls back to standard pipeline with review reason
8. When Azure confidence is low: falls back with review reason
9. Health check works when saving Azure API key in admin
10. OcrMetadata correctly shows `"azure_doc_intelligence"` source
