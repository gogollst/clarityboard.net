using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListDocumentsQuery(Guid EmployeeId) : IRequest<List<EmployeeDocumentDto>>;

public record EmployeeDocumentDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsConfidential { get; init; }
    public DateTime? DeletionScheduledAt { get; init; }
}

public class ListDocumentsQueryHandler : IRequestHandler<ListDocumentsQuery, List<EmployeeDocumentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListDocumentsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<List<EmployeeDocumentDto>> Handle(
        ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        // Verify employee exists and belongs to the current user's entity
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new UnauthorizedAccessException("Access to this employee's documents is not allowed.");

        var docs = await _db.EmployeeDocuments
            .Where(d => d.EmployeeId == request.EmployeeId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new EmployeeDocumentDto
            {
                Id                  = d.Id,
                EmployeeId          = d.EmployeeId,
                DocumentType        = d.DocumentType.ToString(),
                Title               = d.Title,
                FileName            = d.FileName,
                MimeType            = d.MimeType,
                FileSizeBytes       = d.FileSizeBytes,
                UploadedAt          = d.UploadedAt,
                ExpiresAt           = d.ExpiresAt,
                IsConfidential      = d.IsConfidential,
                DeletionScheduledAt = d.DeletionScheduledAt,
            })
            .ToListAsync(cancellationToken);

        return docs;
    }
}
