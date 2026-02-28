using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Budget.DTOs;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Budget.Commands;

public record CreateBudgetCommand : IRequest<Guid>
{
    public short FiscalYear { get; init; }
    public required string Name { get; init; }
    public string BudgetType { get; init; } = "annual";
    public string? Department { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<BudgetLineRequest> Lines { get; init; } = [];
}

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    private static readonly string[] ValidBudgetTypes = ["annual", "quarterly", "rolling"];

    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FiscalYear).InclusiveBetween((short)2000, (short)2099);
        RuleFor(x => x.BudgetType)
            .Must(t => ValidBudgetTypes.Contains(t))
            .WithMessage($"BudgetType must be one of: {string.Join(", ", ValidBudgetTypes)}.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one budget line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Month).InclusiveBetween((short)1, (short)12);
            line.RuleFor(l => l.Amount).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateBudgetCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var budget = Domain.Entities.Budget.Budget.Create(
            entityId,
            request.Name,
            request.FiscalYear,
            request.BudgetType,
            _currentUser.UserId,
            request.Department,
            request.Description);

        foreach (var lineReq in request.Lines)
        {
            var line = Domain.Entities.Budget.BudgetLine.Create(
                budget.Id,
                lineReq.AccountId,
                lineReq.Month,
                lineReq.Amount,
                lineReq.CostCenter,
                lineReq.Notes);

            budget.AddLine(line);
        }

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }
}
