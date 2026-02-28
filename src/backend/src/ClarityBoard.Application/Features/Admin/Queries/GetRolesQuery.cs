using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

[RequirePermission("admin.users.view")]
public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public record RoleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IAppDbContext _db;

    public GetRolesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
            })
            .ToListAsync(cancellationToken);
    }
}
