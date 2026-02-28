using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Integration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Queries;

public record GetWebhookConfigsQuery : IRequest<IReadOnlyList<WebhookConfigDto>>;

public class GetWebhookConfigsQueryHandler
    : IRequestHandler<GetWebhookConfigsQuery, IReadOnlyList<WebhookConfigDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetWebhookConfigsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WebhookConfigDto>> Handle(
        GetWebhookConfigsQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var configs = await _db.WebhookConfigs
            .Where(c => c.EntityId == entityId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new WebhookConfigDto
            {
                Id = c.Id,
                EntityId = c.EntityId,
                SourceType = c.SourceType,
                Name = c.Name,
                EndpointPath = c.EndpointPath,
                HeaderSignatureKey = c.HeaderSignatureKey,
                IsActive = c.IsActive,
                EventFilter = c.EventFilter,
                CreatedAt = c.CreatedAt,
                LastReceivedAt = c.LastReceivedAt,
            })
            .ToListAsync(cancellationToken);

        return configs;
    }
}
