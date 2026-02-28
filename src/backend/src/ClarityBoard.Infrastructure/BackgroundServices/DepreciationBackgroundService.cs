using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Services;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs monthly on the 1st at 01:00 UTC.
/// For each entity, finds active fixed assets due for depreciation and
/// creates DepreciationSchedule entries with corresponding JournalEntries.
/// </summary>
public sealed class DepreciationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DepreciationBackgroundService> _logger;

    public DepreciationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DepreciationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DepreciationBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            _logger.LogInformation(
                "DepreciationBackgroundService sleeping until {NextRun} ({Delay})",
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

            await ProcessDepreciationAsync(stoppingToken);
        }

        _logger.LogInformation("DepreciationBackgroundService stopped");
    }

    /// <summary>
    /// Calculates the delay from now until the next 1st of the month at 01:00 UTC.
    /// </summary>
    internal static TimeSpan CalculateDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;

        // If we are on the 1st at 01:00 within the first minute, run immediately
        if (now.Day == 1 && now.Hour == 1 && now.Minute == 0)
            return TimeSpan.Zero;

        // Calculate next 1st of month at 01:00 UTC
        var targetHour = 1;
        var nextFirst = now.Day == 1 && now.Hour < targetHour
            ? new DateTime(now.Year, now.Month, 1, targetHour, 0, 0, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month, 1, targetHour, 0, 0, DateTimeKind.Utc).AddMonths(1);

        return nextFirst - now;
    }

    internal async Task ProcessDepreciationAsync(CancellationToken ct)
    {
        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodDate = new DateOnly(referenceDate.Year, referenceDate.Month, 1);

        _logger.LogInformation(
            "DepreciationBackgroundService processing for period {PeriodDate}", periodDate);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();
        var depreciationService = scope.ServiceProvider.GetRequiredService<IDepreciationService>();

        List<Domain.Entities.Asset.FixedAsset> activeAssets;
        try
        {
            activeAssets = await context.FixedAssets
                .Where(a => a.Status == "active")
                .Include(a => a.Schedules)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query active fixed assets");
            return;
        }

        _logger.LogInformation("Found {Count} active fixed assets", activeAssets.Count);

        var processedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        // Group by entity for journal entry creation
        var assetsByEntity = activeAssets.GroupBy(a => a.EntityId);

        foreach (var entityGroup in assetsByEntity)
        {
            if (ct.IsCancellationRequested)
                break;

            var entityId = entityGroup.Key;

            foreach (var asset in entityGroup)
            {
                try
                {
                    // Check if depreciation for this period already exists
                    var alreadyPosted = asset.Schedules.Any(
                        s => s.PeriodDate == periodDate && s.IsPosted);

                    if (alreadyPosted)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Calculate the full schedule to find this month's entry
                    var fullSchedule = depreciationService.CalculateSchedule(asset);
                    var monthEntry = fullSchedule.FirstOrDefault(s => s.PeriodDate == periodDate);

                    if (monthEntry is null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Get or create fiscal period
                    var fiscalPeriod = await GetOrCreateFiscalPeriodAsync(
                        context, entityId, referenceDate, ct);

                    // Get next entry number
                    var nextEntryNumber = await context.JournalEntries
                        .Where(j => j.EntityId == entityId)
                        .MaxAsync(j => (long?)j.EntryNumber, ct) ?? 0;
                    nextEntryNumber++;

                    // Create journal entry for depreciation
                    var journalEntry = JournalEntry.Create(
                        entityId: entityId,
                        entryNumber: nextEntryNumber,
                        entryDate: periodDate,
                        description: $"[Depreciation] {asset.Name} ({asset.AssetNumber}) - {periodDate:yyyy-MM}",
                        fiscalPeriodId: fiscalPeriod.Id,
                        createdBy: Guid.Empty, // System-generated
                        sourceType: "depreciation",
                        sourceRef: asset.Id.ToString());

                    // Debit depreciation expense, credit accumulated depreciation
                    journalEntry.AddLine(JournalEntryLine.CreateDebit(
                        lineNumber: 1,
                        accountId: asset.DepreciationAccountId,
                        amount: monthEntry.DepreciationAmount,
                        description: $"Depreciation - {asset.Name}"));

                    journalEntry.AddLine(JournalEntryLine.CreateCredit(
                        lineNumber: 2,
                        accountId: asset.AssetAccountId,
                        amount: monthEntry.DepreciationAmount,
                        description: $"Acc. depreciation - {asset.Name}"));

                    await context.JournalEntries.AddAsync(journalEntry, ct);

                    // Create and post the depreciation schedule entry
                    var scheduleEntry = Domain.Entities.Asset.DepreciationSchedule.Create(
                        asset.Id,
                        periodDate,
                        monthEntry.DepreciationAmount,
                        monthEntry.AccumulatedAmount,
                        monthEntry.BookValueAfter);

                    scheduleEntry.MarkPosted(journalEntry.Id);
                    await context.DepreciationSchedules.AddAsync(scheduleEntry, ct);

                    // Update asset accumulated depreciation
                    asset.RecordDepreciation(monthEntry.DepreciationAmount);

                    processedCount++;

                    _logger.LogInformation(
                        "Processed depreciation for asset {AssetId} ({AssetName}): {Amount} EUR",
                        asset.Id, asset.Name, monthEntry.DepreciationAmount);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex,
                        "Failed to process depreciation for asset {AssetId} ({AssetName})",
                        asset.Id, asset.Name);
                }
            }
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save depreciation batch");
        }

        _logger.LogInformation(
            "DepreciationBackgroundService completed: {Processed} processed, {Skipped} skipped, {Failed} failed",
            processedCount, skippedCount, failedCount);
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

        fiscalPeriod = FiscalPeriod.Create(entityId, year, month);
        await context.FiscalPeriods.AddAsync(fiscalPeriod, ct);
        await context.SaveChangesAsync(ct);

        return fiscalPeriod;
    }
}
