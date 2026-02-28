using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Calculates working capital metrics from the general ledger.
///
/// Account ranges follow the SKR03 chart of accounts:
///   - Accounts Receivable (AR): 1400-1460
///   - Accounts Payable (AP):    1600-1620
///   - Inventory proxy:          3900
///   - Revenue:                  class 8
///   - COGS:                     class 3 expenses (debit balances)
/// </summary>
public class WorkingCapitalService : IWorkingCapitalService
{
    private readonly IAppDbContext _db;

    public WorkingCapitalService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<WorkingCapitalResult> CalculateAsync(
        Guid entityId, DateOnly asOfDate, CancellationToken ct = default)
    {
        var trailingStart = asOfDate.AddDays(-365);

        // ---- Identify relevant accounts ----
        var accounts = await _db.Accounts
            .Where(a => a.EntityId == entityId && a.IsActive)
            .Select(a => new { a.Id, a.AccountNumber, a.AccountClass })
            .ToListAsync(ct);

        var arAccountIds = accounts
            .Where(a => int.TryParse(a.AccountNumber, out var num) && num >= 1400 && num <= 1460)
            .Select(a => a.Id)
            .ToHashSet();

        var apAccountIds = accounts
            .Where(a => int.TryParse(a.AccountNumber, out var num) && num >= 1600 && num <= 1620)
            .Select(a => a.Id)
            .ToHashSet();

        var inventoryAccountIds = accounts
            .Where(a => a.AccountNumber == "3900")
            .Select(a => a.Id)
            .ToHashSet();

        var revenueAccountIds = accounts
            .Where(a => a.AccountClass == 8)
            .Select(a => a.Id)
            .ToHashSet();

        var cogsAccountIds = accounts
            .Where(a => a.AccountClass == 3)
            .Select(a => a.Id)
            .ToHashSet();

        // ---- Fetch posted journal entry lines ----
        // All posted lines up to asOfDate for balance calculations
        var balanceLines = await _db.JournalEntryLines
            .Where(l => _db.JournalEntries
                .Any(je => je.Id == l.JournalEntryId
                           && je.EntityId == entityId
                           && je.Status == "posted"
                           && je.PostingDate <= asOfDate))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        // Trailing 365-day lines for flow calculations (revenue, COGS)
        var trailingLines = await _db.JournalEntryLines
            .Where(l => _db.JournalEntries
                .Any(je => je.Id == l.JournalEntryId
                           && je.EntityId == entityId
                           && je.Status == "posted"
                           && je.PostingDate > trailingStart
                           && je.PostingDate <= asOfDate))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        // ---- Compute balances ----
        // AR = net debit balance on AR accounts
        decimal ar = balanceLines
            .Where(l => arAccountIds.Contains(l.AccountId))
            .Sum(l => l.DebitAmount - l.CreditAmount);
        if (ar < 0) ar = 0;

        // AP = net credit balance on AP accounts
        decimal ap = balanceLines
            .Where(l => apAccountIds.Contains(l.AccountId))
            .Sum(l => l.CreditAmount - l.DebitAmount);
        if (ap < 0) ap = 0;

        // Inventory proxy = net debit balance on account 3900
        decimal inventory = balanceLines
            .Where(l => inventoryAccountIds.Contains(l.AccountId))
            .Sum(l => l.DebitAmount - l.CreditAmount);
        if (inventory < 0) inventory = 0;

        // Revenue (trailing 365 days) = credit balance of class 8
        decimal revenue = trailingLines
            .Where(l => revenueAccountIds.Contains(l.AccountId))
            .Sum(l => l.CreditAmount - l.DebitAmount);

        // COGS (trailing 365 days) = debit balance of class 3
        decimal cogs = trailingLines
            .Where(l => cogsAccountIds.Contains(l.AccountId))
            .Sum(l => l.DebitAmount - l.CreditAmount);

        // ---- Calculate metrics (handle division by zero) ----
        decimal? dso = revenue != 0 ? ar / (revenue / 365m) : null;
        decimal? dio = cogs != 0 ? inventory / (cogs / 365m) : null;
        decimal? dpo = cogs != 0 ? ap / (cogs / 365m) : null;
        decimal? ccc = dso.HasValue && dio.HasValue && dpo.HasValue
            ? dso.Value + dio.Value - dpo.Value
            : null;

        // ---- Aging buckets ----
        var agingBuckets = await CalculateAgingBucketsAsync(
            entityId, asOfDate, arAccountIds, ct);

        return new WorkingCapitalResult
        {
            DSO = dso.HasValue ? Math.Round(dso.Value, 2) : null,
            DIO = dio.HasValue ? Math.Round(dio.Value, 2) : null,
            DPO = dpo.HasValue ? Math.Round(dpo.Value, 2) : null,
            CCC = ccc.HasValue ? Math.Round(ccc.Value, 2) : null,
            AgingBuckets = agingBuckets,
        };
    }

    private async Task<IReadOnlyList<AgingBucket>> CalculateAgingBucketsAsync(
        Guid entityId, DateOnly asOfDate, HashSet<Guid> arAccountIds, CancellationToken ct)
    {
        if (arAccountIds.Count == 0)
        {
            return CreateEmptyBuckets();
        }

        // Fetch AR debit lines with their posting dates
        var arLines = await _db.JournalEntryLines
            .Where(l => arAccountIds.Contains(l.AccountId)
                        && l.DebitAmount > 0
                        && _db.JournalEntries
                            .Any(je => je.Id == l.JournalEntryId
                                       && je.EntityId == entityId
                                       && je.Status == "posted"
                                       && je.PostingDate <= asOfDate))
            .Join(
                _db.JournalEntries,
                l => l.JournalEntryId,
                je => je.Id,
                (l, je) => new { l.DebitAmount, je.PostingDate })
            .ToListAsync(ct);

        var asOfDateTime = asOfDate.ToDateTime(TimeOnly.MinValue);
        var bucket0To30 = 0m;
        var bucket31To60 = 0m;
        var bucket61To90 = 0m;
        var bucket90Plus = 0m;

        foreach (var line in arLines)
        {
            var postingDateTime = line.PostingDate.ToDateTime(TimeOnly.MinValue);
            var ageDays = (int)(asOfDateTime - postingDateTime).TotalDays;

            if (ageDays <= 30)
                bucket0To30 += line.DebitAmount;
            else if (ageDays <= 60)
                bucket31To60 += line.DebitAmount;
            else if (ageDays <= 90)
                bucket61To90 += line.DebitAmount;
            else
                bucket90Plus += line.DebitAmount;
        }

        return
        [
            new AgingBucket { Label = "0-30 days", MinDays = 0, MaxDays = 30, Amount = bucket0To30 },
            new AgingBucket { Label = "31-60 days", MinDays = 31, MaxDays = 60, Amount = bucket31To60 },
            new AgingBucket { Label = "61-90 days", MinDays = 61, MaxDays = 90, Amount = bucket61To90 },
            new AgingBucket { Label = "90+ days", MinDays = 91, MaxDays = int.MaxValue, Amount = bucket90Plus },
        ];
    }

    private static IReadOnlyList<AgingBucket> CreateEmptyBuckets() =>
    [
        new AgingBucket { Label = "0-30 days", MinDays = 0, MaxDays = 30, Amount = 0 },
        new AgingBucket { Label = "31-60 days", MinDays = 31, MaxDays = 60, Amount = 0 },
        new AgingBucket { Label = "61-90 days", MinDays = 61, MaxDays = 90, Amount = 0 },
        new AgingBucket { Label = "90+ days", MinDays = 91, MaxDays = int.MaxValue, Amount = 0 },
    ];
}
