using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetCostCentersQuery : IRequest<List<CostCenterDto>>
{
    public required Guid EntityId { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public record CostCenterDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string ShortName { get; init; }
    public string? Description { get; init; }
    public required string Type { get; init; }
    public Guid? HrEmployeeId { get; init; }
    public Guid? HrDepartmentId { get; init; }
    public bool IsActive { get; init; }
}

public class GetCostCentersQueryHandler : IRequestHandler<GetCostCentersQuery, List<CostCenterDto>>
{
    private readonly IAppDbContext _db;

    public GetCostCentersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<CostCenterDto>> Handle(
        GetCostCentersQuery request, CancellationToken ct)
    {
        var query = _db.CostCenters
            .Where(cc => cc.EntityId == request.EntityId);

        if (request.ActiveOnly)
            query = query.Where(cc => cc.IsActive);

        return await query
            .OrderBy(cc => cc.Code)
            .Select(cc => new CostCenterDto
            {
                Id = cc.Id,
                Code = cc.Code,
                ShortName = cc.ShortName,
                Description = cc.Description,
                Type = cc.Type.ToString(),
                HrEmployeeId = cc.HrEmployeeId,
                HrDepartmentId = cc.HrDepartmentId,
                IsActive = cc.IsActive,
            })
            .ToListAsync(ct);
    }
}
