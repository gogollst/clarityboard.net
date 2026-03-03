using System.Security.Cryptography;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Admin.DTOs;
using ClarityBoard.Domain.Entities.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Commands;

[RequirePermission("admin.users.create")]
public record CreateUserCommand : IRequest<CreateUserResponse>
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _email;

    public CreateUserCommandHandler(
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

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate email
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        if (emailExists)
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");

        // Create user without password (invited status)
        var user = User.Create(request.Email, null, request.FirstName, request.LastName);
        _db.Users.Add(user);

        // Generate invitation token (72 hours)
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                          .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var expiry = DateTime.UtcNow.AddHours(72);
        user.SetInvitationToken(token, expiry);

        // Assign roles scoped to entities
        if (request.RoleIds.Count > 0 && request.EntityIds.Count > 0)
        {
            foreach (var roleId in request.RoleIds)
            {
                foreach (var entityId in request.EntityIds)
                {
                    var userRole = UserRole.Create(user.Id, roleId, entityId, _currentUser.UserId);
                    _db.UserRoles.Add(userRole);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "create",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"email\":\"{user.Email}\",\"firstName\":\"{user.FirstName}\",\"lastName\":\"{user.LastName}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);

        // Send invitation link email (fire-and-forget, does not affect user creation)
        try
        {
            await _email.SendInvitationLinkEmailAsync(
                user.Email, user.FirstName, token,
                invitedBy: _currentUser.Email, cancellationToken);
        }
        catch { /* swallow – user was already created successfully */ }

        return new CreateUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
        };
    }
}
