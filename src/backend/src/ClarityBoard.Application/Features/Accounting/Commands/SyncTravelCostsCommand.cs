using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record SyncTravelCostsCommand : IRequest<SyncTravelCostsResult>
{
    public required Guid EntityId { get; init; }
    public required DateOnly FromDate { get; init; }
    public required DateOnly ToDate { get; init; }
}

public record SyncTravelCostsResult(int SyncedCount, int SkippedCount, List<Guid> JournalEntryIds, List<string> Errors);

public class SyncTravelCostsCommandValidator : AbstractValidator<SyncTravelCostsCommand>
{
    public SyncTravelCostsCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}

[RequirePermission("accounting.post")]
public class SyncTravelCostsCommandHandler : IRequestHandler<SyncTravelCostsCommand, SyncTravelCostsResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccountingHubNotifier _notifier;

    public SyncTravelCostsCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IAccountingHubNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<SyncTravelCostsResult> Handle(
        SyncTravelCostsCommand request, CancellationToken cancellationToken)
    {
        // TravelExpenseReport has no EntityId directly; join through Employee to scope by entity
        var approvedReports = await _db.TravelExpenseReports
            .Join(_db.Employees,
                r => r.EmployeeId,
                e => e.Id,
                (r, e) => new { Report = r, Employee = e })
            .Where(x =>
                x.Employee.EntityId == request.EntityId &&
                x.Report.Status == TravelExpenseStatus.Approved &&
                x.Report.TripStartDate >= request.FromDate &&
                x.Report.TripEndDate <= request.ToDate)
            .Select(x => x.Report)
            .ToListAsync(cancellationToken);

        var syncedCount = 0;
        var skippedCount = 0;
        var journalEntryIds = new List<Guid>();
        var errors = new List<string>();

        foreach (var report in approvedReports)
        {
            // Deduplication check
            var sourceRef = $"hr:travel:{report.Id}";
            var alreadySynced = await _db.JournalEntries
                .AnyAsync(je =>
                    je.EntityId == request.EntityId &&
                    je.SourceRef == sourceRef &&
                    je.Status != "reversed", cancellationToken);

            if (alreadySynced)
            {
                skippedCount++;
                continue;
            }

            try
            {
                // Find active fiscal period
                var entryDate = DateOnly.FromDateTime(DateTime.UtcNow);
                var period = await _db.FiscalPeriods
                    .FirstOrDefaultAsync(fp =>
                        fp.EntityId == request.EntityId &&
                        fp.StartDate <= entryDate &&
                        fp.EndDate >= entryDate &&
                        fp.Status == "open", cancellationToken);

                if (period is null)
                {
                    errors.Add($"Report {report.Id}: No open fiscal period found.");
                    continue;
                }

                // Find accounts (6650 = Reisekosten AN, 3300 = Verbindlichkeiten)
                var travelAccount = await _db.Accounts
                    .FirstOrDefaultAsync(a =>
                        a.EntityId == request.EntityId &&
                        a.AccountNumber == "6650" && a.IsActive, cancellationToken);

                var liabilityAccount = await _db.Accounts
                    .FirstOrDefaultAsync(a =>
                        a.EntityId == request.EntityId &&
                        a.AccountNumber == "3300" && a.IsActive, cancellationToken);

                if (travelAccount is null || liabilityAccount is null)
                {
                    errors.Add($"Report {report.Id}: Required accounts 6650 or 3300 not found.");
                    continue;
                }

                // Get next entry number
                var lastEntryNumber = await _db.JournalEntries
                    .Where(je => je.EntityId == request.EntityId)
                    .MaxAsync(je => (long?)je.EntryNumber, cancellationToken) ?? 0;

                // Amount in EUR decimal (convert from cents)
                var amountDecimal = report.TotalAmountCents / 100m;

                var entry = JournalEntry.Create(
                    entityId: request.EntityId,
                    entryNumber: lastEntryNumber + 1,
                    entryDate: entryDate,
                    description: $"Reisekosten {report.Title}",
                    fiscalPeriodId: period.Id,
                    createdBy: _currentUser.UserId,
                    sourceType: "HrTravel",
                    sourceRef: sourceRef,
                    documentRef: report.Id.ToString()[..8].ToUpperInvariant());

                // Find cost center for this employee
                var costCenter = await _db.CostCenters
                    .FirstOrDefaultAsync(cc =>
                        cc.HrEmployeeId == report.EmployeeId &&
                        cc.EntityId == request.EntityId && cc.IsActive, cancellationToken);

                // Debit: Reisekosten (6650)
                entry.AddLine(JournalEntryLine.CreateDebit(
                    lineNumber: 1,
                    accountId: travelAccount.Id,
                    amount: amountDecimal,
                    costCenterId: costCenter?.Id,
                    hrEmployeeId: report.EmployeeId,
                    hrTravelExpenseId: report.Id));

                // Credit: Verbindlichkeiten (3300)
                entry.AddLine(JournalEntryLine.CreateCredit(
                    lineNumber: 2,
                    accountId: liabilityAccount.Id,
                    amount: amountDecimal,
                    hrTravelExpenseId: report.Id));

                _db.JournalEntries.Add(entry);
                await _db.SaveChangesAsync(cancellationToken);

                journalEntryIds.Add(entry.Id);
                syncedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Report {report.Id}: {ex.Message}");
            }
        }

        if (journalEntryIds.Count > 0)
        {
            await _notifier.NotifyJournalEntryCreatedAsync(request.EntityId, journalEntryIds.Last(), cancellationToken);
        }

        return new SyncTravelCostsResult(syncedCount, skippedCount, journalEntryIds, errors);
    }
}
