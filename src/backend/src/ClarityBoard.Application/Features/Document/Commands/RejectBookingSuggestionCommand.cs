using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Commands;

public record RejectBookingSuggestionCommand : IRequest, IEntityScoped
{
    public Guid EntityId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public string? Reason { get; init; }
}

public class RejectBookingSuggestionCommandHandler : IRequestHandler<RejectBookingSuggestionCommand>
{
    private readonly IAppDbContext _db;

    public RejectBookingSuggestionCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RejectBookingSuggestionCommand request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");

        var suggestion = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == request.DocumentId && bs.Status == "suggested")
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No pending booking suggestion for document {request.DocumentId}.");

        suggestion.Reject(request.UserId, request.Reason);

        await _db.SaveChangesAsync(ct);
    }
}
