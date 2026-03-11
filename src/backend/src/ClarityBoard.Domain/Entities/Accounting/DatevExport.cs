namespace ClarityBoard.Domain.Entities.Accounting;

public enum DatevExportType { Buchungsstapel, Stammdaten }
public enum DatevExportStatus { Pending, Generating, Ready, Failed }

public class DatevExport
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public DatevExportType ExportType { get; private set; }
    public DatevExportStatus Status { get; private set; } = DatevExportStatus.Pending;
    public int? FileCount { get; private set; }
    public int? RecordCount { get; private set; }
    public string? Checksums { get; private set; }          // JSON: { fileName: sha256 }
    public string? FileStorageKeys { get; private set; }    // JSON: { fileName: minioKey }
    public string? ErrorDetails { get; private set; }       // JSON array of errors
    public DateTime CreatedAt { get; private set; }
    public Guid GeneratedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private DatevExport() { }

    public static DatevExport Create(
        Guid entityId, Guid fiscalPeriodId,
        DatevExportType exportType, Guid generatedBy)
    {
        return new DatevExport
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            FiscalPeriodId = fiscalPeriodId,
            ExportType = exportType,
            Status = DatevExportStatus.Pending,
            GeneratedBy = generatedBy,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetGenerating() => Status = DatevExportStatus.Generating;

    public void SetReady(int fileCount, int recordCount,
        string checksums, string fileStorageKeys)
    {
        Status = DatevExportStatus.Ready;
        FileCount = fileCount;
        RecordCount = recordCount;
        Checksums = checksums;
        FileStorageKeys = fileStorageKeys;
        CompletedAt = DateTime.UtcNow;
    }

    public void SetFailed(string errorDetails)
    {
        Status = DatevExportStatus.Failed;
        ErrorDetails = errorDetails;
        CompletedAt = DateTime.UtcNow;
    }
}
