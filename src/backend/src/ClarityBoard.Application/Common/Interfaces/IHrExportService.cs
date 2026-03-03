namespace ClarityBoard.Application.Common.Interfaces;

public interface IHrExportService
{
    Task<byte[]> ExportTravelExpensesCsvAsync(Guid entityId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
