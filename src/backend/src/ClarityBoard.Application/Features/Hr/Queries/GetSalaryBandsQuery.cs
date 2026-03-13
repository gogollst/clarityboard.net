using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.salary.view")]
public record GetSalaryBandsQuery(Guid EntityId) : IRequest<SalaryBandsDto>, IEntityScoped
{
    public Guid? DepartmentId { get; init; }
}

public record SalaryBandsDto
{
    public int? MinSalaryCents { get; init; }
    public int? MaxSalaryCents { get; init; }
    public int? AvgSalaryCents { get; init; }
    public int? MedianSalaryCents { get; init; }
    public List<SalaryBandDto> Bands { get; init; } = [];
}

public record SalaryBandDto
{
    public string Label { get; init; } = string.Empty; // e.g. "0-30k", "30-50k", ...
    public int Count { get; init; }
}

public class GetSalaryBandsQueryHandler : IRequestHandler<GetSalaryBandsQuery, SalaryBandsDto>
{
    private readonly IAppDbContext _db;

    public GetSalaryBandsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SalaryBandsDto> Handle(GetSalaryBandsQuery request, CancellationToken cancellationToken)
    {
        // Use active contracts instead of salary_history for current salary data
        var sortedSalaries = await _db.Contracts
            .Where(c => c.ValidTo == null
                     && c.GrossAmountCents > 0
                     && _db.Employees.Any(e => e.Id == c.EmployeeId
                                            && e.EntityId == request.EntityId
                                            && e.Status != EmployeeStatus.Terminated
                                            && (!request.DepartmentId.HasValue || e.DepartmentId == request.DepartmentId.Value)))
            .OrderBy(c => c.GrossAmountCents)
            .Select(c => c.GrossAmountCents)
            .ToListAsync(cancellationToken);

        if (sortedSalaries.Count == 0)
        {
            return new SalaryBandsDto
            {
                Bands = BuildEmptyBands(),
            };
        }

        var salaries = sortedSalaries;

        var min  = salaries.First();
        var max  = salaries.Last();
        var avg  = (int)salaries.Average();

        // Median
        var mid    = salaries.Count / 2;
        var median = salaries.Count % 2 == 0
            ? (salaries[mid - 1] + salaries[mid]) / 2
            : salaries[mid];

        var bands = new List<SalaryBandDto>
        {
            new() { Label = "0-30k",   Count = salaries.Count(s => s <  3_000_000) },
            new() { Label = "30-50k",  Count = salaries.Count(s => s >= 3_000_000 && s < 5_000_000) },
            new() { Label = "50-70k",  Count = salaries.Count(s => s >= 5_000_000 && s < 7_000_000) },
            new() { Label = "70-100k", Count = salaries.Count(s => s >= 7_000_000 && s < 10_000_000) },
            new() { Label = "100k+",   Count = salaries.Count(s => s >= 10_000_000) },
        };

        return new SalaryBandsDto
        {
            MinSalaryCents    = min,
            MaxSalaryCents    = max,
            AvgSalaryCents    = avg,
            MedianSalaryCents = median,
            Bands             = bands,
        };
    }

    private static List<SalaryBandDto> BuildEmptyBands() =>
    [
        new() { Label = "0-30k",   Count = 0 },
        new() { Label = "30-50k",  Count = 0 },
        new() { Label = "50-70k",  Count = 0 },
        new() { Label = "70-100k", Count = 0 },
        new() { Label = "100k+",   Count = 0 },
    ];
}
