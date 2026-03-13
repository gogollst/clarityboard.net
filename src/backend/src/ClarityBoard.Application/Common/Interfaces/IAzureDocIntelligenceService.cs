namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Azure Document Intelligence service for OCR and structured field extraction from documents.
/// Uses prebuilt models (invoice, receipt) for structured extraction when available,
/// falls back to prebuilt-layout for pure OCR.
/// </summary>
public interface IAzureDocIntelligenceService
{
    /// <summary>
    /// Analyze a document using Azure Document Intelligence.
    /// Uses prebuilt-invoice/receipt models for structured extraction, prebuilt-layout as fallback.
    /// Returns null if the provider is not configured.
    /// </summary>
    Task<AzureDocIntelligenceResult?> AnalyzeDocumentAsync(
        Stream documentStream,
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
