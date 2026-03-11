namespace ClarityBoard.Application.Common.Interfaces;

public interface IDocumentVisionService
{
    Task<DocumentOcrResult> ExtractTextAsync(
        DocumentVisionRequest request,
        CancellationToken ct = default);
}

public record DocumentVisionRequest
{
    public Guid DocumentId { get; init; }
    public Guid EntityId { get; init; }
    public string ContentType { get; init; } = default!;
    public string? FileName { get; init; }
    public IReadOnlyList<VisionPageInput> PageImages { get; init; } = [];
}

public record VisionPageInput(
    int PageNumber,
    byte[] ImageBytes,
    string MimeType);

public record DocumentOcrResult
{
    public string FullText { get; init; } = string.Empty;
    public IReadOnlyList<DocumentOcrPageResult> Pages { get; init; } = [];
    public decimal Confidence { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public string UsedProvider { get; init; } = string.Empty;
    public bool UsedFallback { get; init; }
    public string Source { get; init; } = "vision";
    public int DurationMs { get; init; }
}

public record DocumentOcrPageResult(
    int PageNumber,
    string Text,
    decimal Confidence,
    IReadOnlyList<string> Warnings);
