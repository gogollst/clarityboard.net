using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.self")]
public record ListOnboardingChecklistsQuery : IRequest<List<OnboardingChecklistSummaryDto>>
{
    public required Guid EmployeeId { get; init; }
}

public record OnboardingChecklistSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class ListOnboardingChecklistsQueryHandler
    : IRequestHandler<ListOnboardingChecklistsQuery, List<OnboardingChecklistSummaryDto>>
{
    private readonly IAppDbContext _db;

    public ListOnboardingChecklistsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OnboardingChecklistSummaryDto>> Handle(
        ListOnboardingChecklistsQuery request,
        CancellationToken cancellationToken)
    {
        var checklists = await _db.OnboardingChecklists
            .Where(c => c.EmployeeId == request.EmployeeId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Status,
                c.CreatedAt,
                c.CompletedAt,
                TotalTasks     = c.Tasks.Count,
                CompletedTasks = c.Tasks.Count(t => t.IsCompleted),
            })
            .ToListAsync(cancellationToken);

        return checklists.Select(c => new OnboardingChecklistSummaryDto
        {
            Id             = c.Id,
            Title          = c.Title,
            Status         = c.Status.ToString(),
            TotalTasks     = c.TotalTasks,
            CompletedTasks = c.CompletedTasks,
            CreatedAt      = c.CreatedAt,
            CompletedAt    = c.CompletedAt,
        }).ToList();
    }
}
