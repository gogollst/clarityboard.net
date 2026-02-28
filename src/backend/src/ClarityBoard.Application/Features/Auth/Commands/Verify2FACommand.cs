using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record Verify2FACommand : IRequest<Verify2FAResponse>
{
    public required string Code { get; init; }
}

public record Verify2FAResponse
{
    public required bool Verified { get; init; }
}

public class Verify2FACommandValidator : AbstractValidator<Verify2FACommand>
{
    public Verify2FACommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Verify2FAResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public Verify2FACommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Verify2FAResponse> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("Two-factor authentication has not been set up. Call setup-2fa first.");

        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already verified and enabled.");

        // Validate the TOTP code
        var secretBytes = TotpService.FromBase32(user.TwoFactorSecret);
        var isValid = TotpService.ValidateCode(secretBytes, request.Code);

        if (!isValid)
        {
            return new Verify2FAResponse { Verified = false };
        }

        // Enable 2FA now that code is verified
        user.Confirm2FA();
        await _db.SaveChangesAsync(cancellationToken);

        return new Verify2FAResponse { Verified = true };
    }
}
