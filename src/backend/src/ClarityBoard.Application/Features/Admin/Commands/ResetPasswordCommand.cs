using System.Security.Cryptography;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Admin.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.users.edit")]
public record ResetPasswordCommand : IRequest<ResetPasswordResponse>
{
    public required Guid UserId { get; init; }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public ResetPasswordCommandHandler(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Generate a new temporary password
        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        user.SetPasswordHash(passwordHash);

        // Revoke all active refresh tokens
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
            token.Revoke("password_reset");

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "reset_password",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: "{\"passwordReset\":true}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);

        return new ResetPasswordResponse
        {
            UserId = user.Id,
            TemporaryPassword = temporaryPassword,
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        const string all = upper + lower + digits + special;

        var password = new char[16];
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < password.Length; i++)
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // Shuffle using Fisher-Yates
        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
