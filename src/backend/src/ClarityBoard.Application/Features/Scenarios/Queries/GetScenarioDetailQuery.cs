using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Scenarios.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Scenarios.Queries;

public record GetScenarioDetailQuery : IRequest<ScenarioDetailDto?>
{
    public Guid ScenarioId { get; init; }
}

public class GetScenarioDetailQueryHandler
    : IRequestHandler<GetScenarioDetailQuery, ScenarioDetailDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetScenarioDetailQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ScenarioDetailDto?> Handle(
        GetScenarioDetailQuery request, CancellationToken cancellationToken)
    {
        var scenario = await _db.Scenarios
            .Include(s => s.Parameters)
            .FirstOrDefaultAsync(
                s => s.Id == request.ScenarioId && s.EntityId == _currentUser.EntityId,
                cancellationToken);

        if (scenario is null)
            return null;

        var results = await _db.ScenarioResults
            .Where(r => r.ScenarioId == scenario.Id)
            .OrderBy(r => r.KpiId)
            .ThenBy(r => r.Month)
            .ToListAsync(cancellationToken);

        return new ScenarioDetailDto
        {
            Id = scenario.Id,
            EntityId = scenario.EntityId,
            Name = scenario.Name,
            Description = scenario.Description,
            Type = scenario.Type,
            Status = scenario.Status,
            ProjectionMonths = scenario.ProjectionMonths,
            Version = scenario.Version,
            CreatedAt = scenario.CreatedAt,
            CalculatedAt = scenario.CalculatedAt,
            BaselineDate = scenario.BaselineDate,
            Parameters = scenario.Parameters.Select(p => new ScenarioParameterDto
            {
                Id = p.Id,
                ParameterKey = p.ParameterKey,
                BaseValue = p.BaseValue,
                AdjustedValue = p.AdjustedValue,
                Unit = p.Unit,
                Description = p.Description,
            }).ToList(),
            Results = results.Select(r => new ScenarioResultDto
            {
                Id = r.Id,
                KpiId = r.KpiId,
                Month = r.Month,
                ProjectedValue = r.ProjectedValue,
                BaselineValue = r.BaselineValue,
                DeltaValue = r.DeltaValue,
                DeltaPct = r.DeltaPct,
            }).ToList(),
        };
    }
}
