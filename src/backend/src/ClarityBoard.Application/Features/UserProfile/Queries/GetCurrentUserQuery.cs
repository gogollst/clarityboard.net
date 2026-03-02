using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.UserProfile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.UserProfile.Queries;

public record GetCurrentUserQuery : IRequest<UserProfileResponse>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserProfileResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Locale = user.Locale,
            Timezone = user.Timezone,
            Bio = user.Bio,
            AvatarUrl = user.AvatarPath is not null ? $"/api/avatars/{user.Id}" : null,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
        };
    }
}
