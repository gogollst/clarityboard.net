namespace ClarityBoard.Application.Common.Interfaces;

public interface IDocumentTextAcquisitionService
{
    Task<DocumentTextAcquisitionResult> AcquireTextAsync(
        Stream fileStream,
        string contentType,
        Guid documentId,
        Guid entityId,
        string? fileName = null,
        CancellationToken ct = default);
}

public record DocumentTextAcquisitionResult
{
    public string Text { get; init; } = string.Empty;
    public string Source { get; init; } = "native";
    public decimal Confidence { get; init; } = 1.0m;
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public bool UsedVision { get; init; }
    public string? UsedProvider { get; init; }
    public int NativeTextLength { get; init; }
    public int VisionTextLength { get; init; }
    public IReadOnlyList<string> ReviewReasons { get; init; } = [];
}
