using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manager")]
public record ApproveTravelExpenseCommand : IRequest
{
    public required Guid ReportId { get; init; }
}

public class ApproveTravelExpenseCommandValidator : AbstractValidator<ApproveTravelExpenseCommand>
{
    public ApproveTravelExpenseCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}

public class ApproveTravelExpenseCommandHandler : IRequestHandler<ApproveTravelExpenseCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrHubNotifier _hrHub;

    public ApproveTravelExpenseCommandHandler(IAppDbContext db, ICurrentUser currentUser, IHrHubNotifier hrHub)
    {
        _db          = db;
        _currentUser = currentUser;
        _hrHub       = hrHub;
    }

    public async Task Handle(ApproveTravelExpenseCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.TravelExpenseReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException("TravelExpenseReport", request.ReportId);

        if (report.Status != TravelExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted reports can be approved.");

        report.Approve(_currentUser.UserId);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == report.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", report.EmployeeId);

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyTravelExpenseUpdatedAsync(
            entityId: employee.EntityId,
            reportId: report.Id,
            status:   "Approved",
            ct:       cancellationToken);
    }
}
