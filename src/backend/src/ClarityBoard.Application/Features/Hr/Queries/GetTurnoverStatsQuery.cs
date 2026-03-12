using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetTurnoverStatsQuery(Guid EntityId) : IRequest<TurnoverStatsDto>, IEntityScoped
{
    public Guid? DepartmentId { get; init; }
}

public record TurnoverStatsDto
{
    public List<TurnoverMonthDto> MonthlyTurnover { get; init; } = [];
    public decimal AverageTurnoverRate { get; init; } // percentage (0-100)
}

public record TurnoverMonthDto
{
    public string Month { get; init; } = string.Empty; // "2025-01"
    public int Terminations { get; init; }
    public int NewHires { get; init; }
}

public class GetTurnoverStatsQueryHandler : IRequestHandler<GetTurnoverStatsQuery, TurnoverStatsDto>
{
    private readonly IAppDbContext _db;

    public GetTurnoverStatsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<TurnoverStatsDto> Handle(GetTurnoverStatsQuery request, CancellationToken cancellationToken)
    {
        var employees = await _db.Employees
            .Where(e => e.EntityId == request.EntityId)
            .Select(e => new
            {
                e.HireDate,
                e.TerminationDate,
                e.Status,
            })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthlyTurnover = new List<TurnoverMonthDto>();

        for (var i = 11; i >= 0; i--)
        {
            var month      = today.AddMonths(-i);
            var monthStart = new DateOnly(month.Year, month.Month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
            var label      = $"{month.Year:D4}-{month.Month:D2}";

            var terminations = employees.Count(e =>
                e.TerminationDate.HasValue &&
                e.TerminationDate.Value >= monthStart &&
                e.TerminationDate.Value <= monthEnd);

            var newHires = employees.Count(e =>
                e.HireDate >= monthStart &&
                e.HireDate <= monthEnd);

            monthlyTurnover.Add(new TurnoverMonthDto
            {
                Month        = label,
                Terminations = terminations,
                NewHires     = newHires,
            });
        }

        // Average turnover rate = avg monthly terminations / avg monthly headcount * 100
        var avgTerminations = monthlyTurnover.Average(m => (double)m.Terminations);

        // Compute average monthly headcount over the 12 months
        double avgMonthlyHeadcount = 0;
        for (int i = 0; i < 12; i++)
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-11 + i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var count = employees.Count(e =>
                e.HireDate <= monthEnd &&
                (e.TerminationDate == null || e.TerminationDate >= monthStart));
            avgMonthlyHeadcount += count;
        }
        avgMonthlyHeadcount /= 12.0;

        var averageTurnoverRate = avgMonthlyHeadcount > 0
            ? (decimal)(avgTerminations / avgMonthlyHeadcount * 100.0)
            : 0m;

        return new TurnoverStatsDto
        {
            MonthlyTurnover    = monthlyTurnover,
            AverageTurnoverRate = Math.Round(averageTurnoverRate, 2),
        };
    }
}
