using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.document.upload")]
public record DeleteDocumentCommand : IRequest
{
    public required Guid EmployeeId { get; init; }
    public required Guid DocumentId { get; init; }
}

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrDocumentService _documentService;
    private readonly IEncryptionService _encryption;

    public DeleteDocumentCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IHrDocumentService documentService,
        IEncryptionService encryption)
    {
        _db              = db;
        _currentUser     = currentUser;
        _documentService = documentService;
        _encryption      = encryption;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new UnauthorizedAccessException("Access to this employee is not allowed.");

        var document = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EmployeeId == request.EmployeeId,
                cancellationToken)
            ?? throw new NotFoundException("EmployeeDocument", request.DocumentId);

        // Decrypt the storage path and delete the file from MinIO
        var storagePath = _encryption.Decrypt(document.StoragePath);
        await _documentService.DeleteDocumentAsync(storagePath, cancellationToken);

        // Hard-delete the document entity from the database
        _db.EmployeeDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
