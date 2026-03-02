using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Commands;

public record Disable2FACommand : IRequest
{
    public required string Password { get; init; }
}

public class Disable2FACommandValidator : AbstractValidator<Disable2FACommand>
{
    public Disable2FACommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public Disable2FACommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task Handle(Disable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Password is incorrect.");

        user.Disable2FA();
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "disable_2fa",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: null,
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
