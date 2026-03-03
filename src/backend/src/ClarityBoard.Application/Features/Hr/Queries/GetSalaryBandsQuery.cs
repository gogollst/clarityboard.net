using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.salary.view")]
public record GetSalaryBandsQuery(Guid EntityId) : IRequest<SalaryBandsDto>;

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
        // Get all active employee IDs for this entity (not terminated)
        var activeEmployeeIds = await _db.Employees
            .Where(e => e.EntityId == request.EntityId && e.Status != EmployeeStatus.Terminated)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (activeEmployeeIds.Count == 0)
        {
            return new SalaryBandsDto
            {
                Bands = BuildEmptyBands(),
            };
        }

        // Get current salary entries (ValidTo == null) for active employees
        var salaries = await _db.SalaryHistories
            .Where(s => activeEmployeeIds.Contains(s.EmployeeId) && s.ValidTo == null)
            .Select(s => s.GrossAmountCents)
            .ToListAsync(cancellationToken);

        if (salaries.Count == 0)
        {
            return new SalaryBandsDto
            {
                Bands = BuildEmptyBands(),
            };
        }

        salaries.Sort();

        var min  = salaries.First();
        var max  = salaries.Last();
        var avg  = (int)salaries.Average();

        // Median
        var mid    = salaries.Count / 2;
        var median = salaries.Count % 2 == 0
            ? (salaries[mid - 1] + salaries[mid]) / 2
            : salaries[mid];

        // Salary bands (in cents)
        // 0-30k EUR = 0 to 2_999_999 cents
        // 30-50k EUR = 3_000_000 to 4_999_999
        // 50-70k EUR = 5_000_000 to 6_999_999
        // 70-100k EUR = 7_000_000 to 9_999_999
        // 100k+ EUR = 10_000_000+
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
