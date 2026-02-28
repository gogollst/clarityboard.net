using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record FiscalPeriodDto(
    Guid Id,
    short Year,
    short Month,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTime? ClosedAt,
    DateTime? ExportedAt);

public record GetFiscalPeriodsQuery(Guid EntityId, short? Year = null) : IRequest<List<FiscalPeriodDto>>, IEntityScoped;

public class GetFiscalPeriodsQueryHandler : IRequestHandler<GetFiscalPeriodsQuery, List<FiscalPeriodDto>>
{
    private readonly IAppDbContext _db;

    public GetFiscalPeriodsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<FiscalPeriodDto>> Handle(GetFiscalPeriodsQuery request, CancellationToken ct)
    {
        var query = _db.FiscalPeriods
            .Where(p => p.EntityId == request.EntityId);

        if (request.Year.HasValue)
            query = query.Where(p => p.Year == request.Year.Value);

        return await query
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .Select(p => new FiscalPeriodDto(
                p.Id, p.Year, p.Month, p.StartDate, p.EndDate,
                p.Status, p.ClosedAt, p.ExportedAt))
            .ToListAsync(ct);
    }
}
