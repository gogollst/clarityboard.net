using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

/// <summary>
/// Accepts an invitation by validating the token and setting the user's first password.
/// Public endpoint – no authentication required.
/// </summary>
public record AcceptInvitationCommand : IRequest
{
    public required string Token { get; init; }
    public required string Password { get; init; }
}

public class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

public class AcceptInvitationHandler : IRequestHandler<AcceptInvitationCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public AcceptInvitationHandler(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.InvitationToken == request.Token, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("Invalid or expired invitation link.");

        if (user.InvitationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invitation link has expired.");

        user.AcceptInvitation(_passwordHasher.Hash(request.Password));
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: null,
            action: "accept_invitation",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: "{\"invitationAccepted\":true}",
            userId: user.Id,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
