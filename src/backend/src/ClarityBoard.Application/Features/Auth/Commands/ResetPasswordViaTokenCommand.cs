using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

/// <summary>
/// Validates a password-reset token and sets the new password.
/// Also revokes all existing refresh tokens to invalidate all active sessions.
/// </summary>
public record ResetPasswordViaTokenCommand : IRequest
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}

public class ResetPasswordViaTokenValidator : AbstractValidator<ResetPasswordViaTokenCommand>
{
    public ResetPasswordViaTokenValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

public class ResetPasswordViaTokenHandler : IRequestHandler<ResetPasswordViaTokenCommand>
{
    private readonly IAppDbContext   _db;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordViaTokenHandler(IAppDbContext db, IPasswordHasher passwordHasher)
    {
        _db             = db;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordViaTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user is null
            || user.PasswordResetTokenExpiry is null
            || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The password reset token is invalid or has expired.");
        }

        // Update password and clear the token
        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newHash);
        user.ClearPasswordResetToken();

        // Revoke all active refresh tokens to terminate all existing sessions
        var refreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var rt in refreshTokens)
            rt.Revoke("password-reset-via-token");

        await _db.SaveChangesAsync(cancellationToken);
    }
}
