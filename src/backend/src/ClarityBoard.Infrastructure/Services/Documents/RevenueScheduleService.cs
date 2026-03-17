using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Document;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services.Documents;

public interface IRevenueScheduleService
{
    Task<IReadOnlyList<RevenueScheduleEntry>> CreateScheduleAsync(
        Document document,
        BookingSuggestion suggestion,
        BookingSuggestionResult aiResult,
        CancellationToken ct);

    Task<InvoiceCashflowEntry> CreateCashflowEntryAsync(
        Document document,
        string direction,
        CancellationToken ct);
}

public class RevenueScheduleService : IRevenueScheduleService
{
    private readonly IAppDbContext _db;

    public RevenueScheduleService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RevenueScheduleEntry>> CreateScheduleAsync(
        Document document,
        BookingSuggestion suggestion,
        BookingSuggestionResult aiResult,
        CancellationToken ct)
    {
        var entries = new List<RevenueScheduleEntry>();

        // If AI provided a revenue schedule, use it directly
        if (aiResult.RevenueSchedule.Count > 0)
        {
            foreach (var item in aiResult.RevenueSchedule)
            {
                // Skip the immediate entry (month 1) — that's booked as direct revenue
                if (item.IsImmediate)
                    continue;

                var accountId = await ResolveAccountIdAsync(
                    document.EntityId, item.RevenueAccount, ct);

                var entry = RevenueScheduleEntry.Create(
                    entityId: document.EntityId,
                    documentId: document.Id,
                    periodDate: item.PeriodDate,
                    amount: item.Amount,
                    revenueAccountNumber: item.RevenueAccount ?? "4400",
                    revenueAccountId: accountId,
                    bookingSuggestionId: suggestion.Id);

                entries.Add(entry);
            }
        }
        else if (aiResult.ServicePeriodStart.HasValue && aiResult.ServicePeriodEnd.HasValue)
        {
            // Fallback: compute linear day-exact distribution from service period
            entries.AddRange(ComputeLinearSchedule(
                document, suggestion, aiResult.ServicePeriodStart.Value,
                aiResult.ServicePeriodEnd.Value,
                aiResult.DeferredRevenueAccount));
        }

        if (entries.Count > 0)
        {
            _db.RevenueScheduleEntries.AddRange(entries);
            await _db.SaveChangesAsync(ct);
        }

        return entries;
    }

    public async Task<InvoiceCashflowEntry> CreateCashflowEntryAsync(
        Document document,
        string direction,
        CancellationToken ct)
    {
        var dueDate = document.DueDate
                      ?? (document.InvoiceDate.HasValue
                          ? document.InvoiceDate.Value.AddDays(30)
                          : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));

        var entry = InvoiceCashflowEntry.Create(
            entityId: document.EntityId,
            documentId: document.Id,
            expectedDate: dueDate,
            grossAmount: document.TotalAmount ?? 0,
            direction: direction,
            currency: document.Currency ?? "EUR");

        _db.InvoiceCashflowEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        return entry;
    }

    private List<RevenueScheduleEntry> ComputeLinearSchedule(
        Document document,
        BookingSuggestion suggestion,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? revenueAccountNumber)
    {
        var entries = new List<RevenueScheduleEntry>();
        var totalAmount = suggestion.VatAmount.HasValue ? suggestion.Amount - suggestion.VatAmount.Value : suggestion.Amount;
        var accountNumber = revenueAccountNumber ?? "4400";

        // Calculate total days and monthly distribution
        var totalDays = periodEnd.DayNumber - periodStart.DayNumber + 1;
        if (totalDays <= 0) return entries;

        var dailyRate = totalAmount / totalDays;

        // Walk through each month in the period
        var current = periodStart;
        var monthIndex = 0;

        while (current <= periodEnd)
        {
            var monthEnd = new DateOnly(current.Year, current.Month,
                DateTime.DaysInMonth(current.Year, current.Month));
            if (monthEnd > periodEnd)
                monthEnd = periodEnd;

            var daysInThisMonth = monthEnd.DayNumber - current.DayNumber + 1;
            var monthAmount = Math.Round(dailyRate * daysInThisMonth, 2);

            // Skip month 0 (immediate revenue, handled by the main booking)
            if (monthIndex > 0)
            {
                // Use first day of month as period date
                var periodDate = new DateOnly(current.Year, current.Month, 1);

                entries.Add(RevenueScheduleEntry.Create(
                    entityId: document.EntityId,
                    documentId: document.Id,
                    periodDate: periodDate,
                    amount: monthAmount,
                    revenueAccountNumber: accountNumber,
                    bookingSuggestionId: suggestion.Id));
            }

            // Move to next month
            current = monthEnd.AddDays(1);
            monthIndex++;
        }

        return entries;
    }

    private async Task<Guid?> ResolveAccountIdAsync(Guid entityId, string? accountNumber, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(accountNumber)) return null;

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId && a.AccountNumber == accountNumber, ct);

        return account?.Id;
    }
}
