using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

public record GetAuthConfigQuery : IRequest<AuthConfigResponse>;

public record AuthConfigResponse
{
    public int TokenLifetimeHours { get; init; }
    public int RememberMeTokenLifetimeDays { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class GetAuthConfigQueryHandler : IRequestHandler<GetAuthConfigQuery, AuthConfigResponse>
{
    private readonly IAppDbContext _db;

    public GetAuthConfigQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthConfigResponse> Handle(GetAuthConfigQuery request, CancellationToken ct)
    {
        var config = await _db.AuthConfigs.FirstOrDefaultAsync(ct)
            ?? AuthConfig.CreateDefault();

        return new AuthConfigResponse
        {
            TokenLifetimeHours = config.TokenLifetimeHours,
            RememberMeTokenLifetimeDays = config.RememberMeTokenLifetimeDays,
            UpdatedAt = config.UpdatedAt,
        };
    }
}
