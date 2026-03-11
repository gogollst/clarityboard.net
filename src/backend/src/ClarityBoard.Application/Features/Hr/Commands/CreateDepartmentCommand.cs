using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateDepartmentCommand : IRequest<Guid>
{
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public string? Description { get; init; }
}

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateDepartmentCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Departments
            .AnyAsync(d => d.EntityId == request.EntityId && d.Code == request.Code, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"A department with code '{request.Code}' already exists in this entity.");

        var department = Department.Create(
            entityId: request.EntityId,
            name: request.Name,
            code: request.Code,
            parentDepartmentId: request.ParentDepartmentId,
            managerId: request.ManagerId,
            description: request.Description);

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}
