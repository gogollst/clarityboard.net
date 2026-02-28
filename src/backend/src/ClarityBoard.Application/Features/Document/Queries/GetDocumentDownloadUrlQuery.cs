using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Document.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Queries;

public record GetDocumentDownloadUrlQuery(Guid EntityId, Guid DocumentId) : IRequest<PresignedDownloadUrl?>, IEntityScoped;

public class GetDocumentDownloadUrlQueryHandler : IRequestHandler<GetDocumentDownloadUrlQuery, PresignedDownloadUrl?>
{
    private readonly IAppDbContext _db;
    private readonly IDocumentStorage _storage;

    public GetDocumentDownloadUrlQueryHandler(IAppDbContext db, IDocumentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PresignedDownloadUrl?> Handle(GetDocumentDownloadUrlQuery request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct);

        if (document is null)
            return null;

        var url = await _storage.GetPresignedUrlAsync(
            request.EntityId, document.StoragePath, TimeSpan.FromMinutes(15), ct);

        return new PresignedDownloadUrl { Url = url };
    }
}
