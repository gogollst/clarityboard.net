using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetKpiDrillDownQuery : IRequest<KpiDrillDownDto?>
{
    public required string KpiId { get; init; }
}

public class GetKpiDrillDownQueryValidator : AbstractValidator<GetKpiDrillDownQuery>
{
    public GetKpiDrillDownQueryValidator()
    {
        RuleFor(x => x.KpiId).NotEmpty().MaximumLength(100);
    }
}

public class GetKpiDrillDownQueryHandler
    : IRequestHandler<GetKpiDrillDownQuery, KpiDrillDownDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetKpiDrillDownQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<KpiDrillDownDto?> Handle(
        GetKpiDrillDownQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        // Get KPI definition
        var definition = await _db.KpiDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.KpiId, cancellationToken);

        if (definition is null)
            return null;

        // Get latest snapshot with components
        var snapshot = await _db.KpiSnapshots
            .Where(s => s.EntityId == entityId && s.KpiId == request.KpiId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
            return null;

        // Parse components JSON
        var components = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(snapshot.Components))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshot.Components);
                if (parsed is not null)
                {
                    foreach (var kvp in parsed)
                    {
                        components[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => kvp.Value.GetDecimal(),
                            JsonValueKind.String => kvp.Value.GetString(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => kvp.Value.GetRawText(),
                        };
                    }
                }
            }
            catch (JsonException)
            {
                // Components field is not valid JSON; return empty breakdown
            }
        }

        return new KpiDrillDownDto
        {
            KpiId = definition.Id,
            Name = definition.Name,
            Value = snapshot.Value,
            SnapshotDate = snapshot.SnapshotDate,
            Components = components,
        };
    }
}
