using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.salary.manage")]
public record UpdateSalaryCommand : IRequest<Unit>
{
    public required Guid EmployeeId { get; init; }
    public required int GrossAmountCents { get; init; }
    public required string CurrencyCode { get; init; }
    public int BonusAmountCents { get; init; } = 0;
    public string BonusCurrencyCode { get; init; } = "EUR";
    public required string SalaryType { get; init; }       // "Monthly" | "Hourly" | "DailyRate"
    public int PaymentCycleMonths { get; init; } = 12;
    public required DateTime ValidFrom { get; init; }
    public required string ChangeReason { get; init; }
}

public class UpdateSalaryCommandValidator : AbstractValidator<UpdateSalaryCommand>
{
    private static readonly string[] ValidSalaryTypes = ["Monthly", "Hourly", "DailyRate"];

    public UpdateSalaryCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.GrossAmountCents).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.BonusCurrencyCode).Length(3);
        RuleFor(x => x.SalaryType)
            .Must(t => ValidSalaryTypes.Contains(t))
            .WithMessage("SalaryType must be 'Monthly', 'Hourly', or 'DailyRate'.");
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);
    }
}

public class UpdateSalaryCommandHandler : IRequestHandler<UpdateSalaryCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrHubNotifier _notifier;

    public UpdateSalaryCommandHandler(IAppDbContext db, ICurrentUser currentUser, IHrHubNotifier notifier)
    {
        _db          = db;
        _currentUser = currentUser;
        _notifier    = notifier;
    }

    public async Task<Unit> Handle(UpdateSalaryCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        // Close current active salary record
        var current = await _db.SalaryHistories
            .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId && s.ValidTo == null, cancellationToken);

        if (current != null)
            current.Close(request.ValidFrom);

        var salaryType = Enum.Parse<SalaryType>(request.SalaryType, ignoreCase: true);

        var newSalary = SalaryHistory.Create(
            employeeId:        request.EmployeeId,
            type:              salaryType,
            grossAmountCents:  request.GrossAmountCents,
            currencyCode:      request.CurrencyCode,
            bonusAmountCents:  request.BonusAmountCents,
            bonusCurrencyCode: request.BonusCurrencyCode,
            paymentCycleMonths: request.PaymentCycleMonths,
            validFrom:         request.ValidFrom,
            createdBy:         _currentUser.UserId,
            changeReason:      request.ChangeReason);

        _db.SalaryHistories.Add(newSalary);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifier.NotifySalaryUpdatedAsync(employee.EntityId, request.EmployeeId, cancellationToken);

        return Unit.Value;
    }
}
