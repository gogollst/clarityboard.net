using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Calculates general business KPIs that combine data from multiple domains.
/// Uses a mix of accounting journal data (for expense/revenue-based metrics)
/// and existing KPI snapshots (for cross-domain derived metrics).
///
/// Account ranges follow the SKR03 chart of accounts:
///   - Revenue:            class 8 (credit balances)
///   - COGS/Materials:     class 3 (debit balances)
///   - Operating expenses: classes 4-7 (debit balances)
///   - Debt accounts:      0600-0699, 1700-1799 (long-term + short-term liabilities)
///   - Equity:             0800-0899
/// </summary>
public class GeneralKpiCalculator : IKpiCalculationService
{
    private readonly IAppDbContext _db;

    public GeneralKpiCalculator(IAppDbContext db)
    {
        _db = db;
    }

    public string CalculatorName => "GeneralKpiCalculator";

    public async Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId, DateOnly snapshotDate, CancellationToken ct = default)
    {
        var results = new Dictionary<string, decimal?>();

        // Load accounting data for journal-based calculations
        var accountingData = await LoadAccountingData(entityId, snapshotDate, ct);

        // general.burn_rate: Total Monthly Expenses (classes 3+4+5+6+7)
        results["general.burn_rate"] = CalculateBurnRate(accountingData);

        // general.revenue_growth_yoy: (Current Revenue - YoY Revenue) / YoY Revenue * 100
        results["general.revenue_growth_yoy"] = CalculateRevenueGrowthYoy(accountingData);

        // general.opex_growth_yoy: (Current OpEx - YoY OpEx) / YoY OpEx * 100
        results["general.opex_growth_yoy"] = CalculateOpexGrowthYoy(accountingData);

        // general.debt_to_equity: Total Debt / Shareholders Equity
        results["general.debt_to_equity"] = CalculateDebtToEquity(accountingData);

        // general.rule_of_40: Revenue Growth % + Profit Margin %
        // Derived from financial KPIs where possible, otherwise from accounting data
        results["general.rule_of_40"] = await CalculateRuleOf40(
            entityId, snapshotDate, results["general.revenue_growth_yoy"], ct);

        return results;
    }

    /// <summary>
    /// Calculates monthly burn rate as the average monthly total expenses
    /// over the trailing 12 months. Includes account classes 3 through 7
    /// (COGS, personnel, depreciation, other operating expenses).
    /// </summary>
    private static decimal? CalculateBurnRate(AccountingData data)
    {
        if (data.CurrentExpenses == 0m)
            return null;

        // Current expenses are trailing 12 months; divide by months of data
        var months = data.MonthsCovered > 0 ? data.MonthsCovered : 12;
        return Math.Round(data.CurrentExpenses / months, 2);
    }

    /// <summary>
    /// Calculates year-over-year revenue growth rate. Compares revenue for
    /// the trailing 12 months against the prior 12-month period.
    /// </summary>
    private static decimal? CalculateRevenueGrowthYoy(AccountingData data)
    {
        if (!data.PriorYearRevenue.HasValue || data.PriorYearRevenue.Value == 0m)
            return null;

        return Math.Round(
            (data.CurrentRevenue - data.PriorYearRevenue.Value)
            / Math.Abs(data.PriorYearRevenue.Value) * 100m, 2);
    }

    /// <summary>
    /// Calculates year-over-year operating expense growth rate. Compares
    /// OpEx for the trailing 12 months against the prior 12-month period.
    /// </summary>
    private static decimal? CalculateOpexGrowthYoy(AccountingData data)
    {
        if (!data.PriorYearExpenses.HasValue || data.PriorYearExpenses.Value == 0m)
            return null;

        return Math.Round(
            (data.CurrentExpenses - data.PriorYearExpenses.Value)
            / Math.Abs(data.PriorYearExpenses.Value) * 100m, 2);
    }

    /// <summary>
    /// Calculates debt-to-equity ratio from balance sheet data.
    /// Total debt = long-term liabilities (0600-0699) + short-term liabilities (1700-1799).
    /// Equity = accounts 0800-0899.
    /// </summary>
    private static decimal? CalculateDebtToEquity(AccountingData data)
    {
        if (data.Equity == 0m)
            return null;

        return Math.Round(data.TotalDebt / Math.Abs(data.Equity), 2);
    }

    /// <summary>
    /// Calculates the Rule of 40: Revenue Growth % + Profit Margin %.
    /// First attempts to use already-calculated YoY revenue growth.
    /// Profit margin is sourced from the financial domain's net_margin snapshot.
    /// </summary>
    private async Task<decimal?> CalculateRuleOf40(
        Guid entityId, DateOnly snapshotDate, decimal? revenueGrowthYoy, CancellationToken ct)
    {
        var growthPct = revenueGrowthYoy;
        if (!growthPct.HasValue)
        {
            // Try to get from existing financial snapshot
            growthPct = await GetLatestSnapshotValue(entityId, "general.revenue_growth_yoy", snapshotDate, ct);
        }

        // Get profit margin from financial domain
        var profitMargin = await GetLatestSnapshotValue(entityId, "financial.net_margin", snapshotDate, ct);

        if (!growthPct.HasValue || !profitMargin.HasValue)
            return null;

        return Math.Round(growthPct.Value + profitMargin.Value, 2);
    }

    /// <summary>
    /// Loads all accounting data needed by general KPIs in a single pass:
    /// current period revenue/expenses, prior year revenue/expenses,
    /// debt balances, and equity.
    /// </summary>
    private async Task<AccountingData> LoadAccountingData(
        Guid entityId, DateOnly snapshotDate, CancellationToken ct)
    {
        // Define time windows
        var currentYearStart = snapshotDate.AddDays(-365);
        var priorYearStart = currentYearStart.AddDays(-365);

        // ---- Identify relevant accounts ----
        var accounts = await _db.Accounts
            .Where(a => a.EntityId == entityId && a.IsActive)
            .Select(a => new { a.Id, a.AccountNumber, a.AccountClass })
            .ToListAsync(ct);

        var revenueAccountIds = accounts
            .Where(a => a.AccountClass == 8)
            .Select(a => a.Id)
            .ToHashSet();

        // Expense accounts: classes 3 (COGS), 4 (personnel), 5 (depreciation),
        // 6 (other operating), 7 (extraordinary)
        var expenseAccountIds = accounts
            .Where(a => a.AccountClass >= 3 && a.AccountClass <= 7)
            .Select(a => a.Id)
            .ToHashSet();

        // Debt accounts: long-term (0600-0699) + short-term liabilities (1700-1799)
        var debtAccountIds = accounts
            .Where(a => int.TryParse(a.AccountNumber, out var num)
                        && ((num >= 600 && num <= 699) || (num >= 1700 && num <= 1799)))
            .Select(a => a.Id)
            .ToHashSet();

        // Equity accounts: 0800-0899
        var equityAccountIds = accounts
            .Where(a => int.TryParse(a.AccountNumber, out var num)
                        && num >= 800 && num <= 899)
            .Select(a => a.Id)
            .ToHashSet();

        // ---- Current year trailing lines (snapshotDate - 365 to snapshotDate) ----
        var currentLines = await _db.JournalEntryLines
            .Where(l => _db.JournalEntries
                .Any(je => je.Id == l.JournalEntryId
                           && je.EntityId == entityId
                           && je.Status == "posted"
                           && je.PostingDate > currentYearStart
                           && je.PostingDate <= snapshotDate))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        // ---- Prior year trailing lines (priorYearStart to currentYearStart) ----
        var priorLines = await _db.JournalEntryLines
            .Where(l => _db.JournalEntries
                .Any(je => je.Id == l.JournalEntryId
                           && je.EntityId == entityId
                           && je.Status == "posted"
                           && je.PostingDate > priorYearStart
                           && je.PostingDate <= currentYearStart))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        // ---- Balance sheet lines (all posted lines up to snapshotDate) ----
        var balanceLines = await _db.JournalEntryLines
            .Where(l => _db.JournalEntries
                .Any(je => je.Id == l.JournalEntryId
                           && je.EntityId == entityId
                           && je.Status == "posted"
                           && je.PostingDate <= snapshotDate))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        // ---- Compute aggregates ----
        // Revenue = credit balance on class 8 accounts
        decimal currentRevenue = currentLines
            .Where(l => revenueAccountIds.Contains(l.AccountId))
            .Sum(l => l.CreditAmount - l.DebitAmount);

        // Expenses = debit balance on classes 3-7
        decimal currentExpenses = currentLines
            .Where(l => expenseAccountIds.Contains(l.AccountId))
            .Sum(l => l.DebitAmount - l.CreditAmount);
        if (currentExpenses < 0) currentExpenses = 0;

        decimal? priorYearRevenue = null;
        decimal? priorYearExpenses = null;

        if (priorLines.Count > 0)
        {
            priorYearRevenue = priorLines
                .Where(l => revenueAccountIds.Contains(l.AccountId))
                .Sum(l => l.CreditAmount - l.DebitAmount);

            var priorExp = priorLines
                .Where(l => expenseAccountIds.Contains(l.AccountId))
                .Sum(l => l.DebitAmount - l.CreditAmount);
            priorYearExpenses = priorExp < 0 ? 0 : priorExp;
        }

        // Debt = credit balance on debt accounts (liabilities are credit-normal)
        decimal totalDebt = balanceLines
            .Where(l => debtAccountIds.Contains(l.AccountId))
            .Sum(l => l.CreditAmount - l.DebitAmount);
        if (totalDebt < 0) totalDebt = 0;

        // Equity = credit balance on equity accounts
        decimal equity = balanceLines
            .Where(l => equityAccountIds.Contains(l.AccountId))
            .Sum(l => l.CreditAmount - l.DebitAmount);

        // Calculate months covered for burn rate averaging
        var firstPostingDate = currentLines.Count > 0
            ? await _db.JournalEntries
                .Where(je => je.EntityId == entityId
                             && je.Status == "posted"
                             && je.PostingDate > currentYearStart
                             && je.PostingDate <= snapshotDate)
                .MinAsync(je => je.PostingDate, ct)
            : snapshotDate;

        var daysCovered = snapshotDate.DayNumber - firstPostingDate.DayNumber;
        var monthsCovered = Math.Max(1, (int)Math.Ceiling(daysCovered / 30.44));

        return new AccountingData
        {
            CurrentRevenue = currentRevenue,
            CurrentExpenses = currentExpenses,
            PriorYearRevenue = priorYearRevenue,
            PriorYearExpenses = priorYearExpenses,
            TotalDebt = totalDebt,
            Equity = equity,
            MonthsCovered = monthsCovered,
        };
    }

    /// <summary>
    /// Retrieves the most recent KpiSnapshot value for a given KPI on or before
    /// the specified date. Returns null if no snapshot exists.
    /// </summary>
    private async Task<decimal?> GetLatestSnapshotValue(
        Guid entityId, string kpiId, DateOnly asOfDate, CancellationToken ct)
    {
        return await _db.KpiSnapshots
            .Where(s => s.EntityId == entityId
                        && s.KpiId == kpiId
                        && s.SnapshotDate <= asOfDate)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Internal container for aggregated accounting data used across
    /// multiple KPI calculations.
    /// </summary>
    private sealed class AccountingData
    {
        public decimal CurrentRevenue { get; init; }
        public decimal CurrentExpenses { get; init; }
        public decimal? PriorYearRevenue { get; init; }
        public decimal? PriorYearExpenses { get; init; }
        public decimal TotalDebt { get; init; }
        public decimal Equity { get; init; }
        public int MonthsCovered { get; init; }
    }
}
