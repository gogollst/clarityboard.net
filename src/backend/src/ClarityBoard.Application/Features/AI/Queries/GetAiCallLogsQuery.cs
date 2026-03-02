using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.AI.DTOs;
using ClarityBoard.Domain.Entities.AI;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Queries;

public record GetAiCallLogsQuery : IRequest<PagedResult<AiCallLogDto>>
{
    public string? PromptKey { get; init; }
    public AiProvider? Provider { get; init; }
    public bool? IsSuccess { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetAiCallLogsQueryHandler
    : IRequestHandler<GetAiCallLogsQuery, PagedResult<AiCallLogDto>>
{
    private readonly IAppDbContext _db;

    public GetAiCallLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AiCallLogDto>> Handle(
        GetAiCallLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AiCallLogs
            .Join(_db.AiPrompts, log => log.PromptId, p => p.Id,
                (log, p) => new { log, p.PromptKey })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.PromptKey))
            query = query.Where(x => x.PromptKey == request.PromptKey);

        if (request.Provider.HasValue)
            query = query.Where(x => x.log.UsedProvider == request.Provider.Value);

        if (request.IsSuccess.HasValue)
            query = query.Where(x => x.log.IsSuccess == request.IsSuccess.Value);

        if (request.From.HasValue)
            query = query.Where(x => x.log.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.log.CreatedAt <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.log.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AiCallLogDto
            {
                Id           = x.log.Id,
                PromptId     = x.log.PromptId,
                PromptKey    = x.PromptKey,
                UsedProvider = x.log.UsedProvider,
                UsedFallback = x.log.UsedFallback,
                InputTokens  = x.log.InputTokens,
                OutputTokens = x.log.OutputTokens,
                DurationMs   = x.log.DurationMs,
                IsSuccess    = x.log.IsSuccess,
                ErrorMessage = x.log.ErrorMessage,
                UserId       = x.log.UserId,
                EntityId     = x.log.EntityId,
                CreatedAt    = x.log.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AiCallLogDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}

// ── Stats query ───────────────────────────────────────────────────────────────

public record GetAiCallLogStatsQuery : IRequest<AiCallLogStatsDto>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public class GetAiCallLogStatsQueryHandler
    : IRequestHandler<GetAiCallLogStatsQuery, AiCallLogStatsDto>
{
    private readonly IAppDbContext _db;

    public GetAiCallLogStatsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AiCallLogStatsDto> Handle(
        GetAiCallLogStatsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AiCallLogs.AsQueryable();

        if (request.From.HasValue) query = query.Where(l => l.CreatedAt >= request.From.Value);
        if (request.To.HasValue)   query = query.Where(l => l.CreatedAt <= request.To.Value);

        var total    = await query.CountAsync(cancellationToken);
        var success  = await query.CountAsync(l => l.IsSuccess, cancellationToken);
        var fallback = await query.CountAsync(l => l.UsedFallback, cancellationToken);
        var avgMs    = total > 0 ? (int)await query.AverageAsync(l => (double)l.DurationMs, cancellationToken) : 0;
        var inTok    = await query.SumAsync(l => l.InputTokens, cancellationToken);
        var outTok   = await query.SumAsync(l => l.OutputTokens, cancellationToken);

        return new AiCallLogStatsDto
        {
            TotalCalls        = total,
            SuccessfulCalls   = success,
            SuccessRate       = total > 0 ? Math.Round((double)success / total * 100, 1) : 0,
            AvgDurationMs     = avgMs,
            TotalInputTokens  = inTok,
            TotalOutputTokens = outTok,
            FallbackCount     = fallback,
        };
    }
}

