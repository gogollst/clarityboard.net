using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record DeactivateDepartmentCommand : IRequest
{
    public required Guid DepartmentId { get; init; }
    public required Guid EntityId { get; init; }
}

public class DeactivateDepartmentCommandHandler : IRequestHandler<DeactivateDepartmentCommand>
{
    private readonly IAppDbContext _db;

    public DeactivateDepartmentCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeactivateDepartmentCommand request, CancellationToken ct)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException("Department", request.DepartmentId);

        department.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}
