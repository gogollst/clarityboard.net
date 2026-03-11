using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Implements IDatevExportService: generates a DATEV EXTF Buchungsstapel CSV,
/// uploads it to MinIO via IDocumentStorage, and persists export metadata
/// as a DatevExport entity.
/// </summary>
public class ManagedDatevExportService : IDatevExportService
{
    // Windows-1252 encoding is required by DATEV EXTF v700
    private static readonly Encoding Win1252;

    static ManagedDatevExportService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Win1252 = Encoding.GetEncoding(1252);
    }

    private readonly IAppDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly ILogger<ManagedDatevExportService> _logger;

    public ManagedDatevExportService(
        IAppDbContext db, IDocumentStorage storage,
        ILogger<ManagedDatevExportService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<DatevExport> GenerateExportAsync(
        Guid entityId, Guid fiscalPeriodId,
        DatevExportType exportType, Guid generatedBy,
        CancellationToken ct)
    {
        var export = DatevExport.Create(entityId, fiscalPeriodId, exportType, generatedBy);
        _db.DatevExports.Add(export);
        export.SetGenerating();
        await _db.SaveChangesAsync(ct);

        try
        {
            var entity = await _db.LegalEntityExtensions
                .FirstOrDefaultAsync(e => e.EntityId == entityId, ct);

            var period = await _db.FiscalPeriods
                .FirstOrDefaultAsync(fp => fp.Id == fiscalPeriodId, ct)
                ?? throw new InvalidOperationException($"Fiscal period {fiscalPeriodId} not found.");

            var entries = await _db.JournalEntries
                .Include(je => je.Lines)
                .Where(je =>
                    je.EntityId == entityId &&
                    je.FiscalPeriodId == fiscalPeriodId &&
                    je.Status == "posted")
                .OrderBy(je => je.EntryNumber)
                .ToListAsync(ct);

            // Load accounts for all lines in a single query
            var accountIds = entries
                .SelectMany(e => e.Lines)
                .Select(l => l.AccountId)
                .Distinct()
                .ToList();

            var accounts = await _db.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, ct);

            var csvContent = GenerateBuchungsstapelCsv(entries, entity, period, accounts);
            var csvBytes = Win1252.GetBytes(csvContent);

            var checksum = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(csvBytes));

            var fileName = $"EXTF_Buchungsstapel_{period.Year:D4}{period.Month:D2}.csv";
            using var stream = new MemoryStream(csvBytes);
            var storagePath = await _storage.UploadAsync(
                entityId,
                $"datev/{export.Id}/{fileName}",
                stream,
                "text/csv",
                ct);

            export.SetReady(
                fileCount: 1,
                recordCount: entries.Count,
                checksums: System.Text.Json.JsonSerializer.Serialize(
                    new Dictionary<string, string> { [fileName] = checksum }),
                fileStorageKeys: System.Text.Json.JsonSerializer.Serialize(
                    new Dictionary<string, string> { [fileName] = storagePath }));

            period.IncrementExportCount();
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DATEV export {ExportId} completed: {Count} entries for entity {EntityId}, period {Year}/{Month:D2}",
                export.Id, entries.Count, entityId, period.Year, period.Month);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DATEV export failed for entity {EntityId}", entityId);
            export.SetFailed(
                $"[{{\"message\":\"{ex.Message.Replace("\"", "'")}\",\"severity\":\"error\"}}]");
            await _db.SaveChangesAsync(ct);
        }

        return export;
    }

    public async Task<Stream> GetExportStreamAsync(Guid exportId, CancellationToken ct)
    {
        var export = await _db.DatevExports
            .FirstOrDefaultAsync(e => e.Id == exportId, ct)
            ?? throw new InvalidOperationException($"Export {exportId} not found.");

        if (export.Status != DatevExportStatus.Ready || export.FileStorageKeys is null)
            throw new InvalidOperationException("Export is not ready.");

        var keys = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, string>>(export.FileStorageKeys)!;
        var firstKey = keys.Values.First();

        return await _storage.DownloadAsync(export.EntityId, firstKey, ct);
    }

    private static string GenerateBuchungsstapelCsv(
        List<JournalEntry> entries,
        LegalEntityExtension? entity,
        FiscalPeriod period,
        Dictionary<Guid, Account> accounts)
    {
        var sb = new StringBuilder();
        var now = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        // Row 1: EXTF Header (DATEV v700)
        sb.Append("\"EXTF\";700;21;\"Buchungsstapel\";12;");
        sb.Append($"{now};;\"CB\";\"\";;");
        sb.Append($"{entity?.DatevConsultantNumber ?? string.Empty};");
        sb.Append($"{entity?.DatevClientNumber ?? string.Empty};");
        sb.Append($"{period.Year:D4}0101;4;");
        sb.Append($"{period.StartDate:yyyyMMdd};{period.EndDate:yyyyMMdd};");
        sb.Append($"\"ClarityBoard Export {period.Month:D2}/{period.Year:D4}\";\"\"");
        sb.AppendLine(";1;0;;;\"EUR\";;;\"\"");

        // Row 2: Column headers (DATEV Buchungsstapel standard columns)
        sb.AppendLine(
            "\"Umsatz (ohne Soll/Haben-Kz)\";\"Soll/Haben-Kennzeichen\";\"WKZ Umsatz\";" +
            "\"Kurs\";\"Basis-Umsatz\";\"WKZ Basis-Umsatz\";\"Konto\";" +
            "\"Gegenkonto (ohne BU-Schlüssel)\";\"BU-Schlüssel\";\"Belegdatum\";" +
            "\"Belegfeld 1\";\"Belegfeld 2\";\"Skonto\";\"Buchungstext\"");

        // Booking lines: each JournalEntryLine produces one DATEV row
        foreach (var entry in entries)
        {
            var orderedLines = entry.Lines.OrderBy(l => l.LineNumber).ToList();

            foreach (var line in orderedLines)
            {
                var isDebit = line.DebitAmount > 0;
                var amount = isDebit ? line.DebitAmount : line.CreditAmount;
                var amountStr = amount.ToString("F2",
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

                // Determine Gegenkonto: for a two-line entry, use the other line's account
                var gegenkontoNumber = string.Empty;
                if (orderedLines.Count == 2)
                {
                    var contraLine = orderedLines.FirstOrDefault(l => l.LineNumber != line.LineNumber);
                    if (contraLine is not null &&
                        accounts.TryGetValue(contraLine.AccountId, out var contraAccount))
                    {
                        gegenkontoNumber = contraAccount.AccountNumber;
                    }
                }

                accounts.TryGetValue(line.AccountId, out var account);

                var rawText = line.Description ?? entry.Description;
                var buchungstext = rawText[..Math.Min(rawText.Length, 60)]
                    .Replace("€", "EUR");

                var belegdatum = (entry.DocumentDate ?? entry.EntryDate).ToString("ddMM");

                var fields = new[]
                {
                    amountStr,
                    isDebit ? "\"S\"" : "\"H\"",
                    $"\"{line.Currency}\"",
                    line.ExchangeRate != 1.0m
                        ? line.ExchangeRate.ToString("F6",
                            System.Globalization.CultureInfo.InvariantCulture)
                        : string.Empty,
                    string.Empty, // Basis-Umsatz
                    string.Empty, // WKZ Basis-Umsatz
                    $"\"{account?.AccountNumber ?? string.Empty}\"",
                    $"\"{gegenkontoNumber}\"",
                    !string.IsNullOrEmpty(line.TaxCode ?? line.VatCode)
                        ? $"\"{line.TaxCode ?? line.VatCode}\""
                        : string.Empty,
                    belegdatum,
                    EscapeCsvField(entry.DocumentRef ?? string.Empty),
                    EscapeCsvField(entry.DocumentRef2 ?? string.Empty),
                    string.Empty, // Skonto
                    EscapeCsvField(buchungstext),
                };

                sb.AppendLine(string.Join(";", fields));
            }
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
