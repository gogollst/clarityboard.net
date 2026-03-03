using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manager")]
public record ApproveLeaveRequestCommand : IRequest
{
    public required Guid LeaveRequestId { get; init; }
}

public class ApproveLeaveRequestCommandValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
    }
}

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrHubNotifier _hrHub;

    public ApproveLeaveRequestCommandHandler(IAppDbContext db, ICurrentUser currentUser, IHrHubNotifier hrHub)
    {
        _db          = db;
        _currentUser = currentUser;
        _hrHub       = hrHub;
    }

    public async Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _db.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == request.LeaveRequestId, cancellationToken)
            ?? throw new NotFoundException("LeaveRequest", request.LeaveRequestId);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == leaveRequest.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", leaveRequest.EmployeeId);

        leaveRequest.Approve(_currentUser.UserId);

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == leaveRequest.LeaveTypeId, cancellationToken);

        if (leaveType is { IsDeductedFromBalance: true })
        {
            var year    = leaveRequest.StartDate.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId
                                       && b.LeaveTypeId == leaveRequest.LeaveTypeId
                                       && b.Year == year, cancellationToken);

            if (balance is not null)
            {
                balance.ApprovePending(leaveRequest.WorkingDays);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyLeaveRequestUpdatedAsync(
            entityId:  employee.EntityId,
            requestId: leaveRequest.Id,
            status:    "Approved",
            ct:        cancellationToken);
    }
}
