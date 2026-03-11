using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("entity.manage")]
public record DeleteDepartmentCommand : IRequest
{
    public required Guid DepartmentId { get; init; }
    public required Guid EntityId { get; init; }
}

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IAppDbContext _db;

    public DeleteDepartmentCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken ct)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException("Department", request.DepartmentId);

        var hasSubDepartments = await _db.Departments
            .AnyAsync(d => d.ParentDepartmentId == request.DepartmentId, ct);
        if (hasSubDepartments)
            throw new ValidationException([
                new ValidationFailure(nameof(request.DepartmentId),
                    "Cannot delete department with sub-departments. Remove or reassign them first.")
            ]);

        var hasEmployees = await _db.Employees
            .AnyAsync(e => e.DepartmentId == request.DepartmentId, ct);
        if (hasEmployees)
            throw new ValidationException([
                new ValidationFailure(nameof(request.DepartmentId),
                    "Cannot delete department with assigned employees. Reassign them first.")
            ]);

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(ct);
    }
}
