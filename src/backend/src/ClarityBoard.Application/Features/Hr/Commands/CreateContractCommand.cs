using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateContractCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required string ContractType { get; init; }
    public required decimal WeeklyHours { get; init; }
    public required int WorkdaysPerWeek { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public required int EmployeeNoticeWeeks { get; init; }
    public required DateTime ValidFrom { get; init; }
    public required string ChangeReason { get; init; }

    // Salary fields
    public required string SalaryType { get; init; }
    public required int GrossAmountCents { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public int BonusAmountCents { get; init; } = 0;
    public string BonusCurrencyCode { get; init; } = "EUR";
    public int PaymentCycleMonths { get; init; } = 12;

    // Extended fields
    public string? EmploymentType { get; init; }
    public int EmployerNoticeWeeks { get; init; } = 4;
    public int AnnualVacationDays { get; init; } = 20;
    public string? FixedTermReason { get; init; }
    public int FixedTermExtensionCount { get; init; } = 0;
    public bool Has13thSalary { get; init; }
    public bool HasVacationBonus { get; init; }
    public int VariablePayCents { get; init; } = 0;
    public string? VariablePayDescription { get; init; }
    public string? Notes { get; init; }
}

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.ContractType)
            .Must(t => Enum.TryParse<ContractType>(t, ignoreCase: true, out _))
            .WithMessage("ContractType must be 'Permanent', 'FixedTerm', 'Freelance', or 'WorkingStudent'.");
        RuleFor(x => x.WeeklyHours).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.WorkdaysPerWeek).InclusiveBetween(1, 7);
        RuleFor(x => x.EmployeeNoticeWeeks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EmployerNoticeWeeks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);

        // Salary validation
        RuleFor(x => x.SalaryType)
            .Must(t => Enum.TryParse<Domain.Entities.Hr.SalaryType>(t, ignoreCase: true, out _))
            .WithMessage("SalaryType must be 'Monthly', 'Hourly', or 'DailyRate'.");
        RuleFor(x => x.GrossAmountCents).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.BonusAmountCents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BonusCurrencyCode).Length(3);
        RuleFor(x => x.AnnualVacationDays).InclusiveBetween(0, 365);

        // FixedTerm conditional validation
        RuleFor(x => x.FixedTermReason)
            .NotEmpty()
            .When(x => x.ContractType.Equals("FixedTerm", StringComparison.OrdinalIgnoreCase))
            .WithMessage("FixedTermReason is required for fixed-term contracts.");
        RuleFor(x => x.FixedTermExtensionCount).InclusiveBetween(0, 4);
        RuleFor(x => x.EndDate)
            .NotNull()
            .When(x => x.ContractType.Equals("FixedTerm", StringComparison.OrdinalIgnoreCase))
            .WithMessage("EndDate is required for fixed-term contracts.");
        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => endDate > cmd.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");
        RuleFor(x => x.ProbationEndDate)
            .Must((cmd, probEnd) => probEnd >= cmd.StartDate)
            .When(x => x.ProbationEndDate.HasValue)
            .WithMessage("ProbationEndDate must be on or after StartDate.");

        // EmploymentType validation
        RuleFor(x => x.EmploymentType)
            .Must(t => Enum.TryParse<Domain.Entities.Hr.EmploymentType>(t, ignoreCase: true, out _))
            .When(x => x.EmploymentType != null)
            .WithMessage("EmploymentType must be 'FullTime', 'PartTime', 'MiniJob', 'Internship', or 'WorkingStudent'.");
    }
}

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateContractCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
            throw new NotFoundException("Employee", request.EmployeeId);

        var validFrom = request.ValidFrom.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Utc)
            : request.ValidFrom.ToUniversalTime();

        // Close current active contract record
        var current = await _db.Contracts
            .FirstOrDefaultAsync(c => c.EmployeeId == request.EmployeeId && c.ValidTo == null, cancellationToken);

        if (current != null)
        {
            if (validFrom > current.ValidFrom)
                current.Close(validFrom);
            else
                _db.Contracts.Remove(current); // same-day replacement
        }

        // Also close current active salary record
        var currentSalary = await _db.SalaryHistories
            .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId && s.ValidTo == null, cancellationToken);

        if (currentSalary != null)
        {
            if (validFrom > currentSalary.ValidFrom)
                currentSalary.Close(validFrom);
            else
                _db.SalaryHistories.Remove(currentSalary); // same-day replacement
        }

        var contractType = Enum.Parse<ContractType>(request.ContractType, ignoreCase: true);
        var salaryType   = Enum.Parse<Domain.Entities.Hr.SalaryType>(request.SalaryType, ignoreCase: true);
        var employmentType = request.EmploymentType != null
            ? Enum.Parse<Domain.Entities.Hr.EmploymentType>(request.EmploymentType, ignoreCase: true)
            : (Domain.Entities.Hr.EmploymentType?)null;

        var newContract = Contract.Create(
            employeeId:             request.EmployeeId,
            type:                   contractType,
            weeklyHours:            request.WeeklyHours,
            workdaysPerWeek:        request.WorkdaysPerWeek,
            startDate:              request.StartDate,
            employeeNoticeWeeks:    request.EmployeeNoticeWeeks,
            createdBy:              _currentUser.UserId,
            changeReason:           request.ChangeReason,
            validFrom:              validFrom,
            salaryType:             salaryType,
            grossAmountCents:       request.GrossAmountCents,
            currencyCode:           request.CurrencyCode,
            bonusAmountCents:       request.BonusAmountCents,
            bonusCurrencyCode:      request.BonusCurrencyCode,
            paymentCycleMonths:     request.PaymentCycleMonths,
            employmentType:         employmentType,
            employerNoticeWeeks:    request.EmployerNoticeWeeks,
            annualVacationDays:     request.AnnualVacationDays,
            fixedTermReason:        request.FixedTermReason,
            fixedTermExtensionCount: request.FixedTermExtensionCount,
            has13thSalary:          request.Has13thSalary,
            hasVacationBonus:       request.HasVacationBonus,
            variablePayCents:       request.VariablePayCents,
            variablePayDescription: request.VariablePayDescription,
            notes:                  request.Notes,
            endDate:                request.EndDate,
            probationEndDate:       request.ProbationEndDate);

        _db.Contracts.Add(newContract);
        await _db.SaveChangesAsync(cancellationToken);

        return newContract.Id;
    }
}
