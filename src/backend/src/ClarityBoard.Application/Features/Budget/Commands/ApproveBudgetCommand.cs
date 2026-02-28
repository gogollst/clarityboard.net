using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Budget.Commands;

public record ApproveBudgetCommand : IRequest
{
    public required Guid BudgetId { get; init; }
}

public class ApproveBudgetCommandValidator : AbstractValidator<ApproveBudgetCommand>
{
    public ApproveBudgetCommandValidator()
    {
        RuleFor(x => x.BudgetId).NotEmpty();
    }
}

public class ApproveBudgetCommandHandler : IRequestHandler<ApproveBudgetCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApproveBudgetCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ApproveBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(
                b => b.Id == request.BudgetId && b.EntityId == _currentUser.EntityId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Budget '{request.BudgetId}' not found.");

        if (budget.Status == "locked" || budget.Status == "archived")
            throw new InvalidOperationException(
                $"Cannot approve budget in '{budget.Status}' status.");

        budget.Approve(_currentUser.UserId);
        budget.Activate();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
