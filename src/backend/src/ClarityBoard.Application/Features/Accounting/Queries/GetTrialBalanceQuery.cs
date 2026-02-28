using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record TrialBalanceLineDto(
    string AccountNumber,
    string AccountName,
    string AccountType,
    short AccountClass,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal Balance);

public record TrialBalanceDto(
    short Year,
    short Month,
    List<TrialBalanceLineDto> Lines,
    decimal TotalDebits,
    decimal TotalCredits);

public record GetTrialBalanceQuery(Guid EntityId, short Year, short Month) : IRequest<TrialBalanceDto>, IEntityScoped;

public class GetTrialBalanceQueryHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceDto>
{
    private readonly IAppDbContext _db;

    public GetTrialBalanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<TrialBalanceDto> Handle(GetTrialBalanceQuery request, CancellationToken ct)
    {
        var periodEnd = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));

        var lines = await (
            from a in _db.Accounts.Where(a => a.EntityId == request.EntityId && a.IsActive)
            join jel in _db.JournalEntryLines on a.Id equals jel.AccountId into jelGroup
            from jel in jelGroup.DefaultIfEmpty()
            join je in _db.JournalEntries on jel.JournalEntryId equals je.Id into jeGroup
            from je in jeGroup.DefaultIfEmpty()
            where je == null || (je.EntityId == request.EntityId && je.EntryDate <= periodEnd && je.Status != "reversed")
            group new { jel, a } by new { a.Id, a.AccountNumber, a.Name, a.AccountType, a.AccountClass } into g
            select new TrialBalanceLineDto(
                g.Key.AccountNumber,
                g.Key.Name,
                g.Key.AccountType,
                g.Key.AccountClass,
                g.Sum(x => x.jel != null ? x.jel.DebitAmount : 0),
                g.Sum(x => x.jel != null ? x.jel.CreditAmount : 0),
                g.Sum(x => x.jel != null ? x.jel.DebitAmount - x.jel.CreditAmount : 0))
        ).OrderBy(l => l.AccountNumber).ToListAsync(ct);

        // Filter out zero-balance accounts
        var nonZeroLines = lines.Where(l => l.DebitTotal != 0 || l.CreditTotal != 0).ToList();

        return new TrialBalanceDto(
            request.Year,
            request.Month,
            nonZeroLines,
            nonZeroLines.Sum(l => l.DebitTotal),
            nonZeroLines.Sum(l => l.CreditTotal));
    }
}
