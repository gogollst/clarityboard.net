using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
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
    public string? SocialSecurityNumber { get; init; }
    public string Gender { get; init; } = "NotSpecified";
    public string? Nationality { get; init; }
    public string? Position { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkEmail { get; init; }
    public string? PersonalEmail { get; init; }
    public string? PersonalPhone { get; init; }
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
        RuleFor(x => x.SocialSecurityNumber).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.SocialSecurityNumber));
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Nationality));
        RuleFor(x => x.Position).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Position));
        RuleFor(x => x.WorkEmail).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrWhiteSpace(x.WorkEmail));
        RuleFor(x => x.PersonalEmail).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail));
        RuleFor(x => x.PersonalPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.PersonalPhone));
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

        var gender = Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var g) ? g : Gender.NotSpecified;
        var employmentType = !string.IsNullOrWhiteSpace(request.EmploymentType)
            && Enum.TryParse<Domain.Entities.Hr.EmploymentType>(request.EmploymentType, ignoreCase: true, out var et)
                ? et
                : (Domain.Entities.Hr.EmploymentType?)null;

        employee.UpdateBasicInfo(
            firstName: request.FirstName,
            lastName: request.LastName,
            dateOfBirth: request.DateOfBirth,
            taxId: request.TaxId,
            managerId: request.ManagerId,
            departmentId: request.DepartmentId,
            iban: request.Iban,
            bic: request.Bic,
            entityId: request.EntityId,
            socialSecurityNumber: request.SocialSecurityNumber,
            gender: gender,
            nationality: request.Nationality,
            position: request.Position,
            employmentType: employmentType,
            workEmail: request.WorkEmail,
            personalEmail: request.PersonalEmail,
            personalPhone: request.PersonalPhone);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
