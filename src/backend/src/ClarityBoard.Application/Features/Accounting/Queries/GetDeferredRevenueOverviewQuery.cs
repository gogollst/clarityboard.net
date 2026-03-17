using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record DeferredRevenueOverviewDto(
    decimal TotalPraBalance,
    decimal DueThisMonth,
    decimal DueNextMonth,
    int TotalPlannedEntries,
    int TotalBookedEntries);

public record GetDeferredRevenueOverviewQuery(
    Guid EntityId) : IRequest<DeferredRevenueOverviewDto>, IEntityScoped;

public class GetDeferredRevenueOverviewQueryHandler : IRequestHandler<GetDeferredRevenueOverviewQuery, DeferredRevenueOverviewDto>
{
    private readonly IAppDbContext _db;

    public GetDeferredRevenueOverviewQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DeferredRevenueOverviewDto> Handle(GetDeferredRevenueOverviewQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = thisMonth.AddMonths(1);

        var entries = _db.RevenueScheduleEntries
            .Where(e => e.EntityId == request.EntityId);

        var totalPraBalance = await entries
            .Where(e => e.Status == "planned")
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var dueThisMonth = await entries
            .Where(e => e.Status == "planned"
                && e.PeriodDate.Year == thisMonth.Year
                && e.PeriodDate.Month == thisMonth.Month)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var dueNextMonth = await entries
            .Where(e => e.Status == "planned"
                && e.PeriodDate.Year == nextMonth.Year
                && e.PeriodDate.Month == nextMonth.Month)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var totalPlannedEntries = await entries
            .CountAsync(e => e.Status == "planned", ct);

        var totalBookedEntries = await entries
            .CountAsync(e => e.Status == "booked", ct);

        return new DeferredRevenueOverviewDto(
            totalPraBalance,
            dueThisMonth,
            dueNextMonth,
            totalPlannedEntries,
            totalBookedEntries);
    }
}
