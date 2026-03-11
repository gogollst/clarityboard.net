using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetAccountingScenariosQuery : IRequest<List<AccountingScenarioDto>>
{
    public required Guid EntityId { get; init; }
    public int? Year { get; init; }
}

public record AccountingScenarioDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ScenarioType { get; init; }
    public required int Year { get; init; }
    public required bool IsLocked { get; init; }
    public required bool IsBaseline { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class GetAccountingScenariosQueryHandler
    : IRequestHandler<GetAccountingScenariosQuery, List<AccountingScenarioDto>>
{
    private readonly IAppDbContext _db;

    public GetAccountingScenariosQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<AccountingScenarioDto>> Handle(
        GetAccountingScenariosQuery request, CancellationToken ct)
    {
        var query = _db.AccountingScenarios
            .Where(s => s.EntityId == request.EntityId);

        if (request.Year.HasValue)
            query = query.Where(s => s.Year == request.Year.Value);

        return await query
            .OrderByDescending(s => s.Year)
            .ThenBy(s => s.Name)
            .Select(s => new AccountingScenarioDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ScenarioType = s.ScenarioType.ToString(),
                Year = s.Year,
                IsLocked = s.IsLocked,
                IsBaseline = s.IsBaseline,
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
