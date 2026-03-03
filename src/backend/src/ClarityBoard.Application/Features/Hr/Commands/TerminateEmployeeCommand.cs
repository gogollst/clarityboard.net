using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record TerminateEmployeeCommand : IRequest
{
    public required Guid EmployeeId { get; init; }
    public required DateOnly TerminationDate { get; init; }
    public required string Reason { get; init; }
}

public class TerminateEmployeeCommandValidator : AbstractValidator<TerminateEmployeeCommand>
{
    public TerminateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TerminationDate).NotEmpty();
    }
}

public class TerminateEmployeeCommandHandler : IRequestHandler<TerminateEmployeeCommand>
{
    private readonly IAppDbContext _db;
    private readonly IHrHubNotifier _hrHub;

    public TerminateEmployeeCommandHandler(IAppDbContext db, IHrHubNotifier hrHub)
    {
        _db = db;
        _hrHub = hrHub;
    }

    public async Task Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        if (request.TerminationDate <= employee.HireDate)
            throw new InvalidOperationException("Termination date must be after the hire date.");

        employee.Terminate(request.TerminationDate, request.Reason);
        await _db.SaveChangesAsync(cancellationToken);

        await _hrHub.NotifyEmployeeStatusChangedAsync(
            entityId: employee.EntityId,
            employeeId: employee.Id,
            newStatus: "Terminated",
            ct: cancellationToken);
    }
}
