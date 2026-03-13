using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Accounting.DTOs;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record UpdateJournalEntryCommand : IRequest<JournalEntryDto>
{
    public required Guid Id { get; init; }
    public required Guid EntityId { get; init; }
    public required DateOnly EntryDate { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<CreateJournalEntryLineRequest> Lines { get; init; }
}

public class UpdateJournalEntryCommandValidator : AbstractValidator<UpdateJournalEntryCommand>
{
    public UpdateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(lines =>
        {
            var totalDebit = lines.Sum(l => l.DebitAmount);
            var totalCredit = lines.Sum(l => l.CreditAmount);
            return totalDebit == totalCredit && totalDebit > 0;
        }).WithMessage("Journal entry must be balanced (total debits must equal total credits).");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.DebitAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l).Must(l => !(l.DebitAmount > 0 && l.CreditAmount > 0))
                .WithMessage("A line cannot have both debit and credit amounts.");
        });
    }
}

public class UpdateJournalEntryCommandHandler : IRequestHandler<UpdateJournalEntryCommand, JournalEntryDto>
{
    private readonly IAppDbContext _db;

    public UpdateJournalEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<JournalEntryDto> Handle(
        UpdateJournalEntryCommand request, CancellationToken ct)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.Id && j.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Journal entry '{request.Id}' not found.");

        // Update header (throws if not draft)
        entry.UpdateDraft(request.EntryDate, request.Description);

        // Remove existing lines
        var existingLines = await _db.JournalEntryLines
            .Where(l => l.JournalEntryId == entry.Id)
            .ToListAsync(ct);
        _db.JournalEntryLines.RemoveRange(existingLines);
        entry.ClearLines();

        // Add new lines
        short lineNumber = 1;
        foreach (var line in request.Lines)
        {
            JournalEntryLine entryLine;
            if (line.DebitAmount > 0)
            {
                entryLine = JournalEntryLine.CreateDebit(
                    lineNumber++, line.AccountId, line.DebitAmount,
                    line.VatCode, line.VatAmount ?? 0, line.CostCenter,
                    line.Description, line.Currency, line.ExchangeRate);
            }
            else
            {
                entryLine = JournalEntryLine.CreateCredit(
                    lineNumber++, line.AccountId, line.CreditAmount,
                    line.VatCode, line.VatAmount ?? 0, line.CostCenter,
                    line.Description, line.Currency, line.ExchangeRate);
            }
            entry.AddLine(entryLine);
        }

        await _db.SaveChangesAsync(ct);

        // Load account info for response
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        return new JournalEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            PostingDate = entry.PostingDate,
            Description = entry.Description,
            Status = entry.Status,
            SourceType = entry.SourceType,
            CreatedAt = entry.CreatedAt,
            Lines = entry.Lines.Select(l => new JournalEntryLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                AccountId = l.AccountId,
                AccountNumber = accounts.TryGetValue(l.AccountId, out var acc) ? acc.AccountNumber : "",
                AccountName = acc?.Name ?? "",
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Currency = l.Currency,
                VatCode = l.VatCode,
                VatAmount = l.VatAmount,
                CostCenter = l.CostCenter,
                Description = l.Description,
            }).ToList(),
        };
    }
}
