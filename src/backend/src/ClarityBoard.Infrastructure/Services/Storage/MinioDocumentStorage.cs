using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace ClarityBoard.Infrastructure.Services.Storage;

/// <summary>
/// MinIO-based document storage. Each entity gets its own bucket: "documents-{entityId}".
/// Only PDF, JPEG, PNG, and TIFF content types are accepted.
/// </summary>
public class MinioDocumentStorage : IDocumentStorage
{
    private readonly IMinioClient _minio;
    private readonly ILogger<MinioDocumentStorage> _logger;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
    };

    public MinioDocumentStorage(IMinioClient minio, ILogger<MinioDocumentStorage> logger)
    {
        _minio = minio;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Guid entityId, string fileName, Stream content, string contentType, CancellationToken ct)
    {
        ValidateContentType(contentType);

        var bucketName = GetBucketName(entityId);
        await EnsureBucketExistsAsync(bucketName, ct);

        // Generate unique storage path
        var storagePath = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{fileName}";

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(storagePath)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(putArgs, ct);

        _logger.LogInformation("Uploaded document to MinIO: bucket={Bucket}, path={Path}", bucketName, storagePath);

        return storagePath;
    }

    public async Task<Stream> DownloadAsync(
        Guid entityId, string storagePath, CancellationToken ct)
    {
        var bucketName = GetBucketName(entityId);
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(storagePath)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _minio.GetObjectAsync(getArgs, ct);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(
        Guid entityId, string storagePath, CancellationToken ct)
    {
        var bucketName = GetBucketName(entityId);

        var removeArgs = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(storagePath);

        await _minio.RemoveObjectAsync(removeArgs, ct);

        _logger.LogInformation("Deleted document from MinIO: bucket={Bucket}, path={Path}", bucketName, storagePath);
    }

    public async Task<string> GetPresignedUrlAsync(
        Guid entityId, string storagePath, TimeSpan expiry, CancellationToken ct)
    {
        var bucketName = GetBucketName(entityId);

        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(storagePath)
            .WithExpiry((int)expiry.TotalSeconds);

        var url = await _minio.PresignedGetObjectAsync(presignedArgs);

        return url;
    }

    // ── Private ──────────────────────────────────────────────────────────

    private static string GetBucketName(Guid entityId) =>
        $"documents-{entityId.ToString().ToLowerInvariant()}";

    private static void ValidateContentType(string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException(
                $"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}",
                nameof(contentType));
        }
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minio.BucketExistsAsync(existsArgs, ct);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minio.MakeBucketAsync(makeArgs, ct);
            _logger.LogInformation("Created MinIO bucket: {Bucket}", bucketName);
        }
    }
}
