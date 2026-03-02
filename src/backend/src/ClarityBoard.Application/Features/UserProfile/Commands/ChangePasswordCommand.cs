using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Commands;

public record ChangePasswordCommand : IRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public ChangePasswordCommandHandler(
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

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newHash);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "change_password",
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
