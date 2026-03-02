using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Commands;

public record UpdateProfileCommand : IRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Locale { get; init; }
    public required string Timezone { get; init; }
    public string? Bio { get; init; }
}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly HashSet<string> AllowedLocales = ["de", "en"];

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Locale).NotEmpty().Must(l => AllowedLocales.Contains(l))
            .WithMessage("Locale must be one of: " + string.Join(", ", AllowedLocales));
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Bio).MaximumLength(500);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public UpdateProfileCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        user.UpdateProfile(request.FirstName, request.LastName, request.Locale, request.Timezone);
        user.UpdateBio(request.Bio);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: _currentUser.EntityId,
            action: "update_profile",
            tableName: "users",
            recordId: user.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"firstName\":\"{request.FirstName}\",\"lastName\":\"{request.LastName}\",\"locale\":\"{request.Locale}\",\"timezone\":\"{request.Timezone}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}
