using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetDocumentDownloadQuery(Guid DocumentId, string? IpAddress, string? UserAgent)
    : IRequest<DocumentDownloadResult>;

public record DocumentDownloadResult(Stream Stream, string FileName, string MimeType);

public class GetDocumentDownloadQueryHandler : IRequestHandler<GetDocumentDownloadQuery, DocumentDownloadResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrDocumentService _hrDocumentService;
    private readonly IEncryptionService _encryption;
    private readonly IDataAccessLogger _accessLogger;

    public GetDocumentDownloadQueryHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IHrDocumentService hrDocumentService,
        IEncryptionService encryption,
        IDataAccessLogger accessLogger)
    {
        _db                = db;
        _currentUser       = currentUser;
        _hrDocumentService = hrDocumentService;
        _encryption        = encryption;
        _accessLogger      = accessLogger;
    }

    public async Task<DocumentDownloadResult> Handle(
        GetDocumentDownloadQuery request, CancellationToken cancellationToken)
    {
        var document = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException("EmployeeDocument", request.DocumentId);

        // Verify employee belongs to current user's entity
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == document.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", document.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new UnauthorizedAccessException("Access to this document is not allowed.");

        // Log data access for DSGVO audit trail
        await _accessLogger.LogAsync(
            subjectId:       document.EmployeeId,
            accessedByUserId: _currentUser.UserId,
            accessType:      "Download",
            resourceType:    "EmployeeDocument",
            resourceId:      document.Id,
            ipAddress:       request.IpAddress,
            userAgent:       request.UserAgent,
            ct:              cancellationToken);

        // Decrypt storage path and download from MinIO
        var storagePath = _encryption.Decrypt(document.StoragePath);
        var stream      = await _hrDocumentService.DownloadDocumentAsync(storagePath, cancellationToken);

        return new DocumentDownloadResult(stream, document.FileName, document.MimeType);
    }
}
