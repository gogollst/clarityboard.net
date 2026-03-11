using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record UpdateEmployeeCommand : IRequest
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string TaxId { get; init; }
    public Guid? ManagerId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public Guid? EntityId { get; init; }
}

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Iban).MinimumLength(15).MaximumLength(34).When(x => !string.IsNullOrWhiteSpace(x.Iban));
        RuleFor(x => x.Bic).MaximumLength(11).When(x => !string.IsNullOrWhiteSpace(x.Bic));
    }
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand>
{
    private readonly IAppDbContext _db;

    public UpdateEmployeeCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Employee", request.Id);

        employee.UpdateBasicInfo(
            firstName: request.FirstName,
            lastName: request.LastName,
            dateOfBirth: request.DateOfBirth,
            taxId: request.TaxId,
            managerId: request.ManagerId,
            departmentId: request.DepartmentId,
            iban: request.Iban,
            bic: request.Bic,
            entityId: request.EntityId);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
