using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Application.Features.Document.DTOs;
using MediatR;

namespace ClarityBoard.Application.Features.Document.Commands;

public record UploadDocumentCommand : IRequest<DocumentUploadResult>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream FileStream { get; init; }
    public long FileSize { get; init; }
    public string DocumentType { get; init; } = "invoice";
    public Guid CreatedBy { get; init; }
}

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentUploadResult>
{
    private readonly IAppDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IMessagePublisher _messagePublisher;

    public UploadDocumentCommandHandler(
        IAppDbContext db,
        IDocumentStorage storage,
        IMessagePublisher messagePublisher)
    {
        _db = db;
        _storage = storage;
        _messagePublisher = messagePublisher;
    }

    public async Task<DocumentUploadResult> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        // Upload to MinIO
        var storagePath = await _storage.UploadAsync(
            request.EntityId, request.FileName, request.FileStream, request.ContentType, ct);

        // Create Document entity
        var document = Domain.Entities.Document.Document.Create(
            entityId: request.EntityId,
            fileName: request.FileName,
            contentType: request.ContentType,
            fileSize: request.FileSize,
            storagePath: storagePath,
            documentType: request.DocumentType,
            createdBy: request.CreatedBy);

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);

        // Publish ProcessDocument message via IMessagePublisher
        await _messagePublisher.PublishAsync(new ProcessDocument(document.Id, request.EntityId), ct);

        return new DocumentUploadResult { DocumentId = document.Id };
    }
}
