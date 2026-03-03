using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetEmployeeQuery(Guid EmployeeId) : IRequest<EmployeeDetailDto>;

public record EmployeeDetailDto
{
    public Guid Id { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string EmployeeType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string TaxId { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public string? TerminationReason { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public Guid EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, EmployeeDetailDto>
{
    private readonly IAppDbContext _db;

    public GetEmployeeQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<EmployeeDetailDto> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        // Resolve manager name
        string? managerName = null;
        if (employee.ManagerId.HasValue)
        {
            var manager = await _db.Employees
                .Where(e => e.Id == employee.ManagerId.Value)
                .Select(e => new { e.FirstName, e.LastName })
                .FirstOrDefaultAsync(cancellationToken);
            if (manager is not null)
                managerName = $"{manager.FirstName} {manager.LastName}";
        }

        // Resolve department name
        string? departmentName = null;
        if (employee.DepartmentId.HasValue)
        {
            departmentName = await _db.Departments
                .Where(d => d.Id == employee.DepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new EmployeeDetailDto
        {
            Id                = employee.Id,
            EmployeeNumber    = employee.EmployeeNumber,
            FirstName         = employee.FirstName,
            LastName          = employee.LastName,
            EmployeeType      = employee.EmployeeType.ToString(),
            Status            = employee.Status.ToString(),
            DateOfBirth       = employee.DateOfBirth,
            TaxId             = employee.TaxId,
            HireDate          = employee.HireDate,
            TerminationDate   = employee.TerminationDate,
            TerminationReason = employee.TerminationReason,
            ManagerId         = employee.ManagerId,
            ManagerName       = managerName,
            DepartmentId      = employee.DepartmentId,
            DepartmentName    = departmentName,
            EntityId          = employee.EntityId,
            CreatedAt         = employee.CreatedAt,
        };
    }
}
