using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.admin")]
public record ScheduleDeletionCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
}

public class ScheduleDeletionCommandHandler : IRequestHandler<ScheduleDeletionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ScheduleDeletionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(ScheduleDeletionCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new UnauthorizedAccessException("Access to this employee is not allowed.");

        // Check if a pending deletion request already exists
        var existing = await _db.DeletionRequests
            .AnyAsync(r => r.EmployeeId == request.EmployeeId && r.Status == DeletionRequestStatus.Pending,
                cancellationToken);

        if (existing)
            throw new InvalidOperationException(
                "A pending deletion request already exists for this employee.");

        // Steuerrechtliche Aufbewahrungspflicht: 10 Jahre Aufbewahrungsfrist
        var scheduledAt = DateTime.UtcNow.AddYears(10);

        var deletionRequest = DeletionRequest.Create(
            employeeId:         request.EmployeeId,
            requestedBy:        _currentUser.UserId,
            scheduledDeletionAt: scheduledAt);

        _db.DeletionRequests.Add(deletionRequest);
        await _db.SaveChangesAsync(cancellationToken);

        return deletionRequest.Id;
    }
}
