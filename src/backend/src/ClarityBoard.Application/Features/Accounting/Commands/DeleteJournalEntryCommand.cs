using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.delete")]
public record DeleteJournalEntryCommand(
    Guid EntityId,
    Guid JournalEntryId) : ICommand, IEntityScoped;

public class DeleteJournalEntryCommandHandler : IRequestHandler<DeleteJournalEntryCommand>
{
    private readonly IAppDbContext _db;

    public DeleteJournalEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteJournalEntryCommand request, CancellationToken ct)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.JournalEntryId && j.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Journal entry '{request.JournalEntryId}' not found.");

        if (entry.Status != "draft")
            throw new InvalidOperationException("Only draft journal entries can be deleted.");

        // Clear FK references on dependent entities
        var documents = await _db.Documents
            .Where(d => d.BookedJournalEntryId == entry.Id)
            .ToListAsync(ct);
        foreach (var doc in documents)
            doc.ClearJournalEntry();

        var revenueEntries = await _db.RevenueScheduleEntries
            .Where(r => r.JournalEntryId == entry.Id)
            .ToListAsync(ct);
        foreach (var r in revenueEntries)
            r.ClearJournalEntry();

        var cashflowEntries = await _db.InvoiceCashflowEntries
            .Where(c => c.JournalEntryId == entry.Id)
            .ToListAsync(ct);
        foreach (var c in cashflowEntries)
            c.ClearJournalEntry();

        // Remove lines, then entry
        _db.JournalEntryLines.RemoveRange(entry.Lines);
        _db.JournalEntries.Remove(entry);

        await _db.SaveChangesAsync(ct);
    }
}
