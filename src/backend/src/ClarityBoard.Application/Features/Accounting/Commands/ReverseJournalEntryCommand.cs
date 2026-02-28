using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.reverse")]
public record ReverseJournalEntryCommand(
    Guid EntityId,
    Guid JournalEntryId,
    string Reason) : ICommand<Guid>, IEntityScoped;

public class ReverseJournalEntryCommandHandler : IRequestHandler<ReverseJournalEntryCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly ICurrentUser _currentUser;

    public ReverseJournalEntryCommandHandler(IAppDbContext db, IAccountingRepository accountingRepo, ICurrentUser currentUser)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(ReverseJournalEntryCommand request, CancellationToken ct)
    {
        var original = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.JournalEntryId && j.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Journal entry '{request.JournalEntryId}' not found.");

        if (original.Status == "reversed")
            throw new InvalidOperationException("This journal entry has already been reversed.");

        // Check period is open
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.EntityId == request.EntityId
                && p.Year == (short)original.EntryDate.Year
                && p.Month == (short)original.EntryDate.Month, ct);

        if (period != null && (period.Status == "hard_closed" || period.Status == "exported"))
            throw new Domain.Exceptions.ClosedPeriodException(
                (short)original.EntryDate.Year, (short)original.EntryDate.Month, period.Status);

        var nextNumber = await _accountingRepo.GetNextEntryNumberAsync(request.EntityId, ct);

        // Create reversal entry with swapped debit/credit
        var reversal = JournalEntry.Create(
            request.EntityId,
            nextNumber,
            DateOnly.FromDateTime(DateTime.UtcNow),
            $"Storno: {original.Description} - {request.Reason}",
            original.FiscalPeriodId,
            _currentUser.UserId,
            sourceType: "reversal",
            sourceRef: original.Id.ToString());

        short lineNum = 1;
        foreach (var line in original.Lines)
        {
            JournalEntryLine reversalLine;
            if (line.CreditAmount > 0)
            {
                // Original credit becomes debit in reversal
                reversalLine = JournalEntryLine.CreateDebit(
                    lineNum++,
                    line.AccountId,
                    line.CreditAmount,
                    line.VatCode,
                    line.VatAmount,
                    line.CostCenter,
                    $"Storno line {line.LineNumber}",
                    line.Currency,
                    line.ExchangeRate);
            }
            else
            {
                // Original debit becomes credit in reversal
                reversalLine = JournalEntryLine.CreateCredit(
                    lineNum++,
                    line.AccountId,
                    line.DebitAmount,
                    line.VatCode,
                    line.VatAmount,
                    line.CostCenter,
                    $"Storno line {line.LineNumber}",
                    line.Currency,
                    line.ExchangeRate);
            }
            reversal.AddLine(reversalLine);
        }

        original.MarkReversed(reversal.Id);
        _db.JournalEntries.Add(reversal);
        await _db.SaveChangesAsync(ct);

        return reversal.Id;
    }
}
