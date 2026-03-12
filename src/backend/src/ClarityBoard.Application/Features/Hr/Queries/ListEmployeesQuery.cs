using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListEmployeesQuery : IRequest<PagedResult<EmployeeListDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? EmployeeType { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? EntityId { get; init; }
}

public record EmployeeListDto
{
    public Guid Id { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string EmployeeType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public string? ManagerName { get; init; }
    public string? Position { get; init; }
    public DateOnly HireDate { get; init; }
    public Guid EntityId { get; init; }
}

public class ListEmployeesQueryValidator : AbstractValidator<ListEmployeesQuery>
{
    public ListEmployeesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListEmployeesQueryHandler : IRequestHandler<ListEmployeesQuery, PagedResult<EmployeeListDto>>
{
    private readonly IAppDbContext _db;

    public ListEmployeesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<EmployeeListDto>> Handle(ListEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Employees.AsQueryable();

        // Filter by EntityId
        if (request.EntityId.HasValue)
            query = query.Where(e => e.EntityId == request.EntityId.Value);

        // Filter by Status
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<EmployeeStatus>(request.Status, out var statusEnum))
            query = query.Where(e => e.Status == statusEnum);

        // Filter by EmployeeType
        if (!string.IsNullOrWhiteSpace(request.EmployeeType) && Enum.TryParse<EmployeeType>(request.EmployeeType, out var typeEnum))
            query = query.Where(e => e.EmployeeType == typeEnum);

        // Filter by DepartmentId
        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);

        // Search by full name or employee number
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(e =>
                e.EmployeeNumber.ToLower().Contains(search) ||
                e.FirstName.ToLower().Contains(search) ||
                e.LastName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var employees = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new
            {
                e.Id,
                e.EmployeeNumber,
                e.FirstName,
                e.LastName,
                e.EmployeeType,
                e.Status,
                e.DepartmentId,
                e.ManagerId,
                e.Position,
                e.HireDate,
                e.EntityId,
            })
            .ToListAsync(cancellationToken);

        // Resolve department names
        var departmentIds = employees
            .Where(e => e.DepartmentId.HasValue)
            .Select(e => e.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departmentNames = departmentIds.Count > 0
            ? await _db.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        // Resolve manager names
        var managerIds = employees
            .Where(e => e.ManagerId.HasValue)
            .Select(e => e.ManagerId!.Value)
            .Distinct()
            .ToList();

        var managerNames = managerIds.Count > 0
            ? await _db.Employees
                .Where(e => managerIds.Contains(e.Id))
                .Select(e => new { e.Id, e.FirstName, e.LastName })
                .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken)
            : new Dictionary<Guid, string>();

        var items = employees.Select(e => new EmployeeListDto
        {
            Id             = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            FullName       = $"{e.FirstName} {e.LastName}",
            EmployeeType   = e.EmployeeType.ToString(),
            Status         = e.Status.ToString(),
            DepartmentName = e.DepartmentId.HasValue && departmentNames.TryGetValue(e.DepartmentId.Value, out var dName) ? dName : null,
            ManagerName    = e.ManagerId.HasValue && managerNames.TryGetValue(e.ManagerId.Value, out var mName) ? mName : null,
            Position       = e.Position,
            HireDate       = e.HireDate,
            EntityId       = e.EntityId,
        }).ToList();

        return new PagedResult<EmployeeListDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
