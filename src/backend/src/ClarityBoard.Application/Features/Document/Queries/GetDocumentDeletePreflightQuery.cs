using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Document.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Queries;

public record GetDocumentDeletePreflightQuery(Guid EntityId, Guid DocumentId)
    : IRequest<DeleteDocumentPreflightResult?>, IEntityScoped;

public class GetDocumentDeletePreflightQueryHandler
    : IRequestHandler<GetDocumentDeletePreflightQuery, DeleteDocumentPreflightResult?>
{
    private readonly IAppDbContext _db;

    public GetDocumentDeletePreflightQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DeleteDocumentPreflightResult?> Handle(
        GetDocumentDeletePreflightQuery request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct);

        if (document is null)
            return null;

        var fieldCount = await _db.DocumentFields
            .CountAsync(f => f.DocumentId == document.Id, ct);

        var hasBookingSuggestion = await _db.BookingSuggestions
            .AnyAsync(bs => bs.DocumentId == document.Id, ct);

        var hasJournalEntry = document.BookedJournalEntryId.HasValue;
        var journalEntryWillBeReversed = false;
        string? blockReason = null;

        if (hasJournalEntry)
        {
            var journalEntry = await _db.JournalEntries
                .FirstOrDefaultAsync(j => j.Id == document.BookedJournalEntryId!.Value, ct);

            if (journalEntry is not null && journalEntry.Status == "posted")
            {
                // Check if the period is closed
                var period = await _db.FiscalPeriods
                    .FirstOrDefaultAsync(p => p.EntityId == request.EntityId
                        && p.Year == (short)journalEntry.EntryDate.Year
                        && p.Month == (short)journalEntry.EntryDate.Month, ct);

                if (period is not null && (period.Status == "hard_closed" || period.Status == "exported"))
                {
                    blockReason = $"closed_period:{journalEntry.EntryDate.Year}-{journalEntry.EntryDate.Month:D2}";
                }
                else
                {
                    journalEntryWillBeReversed = true;
                }
            }
            else if (journalEntry is not null && journalEntry.Status == "reversed")
            {
                // Already reversed — no further action needed
                journalEntryWillBeReversed = false;
            }
        }

        return new DeleteDocumentPreflightResult
        {
            CanDelete = blockReason is null,
            BlockReason = blockReason,
            HasBookingSuggestion = hasBookingSuggestion,
            HasJournalEntry = hasJournalEntry,
            JournalEntryWillBeReversed = journalEntryWillBeReversed,
            FieldCount = fieldCount,
        };
    }
}
