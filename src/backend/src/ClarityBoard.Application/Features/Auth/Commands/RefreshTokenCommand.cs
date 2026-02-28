using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Auth.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record RefreshTokenCommand : IRequest<AuthResponse>
{
    public required string RefreshToken { get; init; }
    public required string DeviceFingerprint { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
        RuleFor(x => x.DeviceFingerprint).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IAppDbContext db, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existingToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        if (existingToken.DeviceFingerprint != request.DeviceFingerprint)
        {
            // Device mismatch - revoke all tokens for this user (possible token theft)
            var allTokens = await _db.RefreshTokens
                .Where(t => t.UserId == existingToken.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var t in allTokens)
                t.Revoke("device_mismatch_security");
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Security violation: device mismatch.");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        // Rotate: revoke old, issue new
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.Entities.Identity.RefreshToken.Create(
            user.Id, newRefreshTokenValue, request.DeviceFingerprint,
            DateTime.UtcNow.AddDays(7), request.IpAddress, request.UserAgent);

        existingToken.Revoke("rotated", newRefreshToken.Id);
        _db.RefreshTokens.Add(newRefreshToken);

        var userRoles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.EntityId, RoleName = r.Name })
            .ToListAsync(cancellationToken);

        var roleNames = userRoles.Select(r => r.RoleName).Distinct().ToList();
        var permissions = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var defaultEntityId = userRoles.FirstOrDefault()?.EntityId ?? Guid.Empty;

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, defaultEntityId, roleNames, permissions);

        await _db.SaveChangesAsync(cancellationToken);

        var entityAccess = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.LegalEntities, ur => ur.EntityId, le => le.Id, (ur, le) => new { ur, le })
            .Join(_db.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new EntityAccess
            {
                EntityId = x.le.Id,
                EntityName = x.le.Name,
                Role = r.Name,
            })
            .ToListAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Locale = user.Locale,
                Entities = entityAccess,
            },
        };
    }
}
