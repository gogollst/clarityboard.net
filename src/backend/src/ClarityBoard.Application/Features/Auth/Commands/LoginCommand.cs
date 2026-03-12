using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Auth.DTOs;
using ClarityBoard.Domain.Entities.Admin;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record LoginCommand : IRequest<AuthResponse>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string DeviceFingerprint { get; init; }
    public bool RememberMe { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DeviceFingerprint).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.IsLocked)
            throw new UnauthorizedAccessException($"Account is locked. Try again later.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (user.PasswordHash is null)
            throw new UnauthorizedAccessException("Account not yet activated. Please accept your invitation email.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash!))
        {
            user.RecordFailedLogin();
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.TwoFactorEnabled)
        {
            return new AuthResponse
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.UtcNow,
                RequiresTwoFactor = true,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Locale = user.Locale,
                },
            };
        }

        return await IssueTokens(user, request, cancellationToken);
    }

    private async Task<AuthResponse> IssueTokens(
        Domain.Entities.Identity.User user, LoginCommand request, CancellationToken cancellationToken)
    {
        user.RecordLogin();

        var authConfig = await _db.AuthConfigs.FirstOrDefaultAsync(cancellationToken)
            ?? AuthConfig.CreateDefault();

        var accessTokenExpiry = request.RememberMe
            ? TimeSpan.FromDays(authConfig.RememberMeTokenLifetimeDays)
            : TimeSpan.FromHours(authConfig.TokenLifetimeHours);

        var refreshTokenExpiry = request.RememberMe
            ? TimeSpan.FromDays(authConfig.RememberMeTokenLifetimeDays)
            : TimeSpan.FromDays(7);

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

        var defaultEntityId = entityAccess.FirstOrDefault()?.EntityId ?? Guid.Empty;

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, defaultEntityId, roleNames, permissions, accessTokenExpiry);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = Domain.Entities.Identity.RefreshToken.Create(
            user.Id,
            refreshTokenValue,
            request.DeviceFingerprint,
            DateTime.UtcNow.Add(refreshTokenExpiry),
            request.IpAddress,
            request.UserAgent);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.Add(accessTokenExpiry),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Locale = user.Locale,
                Entities = entityAccess,
                Roles = roleNames,
                Permissions = permissions,
            },
        };
    }
}
