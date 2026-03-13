using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record UpdateContractCommand : IRequest<Unit>
{
    public required Guid EmployeeId { get; init; }
    public required Guid ContractId { get; init; }
    public required string ContractType { get; init; }
    public required decimal WeeklyHours { get; init; }
    public required int WorkdaysPerWeek { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public required int EmployeeNoticeWeeks { get; init; }

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

public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(x => x.ContractType)
            .Must(t => Enum.TryParse<ContractType>(t, ignoreCase: true, out _))
            .WithMessage("ContractType must be 'Permanent', 'FixedTerm', 'Freelance', or 'WorkingStudent'.");
        RuleFor(x => x.WeeklyHours).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.WorkdaysPerWeek).InclusiveBetween(1, 7);
        RuleFor(x => x.EmployeeNoticeWeeks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EmployerNoticeWeeks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalaryType)
            .Must(t => Enum.TryParse<Domain.Entities.Hr.SalaryType>(t, ignoreCase: true, out _))
            .WithMessage("SalaryType must be 'Monthly', 'Hourly', or 'DailyRate'.");
        RuleFor(x => x.GrossAmountCents).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.BonusAmountCents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AnnualVacationDays).InclusiveBetween(0, 365);
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
        RuleFor(x => x.EmploymentType)
            .Must(t => Enum.TryParse<Domain.Entities.Hr.EmploymentType>(t, ignoreCase: true, out _))
            .When(x => x.EmploymentType != null);
    }
}

public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _db.Contracts
            .FirstOrDefaultAsync(c => c.Id == request.ContractId
                                   && c.EmployeeId == request.EmployeeId
                                   && c.ValidTo == null, cancellationToken)
            ?? throw new NotFoundException("Contract", request.ContractId);

        var contractType = Enum.Parse<ContractType>(request.ContractType, ignoreCase: true);
        var salaryType   = Enum.Parse<Domain.Entities.Hr.SalaryType>(request.SalaryType, ignoreCase: true);
        var employmentType = request.EmploymentType != null
            ? Enum.Parse<Domain.Entities.Hr.EmploymentType>(request.EmploymentType, ignoreCase: true)
            : (Domain.Entities.Hr.EmploymentType?)null;

        contract.Update(
            type:                   contractType,
            weeklyHours:            request.WeeklyHours,
            workdaysPerWeek:        request.WorkdaysPerWeek,
            startDate:              request.StartDate,
            employeeNoticeWeeks:    request.EmployeeNoticeWeeks,
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

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
