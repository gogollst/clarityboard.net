using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record CreateAccountingScenarioCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public AccountingScenarioType ScenarioType { get; init; } = AccountingScenarioType.Budget;
    public required int Year { get; init; }
}

public class CreateAccountingScenarioCommandValidator : AbstractValidator<CreateAccountingScenarioCommand>
{
    public CreateAccountingScenarioCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Year).GreaterThan(2000).LessThan(2100);
    }
}

public class CreateAccountingScenarioCommandHandler
    : IRequestHandler<CreateAccountingScenarioCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateAccountingScenarioCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAccountingScenarioCommand request, CancellationToken ct)
    {
        var scenario = AccountingScenario.Create(
            entityId: _currentUser.EntityId,
            name: request.Name,
            type: request.ScenarioType,
            year: request.Year,
            createdBy: _currentUser.UserId,
            description: request.Description);

        _db.AccountingScenarios.Add(scenario);
        await _db.SaveChangesAsync(ct);

        return scenario.Id;
    }
}
