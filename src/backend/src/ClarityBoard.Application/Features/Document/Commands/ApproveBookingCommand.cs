using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Commands;

public record ApproveBookingCommand : IRequest<Guid>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public Guid? HrEmployeeId { get; init; }
    public Guid? TargetEntityId { get; init; }
}

public class ApproveBookingCommandHandler : IRequestHandler<ApproveBookingCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IBookingPatternLearner _patternLearner;

    public ApproveBookingCommandHandler(IAppDbContext db, IAccountingRepository accountingRepo, IBookingPatternLearner patternLearner)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _patternLearner = patternLearner;
    }

    public async Task<Guid> Handle(ApproveBookingCommand request, CancellationToken ct)
    {
        // Load document
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");

        // Load booking suggestion
        var suggestion = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == request.DocumentId && bs.Status == "suggested")
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No pending booking suggestion for document {request.DocumentId}.");

        // Determine target entity: user override > AI suggestion > uploaded entity
        var targetEntityId = request.TargetEntityId
                             ?? suggestion.SuggestedEntityId
                             ?? request.EntityId;

        // Find or create fiscal period for the invoice date
        var invoiceDate = document.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == targetEntityId
                && fp.StartDate <= invoiceDate
                && fp.EndDate >= invoiceDate, ct)
            ?? throw new InvalidOperationException(
                $"No fiscal period found for date {invoiceDate}. Please create the fiscal period first.");

        // Get next entry number
        var entryNumber = await _accountingRepo.GetNextEntryNumberAsync(targetEntityId, ct);

        // Create JournalEntry from BookingSuggestion
        var journalEntry = JournalEntry.Create(
            entityId: targetEntityId,
            entryNumber: entryNumber,
            entryDate: invoiceDate,
            description: suggestion.Description ?? $"Invoice: {document.VendorName} {document.InvoiceNumber}",
            fiscalPeriodId: fiscalPeriod.Id,
            createdBy: request.UserId,
            sourceType: "ai-suggestion",
            sourceRef: suggestion.Id.ToString(),
            documentId: document.Id);

        // Add debit line
        var debitLine = JournalEntryLine.CreateDebit(
            lineNumber: 1,
            accountId: suggestion.DebitAccountId,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: suggestion.VatAmount ?? 0,
            description: suggestion.Description,
            hrEmployeeId: request.HrEmployeeId);
        journalEntry.AddLine(debitLine);

        // Add credit line
        var creditLine = JournalEntryLine.CreateCredit(
            lineNumber: 2,
            accountId: suggestion.CreditAccountId,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: suggestion.VatAmount ?? 0,
            description: suggestion.Description,
            hrEmployeeId: request.HrEmployeeId);
        journalEntry.AddLine(creditLine);

        // Save journal entry
        await _accountingRepo.AddJournalEntryAsync(journalEntry, ct);

        // Accept the booking suggestion
        suggestion.Accept(request.UserId, request.HrEmployeeId);

        // Mark document as booked
        document.MarkBooked(journalEntry.Id);

        await _db.SaveChangesAsync(ct);

        // Learn from the decision for recurring patterns
        await _patternLearner.LearnFromDecisionAsync(
            targetEntityId, document.VendorName, document.BusinessPartnerId,
            suggestion.DebitAccountId, suggestion.CreditAccountId, suggestion.VatCode,
            request.HrEmployeeId, ct);

        await _db.SaveChangesAsync(ct);

        return journalEntry.Id;
    }
}
