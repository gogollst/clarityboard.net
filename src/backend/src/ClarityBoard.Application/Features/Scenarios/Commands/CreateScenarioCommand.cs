using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Scenarios.DTOs;
using ClarityBoard.Domain.Entities.Scenario;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Scenarios.Commands;

public record CreateScenarioCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
    public int ProjectionMonths { get; init; } = 12;
    public IReadOnlyList<ScenarioParameterRequest> Parameters { get; init; } = [];
}

public class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    private static readonly string[] ValidTypes =
        ["best_case", "worst_case", "most_likely", "custom", "stress_test"];

    public CreateScenarioCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}.");
        RuleFor(x => x.ProjectionMonths).InclusiveBetween(1, 60);
        RuleForEach(x => x.Parameters).ChildRules(p =>
        {
            p.RuleFor(x => x.ParameterKey).NotEmpty().MaximumLength(100);
        });
    }
}

public class CreateScenarioCommandHandler : IRequestHandler<CreateScenarioCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateScenarioCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateScenarioCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var scenario = Scenario.Create(
            entityId,
            request.Name,
            request.Type,
            request.ProjectionMonths,
            _currentUser.UserId,
            request.Description);

        foreach (var p in request.Parameters)
        {
            var parameter = ScenarioParameter.Create(
                scenario.Id,
                p.ParameterKey,
                p.BaseValue,
                p.AdjustedValue,
                p.Unit,
                p.Description);

            scenario.AddParameter(parameter);
        }

        _db.Scenarios.Add(scenario);
        await _db.SaveChangesAsync(cancellationToken);

        return scenario.Id;
    }
}
