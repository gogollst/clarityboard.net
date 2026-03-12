using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
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
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? Position { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkEmail { get; init; }
    public string? PersonalEmail { get; init; }
    public string? PersonalPhone { get; init; }
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
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Nationality));
        RuleFor(x => x.Position).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Position));
        RuleFor(x => x.WorkEmail).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrWhiteSpace(x.WorkEmail));
        RuleFor(x => x.PersonalEmail).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail));
        RuleFor(x => x.PersonalPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.PersonalPhone));
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

        var employeeType = Enum.Parse<Domain.Entities.Hr.EmployeeType>(request.EmployeeType, ignoreCase: true);
        var gender = !string.IsNullOrWhiteSpace(request.Gender)
            && Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var g)
                ? g
                : Gender.NotSpecified;
        var employmentType = !string.IsNullOrWhiteSpace(request.EmploymentType)
            && Enum.TryParse<Domain.Entities.Hr.EmploymentType>(request.EmploymentType, ignoreCase: true, out var et)
                ? et
                : (Domain.Entities.Hr.EmploymentType?)null;

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
            departmentId: request.DepartmentId,
            gender: gender,
            nationality: request.Nationality,
            position: request.Position,
            employmentType: employmentType,
            workEmail: request.WorkEmail,
            personalEmail: request.PersonalEmail,
            personalPhone: request.PersonalPhone);

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        // Auto-create an employee cost center for accounting integration
        var ccExists = await _db.CostCenters
            .AnyAsync(cc => cc.HrEmployeeId == employee.Id && cc.EntityId == employee.EntityId,
                cancellationToken);
        if (!ccExists)
        {
            var fullName = $"{employee.FirstName} {employee.LastName}";
            var costCenter = CostCenter.Create(
                entityId: employee.EntityId,
                code: $"E{employee.EmployeeNumber}",
                shortName: fullName[..Math.Min(fullName.Length, 100)],
                type: CostCenterType.Employee,
                hrEmployeeId: employee.Id);
            _db.CostCenters.Add(costCenter);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return employee.Id;
    }
}
