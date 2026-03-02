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
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IEmailService _email;

    public CreateUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IAuditService auditService,
        IEmailService email)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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

        // Generate a temporary password
        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        // Create user
        var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName);
        _db.Users.Add(user);

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

        // Send invitation email (fire-and-forget, does not affect user creation)
        try
        {
            await _email.SendInvitationEmailAsync(
                user.Email, user.FirstName, temporaryPassword,
                invitedBy: _currentUser.Email, cancellationToken);
        }
        catch { /* swallow – user was already created successfully */ }

        return new CreateUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
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
        // Ensure at least one of each type
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
