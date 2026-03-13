using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Queries;

public record ProviderModelDto
{
    public Guid Id { get; init; }
    public AiProvider Provider { get; init; }
    public string ModelId { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Returns all provider models. Optionally filter by provider.
/// </summary>
public record GetProviderModelsQuery : IRequest<IReadOnlyList<ProviderModelDto>>
{
    public AiProvider? Provider { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public class GetProviderModelsQueryHandler
    : IRequestHandler<GetProviderModelsQuery, IReadOnlyList<ProviderModelDto>>
{
    private readonly IAppDbContext _db;

    public GetProviderModelsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProviderModelDto>> Handle(
        GetProviderModelsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AiProviderModels.AsQueryable();

        if (request.Provider.HasValue)
            query = query.Where(m => m.Provider == request.Provider.Value);

        if (request.ActiveOnly)
            query = query.Where(m => m.IsActive);

        return await query
            .OrderBy(m => m.Provider)
            .ThenBy(m => m.SortOrder)
            .Select(m => new ProviderModelDto
            {
                Id          = m.Id,
                Provider    = m.Provider,
                ModelId     = m.ModelId,
                DisplayName = m.DisplayName,
                SortOrder   = m.SortOrder,
                Description = m.Description,
                IsActive    = m.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}
