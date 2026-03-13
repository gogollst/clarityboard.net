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
