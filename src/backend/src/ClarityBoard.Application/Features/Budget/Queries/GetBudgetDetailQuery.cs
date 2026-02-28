using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Budget.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Budget.Queries;

public record GetBudgetDetailQuery : IRequest<BudgetDetailDto?>
{
    public Guid BudgetId { get; init; }
}

public class GetBudgetDetailQueryHandler
    : IRequestHandler<GetBudgetDetailQuery, BudgetDetailDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetBudgetDetailQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BudgetDetailDto?> Handle(
        GetBudgetDetailQuery request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(
                b => b.Id == request.BudgetId && b.EntityId == _currentUser.EntityId,
                cancellationToken);

        if (budget is null)
            return null;

        // Load account names for display
        var accountIds = budget.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var revisions = await _db.BudgetRevisions
            .Where(r => r.BudgetId == budget.Id)
            .OrderByDescending(r => r.RevisionNumber)
            .ToListAsync(cancellationToken);

        return new BudgetDetailDto
        {
            Id = budget.Id,
            EntityId = budget.EntityId,
            Name = budget.Name,
            FiscalYear = budget.FiscalYear,
            BudgetType = budget.BudgetType,
            Status = budget.Status,
            TotalAmount = budget.TotalAmount,
            Currency = budget.Currency,
            Version = budget.Version,
            Department = budget.Department,
            Description = budget.Description,
            CreatedAt = budget.CreatedAt,
            ApprovedAt = budget.ApprovedAt,
            Lines = budget.Lines.Select(l =>
            {
                accounts.TryGetValue(l.AccountId, out var acc);
                return new BudgetLineDto
                {
                    Id = l.Id,
                    AccountId = l.AccountId,
                    AccountNumber = acc?.AccountNumber,
                    AccountName = acc?.Name,
                    CostCenter = l.CostCenter,
                    Month = l.Month,
                    Amount = l.Amount,
                    ActualAmount = l.ActualAmount,
                    Variance = l.Variance,
                    VariancePct = l.VariancePct,
                    Notes = l.Notes,
                };
            }).OrderBy(l => l.Month).ThenBy(l => l.AccountNumber).ToList(),
            Revisions = revisions.Select(r => new BudgetRevisionDto
            {
                Id = r.Id,
                RevisionNumber = r.RevisionNumber,
                Reason = r.Reason,
                Changes = r.Changes,
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt,
            }).ToList(),
        };
    }
}
