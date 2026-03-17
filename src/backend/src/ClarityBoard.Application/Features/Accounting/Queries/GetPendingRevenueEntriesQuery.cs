using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetPendingRevenueEntriesQuery(
    Guid EntityId,
    DateOnly Month,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<RevenueScheduleEntryDto>>, IEntityScoped;

public class GetPendingRevenueEntriesQueryHandler : IRequestHandler<GetPendingRevenueEntriesQuery, PagedResult<RevenueScheduleEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetPendingRevenueEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<RevenueScheduleEntryDto>> Handle(GetPendingRevenueEntriesQuery request, CancellationToken ct)
    {
        var query = _db.RevenueScheduleEntries
            .Where(e => e.EntityId == request.EntityId
                && e.Status == "planned"
                && e.PeriodDate.Year == request.Month.Year
                && e.PeriodDate.Month == request.Month.Month);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.PeriodDate)
            .ThenBy(e => e.DocumentId)
            .ThenBy(e => e.LineItemIndex)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new RevenueScheduleEntryDto(
                e.Id,
                e.DocumentId,
                e.LineItemIndex,
                e.PeriodDate,
                e.Amount,
                e.RevenueAccountNumber,
                e.Status,
                e.JournalEntryId,
                e.PostedAt))
            .ToListAsync(ct);

        return new PagedResult<RevenueScheduleEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
