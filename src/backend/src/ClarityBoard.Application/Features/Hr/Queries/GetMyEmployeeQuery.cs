using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.self")]
public record GetMyEmployeeQuery : IRequest<EmployeeDetailDto>;

public class GetMyEmployeeQueryHandler : IRequestHandler<GetMyEmployeeQuery, EmployeeDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyEmployeeQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<EmployeeDetailDto> Handle(GetMyEmployeeQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Employees
            .Where(e => e.UserId == _currentUser.UserId)
            .Select(e => new EmployeeDetailDto
            {
                Id                = e.Id,
                EmployeeNumber    = e.EmployeeNumber,
                FirstName         = e.FirstName,
                LastName          = e.LastName,
                EmployeeType      = e.EmployeeType.ToString(),
                Status            = e.Status.ToString(),
                DateOfBirth       = e.DateOfBirth,
                TaxId             = e.TaxId,
                HireDate          = e.HireDate,
                TerminationDate   = e.TerminationDate,
                TerminationReason = e.TerminationReason,
                ManagerId         = e.ManagerId,
                ManagerName       = _db.Employees
                    .Where(m => m.Id == e.ManagerId)
                    .Select(m => m.FirstName + " " + m.LastName)
                    .FirstOrDefault(),
                DepartmentId      = e.DepartmentId,
                DepartmentName    = _db.Departments
                    .Where(d => d.Id == e.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                Iban              = e.Iban,
                Bic               = e.Bic,
                EntityId          = e.EntityId,
                CreatedAt         = e.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No employee record linked to the current user account.");

        return dto;
    }
}
