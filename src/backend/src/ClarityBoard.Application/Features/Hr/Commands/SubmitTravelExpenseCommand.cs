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
    private readonly IHrHubNotifier _hrHub;

    public SubmitTravelExpenseCommandHandler(IAppDbContext db, IHrHubNotifier hrHub)
    {
        _db    = db;
        _hrHub = hrHub;
    }

    public async Task Handle(SubmitTravelExpenseCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.TravelExpenseReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException("TravelExpenseReport", request.ReportId);

        if (report.Status != TravelExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft reports can be submitted.");

        if (!report.Items.Any())
            throw new InvalidOperationException("Cannot submit a travel expense report with no items.");

        report.Submit();

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == report.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", report.EmployeeId);

        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyTravelExpenseUpdatedAsync(
            entityId: employee.EntityId,
            reportId: report.Id,
            status:   "Submitted",
            ct:       cancellationToken);
    }
}
