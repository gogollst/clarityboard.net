using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

// ── Result types ──

public record BulkImportRowResult
{
    public int RowIndex { get; init; }
    public bool Success { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? Error { get; init; }
}

public record BulkImportEmployeesResult
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkImportRowResult> Results { get; init; } = [];
}

// ── Item DTO ──

public record BulkImportEmployeeItem
{
    // Employee fields
    public required string EmployeeNumber { get; init; }
    public required string EmployeeType { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string DateOfBirth { get; init; }
    public required string TaxId { get; init; }
    public required string HireDate { get; init; }
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? Position { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkEmail { get; init; }
    public string? PersonalEmail { get; init; }
    public string? PersonalPhone { get; init; }
    public string? SocialSecurityNumber { get; init; }
    public string? Iban { get; init; }
    public string? Bic { get; init; }

    // Contract / Payroll fields (all optional — contract created only when GrossAmount is provided)
    public string? ContractType { get; init; }
    public string? SalaryType { get; init; }
    public string? GrossAmount { get; init; }
    public string? WeeklyHours { get; init; }
    public string? WorkdaysPerWeek { get; init; }
    public string? ContractStartDate { get; init; }
    public string? ContractEndDate { get; init; }
    public string? AnnualVacationDays { get; init; }
    public string? Has13thSalary { get; init; }
    public string? HasVacationBonus { get; init; }
}

// ── Command ──

[RequirePermission("hr.manage")]
public record BulkImportEmployeesCommand : IRequest<BulkImportEmployeesResult>
{
    public required Guid EntityId { get; init; }
    public required List<BulkImportEmployeeItem> Employees { get; init; }
}

// ── Validator ──

public class BulkImportEmployeesCommandValidator : AbstractValidator<BulkImportEmployeesCommand>
{
    public BulkImportEmployeesCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Employees).NotEmpty().Must(e => e.Count <= 500)
            .WithMessage("Maximum 500 employees per import.");
    }
}

// ── Handler ──

public class BulkImportEmployeesCommandHandler : IRequestHandler<BulkImportEmployeesCommand, BulkImportEmployeesResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public BulkImportEmployeesCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BulkImportEmployeesResult> Handle(BulkImportEmployeesCommand request, CancellationToken cancellationToken)
    {
        var results = new List<BulkImportRowResult>();
        var importedNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < request.Employees.Count; i++)
        {
            var item = request.Employees[i];
            try
            {
                // Parse dates
                if (!DateOnly.TryParse(item.DateOfBirth, out var dateOfBirth))
                    throw new InvalidOperationException($"Invalid DateOfBirth: '{item.DateOfBirth}'");
                if (!DateOnly.TryParse(item.HireDate, out var hireDate))
                    throw new InvalidOperationException($"Invalid HireDate: '{item.HireDate}'");

                // Duplicate check within batch
                if (!importedNumbers.Add(item.EmployeeNumber))
                    throw new InvalidOperationException($"Duplicate employee number '{item.EmployeeNumber}' within import batch.");

                // Duplicate check against DB
                var exists = await _db.Employees
                    .AnyAsync(e => e.EntityId == request.EntityId && e.EmployeeNumber == item.EmployeeNumber, cancellationToken);
                if (exists)
                    throw new InvalidOperationException($"An employee with number '{item.EmployeeNumber}' already exists in this entity.");

                // Parse enums
                var employeeType = Enum.Parse<Domain.Entities.Hr.EmployeeType>(item.EmployeeType, ignoreCase: true);
                var gender = !string.IsNullOrWhiteSpace(item.Gender)
                    && Enum.TryParse<Gender>(item.Gender, ignoreCase: true, out var g)
                        ? g
                        : Gender.NotSpecified;
                var employmentType = !string.IsNullOrWhiteSpace(item.EmploymentType)
                    && Enum.TryParse<Domain.Entities.Hr.EmploymentType>(item.EmploymentType, ignoreCase: true, out var et)
                        ? et
                        : (Domain.Entities.Hr.EmploymentType?)null;

                // Create employee
                var employee = Employee.Create(
                    entityId: request.EntityId,
                    employeeNumber: item.EmployeeNumber,
                    type: employeeType,
                    firstName: item.FirstName,
                    lastName: item.LastName,
                    dateOfBirth: dateOfBirth,
                    taxId: item.TaxId,
                    hireDate: hireDate,
                    managerId: null,
                    departmentId: null,
                    gender: gender,
                    nationality: item.Nationality,
                    position: item.Position,
                    employmentType: employmentType,
                    workEmail: item.WorkEmail,
                    personalEmail: item.PersonalEmail,
                    personalPhone: item.PersonalPhone);

                _db.Employees.Add(employee);
                await _db.SaveChangesAsync(cancellationToken);

                // Auto-create cost center
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

                // Create contract if payroll data is provided
                if (!string.IsNullOrWhiteSpace(item.GrossAmount))
                {
                    var grossDecimal = decimal.Parse(item.GrossAmount, System.Globalization.CultureInfo.InvariantCulture);
                    var grossAmountCents = (int)Math.Round(grossDecimal * 100m);
                    if (grossAmountCents <= 0)
                        throw new InvalidOperationException("GrossAmount must be greater than 0.");

                    var salaryType = !string.IsNullOrWhiteSpace(item.SalaryType)
                        ? Enum.Parse<SalaryType>(item.SalaryType, ignoreCase: true)
                        : SalaryType.Monthly;

                    var contractType = !string.IsNullOrWhiteSpace(item.ContractType)
                        ? Enum.Parse<ContractType>(item.ContractType, ignoreCase: true)
                        : ContractType.Permanent;

                    var weeklyHours = !string.IsNullOrWhiteSpace(item.WeeklyHours)
                        ? decimal.Parse(item.WeeklyHours, System.Globalization.CultureInfo.InvariantCulture)
                        : 40m;

                    var workdaysPerWeek = !string.IsNullOrWhiteSpace(item.WorkdaysPerWeek)
                        ? int.Parse(item.WorkdaysPerWeek)
                        : 5;

                    var contractStartDate = !string.IsNullOrWhiteSpace(item.ContractStartDate)
                        && DateOnly.TryParse(item.ContractStartDate, out var csd)
                            ? csd
                            : hireDate;

                    DateOnly? contractEndDate = !string.IsNullOrWhiteSpace(item.ContractEndDate)
                        && DateOnly.TryParse(item.ContractEndDate, out var ced)
                            ? ced
                            : null;

                    var annualVacationDays = !string.IsNullOrWhiteSpace(item.AnnualVacationDays)
                        ? int.Parse(item.AnnualVacationDays)
                        : 20;

                    var has13thSalary = ParseYesNo(item.Has13thSalary);
                    var hasVacationBonus = ParseYesNo(item.HasVacationBonus);

                    var contract = Contract.Create(
                        employeeId: employee.Id,
                        type: contractType,
                        weeklyHours: weeklyHours,
                        workdaysPerWeek: workdaysPerWeek,
                        startDate: contractStartDate,
                        employeeNoticeWeeks: 4,
                        createdBy: _currentUser.UserId,
                        changeReason: "Import",
                        validFrom: DateTime.UtcNow,
                        salaryType: salaryType,
                        grossAmountCents: grossAmountCents,
                        employmentType: employmentType,
                        annualVacationDays: annualVacationDays,
                        has13thSalary: has13thSalary,
                        hasVacationBonus: hasVacationBonus,
                        endDate: contractEndDate,
                        fixedTermReason: contractType == ContractType.FixedTerm ? "Befristeter Vertrag" : null);

                    _db.Contracts.Add(contract);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                results.Add(new BulkImportRowResult
                {
                    RowIndex = i,
                    Success = true,
                    EmployeeId = employee.Id,
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkImportRowResult
                {
                    RowIndex = i,
                    Success = false,
                    Error = ex.Message,
                });
            }
        }

        return new BulkImportEmployeesResult
        {
            TotalRows = request.Employees.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results,
        };
    }

    private static bool ParseYesNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "yes" or "ja" or "да" or "1" or "true";
    }
}
