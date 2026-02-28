using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.KPI.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Queries;

public record GetAlertEventsQuery : IRequest<IReadOnlyList<AlertEventDto>>
{
    public string? Status { get; init; }
}

public class GetAlertEventsQueryValidator : AbstractValidator<GetAlertEventsQuery>
{
    private static readonly string[] ValidStatuses = ["active", "acknowledged", "resolved"];

    public GetAlertEventsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || ValidStatuses.Contains(s))
            .WithMessage("Status must be one of: active, acknowledged, resolved.");
    }
}

public class GetAlertEventsQueryHandler
    : IRequestHandler<GetAlertEventsQuery, IReadOnlyList<AlertEventDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAlertEventsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AlertEventDto>> Handle(
        GetAlertEventsQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var query = _db.KpiAlertEvents
            .Where(e => e.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(e => e.Status == request.Status);
        }

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new AlertEventDto
            {
                Id = e.Id,
                AlertId = e.AlertId,
                EntityId = e.EntityId,
                KpiId = e.KpiId,
                CurrentValue = e.CurrentValue,
                ThresholdValue = e.ThresholdValue,
                Severity = e.Severity,
                Title = e.Title,
                Message = e.Message,
                Status = e.Status,
                AcknowledgedBy = e.AcknowledgedBy,
                AcknowledgedAt = e.AcknowledgedAt,
                ResolvedAt = e.ResolvedAt,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return events;
    }
}
