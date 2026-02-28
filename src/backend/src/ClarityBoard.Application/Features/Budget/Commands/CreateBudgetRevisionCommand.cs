using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Budget.DTOs;
using ClarityBoard.Domain.Entities.Budget;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Budget.Commands;

public record CreateBudgetRevisionCommand : IRequest<Guid>
{
    public required Guid BudgetId { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<BudgetLineRequest> RevisedLines { get; init; } = [];
}

public class CreateBudgetRevisionCommandValidator : AbstractValidator<CreateBudgetRevisionCommand>
{
    public CreateBudgetRevisionCommandValidator()
    {
        RuleFor(x => x.BudgetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RevisedLines).NotEmpty();
        RuleForEach(x => x.RevisedLines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Month).InclusiveBetween((short)1, (short)12);
        });
    }
}

public class CreateBudgetRevisionCommandHandler : IRequestHandler<CreateBudgetRevisionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateBudgetRevisionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateBudgetRevisionCommand request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(
                b => b.Id == request.BudgetId && b.EntityId == _currentUser.EntityId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Budget '{request.BudgetId}' not found.");

        // Get the next revision number
        var lastRevision = await _db.BudgetRevisions
            .Where(r => r.BudgetId == budget.Id)
            .MaxAsync(r => (int?)r.RevisionNumber, cancellationToken) ?? 0;

        // Record the changes as JSON
        var changes = request.RevisedLines.Select(line =>
        {
            var existing = budget.Lines.FirstOrDefault(
                l => l.AccountId == line.AccountId && l.Month == line.Month);
            return new
            {
                line.AccountId,
                line.Month,
                OldAmount = existing?.Amount ?? 0m,
                NewAmount = line.Amount,
            };
        }).ToList();

        var changesJson = JsonSerializer.Serialize(changes, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var revision = BudgetRevision.Create(
            budget.Id,
            lastRevision + 1,
            request.Reason,
            changesJson,
            _currentUser.UserId);

        _db.BudgetRevisions.Add(revision);

        // Apply revised lines: update existing or add new
        foreach (var lineReq in request.RevisedLines)
        {
            var existing = budget.Lines.FirstOrDefault(
                l => l.AccountId == lineReq.AccountId && l.Month == lineReq.Month);

            if (existing is null)
            {
                var newLine = BudgetLine.Create(
                    budget.Id,
                    lineReq.AccountId,
                    lineReq.Month,
                    lineReq.Amount,
                    lineReq.CostCenter,
                    lineReq.Notes);
                _db.BudgetLines.Add(newLine);
            }
            else
            {
                // Update existing line via tracked entity
                // BudgetLine doesn't expose a setter, so we remove and re-add
                _db.BudgetLines.Remove(existing);
                var updatedLine = BudgetLine.Create(
                    budget.Id,
                    lineReq.AccountId,
                    lineReq.Month,
                    lineReq.Amount,
                    lineReq.CostCenter,
                    lineReq.Notes);
                _db.BudgetLines.Add(updatedLine);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return revision.Id;
    }
}
