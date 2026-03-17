using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Budget.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Budget.Queries;

public record GetPlanVsActualQuery : IRequest<PlanVsActualDto?>
{
    public Guid BudgetId { get; init; }
}

public class GetPlanVsActualQueryHandler
    : IRequestHandler<GetPlanVsActualQuery, PlanVsActualDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetPlanVsActualQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PlanVsActualDto?> Handle(
        GetPlanVsActualQuery request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(
                b => b.Id == request.BudgetId && b.EntityId == _currentUser.EntityId,
                cancellationToken);

        if (budget is null)
            return null;

        // Load account names
        var accountIds = budget.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        // Calculate actual amounts from journal entry lines for each budget line's account/month
        var year = budget.FiscalYear;
        var entityId = budget.EntityId;

        var journalLines = await _db.JournalEntryLines
            .Where(jl => _db.JournalEntries
                .Any(je => je.Id == jl.JournalEntryId
                    && je.EntityId == entityId
                    && je.EntryDate.Year == year
                    && je.Status != "reversed"))
            .Where(jl => accountIds.Contains(jl.AccountId))
            .Join(
                _db.JournalEntries.Where(je => je.EntityId == entityId && je.EntryDate.Year == year),
                jl => jl.JournalEntryId,
                je => je.Id,
                (jl, je) => new { jl.AccountId, Month = (short)je.EntryDate.Month, jl.DebitAmount, jl.CreditAmount, jl.VatAmount })
            .ToListAsync(cancellationToken);

        // Aggregate actual amounts by account + month (NET: excluding VAT)
        var actuals = journalLines
            .GroupBy(x => new { x.AccountId, x.Month })
            .ToDictionary(
                g => (g.Key.AccountId, g.Key.Month),
                g => g.Sum(x =>
                    (x.DebitAmount > 0 ? x.DebitAmount - x.VatAmount : 0)
                    - (x.CreditAmount > 0 ? x.CreditAmount - x.VatAmount : 0)));

        var lines = budget.Lines.Select(l =>
        {
            accounts.TryGetValue(l.AccountId, out var acc);
            var actual = actuals.TryGetValue((l.AccountId, l.Month), out var a) ? a : 0m;
            var variance = l.Amount - actual;
            var variancePct = l.Amount != 0 ? (variance / l.Amount) * 100 : 0;

            return new PlanVsActualLineDto
            {
                AccountId = l.AccountId,
                AccountNumber = acc?.AccountNumber,
                AccountName = acc?.Name,
                Month = l.Month,
                PlannedAmount = l.Amount,
                ActualAmount = actual,
                Variance = variance,
                VariancePct = variancePct,
            };
        }).OrderBy(l => l.Month).ThenBy(l => l.AccountNumber).ToList();

        var totalPlanned = lines.Sum(l => l.PlannedAmount);
        var totalActual = lines.Sum(l => l.ActualAmount);
        var totalVariance = totalPlanned - totalActual;

        return new PlanVsActualDto
        {
            BudgetId = budget.Id,
            BudgetName = budget.Name,
            FiscalYear = budget.FiscalYear,
            Lines = lines,
            TotalPlanned = totalPlanned,
            TotalActual = totalActual,
            TotalVariance = totalVariance,
            TotalVariancePct = totalPlanned != 0 ? (totalVariance / totalPlanned) * 100 : 0,
        };
    }
}
