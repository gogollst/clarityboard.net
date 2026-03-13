using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Commands;

public record ModifyBookingSuggestionCommand : IRequest<Guid>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public Guid DebitAccountId { get; init; }
    public Guid CreditAccountId { get; init; }
    public decimal Amount { get; init; }
    public string? VatCode { get; init; }
    public decimal? VatAmount { get; init; }
    public string? Description { get; init; }
    public Guid? HrEmployeeId { get; init; }
    public Guid? TargetEntityId { get; init; }
}

public class ModifyBookingSuggestionCommandHandler : IRequestHandler<ModifyBookingSuggestionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IBookingPatternLearner _patternLearner;

    public ModifyBookingSuggestionCommandHandler(
        IAppDbContext db,
        IAccountingRepository accountingRepo,
        IBookingPatternLearner patternLearner)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _patternLearner = patternLearner;
    }

    public async Task<Guid> Handle(ModifyBookingSuggestionCommand request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");

        var suggestion = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == request.DocumentId && bs.Status == "suggested")
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No pending booking suggestion for document {request.DocumentId}.");

        suggestion.Modify(request.UserId, request.DebitAccountId, request.CreditAccountId,
            request.Amount, request.VatCode, request.VatAmount, request.Description, request.HrEmployeeId);

        // Determine target entity: user override > AI suggestion > uploaded entity
        var targetEntityId = request.TargetEntityId
                             ?? suggestion.SuggestedEntityId
                             ?? request.EntityId;

        // Create journal entry with modified values
        var invoiceDate = document.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == targetEntityId
                && fp.StartDate <= invoiceDate
                && fp.EndDate >= invoiceDate, ct)
            ?? throw new InvalidOperationException(
                $"No fiscal period found for date {invoiceDate}. Please create the fiscal period first.");

        var entryNumber = await _accountingRepo.GetNextEntryNumberAsync(targetEntityId, ct);

        var journalEntry = JournalEntry.Create(
            entityId: targetEntityId,
            entryNumber: entryNumber,
            entryDate: invoiceDate,
            description: request.Description ?? $"Invoice: {document.VendorName} {document.InvoiceNumber}",
            fiscalPeriodId: fiscalPeriod.Id,
            createdBy: request.UserId,
            sourceType: "ai-suggestion-modified",
            sourceRef: suggestion.Id.ToString(),
            documentId: document.Id);

        var debitLine = JournalEntryLine.CreateDebit(
            lineNumber: 1,
            accountId: request.DebitAccountId,
            amount: request.Amount,
            vatCode: request.VatCode,
            vatAmount: request.VatAmount ?? 0,
            description: request.Description,
            hrEmployeeId: request.HrEmployeeId);
        journalEntry.AddLine(debitLine);

        var creditLine = JournalEntryLine.CreateCredit(
            lineNumber: 2,
            accountId: request.CreditAccountId,
            amount: request.Amount,
            vatCode: request.VatCode,
            vatAmount: request.VatAmount ?? 0,
            description: request.Description,
            hrEmployeeId: request.HrEmployeeId);
        journalEntry.AddLine(creditLine);

        await _accountingRepo.AddJournalEntryAsync(journalEntry, ct);

        document.MarkBooked(journalEntry.Id);

        await _patternLearner.LearnFromDecisionAsync(
            targetEntityId, document.VendorName, document.BusinessPartnerId,
            request.DebitAccountId, request.CreditAccountId, request.VatCode, request.HrEmployeeId, ct);

        await _db.SaveChangesAsync(ct);

        return journalEntry.Id;
    }
}

public class ModifyBookingSuggestionCommandValidator : AbstractValidator<ModifyBookingSuggestionCommand>
{
    public ModifyBookingSuggestionCommandValidator()
    {
        RuleFor(x => x.DebitAccountId).NotEmpty();
        RuleFor(x => x.CreditAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
