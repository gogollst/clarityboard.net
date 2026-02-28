using System.Text.Json;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs monthly on the 1st at 00:00 UTC.
/// Queries all active RecurringEntry records, evaluates which are due,
/// and creates JournalEntry records from their template lines.
/// </summary>
public sealed class RecurringEntryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringEntryService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public RecurringEntryService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringEntryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringEntryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextFirstOfMonth();
            _logger.LogInformation(
                "RecurringEntryService sleeping until {NextRun} ({Delay})",
                DateTime.UtcNow.Add(delay),
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessRecurringEntriesAsync(stoppingToken);
        }

        _logger.LogInformation("RecurringEntryService stopped");
    }

    /// <summary>
    /// Calculates the delay from now until the next 1st of the month at 00:00 UTC.
    /// If it is currently the 1st at 00:00 UTC (within the first minute), runs immediately.
    /// </summary>
    internal static TimeSpan CalculateDelayUntilNextFirstOfMonth()
    {
        var now = DateTime.UtcNow;

        // If we are on the 1st within the first minute, run immediately
        if (now.Day == 1 && now.Hour == 0 && now.Minute == 0)
            return TimeSpan.Zero;

        // Calculate next 1st of month at 00:00 UTC
        var nextFirst = now.Day == 1 && now.Hour == 0 && now.Minute < 1
            ? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        return nextFirst - now;
    }

    internal async Task ProcessRecurringEntriesAsync(CancellationToken ct)
    {
        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _logger.LogInformation(
            "RecurringEntryService processing for reference date {ReferenceDate}",
            referenceDate);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();

        List<RecurringEntry> activeEntries;
        try
        {
            activeEntries = await context.RecurringEntries
                .Where(r => r.IsActive)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query active recurring entries");
            return;
        }

        _logger.LogInformation("Found {Count} active recurring entries", activeEntries.Count);

        var generatedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var entry in activeEntries)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                if (!entry.IsDueForGeneration(referenceDate))
                {
                    skippedCount++;
                    _logger.LogDebug(
                        "Recurring entry {EntryId} ({Name}) skipped - not due for {ReferenceDate}",
                        entry.Id, entry.Name, referenceDate);
                    continue;
                }

                await GenerateJournalEntryAsync(context, entry, referenceDate, ct);
                generatedCount++;

                _logger.LogInformation(
                    "Generated journal entry from recurring entry {EntryId} ({Name}) for {ReferenceDate}",
                    entry.Id, entry.Name, referenceDate);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "Failed to generate journal entry from recurring entry {EntryId} ({Name})",
                    entry.Id, entry.Name);
            }
        }

        _logger.LogInformation(
            "RecurringEntryService completed: {Generated} generated, {Skipped} skipped, {Failed} failed",
            generatedCount, skippedCount, failedCount);
    }

    private async Task GenerateJournalEntryAsync(
        ClarityBoardContext context,
        RecurringEntry recurringEntry,
        DateOnly referenceDate,
        CancellationToken ct)
    {
        // Deserialize template lines
        var templateLines = JsonSerializer.Deserialize<List<RecurringEntryLineTemplate>>(
            recurringEntry.TemplateLines, JsonOptions);

        if (templateLines is null || templateLines.Count == 0)
        {
            _logger.LogWarning(
                "Recurring entry {EntryId} has no template lines, skipping",
                recurringEntry.Id);
            return;
        }

        // Resolve or create the fiscal period for this month
        var fiscalPeriod = await GetOrCreateFiscalPeriodAsync(
            context, recurringEntry.EntityId, referenceDate, ct);

        // Get the next entry number for this entity
        var nextEntryNumber = await context.JournalEntries
            .Where(j => j.EntityId == recurringEntry.EntityId)
            .MaxAsync(j => (long?)j.EntryNumber, ct) ?? 0;
        nextEntryNumber++;

        // Determine the entry date: use DayOfMonth from the recurring entry, clamped to month length
        var daysInMonth = DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month);
        var entryDay = Math.Min(recurringEntry.DayOfMonth, daysInMonth);
        var entryDate = new DateOnly(referenceDate.Year, referenceDate.Month, entryDay);

        // Create journal entry using the domain factory method
        var journalEntry = JournalEntry.Create(
            entityId: recurringEntry.EntityId,
            entryNumber: nextEntryNumber,
            entryDate: entryDate,
            description: $"[Recurring] {recurringEntry.Name} - {recurringEntry.Description}",
            fiscalPeriodId: fiscalPeriod.Id,
            createdBy: recurringEntry.CreatedBy,
            sourceType: "recurring",
            sourceRef: recurringEntry.Id.ToString());

        // Add lines from template using domain factory methods
        short lineNumber = 1;
        foreach (var template in templateLines)
        {
            JournalEntryLine line = template.Side.ToLowerInvariant() switch
            {
                "debit" => JournalEntryLine.CreateDebit(
                    lineNumber: lineNumber,
                    accountId: template.AccountId,
                    amount: template.Amount,
                    vatCode: template.VatCode,
                    vatAmount: template.VatAmount,
                    costCenter: template.CostCenter,
                    description: template.Description,
                    currency: template.Currency,
                    exchangeRate: template.ExchangeRate),
                "credit" => JournalEntryLine.CreateCredit(
                    lineNumber: lineNumber,
                    accountId: template.AccountId,
                    amount: template.Amount,
                    vatCode: template.VatCode,
                    vatAmount: template.VatAmount,
                    costCenter: template.CostCenter,
                    description: template.Description,
                    currency: template.Currency,
                    exchangeRate: template.ExchangeRate),
                _ => throw new InvalidOperationException(
                    $"Invalid side '{template.Side}' in template line for recurring entry {recurringEntry.Id}")
            };

            journalEntry.AddLine(line);
            lineNumber++;
        }

        // Validate the journal entry is balanced
        if (!journalEntry.IsBalanced())
        {
            _logger.LogWarning(
                "Recurring entry {EntryId} ({Name}) produces an unbalanced journal entry, skipping",
                recurringEntry.Id, recurringEntry.Name);
            return;
        }

        // Persist
        await context.JournalEntries.AddAsync(journalEntry, ct);
        recurringEntry.MarkGenerated(referenceDate);
        await context.SaveChangesAsync(ct);
    }

    private static async Task<FiscalPeriod> GetOrCreateFiscalPeriodAsync(
        ClarityBoardContext context,
        Guid entityId,
        DateOnly referenceDate,
        CancellationToken ct)
    {
        var year = (short)referenceDate.Year;
        var month = (short)referenceDate.Month;

        var fiscalPeriod = await context.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == entityId
                && fp.Year == year
                && fp.Month == month, ct);

        if (fiscalPeriod is not null)
            return fiscalPeriod;

        // Create a new fiscal period if none exists
        fiscalPeriod = FiscalPeriod.Create(entityId, year, month);
        await context.FiscalPeriods.AddAsync(fiscalPeriod, ct);
        await context.SaveChangesAsync(ct);

        return fiscalPeriod;
    }
}
