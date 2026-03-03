using System.Security.Cryptography;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.users.edit")]
public record ResendInvitationCommand : IRequest
{
    public required Guid UserId { get; init; }
}

public class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _email;

    public ResendInvitationCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService auditService,
        IEmailService email)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
        _email = email;
    }

    public async Task Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Status != UserStatus.Invited)
            throw new InvalidOperationException("User has already accepted their invitation.");

        // Generate a new 72-hour invitation token (invalidates the old one)
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                          .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var expiry = DateTime.UtcNow.AddHours(72);

        user.SetInvitationToken(token, expiry);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "resend_invitation",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: "{\"invitationResent\":true}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);

        try
        {
            await _email.SendInvitationLinkEmailAsync(
                user.Email, user.FirstName, token,
                invitedBy: _currentUser.Email, cancellationToken);
        }
        catch { /* swallow – token was already saved successfully */ }
    }
}
