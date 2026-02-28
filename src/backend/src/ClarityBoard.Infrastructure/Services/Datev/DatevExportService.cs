using System.Security.Cryptography;
using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Datev;

/// <summary>
/// Result of a DATEV EXTF export operation.
/// </summary>
public record DatevExportResult(
    byte[] FileContent,
    string FileName,
    string Checksum,
    int EntryCount,
    DateTime ExportedAt);

/// <summary>
/// Generates DATEV EXTF (Buchungsstapel) export files conforming to the
/// DATEV format specification for financial bookings.
/// Uses Windows-1252 encoding, semicolon-separated fields, and includes
/// a SHA-256 checksum for integrity verification.
/// </summary>
public class DatevExportService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<DatevExportService> _logger;

    private static readonly Encoding Win1252;

    static DatevExportService()
    {
        // Register CodePages provider for Windows-1252 encoding support in .NET Core
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Win1252 = Encoding.GetEncoding(1252);
    }

    // Total number of columns per DATEV EXTF Buchungsstapel row (format version 13)
    private const int TotalColumns = 120;

    // Column indices (0-based) for named fields beyond the initial 14
    private const int ColKost1 = 36;         // Column 37: KOST1 - Kostenstelle
    private const int ColKost2 = 37;         // Column 38: KOST2 - Kostenstelle
    private const int ColKostMenge = 38;     // Column 39: Kost-Menge
    private const int ColBuchungsGuid = 102; // Column 103: Buchungs GUID
    private const int ColLeistungsdatum = 114; // Column 115: Leistungsdatum
    private const int ColFestschreibung = 113; // Column 114: Festschreibung

    public DatevExportService(IAppDbContext db, ILogger<DatevExportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates a DATEV EXTF Buchungsstapel file for the specified entity and period.
    /// Only includes posted journal entries (excludes reversed entries).
    /// </summary>
    /// <param name="entityId">The legal entity to export bookings for.</param>
    /// <param name="year">Fiscal year.</param>
    /// <param name="month">Fiscal month (1-12).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result containing file bytes, filename, SHA-256 checksum, and metadata.</returns>
    public async Task<DatevExportResult> ExportBuchungsstapelAsync(
        Guid entityId, short year, short month, CancellationToken ct = default)
    {
        var entity = await _db.LegalEntities.FirstOrDefaultAsync(e => e.Id == entityId, ct)
            ?? throw new InvalidOperationException($"Entity '{entityId}' not found.");

        ValidateEntityForExport(entity);

        var entries = await _db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.EntityId == entityId
                && j.EntryDate.Year == year
                && j.EntryDate.Month == month
                && j.Status != "reversed")
            .OrderBy(j => j.EntryNumber)
            .ToListAsync(ct);

        var accounts = await _db.Accounts
            .Where(a => a.EntityId == entityId)
            .ToDictionaryAsync(a => a.Id, ct);

        var sb = new StringBuilder();

        // EXTF Header Row 1 (format specification)
        AppendHeaderRow(sb, entity, year, month);

        // EXTF Header Row 2 (column names)
        AppendColumnHeaders(sb);

        // Data rows: each JournalEntryLine becomes one EXTF row
        var entryCount = 0;
        foreach (var entry in entries)
        {
            var orderedLines = entry.Lines.OrderBy(l => l.LineNumber).ToList();

            foreach (var line in orderedLines)
            {
                AppendDataRow(sb, entry, line, orderedLines, accounts);
                entryCount++;
            }
        }

        var content = Win1252.GetBytes(sb.ToString());
        var checksum = Convert.ToHexStringLower(SHA256.HashData(content));
        var fileName = $"EXTF_Buchungsstapel_{entity.DatevClientNumber ?? "00000"}_{year}{month:D2}.csv";

        _logger.LogInformation(
            "DATEV EXTF export completed: {Count} lines for entity {EntityId} ({EntityName}) period {Year}/{Month:D2}",
            entryCount, entityId, entity.Name, year, month);

        return new DatevExportResult(content, fileName, checksum, entryCount, DateTime.UtcNow);
    }

    /// <summary>
    /// Validates that the entity has the required DATEV configuration fields.
    /// </summary>
    private static void ValidateEntityForExport(LegalEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.DatevConsultantNumber))
            throw new InvalidOperationException(
                $"Entity '{entity.Name}' is missing DATEV consultant number (Beraternummer). Configure it before exporting.");

        if (string.IsNullOrWhiteSpace(entity.DatevClientNumber))
            throw new InvalidOperationException(
                $"Entity '{entity.Name}' is missing DATEV client number (Mandantennummer). Configure it before exporting.");
    }

    /// <summary>
    /// Appends the EXTF header row (Row 1) per DATEV specification.
    /// Contains format metadata, consultant/client numbers, fiscal year boundaries, etc.
    /// </summary>
    private static void AppendHeaderRow(StringBuilder sb, LegalEntity entity, short year, short month)
    {
        // Determine fiscal year begin based on entity's fiscal year start month
        var fiscalYearBegin = new DateOnly(year, entity.FiscalYearStartMonth, 1);
        if (entity.FiscalYearStartMonth > month)
            fiscalYearBegin = fiscalYearBegin.AddYears(-1);

        // Determine the Sachkontenlange (G/L account length) from the chart of accounts
        // SKR03/SKR04 typically use 4-digit accounts, but the entity's ChartOfAccounts.Length
        // is the string length of "SKR03"/"SKR04" (5) which is not what DATEV expects.
        // Standard DATEV Sachkontenlange for SKR03/SKR04 is 4.
        var sachkontenlaenge = entity.ChartOfAccounts switch
        {
            "SKR03" => 4,
            "SKR04" => 4,
            _ => 4
        };

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var generatedAt = DateTime.UtcNow;

        var fields = new List<string>
        {
            "\"EXTF\"",                                              // 1: Format identifier
            "700",                                                    // 2: Format version number
            "21",                                                     // 3: Data category (21 = Buchungsstapel)
            "\"Buchungsstapel\"",                                     // 4: Format name
            "13",                                                     // 5: Format version of data category
            $"{generatedAt:yyyyMMddHHmmss}000",                       // 6: Created timestamp (YYYYMMDDHHMMSSmmm)
            "",                                                       // 7: Reserved
            "\"CB\"",                                                 // 8: Source application (ClarityBoard)
            "",                                                       // 9: Reserved
            "",                                                       // 10: Reserved
            $"\"{entity.DatevConsultantNumber}\"",                    // 11: Berater-Nr (consultant number)
            $"\"{entity.DatevClientNumber}\"",                        // 12: Mandanten-Nr (client number)
            $"{fiscalYearBegin:yyyyMMdd}",                            // 13: WJ-Beginn (fiscal year begin)
            $"{sachkontenlaenge}",                                     // 14: Sachkontenlange (G/L account length)
            $"{periodStart:yyyyMMdd}",                                // 15: Datum von (period start)
            $"{periodEnd:yyyyMMdd}",                                  // 16: Datum bis (period end)
            $"\"{EscapeQuotes(entity.Name)}\"",                       // 17: Bezeichnung (description)
            "",                                                       // 18: Diktatkurzel (dictation abbreviation)
            "1",                                                      // 19: Buchungstyp (1 = Finanzbuchfuhrung)
            "0",                                                      // 20: Rechnungslegungszweck (0 = default)
            "0",                                                      // 21: Festschreibung (0 = not locked)
            $"\"{entity.Currency}\"",                                 // 22: WKZ (currency code)
        };

        // Pad to at least 27 fields as per DATEV spec (some implementations expect more header fields)
        while (fields.Count < 27)
            fields.Add("");

        sb.AppendLine(string.Join(";", fields));
    }

    /// <summary>
    /// Appends the EXTF column header row (Row 2) per DATEV Buchungsstapel specification.
    /// These are the official DATEV column names in German.
    /// </summary>
    private static void AppendColumnHeaders(StringBuilder sb)
    {
        var columns = new[]
        {
            /* 1  */ "Umsatz (ohne Soll/Haben-Kz)",
            /* 2  */ "Soll/Haben-Kennzeichen",
            /* 3  */ "WKZ Umsatz",
            /* 4  */ "Kurs",
            /* 5  */ "Basis-Umsatz",
            /* 6  */ "WKZ Basis-Umsatz",
            /* 7  */ "Konto",
            /* 8  */ "Gegenkonto (ohne BU-Schlüssel)",
            /* 9  */ "BU-Schlüssel",
            /* 10 */ "Belegdatum",
            /* 11 */ "Belegfeld 1",
            /* 12 */ "Belegfeld 2",
            /* 13 */ "Skonto",
            /* 14 */ "Buchungstext",
            /* 15 */ "Postensperre",
            /* 16 */ "Diverse Adressnummer",
            /* 17 */ "Geschäftspartnerbank",
            /* 18 */ "Sachverhalt",
            /* 19 */ "Zinssperre",
            /* 20 */ "Beleglink",
            /* 21 */ "Beleginfo - Art 1",
            /* 22 */ "Beleginfo - Inhalt 1",
            /* 23 */ "Beleginfo - Art 2",
            /* 24 */ "Beleginfo - Inhalt 2",
            /* 25 */ "Beleginfo - Art 3",
            /* 26 */ "Beleginfo - Inhalt 3",
            /* 27 */ "Beleginfo - Art 4",
            /* 28 */ "Beleginfo - Inhalt 4",
            /* 29 */ "Beleginfo - Art 5",
            /* 30 */ "Beleginfo - Inhalt 5",
            /* 31 */ "Beleginfo - Art 6",
            /* 32 */ "Beleginfo - Inhalt 6",
            /* 33 */ "Beleginfo - Art 7",
            /* 34 */ "Beleginfo - Inhalt 7",
            /* 35 */ "Beleginfo - Art 8",
            /* 36 */ "Beleginfo - Inhalt 8",
            /* 37 */ "KOST1 - Kostenstelle",
            /* 38 */ "KOST2 - Kostenstelle",
            /* 39 */ "Kost-Menge",
            /* 40 */ "EU-Land u. UStID",
            /* 41 */ "EU-Steuersatz",
            /* 42 */ "Abw. Versteuerungsart",
            /* 43 */ "Sachverhalt L+L",
            /* 44 */ "Funktionsergänzung L+L",
            /* 45 */ "BU 49 Hauptfunktionstyp",
            /* 46 */ "BU 49 Hauptfunktionsnummer",
            /* 47 */ "BU 49 Funktionsergänzung",
            /* 48 */ "Zusatzinformation - Art 1",
            /* 49 */ "Zusatzinformation - Inhalt 1",
            /* 50 */ "Zusatzinformation - Art 2",
            /* 51 */ "Zusatzinformation - Inhalt 2",
            /* 52 */ "Zusatzinformation - Art 3",
            /* 53 */ "Zusatzinformation - Inhalt 3",
            /* 54 */ "Zusatzinformation - Art 4",
            /* 55 */ "Zusatzinformation - Inhalt 4",
            /* 56 */ "Zusatzinformation - Art 5",
            /* 57 */ "Zusatzinformation - Inhalt 5",
            /* 58 */ "Zusatzinformation - Art 6",
            /* 59 */ "Zusatzinformation - Inhalt 6",
            /* 60 */ "Zusatzinformation - Art 7",
            /* 61 */ "Zusatzinformation - Inhalt 7",
            /* 62 */ "Zusatzinformation - Art 8",
            /* 63 */ "Zusatzinformation - Inhalt 8",
            /* 64 */ "Zusatzinformation - Art 9",
            /* 65 */ "Zusatzinformation - Inhalt 9",
            /* 66 */ "Zusatzinformation - Art 10",
            /* 67 */ "Zusatzinformation - Inhalt 10",
            /* 68 */ "Zusatzinformation - Art 11",
            /* 69 */ "Zusatzinformation - Inhalt 11",
            /* 70 */ "Zusatzinformation - Art 12",
            /* 71 */ "Zusatzinformation - Inhalt 12",
            /* 72 */ "Zusatzinformation - Art 13",
            /* 73 */ "Zusatzinformation - Inhalt 13",
            /* 74 */ "Zusatzinformation - Art 14",
            /* 75 */ "Zusatzinformation - Inhalt 14",
            /* 76 */ "Zusatzinformation - Art 15",
            /* 77 */ "Zusatzinformation - Inhalt 15",
            /* 78 */ "Zusatzinformation - Art 16",
            /* 79 */ "Zusatzinformation - Inhalt 16",
            /* 80 */ "Zusatzinformation - Art 17",
            /* 81 */ "Zusatzinformation - Inhalt 17",
            /* 82 */ "Zusatzinformation - Art 18",
            /* 83 */ "Zusatzinformation - Inhalt 18",
            /* 84 */ "Zusatzinformation - Art 19",
            /* 85 */ "Zusatzinformation - Inhalt 19",
            /* 86 */ "Zusatzinformation - Art 20",
            /* 87 */ "Zusatzinformation - Inhalt 20",
            /* 88 */ "Stück",
            /* 89 */ "Gewicht",
            /* 90 */ "Zahlweise",
            /* 91 */ "Forderungsart",
            /* 92 */ "Veranlagungsjahr",
            /* 93 */ "Zugeordnete Fälligkeit",
            /* 94 */ "Skontotyp",
            /* 95 */ "Auftragsnummer",
            /* 96 */ "Buchungstyp",
            /* 97 */ "USt-Schlüssel (Anzahlungen)",
            /* 98 */ "EU-Land (Anzahlungen)",
            /* 99 */ "Sachverhalt L+L (Anzahlungen)",
            /* 100 */ "EU-Steuersatz (Anzahlungen)",
            /* 101 */ "Erlöskonto (Anzahlungen)",
            /* 102 */ "Herkunft-Kz",
            /* 103 */ "Buchungs GUID",
            /* 104 */ "KOST-Datum",
            /* 105 */ "SEPA-Mandatsreferenz",
            /* 106 */ "Skontosperre",
            /* 107 */ "Gesellschaftername",
            /* 108 */ "Beteiligtennummer",
            /* 109 */ "Identifikationsnummer",
            /* 110 */ "Zeichnernummer",
            /* 111 */ "Postensperre bis",
            /* 112 */ "Bezeichnung SoBil-Sachverhalt",
            /* 113 */ "Kennzeichen SoBil-Buchung",
            /* 114 */ "Festschreibung",
            /* 115 */ "Leistungsdatum",
            /* 116 */ "Datum Zuord. Steuerperiode",
            /* 117 */ "Fälligkeit",
            /* 118 */ "Generalumkehr (GU)",
            /* 119 */ "Steuersatz",
            /* 120 */ "Land",
        };

        sb.AppendLine(string.Join(";", columns));
    }

    /// <summary>
    /// Appends a single data row for one JournalEntryLine.
    /// Maps ClarityBoard accounting data to DATEV EXTF Buchungsstapel columns.
    /// </summary>
    private static void AppendDataRow(
        StringBuilder sb,
        JournalEntry entry,
        JournalEntryLine line,
        List<JournalEntryLine> allLines,
        Dictionary<Guid, Account> accounts)
    {
        accounts.TryGetValue(line.AccountId, out var account);

        // Determine Soll/Haben (Debit/Credit) and amount
        var isDebit = line.DebitAmount > 0;
        var amount = isDebit ? line.DebitAmount : line.CreditAmount;
        var sollHaben = isDebit ? "S" : "H";

        // Determine Gegenkonto (counter account):
        // For a two-line entry, the Gegenkonto is the other line's account.
        // For multi-line entries, leave Gegenkonto empty (DATEV handles this via Generalumkehr).
        var gegenkontoNumber = "";
        if (allLines.Count == 2)
        {
            var counterLine = allLines.FirstOrDefault(l => l.Id != line.Id);
            if (counterLine != null && accounts.TryGetValue(counterLine.AccountId, out var counterAccount))
            {
                gegenkontoNumber = counterAccount.AccountNumber;
            }
        }

        // Initialize all columns with empty strings
        var fields = new string[TotalColumns];
        Array.Fill(fields, "");

        // Populate the core booking fields
        fields[0] = FormatDecimal(amount);                                    // 1: Umsatz
        fields[1] = $"\"{sollHaben}\"";                                       // 2: Soll/Haben-Kz
        fields[2] = $"\"{line.Currency}\"";                                   // 3: WKZ Umsatz
        fields[3] = line.ExchangeRate != 1.0m
            ? FormatDecimal(line.ExchangeRate) : "";                          // 4: Kurs
        fields[4] = line.ExchangeRate != 1.0m
            ? FormatDecimal(line.BaseAmount) : "";                            // 5: Basis-Umsatz
        fields[5] = line.ExchangeRate != 1.0m
            ? "\"EUR\"" : "";                                                 // 6: WKZ Basis-Umsatz
        fields[6] = $"\"{account?.AccountNumber ?? ""}\"";                    // 7: Konto
        fields[7] = $"\"{gegenkontoNumber}\"";                                // 8: Gegenkonto
        fields[8] = !string.IsNullOrEmpty(line.VatCode)
            ? $"\"{line.VatCode}\"" : "";                                     // 9: BU-Schlüssel
        fields[9] = $"{entry.EntryDate.Day:D2}{entry.EntryDate.Month:D2}";   // 10: Belegdatum (DDMM)
        fields[10] = $"\"{entry.EntryNumber}\"";                              // 11: Belegfeld 1
        fields[11] = "";                                                      // 12: Belegfeld 2
        fields[12] = "";                                                      // 13: Skonto
        fields[13] = $"\"{EscapeQuotes(TruncateText(entry.Description, 60))}\""; // 14: Buchungstext (max 60 chars)

        // KOST1 - Kostenstelle (column 37, index 36)
        if (!string.IsNullOrEmpty(line.CostCenter))
            fields[ColKost1] = $"\"{line.CostCenter}\"";

        // Buchungs GUID (column 103, index 102) - use the JournalEntryLine ID for traceability
        fields[ColBuchungsGuid] = $"\"{line.Id}\"";

        sb.AppendLine(string.Join(";", fields));
    }

    /// <summary>
    /// Formats a decimal value with comma as decimal separator per DATEV convention.
    /// </summary>
    private static string FormatDecimal(decimal value)
    {
        return value.ToString("F2").Replace(".", ",");
    }

    /// <summary>
    /// Escapes double quotes within a string value for CSV/DATEV compatibility.
    /// Double quotes are escaped by doubling them ("").
    /// </summary>
    private static string EscapeQuotes(string? value)
    {
        if (value == null) return "";
        return value.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Truncates text to the specified maximum length.
    /// DATEV Buchungstext field is limited to 60 characters.
    /// </summary>
    private static string TruncateText(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
