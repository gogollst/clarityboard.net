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
    public string? SocialSecurityNumber { get; init; }
    public DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public string? TerminationReason { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public string Gender { get; init; } = "NotSpecified";
    public string? Nationality { get; init; }
    public string? Position { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkEmail { get; init; }
    public string? PersonalEmail { get; init; }
    public string? PersonalPhone { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? CostCenterName { get; init; }
    public Guid EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, EmployeeDetailDto>
{
    private readonly IAppDbContext _db;

    public GetEmployeeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EmployeeDetailDto> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Employees
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new EmployeeDetailDto
            {
                Id                   = e.Id,
                EmployeeNumber       = e.EmployeeNumber,
                FirstName            = e.FirstName,
                LastName             = e.LastName,
                EmployeeType         = e.EmployeeType.ToString(),
                Status               = e.Status.ToString(),
                DateOfBirth          = e.DateOfBirth,
                TaxId                = e.TaxId,
                SocialSecurityNumber = e.SocialSecurityNumber,
                HireDate             = e.HireDate,
                TerminationDate      = e.TerminationDate,
                TerminationReason    = e.TerminationReason,
                ManagerId            = e.ManagerId,
                ManagerName          = _db.Employees
                    .Where(m => m.Id == e.ManagerId)
                    .Select(m => m.FirstName + " " + m.LastName)
                    .FirstOrDefault(),
                DepartmentId         = e.DepartmentId,
                DepartmentName       = _db.Departments
                    .Where(d => d.Id == e.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                Iban                 = e.Iban,
                Bic                  = e.Bic,
                Gender               = e.Gender.ToString(),
                Nationality          = e.Nationality,
                Position             = e.Position,
                EmploymentType       = e.EmploymentType != null ? e.EmploymentType.ToString() : null,
                WorkEmail            = e.WorkEmail,
                PersonalEmail        = e.PersonalEmail,
                PersonalPhone        = e.PersonalPhone,
                CostCenterId         = e.CostCenterId,
                CostCenterName       = e.CostCenterId != null
                    ? _db.CostCenters
                        .Where(cc => cc.Id == e.CostCenterId)
                        .Select(cc => cc.ShortName)
                        .FirstOrDefault()
                    : null,
                EntityId             = e.EntityId,
                CreatedAt            = e.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        return dto;
    }
}
