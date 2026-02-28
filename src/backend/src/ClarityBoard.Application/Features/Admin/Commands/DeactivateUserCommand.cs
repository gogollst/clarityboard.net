using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.users.delete")]
public record DeactivateUserCommand : IRequest
{
    public required Guid UserId { get; init; }
}

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public DeactivateUserCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (_currentUser.UserId == request.UserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        user.Deactivate();

        // Revoke all active refresh tokens
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
            token.Revoke("user_deactivated");

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "deactivate",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: "{\"isActive\":true}",
            newValues: "{\"isActive\":false}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
