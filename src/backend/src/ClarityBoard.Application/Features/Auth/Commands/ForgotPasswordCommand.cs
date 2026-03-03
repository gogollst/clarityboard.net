using System.Security.Cryptography;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

/// <summary>
/// Initiates a password reset. Generates a 1-hour token, saves it on the user,
/// and sends a reset link via email. Always returns success to prevent email enumeration.
/// </summary>
public record ForgotPasswordCommand : IRequest
{
    public required string Email { get; init; }
}

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;

    public ForgotPasswordHandler(IAppDbContext db, IEmailService email)
    {
        _db    = db;
        _email = email;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always succeed – prevents email enumeration attacks
        if (user is null || !user.IsActive) return;

        // Generate a URL-safe, cryptographically secure token (expiry: 1 hour)
        var token  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "-")
                           .Replace("/", "_")
                           .TrimEnd('=');
        var expiry = DateTime.UtcNow.AddHours(1);

        user.SetPasswordResetToken(token, expiry);
        await _db.SaveChangesAsync(cancellationToken);

        await _email.SendPasswordResetEmailAsync(user.Email, user.FirstName, token, cancellationToken);
    }
}
