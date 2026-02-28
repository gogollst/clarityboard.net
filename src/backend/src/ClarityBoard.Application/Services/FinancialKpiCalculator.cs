using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Services;

/// <summary>
/// Calculates all 29 financial KPIs with CalculationClass = "FinancialKpiCalculator"
/// from journal entry lines using SKR03 account ranges.
///
/// Categories covered:
///   - Profitability (14): revenue, cogs, gross_margin, ebitda, ebitda_margin, ebit,
///     ebit_margin, net_income, net_margin, operating_expense_ratio, cost_income_ratio,
///     personnel_expense_ratio, material_expense_ratio, break_even_revenue
///   - Liquidity (7): current_ratio, quick_ratio, cash_ratio, operating_cash_flow,
///     free_cash_flow, cash_runway_months, working_capital
///   - Returns (4): roe, roa, roi, roce
///   - Tax (4): effective_tax_rate, kst_amount, gewst_amount, tax_shield
/// </summary>
public sealed class FinancialKpiCalculator : IKpiCalculationService
{
    private readonly IAppDbContext _db;

    public FinancialKpiCalculator(IAppDbContext db)
    {
        _db = db;
    }

    public string CalculatorName => "FinancialKpiCalculator";

    public async Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId,
        DateOnly snapshotDate,
        CancellationToken ct = default)
    {
        // Determine fiscal year start (default: January 1 of the snapshot year).
        // The LegalEntity.FiscalYearStartMonth could be used for non-calendar fiscal years.
        var entity = await _db.LegalEntities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, ct);

        var fiscalYearStartMonth = entity?.FiscalYearStartMonth ?? 1;
        var fiscalYearStart = ComputeFiscalYearStart(snapshotDate, fiscalYearStartMonth);

        // Single query: join journal entry lines with accounts and journal entries
        // to get all posted transactions for this entity in the fiscal period.
        var lineData = await _db.JournalEntryLines
            .AsNoTracking()
            .Join(
                _db.JournalEntries.AsNoTracking()
                    .Where(je => je.EntityId == entityId
                                 && je.Status == "posted"
                                 && je.PostingDate >= fiscalYearStart
                                 && je.PostingDate <= snapshotDate),
                jel => jel.JournalEntryId,
                je => je.Id,
                (jel, je) => new { jel.AccountId, jel.DebitAmount, jel.CreditAmount })
            .Join(
                _db.Accounts.AsNoTracking()
                    .Where(a => a.EntityId == entityId),
                x => x.AccountId,
                a => a.Id,
                (x, a) => new AccountLineRecord(
                    a.AccountNumber,
                    a.AccountType,
                    a.AccountClass,
                    x.DebitAmount,
                    x.CreditAmount))
            .ToListAsync(ct);

        // Aggregate balances by account number
        var accountBalances = AggregateBalances(lineData);

        // Compute all intermediate values
        var components = ComputeComponents(accountBalances, snapshotDate, fiscalYearStart);

        // Build the KPI result dictionary
        return BuildKpiResults(components);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal record for raw line data from the query
    // ─────────────────────────────────────────────────────────────────────

    private sealed record AccountLineRecord(
        string AccountNumber,
        string AccountType,
        short AccountClass,
        decimal DebitAmount,
        decimal CreditAmount);

    // ─────────────────────────────────────────────────────────────────────
    // Intermediate component record holding all computed building blocks
    // ─────────────────────────────────────────────────────────────────────

    private sealed record FinancialComponents
    {
        // Profitability components
        public decimal Revenue { get; init; }
        public decimal Cogs { get; init; }
        public decimal GrossProfit { get; init; }
        public decimal OperatingExpenses { get; init; }
        public decimal Depreciation { get; init; }
        public decimal PersonnelExpenses { get; init; }
        public decimal MaterialExpenses { get; init; }
        public decimal Ebitda { get; init; }
        public decimal Ebit { get; init; }
        public decimal TaxExpense { get; init; }
        public decimal InterestExpense { get; init; }
        public decimal NetIncome { get; init; }

        // Balance sheet components
        public decimal Cash { get; init; }
        public decimal AccountsReceivable { get; init; }
        public decimal Inventory { get; init; }
        public decimal CurrentAssets { get; init; }
        public decimal CurrentLiabilities { get; init; }
        public decimal TotalAssets { get; init; }
        public decimal TotalLiabilities { get; init; }
        public decimal Equity { get; init; }
        public decimal LongTermDebt { get; init; }
        public decimal CapEx { get; init; }

        // Tax detail
        public decimal KstAmount { get; init; }
        public decimal SoliAmount { get; init; }
        public decimal GewstAmount { get; init; }

        // Period info
        public int MonthsInPeriod { get; init; }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Balance aggregation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggregates line data into a dictionary of account number -> balance.
    /// For asset/expense accounts: balance = sum(debit) - sum(credit)  (normal debit balance)
    /// For liability/equity/revenue accounts: balance = sum(credit) - sum(debit)  (normal credit balance)
    /// The resulting balance is always the "natural" balance for the account type.
    /// </summary>
    private static Dictionary<string, AccountBalance> AggregateBalances(
        List<AccountLineRecord> lines)
    {
        var balances = new Dictionary<string, AccountBalance>();

        foreach (var line in lines)
        {
            if (!balances.TryGetValue(line.AccountNumber, out var existing))
            {
                existing = new AccountBalance(line.AccountNumber, line.AccountType, line.AccountClass, 0m, 0m);
                balances[line.AccountNumber] = existing;
            }

            balances[line.AccountNumber] = existing with
            {
                TotalDebit = existing.TotalDebit + line.DebitAmount,
                TotalCredit = existing.TotalCredit + line.CreditAmount,
            };
        }

        return balances;
    }

    private sealed record AccountBalance(
        string AccountNumber,
        string AccountType,
        short AccountClass,
        decimal TotalDebit,
        decimal TotalCredit)
    {
        /// <summary>
        /// Returns the natural balance for this account type.
        /// Asset/Expense: debit - credit (positive when debit-heavy).
        /// Liability/Equity/Revenue: credit - debit (positive when credit-heavy).
        /// </summary>
        public decimal NaturalBalance => AccountType switch
        {
            "revenue" or "liability" or "equity" => TotalCredit - TotalDebit,
            _ => TotalDebit - TotalCredit, // asset, expense
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Component computation from aggregated balances
    // ─────────────────────────────────────────────────────────────────────

    private static FinancialComponents ComputeComponents(
        Dictionary<string, AccountBalance> balances,
        DateOnly snapshotDate,
        DateOnly fiscalYearStart)
    {
        // ── Revenue: Class 8 revenue accounts (natural credit balance),
        //    minus contra-revenue accounts 8700-8730 (which are type "expense" in class 8)
        var revenue = SumNaturalBalance(balances, b =>
            b.AccountClass == 8 && b.AccountType == "revenue");

        var revenueContra = SumNaturalBalance(balances, b =>
            b.AccountClass == 8 && b.AccountType == "expense"
            && CompareAccountRange(b.AccountNumber, "8700", "8730"));

        // Also add received discounts (class 3 revenue accounts 3700-3736)
        // which reduce COGS / increase effective revenue
        var receivedDiscounts = SumNaturalBalance(balances, b =>
            b.AccountClass == 3 && b.AccountType == "revenue");

        var netRevenue = revenue - revenueContra;

        // ── COGS: Class 3 expense accounts (material inputs)
        var cogs = SumNaturalBalance(balances, b =>
            b.AccountClass == 3 && b.AccountType == "expense");

        // Reduce COGS by received discounts/boni/rabatte
        var netCogs = cogs - receivedDiscounts;

        // ── Operating Expenses: Class 4 expense accounts
        var opEx = SumNaturalBalance(balances, b =>
            b.AccountClass == 4 && b.AccountType == "expense");

        // ── Depreciation: Accounts 4820-4824 (subset of class 4)
        var depreciation = SumNaturalBalance(balances, b =>
            b.AccountClass == 4 && b.AccountType == "expense"
            && CompareAccountRange(b.AccountNumber, "4820", "4824"));

        // ── Personnel Expenses: Accounts 4100-4199
        var personnelExpenses = SumNaturalBalance(balances, b =>
            b.AccountClass == 4 && b.AccountType == "expense"
            && CompareAccountRange(b.AccountNumber, "4100", "4199"));

        // ── Material Expenses: Accounts 4000-4099
        var materialExpenses = SumNaturalBalance(balances, b =>
            b.AccountClass == 4 && b.AccountType == "expense"
            && CompareAccountRange(b.AccountNumber, "4000", "4099"));

        // ── Interest Expense: Accounts 4960-4969
        var interestExpense = SumNaturalBalance(balances, b =>
            b.AccountClass == 4 && b.AccountType == "expense"
            && CompareAccountRange(b.AccountNumber, "4960", "4969"));

        // ── Tax Expense: KSt (2200), Soli (2203), GewSt (2204)
        var kstAmount = GetBalance(balances, "2200");
        var soliAmount = GetBalance(balances, "2203");
        var gewstAmount = GetBalance(balances, "2204");
        var taxExpense = kstAmount + soliAmount + gewstAmount;

        // ── Profitability aggregates
        var grossProfit = netRevenue - netCogs;
        var ebitda = netRevenue - netCogs - opEx + depreciation; // OpEx includes depreciation, so add it back
        var ebit = netRevenue - netCogs - opEx;
        var netIncome = ebit - taxExpense - interestExpense;

        // ── Balance Sheet: Current Assets (Class 1 assets)
        var currentAssets = SumNaturalBalance(balances, b =>
            b.AccountClass == 1 && b.AccountType == "asset");

        // ── Cash: Accounts 1000, 1200-1220
        var cash = SumNaturalBalance(balances, b =>
            b.AccountClass == 1 && b.AccountType == "asset"
            && (b.AccountNumber == "1000"
                || CompareAccountRange(b.AccountNumber, "1200", "1220")));

        // ── Accounts Receivable: Accounts 1400-1460
        var accountsReceivable = SumNaturalBalance(balances, b =>
            b.AccountClass == 1 && b.AccountType == "asset"
            && CompareAccountRange(b.AccountNumber, "1400", "1460"));

        // ── Inventory: Account 3900 (Bestandsveränderungen) as proxy
        //    In SKR03 there is no explicit inventory balance sheet account in the seed;
        //    inventory changes through account 3900 represent the change, not balance.
        //    For a simplified calculation, treat inventory as 0 (conservative).
        var inventory = 0m;

        // ── Current Liabilities (Class 1 liabilities)
        var currentLiabilities = SumNaturalBalance(balances, b =>
            b.AccountClass == 1 && b.AccountType == "liability");

        // ── Total Assets: Class 0 assets + Class 1 assets + Class 2 assets
        var totalAssets = SumNaturalBalance(balances, b =>
            b.AccountType == "asset" && b.AccountClass <= 2);

        // ── Total Liabilities: Class 0 liabilities + Class 1 liabilities + Class 2 liabilities
        var totalLiabilities = SumNaturalBalance(balances, b =>
            b.AccountType == "liability" && b.AccountClass <= 2);

        // ── Equity: accounts with type = "equity"
        var equity = SumNaturalBalance(balances, b =>
            b.AccountType == "equity");

        // ── Long-Term Debt: Class 0 liabilities (provisions, pensions, etc.)
        var longTermDebt = SumNaturalBalance(balances, b =>
            b.AccountType == "liability" && b.AccountClass == 0);

        // ── CapEx proxy: debit postings to Class 0 asset accounts
        //    (additions to fixed assets during the period)
        var capEx = SumDebitOnly(balances, b =>
            b.AccountClass == 0 && b.AccountType == "asset"
            && !CompareAccountRange(b.AccountNumber, "0300", "0310")); // exclude accumulated depreciation accounts

        // ── Period length in months
        var monthsInPeriod = ((snapshotDate.Year - fiscalYearStart.Year) * 12)
                             + (snapshotDate.Month - fiscalYearStart.Month) + 1;
        if (monthsInPeriod < 1) monthsInPeriod = 1;

        return new FinancialComponents
        {
            Revenue = netRevenue,
            Cogs = netCogs,
            GrossProfit = grossProfit,
            OperatingExpenses = opEx,
            Depreciation = depreciation,
            PersonnelExpenses = personnelExpenses,
            MaterialExpenses = materialExpenses,
            Ebitda = ebitda,
            Ebit = ebit,
            TaxExpense = taxExpense,
            InterestExpense = interestExpense,
            NetIncome = netIncome,
            Cash = cash,
            AccountsReceivable = accountsReceivable,
            Inventory = inventory,
            CurrentAssets = currentAssets,
            CurrentLiabilities = currentLiabilities,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            Equity = equity,
            LongTermDebt = longTermDebt,
            CapEx = capEx,
            KstAmount = kstAmount,
            SoliAmount = soliAmount,
            GewstAmount = gewstAmount,
            MonthsInPeriod = monthsInPeriod,
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // KPI result builder
    // ─────────────────────────────────────────────────────────────────────

    private static Dictionary<string, decimal?> BuildKpiResults(FinancialComponents c)
    {
        var results = new Dictionary<string, decimal?>(33);

        // ═══════════════════════════════════════════════════════════════
        // PROFITABILITY (14)
        // ═══════════════════════════════════════════════════════════════

        // financial.revenue: Net revenue
        results["financial.revenue"] = c.Revenue;

        // financial.cogs: Cost of goods sold
        results["financial.cogs"] = c.Cogs;

        // financial.gross_margin: (Revenue - COGS) / Revenue * 100
        results["financial.gross_margin"] = SafeDividePercent(c.GrossProfit, c.Revenue);

        // financial.ebitda: Earnings before interest, taxes, depreciation, amortization
        results["financial.ebitda"] = c.Ebitda;

        // financial.ebitda_margin: EBITDA / Revenue * 100
        results["financial.ebitda_margin"] = SafeDividePercent(c.Ebitda, c.Revenue);

        // financial.ebit: Earnings before interest and taxes
        results["financial.ebit"] = c.Ebit;

        // financial.ebit_margin: EBIT / Revenue * 100
        results["financial.ebit_margin"] = SafeDividePercent(c.Ebit, c.Revenue);

        // financial.net_income: Net income after tax and interest
        results["financial.net_income"] = c.NetIncome;

        // financial.net_margin: Net Income / Revenue * 100
        results["financial.net_margin"] = SafeDividePercent(c.NetIncome, c.Revenue);

        // financial.operating_expense_ratio: OpEx / Revenue * 100
        results["financial.operating_expense_ratio"] = SafeDividePercent(c.OperatingExpenses, c.Revenue);

        // financial.cost_income_ratio: (COGS + OpEx) / Revenue * 100
        results["financial.cost_income_ratio"] = SafeDividePercent(c.Cogs + c.OperatingExpenses, c.Revenue);

        // financial.personnel_expense_ratio: Personnel Costs / Revenue * 100
        results["financial.personnel_expense_ratio"] = SafeDividePercent(c.PersonnelExpenses, c.Revenue);

        // financial.material_expense_ratio: Material Costs / Revenue * 100
        results["financial.material_expense_ratio"] = SafeDividePercent(c.MaterialExpenses, c.Revenue);

        // financial.break_even_revenue: Fixed Costs / (1 - Variable Costs / Revenue)
        // Approximation: COGS = variable costs, class 4 OpEx = fixed costs
        var variableCostRatio = c.Revenue != 0 ? c.Cogs / c.Revenue : (decimal?)null;
        if (variableCostRatio.HasValue && variableCostRatio.Value != 1m)
        {
            results["financial.break_even_revenue"] = c.OperatingExpenses / (1m - variableCostRatio.Value);
        }
        else
        {
            results["financial.break_even_revenue"] = null;
        }

        // ═══════════════════════════════════════════════════════════════
        // LIQUIDITY (7)
        // ═══════════════════════════════════════════════════════════════

        // financial.current_ratio: Current Assets / Current Liabilities
        results["financial.current_ratio"] = SafeDivide(c.CurrentAssets, c.CurrentLiabilities);

        // financial.quick_ratio: (Current Assets - Inventory) / Current Liabilities
        results["financial.quick_ratio"] = SafeDivide(c.CurrentAssets - c.Inventory, c.CurrentLiabilities);

        // financial.cash_ratio: Cash / Current Liabilities
        results["financial.cash_ratio"] = SafeDivide(c.Cash, c.CurrentLiabilities);

        // financial.operating_cash_flow: Net Income + Depreciation + change in working capital (simplified)
        // Simplified OCF: Net Income + Depreciation
        // (Full working capital change tracking requires prior-period balance comparison,
        //  which is beyond the scope of single-period calculation.)
        var ocf = c.NetIncome + c.Depreciation;
        results["financial.operating_cash_flow"] = ocf;

        // financial.free_cash_flow: OCF - CapEx
        results["financial.free_cash_flow"] = ocf - c.CapEx;

        // financial.cash_runway_months: Cash / Monthly Burn Rate
        // Monthly burn rate = total expenses (COGS + OpEx) / months in period
        var monthlyBurn = c.MonthsInPeriod > 0
            ? (c.Cogs + c.OperatingExpenses) / c.MonthsInPeriod
            : (decimal?)null;
        results["financial.cash_runway_months"] = monthlyBurn.HasValue && monthlyBurn.Value != 0
            ? Math.Round(c.Cash / monthlyBurn.Value, 2)
            : null;

        // financial.working_capital: Current Assets - Current Liabilities
        results["financial.working_capital"] = c.CurrentAssets - c.CurrentLiabilities;

        // ═══════════════════════════════════════════════════════════════
        // RETURNS (4)
        // ═══════════════════════════════════════════════════════════════

        // financial.roe: Net Income / Equity * 100
        results["financial.roe"] = SafeDividePercent(c.NetIncome, c.Equity);

        // financial.roa: Net Income / Total Assets * 100
        results["financial.roa"] = SafeDividePercent(c.NetIncome, c.TotalAssets);

        // financial.roi: (Net Income + Interest) / (Equity + Long-Term Debt) * 100
        var investedCapital = c.Equity + c.LongTermDebt;
        results["financial.roi"] = SafeDividePercent(c.NetIncome + c.InterestExpense, investedCapital);

        // financial.roce: EBIT / (Total Assets - Current Liabilities) * 100
        var capitalEmployed = c.TotalAssets - c.CurrentLiabilities;
        results["financial.roce"] = SafeDividePercent(c.Ebit, capitalEmployed);

        // ═══════════════════════════════════════════════════════════════
        // TAX (4)
        // ═══════════════════════════════════════════════════════════════

        // financial.effective_tax_rate: Tax Expense / Pre-Tax Income * 100
        var preTaxIncome = c.Ebit - c.InterestExpense; // EBT
        results["financial.effective_tax_rate"] = SafeDividePercent(c.TaxExpense, preTaxIncome);

        // financial.kst_amount: KSt (2200) + Soli (2203)
        results["financial.kst_amount"] = c.KstAmount + c.SoliAmount;

        // financial.gewst_amount: GewSt (2204)
        results["financial.gewst_amount"] = c.GewstAmount;

        // financial.tax_shield: Interest Expense * Effective Tax Rate / 100
        var effectiveTaxRate = results["financial.effective_tax_rate"];
        results["financial.tax_shield"] = effectiveTaxRate.HasValue
            ? Math.Round(c.InterestExpense * effectiveTaxRate.Value / 100m, 2)
            : null;

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Divides numerator by denominator and multiplies by 100 for percentage.
    /// Returns null if denominator is zero. Rounds to 2 decimal places.
    /// </summary>
    private static decimal? SafeDividePercent(decimal numerator, decimal denominator)
    {
        if (denominator == 0m) return null;
        return Math.Round(numerator / denominator * 100m, 2);
    }

    /// <summary>
    /// Divides numerator by denominator for ratio values.
    /// Returns null if denominator is zero. Rounds to 2 decimal places.
    /// </summary>
    private static decimal? SafeDivide(decimal numerator, decimal denominator)
    {
        if (denominator == 0m) return null;
        return Math.Round(numerator / denominator, 2);
    }

    /// <summary>
    /// Sums the natural balance of all accounts matching the predicate.
    /// </summary>
    private static decimal SumNaturalBalance(
        Dictionary<string, AccountBalance> balances,
        Func<AccountBalance, bool> predicate)
    {
        return balances.Values.Where(predicate).Sum(b => b.NaturalBalance);
    }

    /// <summary>
    /// Sums only the debit amounts for accounts matching the predicate.
    /// Used for CapEx proxy (debit to fixed asset accounts = additions).
    /// </summary>
    private static decimal SumDebitOnly(
        Dictionary<string, AccountBalance> balances,
        Func<AccountBalance, bool> predicate)
    {
        return balances.Values.Where(predicate).Sum(b => b.TotalDebit);
    }

    /// <summary>
    /// Gets the natural balance for a specific account number, or 0 if not found.
    /// </summary>
    private static decimal GetBalance(
        Dictionary<string, AccountBalance> balances,
        string accountNumber)
    {
        return balances.TryGetValue(accountNumber, out var balance) ? balance.NaturalBalance : 0m;
    }

    /// <summary>
    /// Compares an account number against a range (inclusive) using string comparison
    /// padded to 4 digits. Account numbers in SKR03 are 4-digit strings.
    /// </summary>
    private static bool CompareAccountRange(string accountNumber, string from, string to)
    {
        var padded = accountNumber.PadLeft(4, '0');
        var paddedFrom = from.PadLeft(4, '0');
        var paddedTo = to.PadLeft(4, '0');
        return string.Compare(padded, paddedFrom, StringComparison.Ordinal) >= 0
               && string.Compare(padded, paddedTo, StringComparison.Ordinal) <= 0;
    }

    /// <summary>
    /// Computes the fiscal year start date given a snapshot date and the
    /// month in which the fiscal year begins.
    /// </summary>
    private static DateOnly ComputeFiscalYearStart(DateOnly snapshotDate, int fiscalYearStartMonth)
    {
        var year = snapshotDate.Year;

        // If the snapshot month is before the fiscal year start month,
        // the fiscal year started in the prior calendar year.
        if (snapshotDate.Month < fiscalYearStartMonth)
            year--;

        return new DateOnly(year, fiscalYearStartMonth, 1);
    }
}
