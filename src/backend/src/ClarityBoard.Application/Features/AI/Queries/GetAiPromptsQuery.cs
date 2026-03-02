using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.AI.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Queries;

public record GetAiPromptsQuery : IRequest<IReadOnlyList<AiPromptListDto>>
{
    public string? Module { get; init; }
}

public class GetAiPromptsQueryHandler
    : IRequestHandler<GetAiPromptsQuery, IReadOnlyList<AiPromptListDto>>
{
    private readonly IAppDbContext _db;

    public GetAiPromptsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AiPromptListDto>> Handle(
        GetAiPromptsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AiPrompts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(p => p.Module == request.Module);

        return await query
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PromptKey)
            .Select(p => new AiPromptListDto
            {
                Id              = p.Id,
                PromptKey       = p.PromptKey,
                Name            = p.Name,
                Module          = p.Module,
                PrimaryProvider = p.PrimaryProvider,
                FallbackProvider= p.FallbackProvider,
                IsActive        = p.IsActive,
                IsSystemPrompt  = p.IsSystemPrompt,
                Version         = p.Version,
                UpdatedAt       = p.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}

// ── Get single prompt with version history ────────────────────────────────────

public record GetAiPromptDetailQuery : IRequest<AiPromptDetailDto?>
{
    public required string PromptKey { get; init; }
}

public class GetAiPromptDetailQueryHandler
    : IRequestHandler<GetAiPromptDetailQuery, AiPromptDetailDto?>
{
    private readonly IAppDbContext _db;

    public GetAiPromptDetailQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AiPromptDetailDto?> Handle(
        GetAiPromptDetailQuery request, CancellationToken cancellationToken)
    {
        var prompt = await _db.AiPrompts
            .FirstOrDefaultAsync(p => p.PromptKey == request.PromptKey, cancellationToken);

        if (prompt is null) return null;

        var versions = await _db.AiPromptVersions
            .Where(v => v.PromptId == prompt.Id)
            .OrderByDescending(v => v.Version)
            .Select(v => new AiPromptVersionDto
            {
                Id                 = v.Id,
                Version            = v.Version,
                SystemPrompt       = v.SystemPrompt,
                UserPromptTemplate = v.UserPromptTemplate,
                PrimaryProvider    = v.PrimaryProvider,
                FallbackProvider   = v.FallbackProvider,
                ChangeSummary      = v.ChangeSummary,
                CreatedAt          = v.CreatedAt,
                CreatedByUserId    = v.CreatedByUserId,
            })
            .ToListAsync(cancellationToken);

        return new AiPromptDetailDto
        {
            Id                  = prompt.Id,
            PromptKey           = prompt.PromptKey,
            Name                = prompt.Name,
            Description         = prompt.Description,
            Module              = prompt.Module,
            FunctionDescription = prompt.FunctionDescription,
            SystemPrompt        = prompt.SystemPrompt,
            UserPromptTemplate  = prompt.UserPromptTemplate,
            ExampleInput        = prompt.ExampleInput,
            ExampleOutput       = prompt.ExampleOutput,
            PrimaryProvider     = prompt.PrimaryProvider,
            PrimaryModel        = prompt.PrimaryModel,
            FallbackProvider    = prompt.FallbackProvider,
            FallbackModel       = prompt.FallbackModel,
            Temperature         = prompt.Temperature,
            MaxTokens           = prompt.MaxTokens,
            IsActive            = prompt.IsActive,
            IsSystemPrompt      = prompt.IsSystemPrompt,
            Version             = prompt.Version,
            CreatedAt           = prompt.CreatedAt,
            UpdatedAt           = prompt.UpdatedAt,
            LastEditedByUserId  = prompt.LastEditedByUserId,
            Versions            = versions,
        };
    }
}

