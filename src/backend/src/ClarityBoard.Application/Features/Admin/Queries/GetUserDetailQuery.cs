using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Admin.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

[RequirePermission("admin.users.view")]
public record GetUserDetailQuery : IRequest<UserDetailDto>
{
    public required Guid UserId { get; init; }
}

public class GetUserDetailQueryValidator : AbstractValidator<GetUserDetailQuery>
{
    public GetUserDetailQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto>
{
    private readonly IAppDbContext _db;

    public GetUserDetailQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<UserDetailDto> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Load roles with entity names
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
            .Join(_db.LegalEntities, x => x.ur.EntityId, e => e.Id, (x, e) => new UserRoleDto
            {
                RoleId = x.r.Id,
                RoleName = x.r.Name,
                EntityId = x.ur.EntityId,
                EntityName = e.Name,
                AssignedAt = x.ur.AssignedAt,
            })
            .ToListAsync(cancellationToken);

        // Group into entity access
        var entityAccess = roles
            .GroupBy(r => new { r.EntityId, r.EntityName })
            .Select(g => new UserEntityAccessDto
            {
                EntityId = g.Key.EntityId,
                EntityName = g.Key.EntityName,
                Roles = g.Select(r => r.RoleName).Distinct().ToList(),
            })
            .ToList();

        // Load recent audit logs for this user (last 50)
        var recentAuditLogs = await _db.AuditLogs
            .Where(a => a.UserId == request.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityId = a.EntityId,
                UserId = a.UserId,
                UserEmail = null,
                Action = a.Action,
                TableName = a.TableName,
                RecordId = a.RecordId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Locale = user.Locale,
            Timezone = user.Timezone,
            IsActive = user.IsActive,
            TwoFactorEnabled = user.TwoFactorEnabled,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockedUntil = user.LockedUntil,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles,
            EntityAccess = entityAccess,
            RecentAuditLogs = recentAuditLogs,
        };
    }
}
