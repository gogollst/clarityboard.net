using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetKpiAlertsQuery : IRequest<IReadOnlyList<KpiAlertDto>>;

public class GetKpiAlertsQueryHandler
    : IRequestHandler<GetKpiAlertsQuery, IReadOnlyList<KpiAlertDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetKpiAlertsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<KpiAlertDto>> Handle(
        GetKpiAlertsQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var alerts = await _db.KpiAlerts
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new KpiAlertDto
            {
                Id = a.Id,
                EntityId = a.EntityId,
                KpiId = a.KpiId,
                Name = a.Name,
                Condition = a.Condition,
                ThresholdValue = a.ThresholdValue,
                Severity = a.Severity,
                TargetRoles = a.TargetRoles,
                Channels = a.Channels,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return alerts;
    }
}
