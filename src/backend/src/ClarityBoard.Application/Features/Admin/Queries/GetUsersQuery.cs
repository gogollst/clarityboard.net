using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Admin.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

[RequirePermission("admin.users.view")]
public record GetUsersQuery : IRequest<PagedResult<UserListDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
}

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListDto>>
{
    private readonly IAppDbContext _db;

    public GetUsersQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsQueryable();

        // Filter by active status
        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        // Search by name or email
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.Status,
                u.TwoFactorEnabled,
                u.LastLoginAt,
                u.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        // Load roles for these users
        var userIds = users.Select(u => u.Id).ToList();
        var userRolesWithUser = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, ur.RoleId, RoleName = r.Name, ur.EntityId, ur.AssignedAt })
            .Join(_db.LegalEntities, x => x.EntityId, e => e.Id, (x, e) => new { x.UserId, Role = new UserRoleDto
            {
                RoleId = x.RoleId,
                RoleName = x.RoleName,
                EntityId = x.EntityId,
                EntityName = e.Name,
                AssignedAt = x.AssignedAt,
            }})
            .ToListAsync(cancellationToken);

        var rolesByUserId = userRolesWithUser
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Role).ToList());

        var items = users.Select(u => new UserListDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive,
            Status = u.Status.ToString(),
            TwoFactorEnabled = u.TwoFactorEnabled,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            Roles = rolesByUserId.TryGetValue(u.Id, out var roles) ? roles : [],
        }).ToList();

        return new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
