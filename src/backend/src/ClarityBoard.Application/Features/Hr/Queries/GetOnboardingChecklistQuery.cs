using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.self")]
public record GetOnboardingChecklistQuery : IRequest<OnboardingChecklistDetailDto>
{
    public required Guid ChecklistId { get; init; }
}

public record OnboardingChecklistDetailDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<OnboardingTaskDto> Tasks { get; init; } = [];
}

public record OnboardingTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateOnly? DueDate { get; init; }
    public int SortOrder { get; init; }
}

public class GetOnboardingChecklistQueryHandler
    : IRequestHandler<GetOnboardingChecklistQuery, OnboardingChecklistDetailDto>
{
    private readonly IAppDbContext _db;

    public GetOnboardingChecklistQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<OnboardingChecklistDetailDto> Handle(
        GetOnboardingChecklistQuery request,
        CancellationToken cancellationToken)
    {
        var checklist = await _db.OnboardingChecklists
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistId, cancellationToken)
            ?? throw new NotFoundException("OnboardingChecklist", request.ChecklistId);

        return new OnboardingChecklistDetailDto
        {
            Id          = checklist.Id,
            EmployeeId  = checklist.EmployeeId,
            Title       = checklist.Title,
            Status      = checklist.Status.ToString(),
            CreatedAt   = checklist.CreatedAt,
            CompletedAt = checklist.CompletedAt,
            Tasks = checklist.Tasks
                .OrderBy(t => t.SortOrder)
                .Select(t => new OnboardingTaskDto
                {
                    Id          = t.Id,
                    Title       = t.Title,
                    Description = t.Description,
                    IsCompleted = t.IsCompleted,
                    CompletedAt = t.CompletedAt,
                    DueDate     = t.DueDate,
                    SortOrder   = t.SortOrder,
                })
                .ToList(),
        };
    }
}
