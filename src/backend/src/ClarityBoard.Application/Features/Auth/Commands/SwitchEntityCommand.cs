using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record SwitchEntityCommand : IRequest<SwitchEntityResponse>
{
    public required Guid EntityId { get; init; }
}

public record SwitchEntityResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public class SwitchEntityCommandValidator : AbstractValidator<SwitchEntityCommand>
{
    public SwitchEntityCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
    }
}

public class SwitchEntityCommandHandler : IRequestHandler<SwitchEntityCommand, SwitchEntityResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IJwtTokenService _jwt;

    public SwitchEntityCommandHandler(IAppDbContext db, ICurrentUser currentUser, IJwtTokenService jwt)
    {
        _db = db;
        _currentUser = currentUser;
        _jwt = jwt;
    }

    public async Task<SwitchEntityResponse> Handle(SwitchEntityCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        // Verify user has a role assignment in the target entity
        var hasAccess = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.EntityId == request.EntityId, ct);

        if (!hasAccess)
            throw new UnauthorizedAccessException("You do not have access to this entity.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        // Fetch roles and permissions (consistent with login/refresh pattern)
        var userRoles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { RoleName = r.Name })
            .ToListAsync(ct);

        var roleNames = userRoles.Select(r => r.RoleName).Distinct().ToList();

        var permissions = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct()
            .ToListAsync(ct);

        var accessToken = _jwt.GenerateAccessToken(
            userId, user.Email, request.EntityId, roleNames, permissions);

        return new SwitchEntityResponse
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };
    }
}
