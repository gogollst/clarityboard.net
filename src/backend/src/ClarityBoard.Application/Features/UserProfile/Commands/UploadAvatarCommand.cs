using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Commands;

public record UploadAvatarCommand : IRequest
{
    public required string FileName { get; init; }
    public required Stream ContentStream { get; init; }
    public required string ContentType { get; init; }
}

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
    };

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only JPEG and PNG images are allowed.");
        RuleFor(x => x.ContentStream).NotNull()
            .Must(s => s.Length <= 5 * 1024 * 1024)
            .WithMessage("File size must not exceed 5 MB.");
    }
}

public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentStorage _documentStorage;

    public UploadAvatarCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDocumentStorage documentStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _documentStorage = documentStorage;
    }

    public async Task Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        // Delete old avatar if exists
        if (user.AvatarPath is not null)
        {
            await _documentStorage.DeleteAsync(_currentUser.EntityId, user.AvatarPath, cancellationToken);
        }

        // Upload new avatar with a user-specific path
        var extension = Path.GetExtension(request.FileName);
        var avatarFileName = $"avatars/{_currentUser.UserId:N}{extension}";

        var storagePath = await _documentStorage.UploadAsync(
            _currentUser.EntityId, avatarFileName, request.ContentStream, request.ContentType, cancellationToken);

        user.SetAvatarPath(storagePath);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
