using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("entity.manage")]
public record UpdateDepartmentCommand : IRequest
{
    public required Guid DepartmentId { get; init; }
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public string? Description { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
}

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand>
{
    private readonly IAppDbContext _db;

    public UpdateDepartmentCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.EntityId == request.EntityId, cancellationToken)
            ?? throw new NotFoundException("Department", request.DepartmentId);

        if (request.ParentDepartmentId == request.DepartmentId)
            throw new InvalidOperationException("A department cannot be its own parent.");

        department.Update(
            name: request.Name,
            code: request.Code,
            parentDepartmentId: request.ParentDepartmentId,
            managerId: request.ManagerId,
            description: request.Description);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
