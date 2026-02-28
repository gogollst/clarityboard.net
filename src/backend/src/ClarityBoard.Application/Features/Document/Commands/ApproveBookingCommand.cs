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
}

public class ApproveBookingCommandHandler : IRequestHandler<ApproveBookingCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;

    public ApproveBookingCommandHandler(IAppDbContext db, IAccountingRepository accountingRepo)
    {
        _db = db;
        _accountingRepo = accountingRepo;
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

        // Find or create fiscal period for the invoice date
        var invoiceDate = document.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == request.EntityId
                && fp.StartDate <= invoiceDate
                && fp.EndDate >= invoiceDate, ct)
            ?? throw new InvalidOperationException(
                $"No fiscal period found for date {invoiceDate}. Please create the fiscal period first.");

        // Get next entry number
        var entryNumber = await _accountingRepo.GetNextEntryNumberAsync(request.EntityId, ct);

        // Create JournalEntry from BookingSuggestion
        var journalEntry = JournalEntry.Create(
            entityId: request.EntityId,
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
            description: suggestion.Description);
        journalEntry.AddLine(debitLine);

        // Add credit line
        var creditLine = JournalEntryLine.CreateCredit(
            lineNumber: 2,
            accountId: suggestion.CreditAccountId,
            amount: suggestion.Amount,
            vatCode: suggestion.VatCode,
            vatAmount: suggestion.VatAmount ?? 0,
            description: suggestion.Description);
        journalEntry.AddLine(creditLine);

        // Save journal entry
        await _accountingRepo.AddJournalEntryAsync(journalEntry, ct);

        // Accept the booking suggestion
        suggestion.Accept(request.UserId);

        // Mark document as booked
        document.MarkBooked(journalEntry.Id);

        await _db.SaveChangesAsync(ct);

        return journalEntry.Id;
    }
}
