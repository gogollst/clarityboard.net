using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.users.edit")]
public record ReactivateUserCommand : IRequest
{
    public required Guid UserId { get; init; }
}

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public ReactivateUserCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        user.Activate();

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "reactivate",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: "{\"isActive\":false}",
            newValues: "{\"isActive\":true}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
