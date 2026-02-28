using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Scenarios.DTOs;
using ClarityBoard.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Scenarios.Commands;

public record RunScenarioCommand : IRequest<IReadOnlyList<ScenarioResultDto>>
{
    public required Guid ScenarioId { get; init; }
    public IReadOnlyList<ScenarioParameterRequest>? AdditionalParameters { get; init; }
}

public class RunScenarioCommandValidator : AbstractValidator<RunScenarioCommand>
{
    public RunScenarioCommandValidator()
    {
        RuleFor(x => x.ScenarioId).NotEmpty();
    }
}

public class RunScenarioCommandHandler
    : IRequestHandler<RunScenarioCommand, IReadOnlyList<ScenarioResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IScenarioEngine _scenarioEngine;

    public RunScenarioCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IScenarioEngine scenarioEngine)
    {
        _db = db;
        _currentUser = currentUser;
        _scenarioEngine = scenarioEngine;
    }

    public async Task<IReadOnlyList<ScenarioResultDto>> Handle(
        RunScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = await _db.Scenarios
            .Include(s => s.Parameters)
            .FirstOrDefaultAsync(
                s => s.Id == request.ScenarioId && s.EntityId == _currentUser.EntityId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{request.ScenarioId}' not found.");

        scenario.MarkCalculating();
        await _db.SaveChangesAsync(cancellationToken);

        var results = await _scenarioEngine.CalculateAsync(scenario, cancellationToken);

        // Persist results
        foreach (var result in results)
        {
            _db.ScenarioResults.Add(result);
        }

        scenario.MarkCompleted();
        await _db.SaveChangesAsync(cancellationToken);

        return results.Select(r => new ScenarioResultDto
        {
            Id = r.Id,
            KpiId = r.KpiId,
            Month = r.Month,
            ProjectedValue = r.ProjectedValue,
            BaselineValue = r.BaselineValue,
            DeltaValue = r.DeltaValue,
            DeltaPct = r.DeltaPct,
        }).ToList();
    }
}
