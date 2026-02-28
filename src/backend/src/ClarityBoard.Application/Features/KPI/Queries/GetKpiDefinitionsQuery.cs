using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetKpiDefinitionsQuery : IRequest<IReadOnlyList<KpiDefinitionDto>>
{
    public string? Domain { get; init; }
}

public class GetKpiDefinitionsQueryHandler
    : IRequestHandler<GetKpiDefinitionsQuery, IReadOnlyList<KpiDefinitionDto>>
{
    private readonly IAppDbContext _db;

    public GetKpiDefinitionsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<KpiDefinitionDto>> Handle(
        GetKpiDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.KpiDefinitions
            .Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Domain))
        {
            query = query.Where(d => d.Domain == request.Domain);
        }

        var definitions = await query
            .OrderBy(d => d.DisplayOrder)
            .Select(d => new KpiDefinitionDto
            {
                Id = d.Id,
                Domain = d.Domain,
                Name = d.Name,
                Formula = d.Formula,
                Unit = d.Unit,
                Direction = d.Direction,
                Category = d.Category,
                DisplayOrder = d.DisplayOrder,
            })
            .ToListAsync(cancellationToken);

        return definitions;
    }
}
