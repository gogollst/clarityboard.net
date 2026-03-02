using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.AI.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Queries;

public record GetAiProvidersQuery : IRequest<IReadOnlyList<AiProviderConfigDto>>;

public class GetAiProvidersQueryHandler
    : IRequestHandler<GetAiProvidersQuery, IReadOnlyList<AiProviderConfigDto>>
{
    private readonly IAppDbContext _db;

    public GetAiProvidersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AiProviderConfigDto>> Handle(
        GetAiProvidersQuery request, CancellationToken cancellationToken)
    {
        return await _db.AiProviderConfigs
            .OrderBy(p => p.Provider)
            .Select(p => new AiProviderConfigDto
            {
                Id           = p.Id,
                Provider     = p.Provider,
                ProviderName = p.Provider.ToString(),
                KeyHint      = $"****...{p.KeyHint}",
                IsActive     = p.IsActive,
                IsHealthy    = p.IsHealthy,
                LastTestedAt = p.LastTestedAt,
                BaseUrl      = p.BaseUrl,
                ModelDefault = p.ModelDefault,
                CreatedAt    = p.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}

