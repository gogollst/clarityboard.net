using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateContractCommand : IRequest<Unit>
{
    public required Guid EmployeeId { get; init; }
    public required string ContractType { get; init; }     // "Permanent" | "FixedTerm" | "Freelance" | "WorkingStudent"
    public required decimal WeeklyHours { get; init; }
    public required int WorkdaysPerWeek { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public required int NoticeWeeks { get; init; }
    public required DateTime ValidFrom { get; init; }
    public required string ChangeReason { get; init; }
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
        RuleFor(x => x.NoticeWeeks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);
    }
}

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateContractCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
            throw new NotFoundException("Employee", request.EmployeeId);

        // Close current active contract record
        var current = await _db.Contracts
            .FirstOrDefaultAsync(c => c.EmployeeId == request.EmployeeId && c.ValidTo == null, cancellationToken);

        if (current != null)
            current.Close(request.ValidFrom);

        var contractType = Enum.Parse<ContractType>(request.ContractType, ignoreCase: true);

        var newContract = Contract.Create(
            employeeId:      request.EmployeeId,
            type:            contractType,
            weeklyHours:     request.WeeklyHours,
            workdaysPerWeek: request.WorkdaysPerWeek,
            startDate:       request.StartDate,
            noticeWeeks:     request.NoticeWeeks,
            createdBy:       _currentUser.UserId,
            changeReason:    request.ChangeReason,
            endDate:         request.EndDate,
            probationEndDate: request.ProbationEndDate);

        _db.Contracts.Add(newContract);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
