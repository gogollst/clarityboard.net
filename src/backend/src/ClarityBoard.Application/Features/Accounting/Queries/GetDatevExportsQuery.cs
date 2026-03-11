using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetDatevExportsQuery : IRequest<List<DatevExportDto>>
{
    public required Guid EntityId { get; init; }
}

public record DatevExportDto
{
    public required Guid Id { get; init; }
    public required Guid FiscalPeriodId { get; init; }
    public required string ExportType { get; init; }
    public required string Status { get; init; }
    public int? FileCount { get; init; }
    public int? RecordCount { get; init; }
    public string? ErrorDetails { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class GetDatevExportsQueryHandler : IRequestHandler<GetDatevExportsQuery, List<DatevExportDto>>
{
    private readonly IAppDbContext _db;

    public GetDatevExportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<DatevExportDto>> Handle(
        GetDatevExportsQuery request, CancellationToken ct)
    {
        return await _db.DatevExports
            .Where(e => e.EntityId == request.EntityId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new DatevExportDto
            {
                Id = e.Id,
                FiscalPeriodId = e.FiscalPeriodId,
                ExportType = e.ExportType.ToString(),
                Status = e.Status.ToString(),
                FileCount = e.FileCount,
                RecordCount = e.RecordCount,
                ErrorDetails = e.ErrorDetails,
                CreatedAt = e.CreatedAt,
                CompletedAt = e.CompletedAt,
            })
            .ToListAsync(ct);
    }
}
