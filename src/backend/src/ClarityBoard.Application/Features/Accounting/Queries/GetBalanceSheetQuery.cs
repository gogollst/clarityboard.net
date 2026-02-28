using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record BalanceSheetLineItem(string Label, decimal Amount, decimal? PriorAmount = null);
public record BalanceSheetSection(string Name, List<BalanceSheetLineItem> Items, decimal Subtotal, decimal? PriorSubtotal = null);

public record BalanceSheetDto(
    DateOnly AsOfDate,
    DateOnly? PriorDate,
    List<BalanceSheetSection> Assets,
    decimal TotalAssets,
    decimal? PriorTotalAssets,
    List<BalanceSheetSection> LiabilitiesAndEquity,
    decimal TotalLiabilitiesAndEquity,
    decimal? PriorTotalLiabilitiesAndEquity);

public record GetBalanceSheetQuery(
    Guid EntityId,
    short Year,
    short Month,
    short? CompareYear = null,
    short? CompareMonth = null) : IRequest<BalanceSheetDto>, IEntityScoped;

public class GetBalanceSheetQueryHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    private readonly IAppDbContext _db;

    public GetBalanceSheetQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken ct)
    {
        var asOfDate = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
        var balances = await GetCumulativeBalancesAsync(request.EntityId, asOfDate, ct);

        Dictionary<string, decimal>? priorBalances = null;
        DateOnly? priorDate = null;
        if (request.CompareYear.HasValue && request.CompareMonth.HasValue)
        {
            priorDate = new DateOnly(request.CompareYear.Value, request.CompareMonth.Value,
                DateTime.DaysInMonth(request.CompareYear.Value, request.CompareMonth.Value));
            priorBalances = await GetCumulativeBalancesAsync(request.EntityId, priorDate.Value, ct);
        }

        // AKTIVA (Assets)
        var assets = new List<BalanceSheetSection>();

        // A. Fixed Assets (Anlagevermoegen) - accounts 0010-0399
        var fixedAssets = SumRange(balances, "0010", "0399");
        var priorFixed = priorBalances != null ? SumRange(priorBalances, "0010", "0399") : (decimal?)null;
        assets.Add(new BalanceSheetSection("A. Anlagevermoegen", [
            new BalanceSheetLineItem("I. Immaterielle Vermoegensgegenstande", SumRange(balances, "0200", "0299"), priorBalances != null ? SumRange(priorBalances, "0200", "0299") : null),
            new BalanceSheetLineItem("II. Sachanlagen", SumRange(balances, "0010", "0099"), priorBalances != null ? SumRange(priorBalances, "0010", "0099") : null),
            new BalanceSheetLineItem("III. Finanzanlagen", SumRange(balances, "0100", "0199"), priorBalances != null ? SumRange(priorBalances, "0100", "0199") : null),
        ], fixedAssets, priorFixed));

        // B. Current Assets (Umlaufvermoegen) - accounts 1000-1599
        var currentAssets = SumRange(balances, "1000", "1599");
        var priorCurrent = priorBalances != null ? SumRange(priorBalances, "1000", "1599") : (decimal?)null;
        assets.Add(new BalanceSheetSection("B. Umlaufvermoegen", [
            new BalanceSheetLineItem("I. Vorraete", SumRange(balances, "1050", "1099"), priorBalances != null ? SumRange(priorBalances, "1050", "1099") : null),
            new BalanceSheetLineItem("II. Forderungen", SumRange(balances, "1400", "1499"), priorBalances != null ? SumRange(priorBalances, "1400", "1499") : null),
            new BalanceSheetLineItem("III. Wertpapiere", SumRange(balances, "1300", "1399"), priorBalances != null ? SumRange(priorBalances, "1300", "1399") : null),
            new BalanceSheetLineItem("IV. Kassenbestand, Bankguthaben", SumRange(balances, "1000", "1049") + SumRange(balances, "1200", "1299"), priorBalances != null ? SumRange(priorBalances, "1000", "1049") + SumRange(priorBalances, "1200", "1299") : null),
            new BalanceSheetLineItem("V. Sonstige Vermoegensgegenstande", SumRange(balances, "1500", "1599"), priorBalances != null ? SumRange(priorBalances, "1500", "1599") : null),
        ], currentAssets, priorCurrent));

        // C. Prepaid (Rechnungsabgrenzung) - accounts 2000-2099
        var prepaid = SumRange(balances, "2000", "2099");
        var priorPrepaid = priorBalances != null ? SumRange(priorBalances, "2000", "2099") : (decimal?)null;
        assets.Add(new BalanceSheetSection("C. Rechnungsabgrenzungsposten", [
            new BalanceSheetLineItem("Aktive Rechnungsabgrenzung", prepaid, priorPrepaid)
        ], prepaid, priorPrepaid));

        var totalAssets = assets.Sum(s => s.Subtotal);
        var priorTotalAssets = priorBalances != null ? assets.Sum(s => s.PriorSubtotal ?? 0) : (decimal?)null;

        // PASSIVA (Equity + Liabilities)
        var passiva = new List<BalanceSheetSection>();

        // A. Equity (Eigenkapital) - accounts 0400-0499
        var equity = -SumRange(balances, "0400", "0499"); // Negate (credit balances)
        var priorEquity = priorBalances != null ? -SumRange(priorBalances, "0400", "0499") : (decimal?)null;
        passiva.Add(new BalanceSheetSection("A. Eigenkapital", [
            new BalanceSheetLineItem("I. Gezeichnetes Kapital", -SumRange(balances, "0400", "0409"), priorBalances != null ? -SumRange(priorBalances, "0400", "0409") : null),
            new BalanceSheetLineItem("II. Kapitalruecklage", -SumRange(balances, "0420", "0429"), priorBalances != null ? -SumRange(priorBalances, "0420", "0429") : null),
            new BalanceSheetLineItem("III. Gewinnruecklagen", -SumRange(balances, "0430", "0449"), priorBalances != null ? -SumRange(priorBalances, "0430", "0449") : null),
            new BalanceSheetLineItem("IV. Gewinn-/Verlustvortrag", -SumRange(balances, "0450", "0459"), priorBalances != null ? -SumRange(priorBalances, "0450", "0459") : null),
            new BalanceSheetLineItem("V. Jahresueberschuss/-fehlbetrag", -SumRange(balances, "0460", "0489"), priorBalances != null ? -SumRange(priorBalances, "0460", "0489") : null),
        ], equity, priorEquity));

        // B. Provisions (Rueckstellungen) - accounts 0700-0799
        var provisions = -SumRange(balances, "0700", "0799");
        var priorProvisions = priorBalances != null ? -SumRange(priorBalances, "0700", "0799") : (decimal?)null;
        passiva.Add(new BalanceSheetSection("B. Rueckstellungen", [
            new BalanceSheetLineItem("Rueckstellungen", provisions, priorProvisions)
        ], provisions, priorProvisions));

        // C. Liabilities (Verbindlichkeiten) - accounts 1600-1799
        var liabilities = -SumRange(balances, "1600", "1799");
        var priorLiabilities = priorBalances != null ? -SumRange(priorBalances, "1600", "1799") : (decimal?)null;
        passiva.Add(new BalanceSheetSection("C. Verbindlichkeiten", [
            new BalanceSheetLineItem("I. Verbindlichkeiten gegenueber Kreditinstituten", -SumRange(balances, "1720", "1739"), priorBalances != null ? -SumRange(priorBalances, "1720", "1739") : null),
            new BalanceSheetLineItem("II. Verbindlichkeiten aus L+L", -SumRange(balances, "1600", "1629"), priorBalances != null ? -SumRange(priorBalances, "1600", "1629") : null),
            new BalanceSheetLineItem("III. Steuerverbindlichkeiten", -SumRange(balances, "1740", "1799"), priorBalances != null ? -SumRange(priorBalances, "1740", "1799") : null),
            new BalanceSheetLineItem("IV. Sonstige Verbindlichkeiten", -SumRange(balances, "1700", "1719"), priorBalances != null ? -SumRange(priorBalances, "1700", "1719") : null),
        ], liabilities, priorLiabilities));

        // D. Deferred income - accounts 2100-2199
        var deferred = -SumRange(balances, "2100", "2199");
        var priorDeferred = priorBalances != null ? -SumRange(priorBalances, "2100", "2199") : (decimal?)null;
        passiva.Add(new BalanceSheetSection("D. Rechnungsabgrenzungsposten", [
            new BalanceSheetLineItem("Passive Rechnungsabgrenzung", deferred, priorDeferred)
        ], deferred, priorDeferred));

        var totalPassiva = passiva.Sum(s => s.Subtotal);
        var priorTotalPassiva = priorBalances != null ? passiva.Sum(s => s.PriorSubtotal ?? 0) : (decimal?)null;

        return new BalanceSheetDto(
            asOfDate, priorDate,
            assets, totalAssets, priorTotalAssets,
            passiva, totalPassiva, priorTotalPassiva);
    }

    private async Task<Dictionary<string, decimal>> GetCumulativeBalancesAsync(
        Guid entityId, DateOnly asOfDate, CancellationToken ct)
    {
        return await (
            from a in _db.Accounts.Where(a => a.EntityId == entityId)
            join jel in _db.JournalEntryLines on a.Id equals jel.AccountId
            join je in _db.JournalEntries on jel.JournalEntryId equals je.Id
            where je.EntityId == entityId
                && je.EntryDate <= asOfDate
                && je.Status != "reversed"
            group jel by a.AccountNumber into g
            select new { AccountNumber = g.Key, Balance = g.Sum(l => l.DebitAmount - l.CreditAmount) }
        ).ToDictionaryAsync(x => x.AccountNumber, x => x.Balance, ct);
    }

    private static decimal SumRange(Dictionary<string, decimal> balances, string from, string to)
    {
        return balances
            .Where(kvp => string.Compare(kvp.Key, from, StringComparison.Ordinal) >= 0
                && string.Compare(kvp.Key, to, StringComparison.Ordinal) <= 0)
            .Sum(kvp => kvp.Value);
    }
}
