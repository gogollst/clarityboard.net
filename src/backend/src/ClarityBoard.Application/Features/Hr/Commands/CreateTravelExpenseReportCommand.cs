using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record CreateTravelExpenseReportCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required string Title { get; init; }
    public required DateOnly TripStartDate { get; init; }
    public required DateOnly TripEndDate { get; init; }
    public required string Destination { get; init; }
    public required string BusinessPurpose { get; init; }
}

public class CreateTravelExpenseReportCommandValidator : AbstractValidator<CreateTravelExpenseReportCommand>
{
    public CreateTravelExpenseReportCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TripStartDate).NotEmpty();
        RuleFor(x => x.TripEndDate).NotEmpty();
        RuleFor(x => x.TripEndDate)
            .GreaterThanOrEqualTo(x => x.TripStartDate)
            .WithMessage("TripEndDate must be greater than or equal to TripStartDate.");
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BusinessPurpose).NotEmpty().MaximumLength(500);
    }
}

public class CreateTravelExpenseReportCommandHandler : IRequestHandler<CreateTravelExpenseReportCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateTravelExpenseReportCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTravelExpenseReportCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.FindAsync([request.EmployeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        if (employee.EntityId != _currentUser.EntityId)
            throw new InvalidOperationException("Access denied to this employee.");

        var report = TravelExpenseReport.Create(
            employeeId:      request.EmployeeId,
            title:           request.Title,
            tripStart:       request.TripStartDate,
            tripEnd:         request.TripEndDate,
            destination:     request.Destination,
            businessPurpose: request.BusinessPurpose);

        _db.TravelExpenseReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
