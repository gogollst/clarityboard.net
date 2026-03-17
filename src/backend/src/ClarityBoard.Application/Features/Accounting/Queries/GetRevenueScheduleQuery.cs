using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record RevenueScheduleEntryDto(
    Guid Id,
    Guid DocumentId,
    int? LineItemIndex,
    DateOnly PeriodDate,
    decimal Amount,
    string RevenueAccountNumber,
    string Status,
    Guid? JournalEntryId,
    DateTime? PostedAt);

public record GetRevenueScheduleQuery(
    Guid EntityId,
    Guid DocumentId) : IRequest<IReadOnlyList<RevenueScheduleEntryDto>>, IEntityScoped;

public class GetRevenueScheduleQueryHandler : IRequestHandler<GetRevenueScheduleQuery, IReadOnlyList<RevenueScheduleEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetRevenueScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RevenueScheduleEntryDto>> Handle(GetRevenueScheduleQuery request, CancellationToken ct)
    {
        return await _db.RevenueScheduleEntries
            .Where(e => e.EntityId == request.EntityId && e.DocumentId == request.DocumentId)
            .OrderBy(e => e.PeriodDate)
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
    }
}
