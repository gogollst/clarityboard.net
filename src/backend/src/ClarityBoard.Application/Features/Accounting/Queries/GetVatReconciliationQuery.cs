using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record VatCategoryAmounts(decimal NetAmount, decimal VatAmount);

public record VatLineDetailDto(
    string AccountNumber,
    string AccountName,
    string VatCode,
    decimal VatRate,
    string VatType,
    decimal NetAmount,
    decimal VatAmount);

public record VatReconciliationDto(
    Guid EntityId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    VatCategoryAmounts OutputVat19,
    VatCategoryAmounts OutputVat7,
    VatCategoryAmounts OutputVat0,
    VatCategoryAmounts InputVat,
    VatCategoryAmounts ReverseChargeVat,
    VatCategoryAmounts IntraEuAcquisitions,
    decimal TotalOutputVat,
    decimal TotalInputVat,
    decimal NetPayable,
    List<VatLineDetailDto> LineDetails);

// ── Query ─────────────────────────────────────────────────────────────────

public record GetVatReconciliationQuery(
    Guid EntityId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<VatReconciliationDto>, IEntityScoped;

// ── Handler ───────────────────────────────────────────────────────────────

internal record VatJournalLine(
    string AccountNumber,
    string AccountName,
    string? VatCode,
    decimal VatAmount,
    decimal DebitAmount,
    decimal CreditAmount);

public class GetVatReconciliationQueryHandler : IRequestHandler<GetVatReconciliationQuery, VatReconciliationDto>
{
    private readonly IAppDbContext _db;

    // SKR03 Output VAT accounts (Umsatzsteuer)
    private static readonly HashSet<string> OutputVat19Accounts = new() { "1776" };
    private static readonly HashSet<string> OutputVat7Accounts = new() { "1756", "1770", "1771" };

    // SKR03 Input VAT accounts (Vorsteuer)
    private static readonly HashSet<string> InputVatAccounts = new() { "1510", "1511", "1570", "1571", "1576" };

    // Import VAT (Einfuhrumsatzsteuer)
    private static readonly HashSet<string> ImportVatAccounts = new() { "1518" };

    // All VAT-related account numbers for filtering
    private static readonly HashSet<string> AllVatAccounts = new(
        OutputVat19Accounts
            .Concat(OutputVat7Accounts)
            .Concat(InputVatAccounts)
            .Concat(ImportVatAccounts));

    public GetVatReconciliationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<VatReconciliationDto> Handle(GetVatReconciliationQuery request, CancellationToken ct)
    {
        // ── 1. Fetch all posted journal entry lines on VAT accounts in the period ──

        var vatLines = await (
            from a in _db.Accounts.Where(a => a.EntityId == request.EntityId)
            join jel in _db.JournalEntryLines on a.Id equals jel.AccountId
            join je in _db.JournalEntries on jel.JournalEntryId equals je.Id
            where je.EntityId == request.EntityId
                && je.EntryDate >= request.PeriodStart
                && je.EntryDate <= request.PeriodEnd
                && je.Status == "posted"
                && AllVatAccounts.Contains(a.AccountNumber)
            select new VatJournalLine(
                a.AccountNumber,
                a.Name,
                jel.VatCode,
                jel.VatAmount,
                jel.DebitAmount,
                jel.CreditAmount)
        ).ToListAsync(ct);

        // ── 2. Also fetch VatRecords for richer detail (rate, type info) ──

        var vatRecords = await (
            from vr in _db.VatRecords
            where vr.EntityId == request.EntityId
                && (vr.Year * 100 + vr.Month) >= (request.PeriodStart.Year * 100 + request.PeriodStart.Month)
                && (vr.Year * 100 + vr.Month) <= (request.PeriodEnd.Year * 100 + request.PeriodEnd.Month)
            select vr
        ).ToListAsync(ct);

        // ── 3. Categorize amounts from journal entry lines ──

        decimal outputVat19Net = 0, outputVat19Vat = 0;
        decimal outputVat7Net = 0, outputVat7Vat = 0;
        decimal outputVat0Net = 0, outputVat0Vat = 0;
        decimal inputVatNet = 0, inputVatTotal = 0;
        decimal reverseChargeNet = 0, reverseChargeVat = 0;
        decimal intraEuNet = 0, intraEuVat = 0;

        foreach (var line in vatLines)
        {
            // VAT amount on the account line (debit - credit for balance)
            var vatAmount = line.DebitAmount - line.CreditAmount;
            // Estimate the net base from the VAT amount where possible
            var code = line.VatCode ?? "";

            if (OutputVat19Accounts.Contains(line.AccountNumber))
            {
                // Check if this is reverse charge or intra-EU based on VatCode
                if (IsReverseChargeCode(code))
                {
                    reverseChargeVat += vatAmount;
                    reverseChargeNet += vatAmount != 0 ? Math.Round(vatAmount / 0.19m, 2, MidpointRounding.AwayFromZero) : 0;
                }
                else if (IsIntraEuCode(code))
                {
                    intraEuVat += vatAmount;
                    intraEuNet += vatAmount != 0 ? Math.Round(vatAmount / 0.19m, 2, MidpointRounding.AwayFromZero) : 0;
                }
                else
                {
                    outputVat19Vat += vatAmount;
                    outputVat19Net += vatAmount != 0 ? Math.Round(vatAmount / 0.19m, 2, MidpointRounding.AwayFromZero) : 0;
                }
            }
            else if (OutputVat7Accounts.Contains(line.AccountNumber))
            {
                if (IsIntraEuCode(code))
                {
                    intraEuVat += vatAmount;
                    intraEuNet += vatAmount != 0 ? Math.Round(vatAmount / 0.07m, 2, MidpointRounding.AwayFromZero) : 0;
                }
                else
                {
                    outputVat7Vat += vatAmount;
                    outputVat7Net += vatAmount != 0 ? Math.Round(vatAmount / 0.07m, 2, MidpointRounding.AwayFromZero) : 0;
                }
            }
            else if (InputVatAccounts.Contains(line.AccountNumber))
            {
                inputVatTotal += vatAmount;
                // Determine rate from account number
                var rate = IsInputVat7Account(line.AccountNumber) ? 0.07m : 0.19m;
                inputVatNet += vatAmount != 0 ? Math.Round(vatAmount / rate, 2, MidpointRounding.AwayFromZero) : 0;
            }
            else if (ImportVatAccounts.Contains(line.AccountNumber))
            {
                inputVatTotal += vatAmount;
                inputVatNet += vatAmount != 0 ? Math.Round(vatAmount / 0.19m, 2, MidpointRounding.AwayFromZero) : 0;
            }
        }

        // ── 4. Enrich with VatRecords if available (prefer VatRecords for net amounts) ──

        if (vatRecords.Count > 0)
        {
            // Reset and recalculate from VatRecords which have explicit net amounts
            outputVat19Net = 0; outputVat19Vat = 0;
            outputVat7Net = 0; outputVat7Vat = 0;
            outputVat0Net = 0; outputVat0Vat = 0;
            inputVatNet = 0; inputVatTotal = 0;
            reverseChargeNet = 0; reverseChargeVat = 0;
            intraEuNet = 0; intraEuVat = 0;

            foreach (var vr in vatRecords)
            {
                switch (vr.VatType)
                {
                    case "output" when vr.VatRate == 19m:
                        outputVat19Net += vr.NetAmount;
                        outputVat19Vat += vr.VatAmount;
                        break;
                    case "output" when vr.VatRate == 7m:
                        outputVat7Net += vr.NetAmount;
                        outputVat7Vat += vr.VatAmount;
                        break;
                    case "output" when vr.VatRate == 0m:
                        outputVat0Net += vr.NetAmount;
                        outputVat0Vat += vr.VatAmount;
                        break;
                    case "input":
                        inputVatNet += vr.NetAmount;
                        inputVatTotal += vr.VatAmount;
                        break;
                    case "reverse_charge":
                        reverseChargeNet += vr.NetAmount;
                        reverseChargeVat += vr.VatAmount;
                        break;
                    case "intra_eu":
                        intraEuNet += vr.NetAmount;
                        intraEuVat += vr.VatAmount;
                        break;
                }
            }
        }

        // ── 5. Build line details breakdown ──

        var lineDetails = vatRecords.Count > 0
            ? BuildLineDetailsFromVatRecords(vatRecords)
            : BuildLineDetailsFromJournalLines(vatLines);

        // ── 6. Calculate totals ──

        var totalOutputVat = outputVat19Vat + outputVat7Vat + outputVat0Vat + reverseChargeVat + intraEuVat;
        var totalInputVat = inputVatTotal;
        var netPayable = totalOutputVat - totalInputVat;

        return new VatReconciliationDto(
            request.EntityId,
            request.PeriodStart,
            request.PeriodEnd,
            new VatCategoryAmounts(outputVat19Net, outputVat19Vat),
            new VatCategoryAmounts(outputVat7Net, outputVat7Vat),
            new VatCategoryAmounts(outputVat0Net, outputVat0Vat),
            new VatCategoryAmounts(inputVatNet, inputVatTotal),
            new VatCategoryAmounts(reverseChargeNet, reverseChargeVat),
            new VatCategoryAmounts(intraEuNet, intraEuVat),
            totalOutputVat,
            totalInputVat,
            netPayable,
            lineDetails);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static bool IsReverseChargeCode(string vatCode)
        => vatCode is "UVRC" or "94";

    private static bool IsIntraEuCode(string vatCode)
        => vatCode is "UVEU_ACQ" or "91" or "92";

    private static bool IsInputVat7Account(string accountNumber)
        => accountNumber is "1511" or "1570" or "1571";

    private static List<VatLineDetailDto> BuildLineDetailsFromVatRecords(
        List<Domain.Entities.Accounting.VatRecord> records)
    {
        return records
            .GroupBy(vr => new { vr.VatCode, vr.VatRate, vr.VatType })
            .Select(g => new VatLineDetailDto(
                AccountNumber: GetAccountForVatType(g.Key.VatType, g.Key.VatRate),
                AccountName: GetAccountNameForVatType(g.Key.VatType, g.Key.VatRate),
                VatCode: g.Key.VatCode,
                VatRate: g.Key.VatRate,
                VatType: g.Key.VatType,
                NetAmount: g.Sum(r => r.NetAmount),
                VatAmount: g.Sum(r => r.VatAmount)))
            .OrderBy(d => d.VatType)
            .ThenBy(d => d.VatRate)
            .ToList();
    }

    private static List<VatLineDetailDto> BuildLineDetailsFromJournalLines(
        List<VatJournalLine> lines)
    {
        // Group the lines by account number and VAT code
        var grouped = new Dictionary<string, (string AccountName, string VatCode, decimal Vat)>();

        foreach (var line in lines)
        {
            var vatCode = line.VatCode ?? "";
            var vatAmount = line.DebitAmount - line.CreditAmount;

            var key = $"{line.AccountNumber}|{vatCode}";
            if (grouped.TryGetValue(key, out var existing))
            {
                grouped[key] = (line.AccountName, vatCode, existing.Vat + vatAmount);
            }
            else
            {
                grouped[key] = (line.AccountName, vatCode, vatAmount);
            }
        }

        return grouped
            .Select(kvp =>
            {
                var accountNumber = kvp.Key.Split('|')[0];
                var rate = GetRateForAccount(accountNumber);
                var vatAmount = kvp.Value.Vat;
                var netAmount = rate > 0 ? Math.Round(vatAmount / rate, 2, MidpointRounding.AwayFromZero) : 0;

                return new VatLineDetailDto(
                    AccountNumber: accountNumber,
                    AccountName: kvp.Value.AccountName,
                    VatCode: kvp.Value.VatCode,
                    VatRate: rate * 100,
                    VatType: GetVatTypeForAccount(accountNumber),
                    NetAmount: netAmount,
                    VatAmount: vatAmount);
            })
            .OrderBy(d => d.VatType)
            .ThenBy(d => d.VatRate)
            .ToList();
    }

    private static decimal GetRateForAccount(string accountNumber) => accountNumber switch
    {
        "1776" => 0.19m,
        "1756" or "1770" or "1771" => 0.07m,
        "1576" or "1510" => 0.19m,
        "1570" or "1571" or "1511" => 0.07m,
        "1518" => 0.19m,
        _ => 0m,
    };

    private static string GetVatTypeForAccount(string accountNumber) => accountNumber switch
    {
        "1776" or "1756" or "1770" or "1771" => "output",
        "1510" or "1511" or "1570" or "1571" or "1576" or "1518" => "input",
        _ => "unknown",
    };

    private static string GetAccountForVatType(string vatType, decimal vatRate) => vatType switch
    {
        "output" when vatRate == 19m => "1776",
        "output" when vatRate == 7m => "1756",
        "output" when vatRate == 0m => "-",
        "input" when vatRate == 19m => "1576",
        "input" when vatRate == 7m => "1571",
        "reverse_charge" => "1776/1576",
        "intra_eu" => "1776/1576",
        _ => "-",
    };

    private static string GetAccountNameForVatType(string vatType, decimal vatRate) => vatType switch
    {
        "output" when vatRate == 19m => "Umsatzsteuer 19%",
        "output" when vatRate == 7m => "Umsatzsteuer 7%",
        "output" when vatRate == 0m => "Steuerfreie Umsaetze",
        "input" when vatRate == 19m => "Abziehbare Vorsteuer 19%",
        "input" when vatRate == 7m => "Abziehbare Vorsteuer 7%",
        "reverse_charge" => "Steuerschuld Reverse Charge",
        "intra_eu" => "Innergemeinschaftlicher Erwerb",
        _ => "Sonstige USt",
    };
}
