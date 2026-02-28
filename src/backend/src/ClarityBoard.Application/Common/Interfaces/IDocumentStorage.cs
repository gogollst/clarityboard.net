namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Abstraction for document blob storage (MinIO/S3-compatible).
/// </summary>
public interface IDocumentStorage
{
    Task<string> UploadAsync(Guid entityId, string fileName, Stream content, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(Guid entityId, string storagePath, CancellationToken ct);
    Task DeleteAsync(Guid entityId, string storagePath, CancellationToken ct);
    Task<string> GetPresignedUrlAsync(Guid entityId, string storagePath, TimeSpan expiry, CancellationToken ct);
}
