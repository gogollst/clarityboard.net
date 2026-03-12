using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListDepartmentsQuery(Guid EntityId) : IRequest<List<DepartmentDto>>;

public record DepartmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public bool IsActive { get; init; }
    public int EmployeeCount { get; init; }
}

public class ListDepartmentsQueryHandler : IRequestHandler<ListDepartmentsQuery, List<DepartmentDto>>
{
    private readonly IAppDbContext _db;

    public ListDepartmentsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DepartmentDto>> Handle(ListDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _db.Departments
            .Where(d => d.EntityId == request.EntityId)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Code,
                d.Description,
                d.ParentDepartmentId,
                d.ManagerId,
                d.IsActive,
            })
            .ToListAsync(cancellationToken);

        // Resolve manager names
        var managerIds = departments
            .Where(d => d.ManagerId.HasValue)
            .Select(d => d.ManagerId!.Value)
            .Distinct()
            .ToList();

        var managerNames = managerIds.Count > 0
            ? await _db.Employees
                .Where(e => managerIds.Contains(e.Id))
                .Select(e => new { e.Id, e.FirstName, e.LastName })
                .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken)
            : new Dictionary<Guid, string>();

        // Resolve employee counts per department
        var departmentIds = departments.Select(d => d.Id).ToList();
        var employeeCounts = await _db.Employees
            .Where(e => e.DepartmentId.HasValue && departmentIds.Contains(e.DepartmentId.Value))
            .GroupBy(e => e.DepartmentId!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.DepartmentId, g => g.Count, cancellationToken);

        return departments.Select(d => new DepartmentDto
        {
            Id                 = d.Id,
            Name               = d.Name,
            Code               = d.Code,
            Description        = d.Description,
            ParentDepartmentId = d.ParentDepartmentId,
            ManagerId          = d.ManagerId,
            ManagerName        = d.ManagerId.HasValue && managerNames.TryGetValue(d.ManagerId.Value, out var name) ? name : null,
            IsActive           = d.IsActive,
            EmployeeCount      = employeeCounts.TryGetValue(d.Id, out var count) ? count : 0,
        }).ToList();
    }
}
