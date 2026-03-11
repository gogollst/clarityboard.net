using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record PostJournalEntryCommand : IRequest<PostJournalEntryResult>
{
    public required Guid JournalEntryId { get; init; }
}

public record PostJournalEntryResult(long EntryNumber, string Hash);

public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
{
    public PostJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId).NotEmpty();
    }
}

[RequirePermission("accounting.post")]
public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, PostJournalEntryResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IJournalEntryHashService _hashService;
    private readonly IAccountingHubNotifier _notifier;

    public PostJournalEntryCommandHandler(
        IAppDbContext db, ICurrentUser currentUser,
        IJournalEntryHashService hashService, IAccountingHubNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _hashService = hashService;
        _notifier = notifier;
    }

    public async Task<PostJournalEntryResult> Handle(
        PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var entry = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e =>
                e.Id == request.JournalEntryId &&
                e.EntityId == entityId, cancellationToken)
            ?? throw new NotFoundException("JournalEntry", request.JournalEntryId);

        if (entry.Status != "draft")
            throw new InvalidOperationException($"Entry is already in status '{entry.Status}'.");

        if (!entry.IsBalanced())
            throw new InvalidOperationException("Journal entry is not balanced.");

        // Get previous hash for SHA-256 chain
        var previousHash = await _db.JournalEntries
            .Where(e => e.EntityId == entityId && e.Status == "posted")
            .OrderByDescending(e => e.EntryNumber)
            .Select(e => e.Hash)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "0000000000000000000000000000000000000000000000000000000000000000";

        // Compute hash
        var lineData = entry.Lines.Select(l =>
            (l.AccountId, l.DebitAmount, l.CreditAmount));
        var hash = _hashService.ComputeHash(
            entry.Id, entry.EntityId, entry.EntryNumber,
            entry.EntryDate, entry.Description, lineData, previousHash);

        entry.Post(hash, previousHash);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyJournalEntryPostedAsync(entityId, entry.Id, cancellationToken);

        return new PostJournalEntryResult(entry.EntryNumber, entry.Hash);
    }
}
