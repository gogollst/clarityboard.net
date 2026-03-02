using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Commands;

public record DeleteAvatarCommand : IRequest;

public class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentStorage _documentStorage;

    public DeleteAvatarCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDocumentStorage documentStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _documentStorage = documentStorage;
    }

    public async Task Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (user.AvatarPath is not null)
        {
            await _documentStorage.DeleteAsync(_currentUser.EntityId, user.AvatarPath, cancellationToken);
            user.SetAvatarPath(null);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
