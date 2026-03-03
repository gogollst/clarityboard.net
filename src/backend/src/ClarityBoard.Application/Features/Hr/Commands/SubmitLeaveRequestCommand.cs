using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record SubmitLeaveRequestCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required Guid LeaveTypeId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public bool HalfDay { get; init; }
    public string? Notes { get; init; }
}

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
        // Fix 2: half-day requests must be a single day
        RuleFor(x => x.EndDate)
            .Equal(x => x.StartDate)
            .When(x => x.HalfDay)
            .WithMessage("End date must equal start date for half-day requests.");
        // Fix 3: leave requests must stay within a single calendar year
        RuleFor(x => x.EndDate.Year)
            .Equal(x => x.StartDate.Year)
            .WithMessage("Leave requests cannot span multiple calendar years. Please submit separate requests.");
    }
}

public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IHrHubNotifier _hrHub;

    public SubmitLeaveRequestCommandHandler(IAppDbContext db, IHrHubNotifier hrHub)
    {
        _db    = db;
        _hrHub = hrHub;
    }

    public async Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        // TODO (Fix 4): verify that _currentUser owns this employee record (or has hr.manage permission)
        // once ICurrentUser exposes an EmployeeId / HrEmployeeId property.

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException("LeaveType", request.LeaveTypeId);

        // Load public holidays for employee's entity in the date range
        var holidays  = await _db.PublicHolidays
            .Where(h => h.EntityId == employee.EntityId
                     && h.Date >= request.StartDate
                     && h.Date <= request.EndDate)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        var holidaySet = new HashSet<DateOnly>(holidays);

        // Calculate working days
        decimal workingDays;
        if (request.HalfDay)
        {
            workingDays = 0.5m;
            // Fix 2: ensure the selected date is actually a working day
            if (request.StartDate.DayOfWeek == DayOfWeek.Saturday
             || request.StartDate.DayOfWeek == DayOfWeek.Sunday
             || holidaySet.Contains(request.StartDate))
            {
                throw new InvalidOperationException("The selected date is not a working day.");
            }
        }
        else
        {
            workingDays = 0;
            var current = request.StartDate;
            while (current <= request.EndDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday
                 && current.DayOfWeek != DayOfWeek.Sunday
                 && !holidaySet.Contains(current))
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }
        }

        if (workingDays <= 0)
            throw new InvalidOperationException("The requested period contains no working days.");

        var leaveRequest = LeaveRequest.Create(
            employeeId:  request.EmployeeId,
            leaveTypeId: request.LeaveTypeId,
            startDate:   request.StartDate,
            endDate:     request.EndDate,
            workingDays: workingDays,
            halfDay:     request.HalfDay,
            notes:       request.Notes);

        _db.LeaveRequests.Add(leaveRequest);

        if (leaveType.IsDeductedFromBalance)
        {
            var year    = request.StartDate.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId
                                       && b.LeaveTypeId == request.LeaveTypeId
                                       && b.Year == year, cancellationToken);

            if (balance is null)
            {
                balance = LeaveBalance.Create(
                    employeeId:     request.EmployeeId,
                    leaveTypeId:    request.LeaveTypeId,
                    year:           year,
                    entitlementDays: leaveType.MaxDaysPerYear.HasValue ? leaveType.MaxDaysPerYear.Value : 0);
                _db.LeaveBalances.Add(balance);
            }

            balance.AddPending(workingDays);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyLeaveRequestUpdatedAsync(
            entityId:  employee.EntityId,
            requestId: leaveRequest.Id,
            status:    "Pending",
            ct:        cancellationToken);

        return leaveRequest.Id;
    }
}
