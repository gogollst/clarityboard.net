using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateDepartmentCommand : IRequest<Guid>
{
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
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
        var department = Department.Create(
            entityId: request.EntityId,
            name: request.Name,
            code: request.Code,
            parentDepartmentId: request.ParentDepartmentId,
            managerId: request.ManagerId);

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}
