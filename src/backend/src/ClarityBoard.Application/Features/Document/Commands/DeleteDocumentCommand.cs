using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Hr;
using ClarityBoard.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Application.Features.Document.Commands;

public record DeleteDocumentCommand : ICommand, IEntityScoped
{
    public Guid EntityId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
}

public record DeleteDocumentPreflightResult
{
    public bool CanDelete { get; init; }
    public string? BlockReason { get; init; }
    public bool HasBookingSuggestion { get; init; }
    public bool HasJournalEntry { get; init; }
    public bool JournalEntryWillBeReversed { get; init; }
    public int FieldCount { get; init; }
}

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IAccountingHubNotifier _notifier;
    private readonly ILogger<DeleteDocumentCommandHandler> _logger;

    public DeleteDocumentCommandHandler(
        IAppDbContext db,
        IDocumentStorage storage,
        IAccountingRepository accountingRepo,
        IAccountingHubNotifier notifier,
        ILogger<DeleteDocumentCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _accountingRepo = accountingRepo;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct)
            ?? throw new InvalidOperationException("Document not found.");

        // 1. Handle booked journal entry — reverse it if it exists and is posted
        if (document.BookedJournalEntryId.HasValue)
        {
            var journalEntry = await _db.JournalEntries
                .Include(j => j.Lines)
                .FirstOrDefaultAsync(j => j.Id == document.BookedJournalEntryId.Value, ct);

            if (journalEntry is not null)
            {
                if (journalEntry.Status == "posted")
                {
                    // Check if the fiscal period is still open
                    var period = await _db.FiscalPeriods
                        .FirstOrDefaultAsync(p => p.EntityId == request.EntityId
                            && p.Year == (short)journalEntry.EntryDate.Year
                            && p.Month == (short)journalEntry.EntryDate.Month, ct);

                    if (period is not null && (period.Status == "hard_closed" || period.Status == "exported"))
                        throw new InvalidOperationException(
                            $"Cannot delete document: the associated journal entry is in a closed fiscal period ({journalEntry.EntryDate.Year}-{journalEntry.EntryDate.Month:D2}).");

                    // Create reversal entry
                    var nextNumber = await _accountingRepo.GetNextEntryNumberAsync(request.EntityId, ct);
                    var reversal = JournalEntry.Create(
                        request.EntityId,
                        nextNumber,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        $"Storno (Dokumentlöschung): {journalEntry.Description}",
                        journalEntry.FiscalPeriodId,
                        request.UserId,
                        sourceType: "document_deletion",
                        sourceRef: document.Id.ToString());

                    short lineNum = 1;
                    foreach (var line in journalEntry.Lines)
                    {
                        JournalEntryLine reversalLine;
                        if (line.CreditAmount > 0)
                        {
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

                    journalEntry.MarkReversed(reversal.Id);
                    _db.JournalEntries.Add(reversal);
                }

            }
        }

        var dbContext = (DbContext)_db;

        // 2. Clear DocumentId on all journal entries referencing this document
        var linkedJournalEntries = await _db.JournalEntries
            .Where(j => j.DocumentId == document.Id)
            .ToListAsync(ct);
        foreach (var je in linkedJournalEntries)
        {
            dbContext.Entry(je).Property("DocumentId").CurrentValue = null;
        }

        // 3. Clear TravelExpenseItem.ReceiptDocumentId references
        var linkedExpenseItems = await _db.TravelExpenseItems
            .Where(t => t.ReceiptDocumentId == document.Id)
            .ToListAsync(ct);
        foreach (var item in linkedExpenseItems)
        {
            dbContext.Entry(item).Property(nameof(TravelExpenseItem.ReceiptDocumentId)).CurrentValue = null;
        }

        // 4. Delete revenue schedule entries referencing this document
        var revenueEntries = await _db.RevenueScheduleEntries
            .Where(r => r.DocumentId == document.Id)
            .ToListAsync(ct);
        _db.RevenueScheduleEntries.RemoveRange(revenueEntries);

        // 5. Delete cashflow entries referencing this document
        var cashflowEntries = await _db.InvoiceCashflowEntries
            .Where(c => c.DocumentId == document.Id)
            .ToListAsync(ct);
        _db.InvoiceCashflowEntries.RemoveRange(cashflowEntries);

        // 6. Delete booking suggestions
        var bookingSuggestions = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == document.Id)
            .ToListAsync(ct);
        _db.BookingSuggestions.RemoveRange(bookingSuggestions);

        // 7. Delete document fields
        var fields = await _db.DocumentFields
            .Where(f => f.DocumentId == document.Id)
            .ToListAsync(ct);
        _db.DocumentFields.RemoveRange(fields);

        // 8. Delete the document record
        _db.Documents.Remove(document);

        // 9. Save all DB changes in a single transaction (ICommand ensures this)
        await _db.SaveChangesAsync(ct);

        // 10. Delete file from MinIO (after DB commit — non-critical if it fails)
        try
        {
            if (!string.IsNullOrEmpty(document.StoragePath))
            {
                await _storage.DeleteAsync(document.EntityId, document.StoragePath, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file from storage for document {DocumentId}. Orphaned file: {StoragePath}",
                document.Id, document.StoragePath);
        }

        // 11. Notify via SignalR
        try
        {
            await _notifier.NotifyDocumentStatusChangedAsync(request.EntityId, request.DocumentId, "deleted", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR notification for document deletion {DocumentId}", document.Id);
        }
    }
}
