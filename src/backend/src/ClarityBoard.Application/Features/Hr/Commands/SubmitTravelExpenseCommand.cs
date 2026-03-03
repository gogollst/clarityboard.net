using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record SubmitTravelExpenseCommand : IRequest
{
    public required Guid ReportId { get; init; }
}

public class SubmitTravelExpenseCommandValidator : AbstractValidator<SubmitTravelExpenseCommand>
{
    public SubmitTravelExpenseCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}

public class SubmitTravelExpenseCommandHandler : IRequestHandler<SubmitTravelExpenseCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHrHubNotifier _hrHub;

    public SubmitTravelExpenseCommandHandler(IAppDbContext db, ICurrentUser currentUser, IHrHubNotifier hrHub)
    {
        _db          = db;
        _currentUser = currentUser;
        _hrHub       = hrHub;
    }

    public async Task Handle(SubmitTravelExpenseCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.TravelExpenseReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException("TravelExpenseReport", request.ReportId);

        // Load employee first for entity ownership check and hub notification
        var employee = await _db.Employees.FindAsync([report.EmployeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", report.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new InvalidOperationException("Access denied to this report.");

        if (!report.Items.Any())
            throw new InvalidOperationException("Cannot submit a travel expense report with no items.");

        // Domain method already enforces Draft status; no duplicate guard needed here
        report.Submit();

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyTravelExpenseUpdatedAsync(
            entityId: employee.EntityId,
            reportId: report.Id,
            status:   "Submitted",
            ct:       cancellationToken);
    }
}
