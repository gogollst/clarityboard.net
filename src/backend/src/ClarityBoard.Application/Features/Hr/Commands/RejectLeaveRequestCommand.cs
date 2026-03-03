using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manager")]
public record RejectLeaveRequestCommand : IRequest
{
    public required Guid LeaveRequestId { get; init; }
    public required string Reason { get; init; }
}

public class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand>
{
    private readonly IAppDbContext _db;
    private readonly IHrHubNotifier _hrHub;

    public RejectLeaveRequestCommandHandler(IAppDbContext db, IHrHubNotifier hrHub)
    {
        _db    = db;
        _hrHub = hrHub;
    }

    public async Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _db.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == request.LeaveRequestId, cancellationToken)
            ?? throw new NotFoundException("LeaveRequest", request.LeaveRequestId);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == leaveRequest.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", leaveRequest.EmployeeId);

        leaveRequest.Reject(request.Reason);

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == leaveRequest.LeaveTypeId, cancellationToken);

        if (leaveType is { IsDeductedFromBalance: true })
        {
            var year    = leaveRequest.StartDate.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId
                                       && b.LeaveTypeId == leaveRequest.LeaveTypeId
                                       && b.Year == year, cancellationToken);

            // Fix 6: throw instead of silently skipping when balance record is missing
            if (balance is null)
                throw new InvalidOperationException(
                    $"No leave balance record found for employee {leaveRequest.EmployeeId}, " +
                    $"leave type {leaveRequest.LeaveTypeId}, year {year}.");
            balance.RejectPending(leaveRequest.WorkingDays);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyLeaveRequestUpdatedAsync(
            entityId:  employee.EntityId,
            requestId: leaveRequest.Id,
            status:    "Rejected",
            ct:        cancellationToken);
    }
}
