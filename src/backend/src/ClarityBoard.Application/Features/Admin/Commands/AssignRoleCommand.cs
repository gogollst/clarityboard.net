using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.roles.manage")]
public record AssignRoleCommand : IRequest
{
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public required Guid EntityId { get; init; }
}

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public AssignRoleCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var userExists = await _db.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
            throw new NotFoundException("User", request.UserId);

        // Verify role exists
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        // Verify entity exists
        var entityExists = await _db.LegalEntities
            .AnyAsync(e => e.Id == request.EntityId, cancellationToken);
        if (!entityExists)
            throw new NotFoundException("LegalEntity", request.EntityId);

        // Check for duplicate assignment
        var alreadyAssigned = await _db.UserRoles
            .AnyAsync(ur =>
                ur.UserId == request.UserId &&
                ur.RoleId == request.RoleId &&
                ur.EntityId == request.EntityId, cancellationToken);
        if (alreadyAssigned)
            throw new InvalidOperationException("This role is already assigned to the user for this entity.");

        var userRole = UserRole.Create(request.UserId, request.RoleId, request.EntityId, _currentUser.UserId);
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: request.EntityId,
            action: "assign_role",
            tableName: "user_roles",
            recordId: userRole.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"userId\":\"{request.UserId}\",\"roleId\":\"{request.RoleId}\",\"roleName\":\"{role.Name}\",\"entityId\":\"{request.EntityId}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
