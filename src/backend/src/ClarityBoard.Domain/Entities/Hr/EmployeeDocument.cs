namespace ClarityBoard.Domain.Entities.Hr;

public class EmployeeDocument
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;  // AES-GCM encrypted
    public string MimeType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsConfidential { get; private set; }
    public DateTime? DeletionScheduledAt { get; private set; }

    private EmployeeDocument() { }

    public static EmployeeDocument Create(Guid employeeId, DocumentType type, string title,
        string fileName, string storagePath, string mimeType, long fileSizeBytes, Guid uploadedBy,
        bool isConfidential = false, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Storage path is required.", nameof(storagePath));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(mimeType)) throw new ArgumentException("MIME type is required.", nameof(mimeType));
        return new EmployeeDocument
        {
            Id                  = Guid.NewGuid(),
            EmployeeId          = employeeId,
            DocumentType        = type,
            Title               = title,
            FileName            = fileName,
            StoragePath         = storagePath,
            MimeType            = mimeType,
            FileSizeBytes       = fileSizeBytes,
            UploadedBy          = uploadedBy,
            UploadedAt          = DateTime.UtcNow,
            IsConfidential      = isConfidential,
            ExpiresAt           = expiresAt,
        };
    }

    public void ScheduleDeletion(DateTime scheduledAt) => DeletionScheduledAt = scheduledAt;
}
