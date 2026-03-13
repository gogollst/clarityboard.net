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
/// Azure Document Intelligence service for OCR text extraction.
/// Uses prebuilt-layout model to extract text from documents.
/// The extracted text is then passed to an AI provider for structured field extraction.
/// </summary>
public sealed class AzureDocIntelligenceService : IAzureDocIntelligenceService
{
    private readonly IServiceProvider _sp;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<AzureDocIntelligenceService> _logger;

    private const string LayoutModel = "prebuilt-layout";

    public AzureDocIntelligenceService(
        IServiceProvider sp,
        IEncryptionService encryption,
        ILogger<AzureDocIntelligenceService> logger)
    {
        _sp = sp;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<AzureOcrResult?> ExtractTextAsync(
        Stream documentStream, Guid documentId, CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        if (config is null)
        {
            _logger.LogDebug("Azure Document Intelligence not configured, skipping");
            return null;
        }

        var client = CreateClient(config);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting Azure Document Intelligence OCR for document {DocumentId} with model {Model}",
            documentId, LayoutModel);

        try
        {
            var content = await BinaryData.FromStreamAsync(documentStream, ct);

            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                LayoutModel,
                content,
                cancellationToken: ct);

            var result = operation.Value;
            sw.Stop();

            var ocrText = result.Content ?? string.Empty;
            var pageCount = result.Pages?.Count ?? 0;
            var confidence = CalculateWordConfidence(result);

            _logger.LogInformation(
                "Azure Document Intelligence OCR completed for document {DocumentId} in {DurationMs}ms — {PageCount} pages, {TextLength} chars, confidence {Confidence}",
                documentId, sw.ElapsedMilliseconds, pageCount, ocrText.Length, confidence);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning(
                    "Azure Document Intelligence returned empty text for document {DocumentId}",
                    documentId);
                return new AzureOcrResult
                {
                    OcrText = string.Empty,
                    Confidence = 0m,
                    PageCount = pageCount,
                    Warnings = ["azure_empty_text"],
                };
            }

            return new AzureOcrResult
            {
                OcrText = ocrText,
                Confidence = confidence,
                PageCount = pageCount,
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
            _logger.LogError(ex, "Azure Document Intelligence OCR failed for document {DocumentId} after {DurationMs}ms",
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
                LayoutModel,
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
