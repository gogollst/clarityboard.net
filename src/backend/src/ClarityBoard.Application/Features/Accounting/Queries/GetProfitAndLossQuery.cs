using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record PnlLineItem(string Label, decimal Amount, decimal? PriorAmount = null);

public record ProfitAndLossDto(
    short Year,
    short Month,
    short? CompareYear,
    short? CompareMonth,
    List<PnlSection> Sections,
    decimal NetIncome,
    decimal? PriorNetIncome);

public record PnlSection(string Name, List<PnlLineItem> Items, decimal Subtotal, decimal? PriorSubtotal = null);

public record GetProfitAndLossQuery(
    Guid EntityId,
    short Year,
    short Month,
    short? CompareYear = null,
    short? CompareMonth = null,
    Guid? DepartmentId = null) : IRequest<ProfitAndLossDto>, IEntityScoped;

public class GetProfitAndLossQueryHandler : IRequestHandler<GetProfitAndLossQuery, ProfitAndLossDto>
{
    private readonly IAppDbContext _db;

    public GetProfitAndLossQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProfitAndLossDto> Handle(GetProfitAndLossQuery request, CancellationToken ct)
    {
        var balances = await GetAccountBalancesAsync(request.EntityId, request.Year, request.Month, request.DepartmentId, ct);
        Dictionary<string, decimal>? priorBalances = null;

        if (request.CompareYear.HasValue && request.CompareMonth.HasValue)
            priorBalances = await GetAccountBalancesAsync(request.EntityId, request.CompareYear.Value, request.CompareMonth.Value, request.DepartmentId, ct);

        var sections = new List<PnlSection>();

        // ── Revenue sections (Ertraege) ──────────────────────────────────
        // Revenue accounts (class 8) have a natural CREDIT balance → negative in debit-credit.
        // We negate to show positive values for display; these ADD to net income.

        // 1. Revenue (Umsatzerloese) - accounts 8000-8599
        var revenue = -SumRange(balances, "8000", "8599"); // negate credit balance → positive
        var priorRevenue = priorBalances != null ? -SumRange(priorBalances, "8000", "8599") : (decimal?)null;
        sections.Add(new PnlSection("Umsatzerloese", [
            new PnlLineItem("Umsatzerloese", revenue, priorRevenue)
        ], revenue, priorRevenue));

        // 2. Revenue deductions (Erloesschmaelerungen) - accounts 8700-8739
        // These are debit-balance accounts that REDUCE revenue
        var deductions = SumRange(balances, "8700", "8739");
        var priorDeductions = priorBalances != null ? SumRange(priorBalances, "8700", "8739") : (decimal?)null;

        // 3. Inventory changes / own work capitalised - accounts 7000-7099
        var inventoryChanges = -SumRange(balances, "7000", "7099");
        var priorInventory = priorBalances != null ? -SumRange(priorBalances, "7000", "7099") : (decimal?)null;
        sections.Add(new PnlSection("Bestandsveraenderungen / Eigenleistungen", [
            new PnlLineItem("Bestandsveraenderungen", inventoryChanges, priorInventory)
        ], inventoryChanges, priorInventory));

        // 4. Other operating income - accounts 8800-8999
        var otherIncome = -SumRange(balances, "8800", "8999");
        var priorOtherIncome = priorBalances != null ? -SumRange(priorBalances, "8800", "8999") : (decimal?)null;
        sections.Add(new PnlSection("Sonstige betriebliche Ertraege", [
            new PnlLineItem("Sonstige betriebliche Ertraege", otherIncome, priorOtherIncome)
        ], otherIncome, priorOtherIncome));

        // ── Expense sections (Aufwendungen) ──────────────────────────────
        // Expense accounts have a natural DEBIT balance → positive in debit-credit.
        // Displayed as positive values; these SUBTRACT from net income.

        // 5. Material expenses - Class 3
        var materialExpense = SumRange(balances, "3000", "3899");
        var priorMaterial = priorBalances != null ? SumRange(priorBalances, "3000", "3899") : (decimal?)null;
        sections.Add(new PnlSection("Materialaufwand", [
            new PnlLineItem("Aufwendungen fuer Roh-, Hilfs- und Betriebsstoffe", materialExpense, priorMaterial)
        ], materialExpense, priorMaterial));

        // 6. Personnel costs - accounts 4100-4199
        var personnelCosts = SumRange(balances, "4100", "4199");
        var priorPersonnel = priorBalances != null ? SumRange(priorBalances, "4100", "4199") : (decimal?)null;
        sections.Add(new PnlSection("Personalaufwand", [
            new PnlLineItem("Loehne und Gehaelter", SumRange(balances, "4100", "4129"), priorBalances != null ? SumRange(priorBalances, "4100", "4129") : null),
            new PnlLineItem("Soziale Abgaben und Altersversorgung", SumRange(balances, "4130", "4199"), priorBalances != null ? SumRange(priorBalances, "4130", "4199") : null)
        ], personnelCosts, priorPersonnel));

        // 7. Depreciation - accounts 4820-4849
        var depreciation = SumRange(balances, "4820", "4849");
        var priorDepr = priorBalances != null ? SumRange(priorBalances, "4820", "4849") : (decimal?)null;
        sections.Add(new PnlSection("Abschreibungen", [
            new PnlLineItem("Abschreibungen auf Sachanlagen und immaterielle VG", depreciation, priorDepr)
        ], depreciation, priorDepr));

        // 8. Other operating expenses - remaining Class 4 (4200-4819, 4850-4999)
        var otherOpex = SumRange(balances, "4200", "4819") + SumRange(balances, "4850", "4999");
        var priorOtherOpex = priorBalances != null
            ? SumRange(priorBalances, "4200", "4819") + SumRange(priorBalances, "4850", "4999")
            : (decimal?)null;
        sections.Add(new PnlSection("Sonstige betriebliche Aufwendungen", [
            new PnlLineItem("Sonstige betriebliche Aufwendungen", otherOpex, priorOtherOpex)
        ], otherOpex, priorOtherOpex));

        // 9. Taxes - Class 2 (KSt, GewSt, Soli)
        var taxes = SumRange(balances, "2200", "2299");
        var priorTaxes = priorBalances != null ? SumRange(priorBalances, "2200", "2299") : (decimal?)null;
        sections.Add(new PnlSection("Steuern vom Einkommen und Ertrag", [
            new PnlLineItem("Koerperschaftsteuer", SumRange(balances, "2200", "2202"), priorBalances != null ? SumRange(priorBalances, "2200", "2202") : null),
            new PnlLineItem("Solidaritaetszuschlag", SumRange(balances, "2203", "2203"), priorBalances != null ? SumRange(priorBalances, "2203", "2203") : null),
            new PnlLineItem("Gewerbesteuer", SumRange(balances, "2204", "2209"), priorBalances != null ? SumRange(priorBalances, "2204", "2209") : null)
        ], taxes, priorTaxes));

        // ── Net Income (HGB §275) ────────────────────────────────────────
        // Revenue sections are positive (income), expense sections are positive (costs).
        // Net Income = Total Revenue - Total Expenses
        var totalRevenue = revenue + inventoryChanges + otherIncome - deductions;
        var totalExpenses = materialExpense + personnelCosts + depreciation + otherOpex + taxes;
        var netIncome = totalRevenue - totalExpenses;

        decimal? priorNetIncome = null;
        if (priorBalances != null)
        {
            var priorTotalRevenue = (priorRevenue ?? 0) + (priorInventory ?? 0) + (priorOtherIncome ?? 0) - (priorDeductions ?? 0);
            var priorTotalExpenses = (priorMaterial ?? 0) + (priorPersonnel ?? 0) + (priorDepr ?? 0) + (priorOtherOpex ?? 0) + (priorTaxes ?? 0);
            priorNetIncome = priorTotalRevenue - priorTotalExpenses;
        }

        return new ProfitAndLossDto(
            request.Year, request.Month,
            request.CompareYear, request.CompareMonth,
            sections,
            netIncome,
            priorNetIncome);
    }

    private async Task<Dictionary<string, decimal>> GetAccountBalancesAsync(
        Guid entityId, short year, short month, Guid? departmentId, CancellationToken ct)
    {
        var periodStart = new DateOnly(year, 1, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // If department filter is set, resolve matching cost center IDs first
        HashSet<Guid>? costCenterIds = null;
        if (departmentId.HasValue)
        {
            costCenterIds = (await _db.CostCenters
                .Where(cc => cc.HrDepartmentId == departmentId.Value)
                .Select(cc => cc.Id)
                .ToListAsync(ct)).ToHashSet();
        }

        var query = from a in _db.Accounts.Where(a => a.EntityId == entityId)
            join jel in _db.JournalEntryLines on a.Id equals jel.AccountId
            join je in _db.JournalEntries on jel.JournalEntryId equals je.Id
            where je.EntityId == entityId
                && je.EntryDate >= periodStart && je.EntryDate <= periodEnd
                && je.Status != "reversed"
            select new { a.AccountNumber, jel };

        if (costCenterIds != null)
            query = query.Where(x => x.jel.CostCenterId != null && costCenterIds.Contains(x.jel.CostCenterId.Value));

        // Use NET amounts: subtract VatAmount from each line's effective side.
        // This ensures P&L always shows amounts excluding VAT (Umsatzsteuer),
        // regardless of whether the journal entry was booked gross or net+VAT-split.
        return await query
            .GroupBy(x => x.AccountNumber)
            .Select(g => new
            {
                AccountNumber = g.Key,
                Balance = g.Sum(x =>
                    (x.jel.DebitAmount > 0 ? x.jel.DebitAmount - x.jel.VatAmount : 0)
                    - (x.jel.CreditAmount > 0 ? x.jel.CreditAmount - x.jel.VatAmount : 0))
            })
            .ToDictionaryAsync(x => x.AccountNumber, x => x.Balance, ct);
    }

    private static decimal SumRange(Dictionary<string, decimal> balances, string from, string to)
    {
        return balances
            .Where(kvp => string.Compare(kvp.Key, from, StringComparison.Ordinal) >= 0
                && string.Compare(kvp.Key, to, StringComparison.Ordinal) <= 0)
            .Sum(kvp => kvp.Value);
    }
}
