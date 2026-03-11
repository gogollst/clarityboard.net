using ClarityBoard.Domain.Entities.Accounting;

namespace ClarityBoard.Application.Common.Interfaces;

public interface IDatevExportService
{
    Task<DatevExport> GenerateExportAsync(
        Guid entityId, Guid fiscalPeriodId,
        DatevExportType exportType, Guid generatedBy,
        CancellationToken ct);

    Task<Stream> GetExportStreamAsync(Guid exportId, CancellationToken ct);
}
