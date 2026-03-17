using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record PostRevenueScheduleEntryCommand(
    Guid EntityId,
    Guid EntryId,
    Guid UserId) : ICommand<PostRevenueScheduleEntryResult>, IEntityScoped;

public record PostRevenueScheduleEntryResult(Guid JournalEntryId, long EntryNumber);

public class PostRevenueScheduleEntryCommandValidator : AbstractValidator<PostRevenueScheduleEntryCommand>
{
    public PostRevenueScheduleEntryCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class PostRevenueScheduleEntryCommandHandler
    : IRequestHandler<PostRevenueScheduleEntryCommand, PostRevenueScheduleEntryResult>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IJournalEntryHashService _hashService;

    public PostRevenueScheduleEntryCommandHandler(
        IAppDbContext db,
        IAccountingRepository accountingRepo,
        IJournalEntryHashService hashService)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _hashService = hashService;
    }

    public async Task<PostRevenueScheduleEntryResult> Handle(
        PostRevenueScheduleEntryCommand request, CancellationToken ct)
    {
        var entry = await _db.RevenueScheduleEntries
            .FirstOrDefaultAsync(e => e.Id == request.EntryId && e.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException("RevenueScheduleEntry", request.EntryId);

        if (entry.Status != "planned")
            throw new InvalidOperationException(
                $"Revenue schedule entry is in status '{entry.Status}', expected 'planned'.");

        // Resolve PRA account (3900) and revenue account
        var praAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == request.EntityId && a.AccountNumber == "3900", ct)
            ?? throw new InvalidOperationException("PRA account 3900 not found for this entity.");

        var revenueAccount = entry.RevenueAccountId.HasValue
            ? await _db.Accounts.FirstOrDefaultAsync(a => a.Id == entry.RevenueAccountId && a.EntityId == request.EntityId, ct)
            : await _db.Accounts.FirstOrDefaultAsync(a => a.EntityId == request.EntityId && a.AccountNumber == entry.RevenueAccountNumber, ct);

        if (revenueAccount is null)
            throw new InvalidOperationException(
                $"Revenue account '{entry.RevenueAccountNumber}' not found for this entity.");

        // Find fiscal period for the entry's PeriodDate
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p =>
                p.EntityId == request.EntityId
                && p.Year == (short)entry.PeriodDate.Year
                && p.Month == (short)entry.PeriodDate.Month, ct)
            ?? throw new InvalidOperationException(
                $"No fiscal period found for {entry.PeriodDate.Year}-{entry.PeriodDate.Month:D2}.");

        if (fiscalPeriod.Status is "hard_closed" or "exported")
            throw new Domain.Exceptions.ClosedPeriodException(
                (short)entry.PeriodDate.Year, (short)entry.PeriodDate.Month, fiscalPeriod.Status);

        // Create journal entry: Soll 3900 (PRA) an Haben revenue account
        var nextNumber = await _accountingRepo.GetNextEntryNumberAsync(request.EntityId, ct);

        var journalEntry = JournalEntry.Create(
            request.EntityId,
            nextNumber,
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
        var previousHash = await _db.JournalEntries
            .Where(j => j.EntityId == request.EntityId && j.Status == "posted")
            .OrderByDescending(j => j.EntryNumber)
            .Select(j => j.Hash)
            .FirstOrDefaultAsync(ct)
            ?? "0000000000000000000000000000000000000000000000000000000000000000";

        var lineData = journalEntry.Lines.Select(l =>
            (l.AccountId, l.DebitAmount, l.CreditAmount));
        var hash = _hashService.ComputeHash(
            journalEntry.Id, journalEntry.EntityId, journalEntry.EntryNumber,
            journalEntry.EntryDate, journalEntry.Description, lineData, previousHash);

        journalEntry.Post(hash, previousHash);

        _db.JournalEntries.Add(journalEntry);
        entry.MarkBooked(journalEntry.Id);

        await _db.SaveChangesAsync(ct);

        return new PostRevenueScheduleEntryResult(journalEntry.Id, journalEntry.EntryNumber);
    }
}
