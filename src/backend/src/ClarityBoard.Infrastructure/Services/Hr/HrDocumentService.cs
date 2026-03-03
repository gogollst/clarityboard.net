using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace ClarityBoard.Infrastructure.Services.Hr;

/// <summary>
/// HR-specific document storage using MinIO.
/// Documents are stored in the "hr-documents" bucket under {employeeId}/{guid}_{fileName}.
/// </summary>
public class HrDocumentService : IHrDocumentService
{
    private const string BucketName = "hr-documents";

    private readonly IMinioClient _minio;
    private readonly ILogger<HrDocumentService> _logger;

    public HrDocumentService(IMinioClient minio, ILogger<HrDocumentService> logger)
    {
        _minio  = minio;
        _logger = logger;
    }

    public async Task<string> UploadDocumentAsync(
        Guid employeeId, string fileName, string mimeType, Stream content, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var storagePath = $"{employeeId}/{Guid.NewGuid():N}_{fileName}";

        var putArgs = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(storagePath)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(mimeType);

        await _minio.PutObjectAsync(putArgs, ct);

        _logger.LogInformation(
            "Uploaded HR document to MinIO: bucket={Bucket}, path={Path}", BucketName, storagePath);

        return storagePath;
    }

    public async Task<Stream> DownloadDocumentAsync(string storagePath, CancellationToken ct = default)
    {
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(storagePath)
            .WithCallbackStream(async (stream, cancellationToken) =>
                await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false));

        await _minio.GetObjectAsync(getArgs, ct);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteDocumentAsync(string storagePath, CancellationToken ct = default)
    {
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(storagePath);

        await _minio.RemoveObjectAsync(removeArgs, ct);

        _logger.LogInformation(
            "Deleted HR document from MinIO: bucket={Bucket}, path={Path}", BucketName, storagePath);
    }

    // ── Private ──────────────────────────────────────────────────────────

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(BucketName);
        var exists = await _minio.BucketExistsAsync(existsArgs, ct);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(BucketName);
            await _minio.MakeBucketAsync(makeArgs, ct);
            _logger.LogInformation("Created MinIO bucket: {Bucket}", BucketName);
        }
    }
}
