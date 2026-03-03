using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record LogoutCommand : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == _currentUser.UserId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke("logout");

        await _db.SaveChangesAsync(ct);
    }
}
