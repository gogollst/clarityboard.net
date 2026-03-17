using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record PostAllDueRevenueEntriesCommand(
    Guid EntityId,
    Guid UserId,
    DateOnly UpToMonth) : ICommand<PostAllDueRevenueEntriesResult>, IEntityScoped;

public record PostAllDueRevenueEntriesResult(int PostedCount);

public class PostAllDueRevenueEntriesCommandValidator : AbstractValidator<PostAllDueRevenueEntriesCommand>
{
    public PostAllDueRevenueEntriesCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UpToMonth).NotEmpty();
    }
}

public class PostAllDueRevenueEntriesCommandHandler
    : IRequestHandler<PostAllDueRevenueEntriesCommand, PostAllDueRevenueEntriesResult>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IJournalEntryHashService _hashService;

    public PostAllDueRevenueEntriesCommandHandler(
        IAppDbContext db,
        IAccountingRepository accountingRepo,
        IJournalEntryHashService hashService)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _hashService = hashService;
    }

    public async Task<PostAllDueRevenueEntriesResult> Handle(
        PostAllDueRevenueEntriesCommand request, CancellationToken ct)
    {
        var plannedEntries = await _db.RevenueScheduleEntries
            .Where(e => e.EntityId == request.EntityId
                && e.Status == "planned"
                && e.PeriodDate <= request.UpToMonth)
            .OrderBy(e => e.PeriodDate)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);

        if (plannedEntries.Count == 0)
            return new PostAllDueRevenueEntriesResult(0);

        // Resolve PRA account (3900)
        var praAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == request.EntityId && a.AccountNumber == "3900", ct)
            ?? throw new InvalidOperationException("PRA account 3900 not found for this entity.");

        // Pre-load all needed revenue accounts
        var revenueAccountNumbers = plannedEntries
            .Select(e => e.RevenueAccountNumber)
            .Distinct()
            .ToList();
        var revenueAccountIds = plannedEntries
            .Where(e => e.RevenueAccountId.HasValue)
            .Select(e => e.RevenueAccountId!.Value)
            .Distinct()
            .ToList();

        var accountsByNumber = await _db.Accounts
            .Where(a => a.EntityId == request.EntityId && revenueAccountNumbers.Contains(a.AccountNumber))
            .ToDictionaryAsync(a => a.AccountNumber, ct);

        var accountsById = await _db.Accounts
            .Where(a => revenueAccountIds.Contains(a.Id) && a.EntityId == request.EntityId)
            .ToDictionaryAsync(a => a.Id, ct);

        // Pre-load fiscal periods for all relevant months
        var periodKeys = plannedEntries
            .Select(e => new { Year = (short)e.PeriodDate.Year, Month = (short)e.PeriodDate.Month })
            .Distinct()
            .ToList();

        var fiscalPeriods = await _db.FiscalPeriods
            .Where(p => p.EntityId == request.EntityId)
            .ToListAsync(ct);

        var periodLookup = fiscalPeriods.ToDictionary(p => (p.Year, p.Month));

        // Get latest hash for chain
        var previousHash = await _db.JournalEntries
            .Where(j => j.EntityId == request.EntityId && j.Status == "posted")
            .OrderByDescending(j => j.EntryNumber)
            .Select(j => j.Hash)
            .FirstOrDefaultAsync(ct)
            ?? "0000000000000000000000000000000000000000000000000000000000000000";

        var nextNumber = await _accountingRepo.GetNextEntryNumberAsync(request.EntityId, ct);
        var postedCount = 0;

        foreach (var entry in plannedEntries)
        {
            // Resolve revenue account
            Account? revenueAccount = null;
            if (entry.RevenueAccountId.HasValue)
                accountsById.TryGetValue(entry.RevenueAccountId.Value, out revenueAccount);
            revenueAccount ??= accountsByNumber.GetValueOrDefault(entry.RevenueAccountNumber);

            if (revenueAccount is null)
                throw new InvalidOperationException(
                    $"Revenue account '{entry.RevenueAccountNumber}' not found for this entity.");

            // Find fiscal period
            var key = ((short)entry.PeriodDate.Year, (short)entry.PeriodDate.Month);
            if (!periodLookup.TryGetValue(key, out var fiscalPeriod))
                throw new InvalidOperationException(
                    $"No fiscal period found for {entry.PeriodDate.Year}-{entry.PeriodDate.Month:D2}.");

            if (fiscalPeriod.Status is "hard_closed" or "exported")
                throw new Domain.Exceptions.ClosedPeriodException(
                    key.Item1, key.Item2, fiscalPeriod.Status);

            // Create journal entry
            var journalEntry = JournalEntry.Create(
                request.EntityId,
                nextNumber++,
                entry.PeriodDate,
                $"Erlösauflösung {entry.PeriodDate:yyyy-MM} – Konto {entry.RevenueAccountNumber}",
                fiscalPeriod.Id,
                request.UserId,
                sourceType: "revenue_schedule",
                sourceRef: entry.Id.ToString(),
                documentId: entry.DocumentId);

            journalEntry.AddLine(JournalEntryLine.CreateDebit(1, praAccount.Id, entry.Amount,
                description: $"PRA Auflösung {entry.PeriodDate:yyyy-MM}"));
            journalEntry.AddLine(JournalEntryLine.CreateCredit(2, revenueAccount.Id, entry.Amount,
                description: $"Erlös {entry.PeriodDate:yyyy-MM}"));

            // Post with hash chain
            var lineData = journalEntry.Lines.Select(l =>
                (l.AccountId, l.DebitAmount, l.CreditAmount));
            var hash = _hashService.ComputeHash(
                journalEntry.Id, journalEntry.EntityId, journalEntry.EntryNumber,
                journalEntry.EntryDate, journalEntry.Description, lineData, previousHash);

            journalEntry.Post(hash, previousHash);
            previousHash = hash;

            _db.JournalEntries.Add(journalEntry);
            entry.MarkBooked(journalEntry.Id);
            postedCount++;
        }

        await _db.SaveChangesAsync(ct);

        return new PostAllDueRevenueEntriesResult(postedCount);
    }
}
