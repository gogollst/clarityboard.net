using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Integration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Queries;

public record GetDeadLetterEventsQuery : IRequest<PagedResult<WebhookEventDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? SourceType { get; init; }
}

public class GetDeadLetterEventsQueryHandler
    : IRequestHandler<GetDeadLetterEventsQuery, PagedResult<WebhookEventDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetDeadLetterEventsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<WebhookEventDto>> Handle(
        GetDeadLetterEventsQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var query = _db.WebhookEvents
            .Where(e =>
                e.EntityId == entityId
                && (e.Status == "failed" || e.Status == "dead_letter"));

        if (!string.IsNullOrWhiteSpace(request.SourceType))
            query = query.Where(e => e.SourceType == request.SourceType);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ReceivedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new WebhookEventDto
            {
                Id = e.Id,
                SourceType = e.SourceType,
                SourceId = e.SourceId,
                EventType = e.EventType,
                IdempotencyKey = e.IdempotencyKey,
                Status = e.Status,
                ErrorMessage = e.ErrorMessage,
                RetryCount = e.RetryCount,
                ProcessingDurationMs = e.ProcessingDurationMs,
                ReceivedAt = e.ReceivedAt,
                ProcessedAt = e.ProcessedAt,
                EntityId = e.EntityId,
                MappingRuleId = e.MappingRuleId,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<WebhookEventDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
