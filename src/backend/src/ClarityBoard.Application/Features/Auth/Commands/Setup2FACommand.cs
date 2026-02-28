using System.Text.Json;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record Setup2FACommand : IRequest<Setup2FAResponse>;

public record Setup2FAResponse
{
    public required string QrCodeUri { get; init; }
    public required string Secret { get; init; }
    public required IReadOnlyList<string> RecoveryCodes { get; init; }
}

public class Setup2FACommandHandler : IRequestHandler<Setup2FACommand, Setup2FAResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public Setup2FACommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Setup2FAResponse> Handle(Setup2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already enabled. Disable it first before setting up again.");

        // Generate TOTP secret
        var secretBytes = TotpService.GenerateSecret();
        var base32Secret = TotpService.ToBase32(secretBytes);

        // Generate recovery codes
        var recoveryCodes = TotpService.GenerateRecoveryCodes();
        var hashedCodes = recoveryCodes
            .Select(TotpService.HashRecoveryCode)
            .ToList();

        var recoveryCodesHashJson = JsonSerializer.Serialize(hashedCodes);

        // Generate QR code URI
        var qrCodeUri = TotpService.GenerateQrCodeUri(user.Email, base32Secret);

        // Store secret and hashed recovery codes (2FA not yet enabled until verification)
        user.Setup2FA(base32Secret, recoveryCodesHashJson);
        await _db.SaveChangesAsync(cancellationToken);

        return new Setup2FAResponse
        {
            QrCodeUri = qrCodeUri,
            Secret = base32Secret,
            RecoveryCodes = recoveryCodes,
        };
    }
}
