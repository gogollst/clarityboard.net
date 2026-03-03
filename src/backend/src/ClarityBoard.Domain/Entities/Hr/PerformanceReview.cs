namespace ClarityBoard.Domain.Entities.Hr;

public class PerformanceReview
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public DateOnly ReviewPeriodStart { get; private set; }
    public DateOnly ReviewPeriodEnd { get; private set; }
    public ReviewType ReviewType { get; private set; }
    public ReviewStatus Status { get; private set; }
    public int? OverallRating { get; private set; }
    public string? StrengthsNotes { get; private set; }
    public string? ImprovementNotes { get; private set; }
    public string? GoalsNotes { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<FeedbackEntry> FeedbackEntries { get; private set; } = [];

    private PerformanceReview() { }

    public static PerformanceReview Create(Guid employeeId, Guid reviewerId,
        DateOnly periodStart, DateOnly periodEnd, ReviewType type)
    => new()
    {
        Id                = Guid.NewGuid(),
        EmployeeId        = employeeId,
        ReviewerId        = reviewerId,
        ReviewPeriodStart = periodStart,
        ReviewPeriodEnd   = periodEnd,
        ReviewType        = type,
        Status            = ReviewStatus.Draft,
        CreatedAt         = DateTime.UtcNow,
    };

    public void StartProgress() => Status = ReviewStatus.InProgress;

    public void Complete(int? rating, string? strengths, string? improvement, string? goals)
    {
        Status           = ReviewStatus.Completed;
        OverallRating    = rating;
        StrengthsNotes   = strengths;
        ImprovementNotes = improvement;
        GoalsNotes       = goals;
        CompletedAt      = DateTime.UtcNow;
    }
}
