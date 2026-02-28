using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.roles.manage")]
public record RemoveRoleCommand : IRequest
{
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public required Guid EntityId { get; init; }
}

public class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public RemoveRoleCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var userRole = await _db.UserRoles
            .FirstOrDefaultAsync(ur =>
                ur.UserId == request.UserId &&
                ur.RoleId == request.RoleId &&
                ur.EntityId == request.EntityId, cancellationToken)
            ?? throw new NotFoundException("UserRole",
                $"UserId={request.UserId}, RoleId={request.RoleId}, EntityId={request.EntityId}");

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: request.EntityId,
            action: "remove_role",
            tableName: "user_roles",
            recordId: userRole.Id.ToString(),
            oldValues: $"{{\"userId\":\"{request.UserId}\",\"roleId\":\"{request.RoleId}\",\"entityId\":\"{request.EntityId}\"}}",
            newValues: null,
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
