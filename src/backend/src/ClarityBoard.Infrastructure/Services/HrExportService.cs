using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Exports approved/reimbursed travel expense items to a DATEV-compatible CSV file.
/// Format: semicolon-separated, German decimal comma, UTF-8 with BOM (required for German Excel compatibility).
/// </summary>
public class HrExportService : IHrExportService
{
    private readonly IAppDbContext _db;

    public HrExportService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportTravelExpensesCsvAsync(
        Guid entityId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Load all approved/reimbursed items for the entity in the date range
        var approvedStatuses = new[] { TravelExpenseStatus.Approved, TravelExpenseStatus.Reimbursed };

        var rows = await _db.TravelExpenseItems
            .Join(_db.TravelExpenseReports,
                  item   => item.ReportId,
                  report => report.Id,
                  (item, report) => new { item, report })
            .Join(_db.Employees,
                  x        => x.report.EmployeeId,
                  employee => employee.Id,
                  (x, employee) => new { x.item, x.report, employee })
            .Where(x => approvedStatuses.Contains(x.report.Status)
                     && x.item.ExpenseDate >= from
                     && x.item.ExpenseDate <= to
                     && x.employee.EntityId == entityId)
            .OrderBy(x => x.employee.LastName)
            .ThenBy(x => x.employee.FirstName)
            .ThenBy(x => x.item.ExpenseDate)
            .Select(x => new
            {
                ReportId             = x.report.Id,
                ExpenseDate          = x.item.ExpenseDate,
                ExpenseType          = x.item.ExpenseType,
                AmountCents          = x.item.AmountCents,
                OriginalCurrencyCode = x.item.OriginalCurrencyCode,
                OriginalAmountCents  = x.item.OriginalAmountCents,
                ExchangeRate         = x.item.ExchangeRate,
                Description          = x.item.Description,
                EmployeeFullName     = x.employee.FirstName + " " + x.employee.LastName,
                BusinessPurpose      = x.report.BusinessPurpose,
                IsDeductible         = x.item.IsDeductible,
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("BelegNr;Datum;Belegart;Betrag (EUR);Währung Original;Betrag Original;Kurs;Beschreibung;Mitarbeiter;Reisezweck;Abzugsfähig");

        foreach (var row in rows)
        {
            var fields = new[]
            {
                EscapeCsv(row.ReportId.ToString("N")[..8].ToUpperInvariant()),
                row.ExpenseDate.ToString("dd.MM.yyyy"),
                EscapeCsv(row.ExpenseType.ToString()),
                FormatDecimal(row.AmountCents / 100m),
                EscapeCsv(row.OriginalCurrencyCode),
                FormatDecimal(row.OriginalAmountCents / 100m),
                FormatDecimal(row.ExchangeRate),
                EscapeCsv(row.Description),
                EscapeCsv(row.EmployeeFullName),
                EscapeCsv(row.BusinessPurpose),
                row.IsDeductible ? "Ja" : "Nein",
            };

            sb.AppendLine(string.Join(";", fields));
        }

        // UTF-8 with BOM for German Excel compatibility
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Formats a decimal value using German convention (comma as decimal separator).
    /// </summary>
    private static string FormatDecimal(decimal value)
        => value.ToString("F2").Replace(".", ",");

    /// <summary>
    /// Wraps a CSV field in quotes and escapes internal double quotes.
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
