using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record DeleteDocumentCommand : IRequest
{
    public required Guid EmployeeId { get; init; }
    public required Guid DocumentId { get; init; }
}

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteDocumentCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
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

        // Soft-delete: schedule deletion for now (immediate marker)
        document.ScheduleDeletion(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
