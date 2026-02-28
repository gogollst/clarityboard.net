using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetKpiHistoryQuery : IRequest<IReadOnlyList<KpiSnapshotDto>>
{
    public required string KpiId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public class GetKpiHistoryQueryValidator : AbstractValidator<GetKpiHistoryQuery>
{
    public GetKpiHistoryQueryValidator()
    {
        RuleFor(x => x.KpiId).NotEmpty().MaximumLength(100);
    }
}

public class GetKpiHistoryQueryHandler : IRequestHandler<GetKpiHistoryQuery, IReadOnlyList<KpiSnapshotDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetKpiHistoryQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<KpiSnapshotDto>> Handle(
        GetKpiHistoryQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12));
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var snapshots = await _db.KpiSnapshots
            .Where(s => s.EntityId == entityId
                        && s.KpiId == request.KpiId
                        && s.SnapshotDate >= from
                        && s.SnapshotDate <= to)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => new KpiSnapshotDto
            {
                KpiId = s.KpiId,
                SnapshotDate = s.SnapshotDate,
                Value = s.Value,
                PreviousValue = s.PreviousValue,
                ChangePct = s.ChangePct,
                TargetValue = s.TargetValue,
                IsProvisional = s.IsProvisional,
            })
            .ToListAsync(cancellationToken);

        return snapshots;
    }
}
