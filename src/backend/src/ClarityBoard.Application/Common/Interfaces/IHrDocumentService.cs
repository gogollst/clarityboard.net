namespace ClarityBoard.Application.Common.Interfaces;

public interface IHrDocumentService
{
    Task<string> UploadDocumentAsync(
        Guid employeeId, string fileName, string mimeType, Stream content, CancellationToken ct = default);

    Task<Stream> DownloadDocumentAsync(string storagePath, CancellationToken ct = default);

    Task DeleteDocumentAsync(string storagePath, CancellationToken ct = default);
}
