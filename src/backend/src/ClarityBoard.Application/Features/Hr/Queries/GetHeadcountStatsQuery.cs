using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetHeadcountStatsQuery(Guid EntityId) : IRequest<HeadcountStatsDto>;

public record HeadcountStatsDto
{
    public int TotalActive { get; init; }
    public int TotalContractors { get; init; }
    public int TotalEmployees { get; init; }
    public List<HeadcountMonthDto> MonthlyTrend { get; init; } = [];
}

public record HeadcountMonthDto
{
    public string Month { get; init; } = string.Empty; // "2025-01"
    public int Count { get; init; }
}

public class GetHeadcountStatsQueryHandler : IRequestHandler<GetHeadcountStatsQuery, HeadcountStatsDto>
{
    private readonly IAppDbContext _db;

    public GetHeadcountStatsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<HeadcountStatsDto> Handle(GetHeadcountStatsQuery request, CancellationToken cancellationToken)
    {
        var employees = await _db.Employees
            .Where(e => e.EntityId == request.EntityId)
            .Select(e => new
            {
                e.Status,
                e.EmployeeType,
                e.HireDate,
                e.TerminationDate,
            })
            .ToListAsync(cancellationToken);

        var totalActive      = employees.Count(e => e.Status != EmployeeStatus.Terminated);
        var totalContractors = employees.Count(e => e.Status != EmployeeStatus.Terminated && e.EmployeeType == EmployeeType.Contractor);
        var totalEmployees   = employees.Count(e => e.Status != EmployeeStatus.Terminated && e.EmployeeType == EmployeeType.Employee);

        // Monthly trend: for each of the last 12 months, count active employees
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthlyTrend = new List<HeadcountMonthDto>();

        for (var i = 11; i >= 0; i--)
        {
            var month      = today.AddMonths(-i);
            var monthStart = new DateOnly(month.Year, month.Month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
            var label      = $"{month.Year:D4}-{month.Month:D2}";

            var count = employees.Count(e =>
                e.HireDate <= monthEnd &&
                (e.TerminationDate == null || e.TerminationDate >= monthStart));

            monthlyTrend.Add(new HeadcountMonthDto { Month = label, Count = count });
        }

        return new HeadcountStatsDto
        {
            TotalActive      = totalActive,
            TotalContractors = totalContractors,
            TotalEmployees   = totalEmployees,
            MonthlyTrend     = monthlyTrend,
        };
    }
}
