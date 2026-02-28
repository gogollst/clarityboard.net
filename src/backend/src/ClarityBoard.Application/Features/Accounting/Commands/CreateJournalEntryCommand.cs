using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Accounting.DTOs;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record CreateJournalEntryCommand : IRequest<JournalEntryDto>
{
    public required DateOnly EntryDate { get; init; }
    public required string Description { get; init; }
    public Guid? DocumentId { get; init; }
    public string? SourceType { get; init; }
    public string? SourceRef { get; init; }
    public required IReadOnlyList<CreateJournalEntryLineRequest> Lines { get; init; }
}

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
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

public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, JournalEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateJournalEntryCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<JournalEntryDto> Handle(
        CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        // Get fiscal period
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == entityId &&
                fp.StartDate <= request.EntryDate &&
                fp.EndDate >= request.EntryDate &&
                fp.Status == "open",
                cancellationToken)
            ?? throw new InvalidOperationException($"No open fiscal period found for date {request.EntryDate}.");

        // Get next entry number
        var lastEntryNumber = await _db.JournalEntries
            .Where(je => je.EntityId == entityId)
            .MaxAsync(je => (long?)je.EntryNumber, cancellationToken) ?? 0;

        var entry = JournalEntry.Create(
            entityId, lastEntryNumber + 1, request.EntryDate,
            request.Description, fiscalPeriod.Id, _currentUser.UserId,
            request.SourceType, request.SourceRef, request.DocumentId);

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

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        // Load account info for response
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

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
