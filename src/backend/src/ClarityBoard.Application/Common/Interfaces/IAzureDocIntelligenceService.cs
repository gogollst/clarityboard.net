namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Azure Document Intelligence service for OCR text extraction from documents.
/// This is NOT a chat-completion provider — it takes binary documents and returns OCR text.
/// The extracted text is then passed to an AI provider for structured field extraction.
/// </summary>
public interface IAzureDocIntelligenceService
{
    /// <summary>
    /// Extract OCR text from a document using Azure Document Intelligence (prebuilt-layout model).
    /// Returns the full OCR text or null if the provider is not configured.
    /// </summary>
    Task<AzureOcrResult?> ExtractTextAsync(
        Stream documentStream,
        Guid documentId,
        CancellationToken ct);

    /// <summary>
    /// Test connectivity to Azure Document Intelligence.
    /// </summary>
    Task<bool> TestConnectivityAsync(CancellationToken ct);
}

public record AzureOcrResult
{
    public required string OcrText { get; init; }
    public required decimal Confidence { get; init; }
    public required int PageCount { get; init; }
    public List<string> Warnings { get; init; } = [];
}
