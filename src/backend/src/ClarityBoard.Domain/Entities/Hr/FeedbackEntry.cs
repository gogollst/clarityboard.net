namespace ClarityBoard.Domain.Entities.Hr;

public class FeedbackEntry
{
    public Guid Id { get; private set; }
    public Guid ReviewId { get; private set; }
    public Guid RespondentId { get; private set; }
    public RespondentType RespondentType { get; private set; }
    public bool IsAnonymous { get; private set; }
    public int Rating { get; private set; }
    public string? Comments { get; private set; }
    public string? CompetencyScores { get; private set; }  // JSON string
    public DateTime? SubmittedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FeedbackEntry() { }

    public static FeedbackEntry Create(Guid reviewId, Guid respondentId, RespondentType type, bool isAnonymous)
    => new()
    {
        Id             = Guid.NewGuid(),
        ReviewId       = reviewId,
        RespondentId   = respondentId,
        RespondentType = type,
        IsAnonymous    = isAnonymous,
        Rating         = 0,
        CreatedAt      = DateTime.UtcNow,
    };

    public void Submit(int rating, string? comments, string? competencyScoresJson)
    {
        Rating            = rating;
        Comments          = comments;
        CompetencyScores  = competencyScoresJson;
        SubmittedAt       = DateTime.UtcNow;
    }
}
