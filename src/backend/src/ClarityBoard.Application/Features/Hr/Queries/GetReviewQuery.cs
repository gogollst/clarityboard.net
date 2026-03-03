using System.Text.Json;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetReviewQuery(Guid ReviewId) : IRequest<PerformanceReviewDetailDto>;

public record PerformanceReviewDetailDto : PerformanceReviewDto
{
    public string? StrengthsNotes { get; init; }
    public string? ImprovementNotes { get; init; }
    public string? GoalsNotes { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<FeedbackEntryDto> FeedbackEntries { get; init; } = [];
}

public record FeedbackEntryDto
{
    public Guid Id { get; init; }
    public Guid ReviewId { get; init; }
    public string RespondentType { get; init; } = string.Empty;
    public bool IsAnonymous { get; init; }
    public int Rating { get; init; }
    public string? Comments { get; init; }
    public Dictionary<string, int>? CompetencyScores { get; init; }
    public DateTime? SubmittedAt { get; init; }
}

public class GetReviewQueryHandler : IRequestHandler<GetReviewQuery, PerformanceReviewDetailDto>
{
    private readonly IAppDbContext _db;

    public GetReviewQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PerformanceReviewDetailDto> Handle(
        GetReviewQuery request, CancellationToken cancellationToken)
    {
        var review = await _db.PerformanceReviews
            .Include(r => r.FeedbackEntries)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken)
            ?? throw new InvalidOperationException($"Performance review '{request.ReviewId}' not found.");

        // Resolve employee name
        var employee = await _db.Employees
            .Where(e => e.Id == review.EmployeeId)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        // Resolve reviewer name
        var reviewer = await _db.Users
            .Where(u => u.Id == review.ReviewerId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        var feedbackDtos = review.FeedbackEntries.Select(f => new FeedbackEntryDto
        {
            Id             = f.Id,
            ReviewId       = f.ReviewId,
            RespondentType = f.RespondentType.ToString(),
            IsAnonymous    = f.IsAnonymous,
            Rating         = f.Rating,
            Comments       = f.Comments,
            CompetencyScores = f.CompetencyScores != null
                ? JsonSerializer.Deserialize<Dictionary<string, int>>(f.CompetencyScores)
                : null,
            SubmittedAt = f.SubmittedAt,
        }).ToList();

        return new PerformanceReviewDetailDto
        {
            Id               = review.Id,
            EmployeeId       = review.EmployeeId,
            EmployeeFullName = employee != null ? $"{employee.FirstName} {employee.LastName}" : string.Empty,
            ReviewerId       = review.ReviewerId,
            ReviewerFullName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : string.Empty,
            ReviewPeriodStart = review.ReviewPeriodStart,
            ReviewPeriodEnd   = review.ReviewPeriodEnd,
            ReviewType       = review.ReviewType.ToString(),
            Status           = review.Status.ToString(),
            OverallRating    = review.OverallRating,
            CreatedAt        = review.CreatedAt,
            StrengthsNotes   = review.StrengthsNotes,
            ImprovementNotes = review.ImprovementNotes,
            GoalsNotes       = review.GoalsNotes,
            CompletedAt      = review.CompletedAt,
            FeedbackEntries  = feedbackDtos,
        };
    }
}
