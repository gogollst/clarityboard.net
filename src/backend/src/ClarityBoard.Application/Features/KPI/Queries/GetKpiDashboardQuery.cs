using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetKpiDashboardQuery : IRequest<KpiDashboardDto>;

public class GetKpiDashboardQueryHandler : IRequestHandler<GetKpiDashboardQuery, KpiDashboardDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetKpiDashboardQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<KpiDashboardDto> Handle(GetKpiDashboardQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        // Get latest snapshot per KPI for the entity
        var latestSnapshots = await _db.KpiSnapshots
            .Where(s => s.EntityId == entityId)
            .GroupBy(s => s.KpiId)
            .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
            .ToListAsync(cancellationToken);

        var kpiDefinitions = await _db.KpiDefinitions
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync(cancellationToken);

        var kpis = kpiDefinitions.Select(def =>
        {
            var snapshot = latestSnapshots.FirstOrDefault(s => s.KpiId == def.Id);
            string? trendDirection = null;
            if (snapshot?.ChangePct > 0.5m) trendDirection = "up";
            else if (snapshot?.ChangePct < -0.5m) trendDirection = "down";
            else if (snapshot is not null) trendDirection = "flat";

            return new KpiSummaryDto
            {
                KpiId = def.Id,
                Name = def.Name,
                Domain = def.Domain,
                Unit = def.Unit,
                Direction = def.Direction,
                Value = snapshot?.Value,
                PreviousValue = snapshot?.PreviousValue,
                ChangePct = snapshot?.ChangePct,
                TargetValue = snapshot?.TargetValue,
                SnapshotDate = snapshot?.SnapshotDate,
                TrendDirection = trendDirection,
            };
        }).ToList();

        var activeAlerts = await _db.KpiAlertEvents
            .CountAsync(a => a.EntityId == entityId && a.Status == "active", cancellationToken);

        return new KpiDashboardDto
        {
            Kpis = kpis,
            ActiveAlerts = activeAlerts,
            LastUpdated = latestSnapshots.MaxBy(s => s.CalculatedAt)?.CalculatedAt ?? DateTime.UtcNow,
        };
    }
}
