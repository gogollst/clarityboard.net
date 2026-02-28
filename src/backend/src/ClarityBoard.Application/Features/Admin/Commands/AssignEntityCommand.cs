using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

/// <summary>
/// Assigns entity access to a user by creating a UserRole with the "viewer" role for the given entity.
/// For specific roles, use AssignRoleCommand instead.
/// </summary>
[RequirePermission("admin.roles.manage")]
public record AssignEntityCommand : IRequest
{
    public required Guid UserId { get; init; }
    public required Guid EntityId { get; init; }
    public Guid? RoleId { get; init; }
}

public class AssignEntityCommandValidator : AbstractValidator<AssignEntityCommand>
{
    public AssignEntityCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}

public class AssignEntityCommandHandler : IRequestHandler<AssignEntityCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public AssignEntityCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(AssignEntityCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var userExists = await _db.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
            throw new NotFoundException("User", request.UserId);

        // Verify entity exists
        var entity = await _db.LegalEntities
            .FirstOrDefaultAsync(e => e.Id == request.EntityId, cancellationToken)
            ?? throw new NotFoundException("LegalEntity", request.EntityId);

        // Determine role: if RoleId provided, use it; otherwise default to "viewer"
        Guid roleId;
        if (request.RoleId.HasValue)
        {
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == request.RoleId.Value, cancellationToken);
            if (!roleExists)
                throw new NotFoundException("Role", request.RoleId.Value);
            roleId = request.RoleId.Value;
        }
        else
        {
            var viewerRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == "viewer", cancellationToken)
                ?? throw new NotFoundException("Role", "viewer");
            roleId = viewerRole.Id;
        }

        // Check for duplicate
        var alreadyAssigned = await _db.UserRoles
            .AnyAsync(ur =>
                ur.UserId == request.UserId &&
                ur.EntityId == request.EntityId &&
                ur.RoleId == roleId, cancellationToken);
        if (alreadyAssigned)
            throw new InvalidOperationException("User already has this role for the specified entity.");

        var userRole = UserRole.Create(request.UserId, roleId, request.EntityId, _currentUser.UserId);
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: request.EntityId,
            action: "assign_entity",
            tableName: "user_roles",
            recordId: userRole.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"userId\":\"{request.UserId}\",\"entityId\":\"{request.EntityId}\",\"entityName\":\"{entity.Name}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
