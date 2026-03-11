using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

public record DepartmentNodeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public bool IsActive { get; init; }
    public List<DepartmentNodeDto> Children { get; set; } = new List<DepartmentNodeDto>();
}

public record GetDepartmentTreeQuery : IRequest<List<DepartmentNodeDto>>
{
    public required Guid EntityId { get; init; }
}

[RequirePermission("entity.view")]
public class GetDepartmentTreeQueryHandler : IRequestHandler<GetDepartmentTreeQuery, List<DepartmentNodeDto>>
{
    private readonly IAppDbContext _db;

    public GetDepartmentTreeQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DepartmentNodeDto>> Handle(GetDepartmentTreeQuery request, CancellationToken cancellationToken)
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

        // Project to DepartmentNodeDto with mutable Children list
        var allNodes = departments.Select(d => new DepartmentNodeDto
        {
            Id                 = d.Id,
            Name               = d.Name,
            Code               = d.Code,
            Description        = d.Description,
            ParentDepartmentId = d.ParentDepartmentId,
            ManagerId          = d.ManagerId,
            ManagerName        = d.ManagerId.HasValue && managerNames.TryGetValue(d.ManagerId.Value, out var name) ? name : null,
            IsActive           = d.IsActive,
            Children           = new List<DepartmentNodeDto>(),
        }).ToList();

        // Build tree
        var lookup = allNodes.ToDictionary(n => n.Id);
        var roots = new List<DepartmentNodeDto>();
        foreach (var node in allNodes)
        {
            if (node.ParentDepartmentId.HasValue && lookup.TryGetValue(node.ParentDepartmentId.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }
}
