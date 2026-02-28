using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Scenarios.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Scenarios.Queries;

public record GetScenariosQuery : IRequest<IReadOnlyList<ScenarioListDto>>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public string? Type { get; init; }
    public string? Status { get; init; }
}

public class GetScenariosQueryHandler
    : IRequestHandler<GetScenariosQuery, IReadOnlyList<ScenarioListDto>>
{
    private readonly IAppDbContext _db;

    public GetScenariosQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ScenarioListDto>> Handle(
        GetScenariosQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Scenarios
            .Where(s => s.EntityId == request.EntityId);

        if (!string.IsNullOrEmpty(request.Type))
            query = query.Where(s => s.Type == request.Type);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(s => s.Status == request.Status);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ScenarioListDto
            {
                Id = s.Id,
                EntityId = s.EntityId,
                Name = s.Name,
                Type = s.Type,
                Status = s.Status,
                ProjectionMonths = s.ProjectionMonths,
                ParameterCount = s.Parameters.Count,
                CreatedAt = s.CreatedAt,
                CalculatedAt = s.CalculatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
