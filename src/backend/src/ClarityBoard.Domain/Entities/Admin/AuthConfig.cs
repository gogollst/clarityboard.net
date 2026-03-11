namespace ClarityBoard.Domain.Entities.Admin;

public class AuthConfig
{
    public Guid Id { get; private set; }
    public int TokenLifetimeHours { get; private set; } = 24;
    public int RememberMeTokenLifetimeDays { get; private set; } = 30;
    public DateTime UpdatedAt { get; private set; }

    public static AuthConfig CreateDefault() => new()
    {
        Id = Guid.NewGuid(),
        TokenLifetimeHours = 24,
        RememberMeTokenLifetimeDays = 30,
        UpdatedAt = DateTime.UtcNow,
    };

    public void Update(int tokenLifetimeHours, int rememberMeTokenLifetimeDays)
    {
        TokenLifetimeHours = tokenLifetimeHours;
        RememberMeTokenLifetimeDays = rememberMeTokenLifetimeDays;
        UpdatedAt = DateTime.UtcNow;
    }
}
