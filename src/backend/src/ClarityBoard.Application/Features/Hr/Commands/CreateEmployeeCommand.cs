using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateEmployeeCommand : IRequest<Guid>
{
    public required Guid EntityId { get; init; }
    public required string EmployeeNumber { get; init; }
    public required string EmployeeType { get; init; }  // "Employee" | "Contractor"
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string TaxId { get; init; }
    public required DateOnly HireDate { get; init; }
    public Guid? ManagerId { get; init; }
    public Guid? DepartmentId { get; init; }
}

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.EmployeeNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.EmployeeType)
            .NotEmpty()
            .Must(t => t == "Employee" || t == "Contractor")
            .WithMessage("EmployeeType must be 'Employee' or 'Contractor'.");
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateEmployeeCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Check duplicate EmployeeNumber within the same EntityId
        var exists = await _db.Employees
            .AnyAsync(e => e.EntityId == request.EntityId && e.EmployeeNumber == request.EmployeeNumber, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"An employee with number '{request.EmployeeNumber}' already exists in this entity.");

        var employeeType = Enum.Parse<EmployeeType>(request.EmployeeType, ignoreCase: true);

        var employee = Employee.Create(
            entityId: request.EntityId,
            employeeNumber: request.EmployeeNumber,
            type: employeeType,
            firstName: request.FirstName,
            lastName: request.LastName,
            dateOfBirth: request.DateOfBirth,
            taxId: request.TaxId,
            hireDate: request.HireDate,
            managerId: request.ManagerId,
            departmentId: request.DepartmentId);

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
