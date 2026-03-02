namespace ClarityBoard.Domain.Entities.Mail;

public enum EmailType
{
    WelcomeEmail   = 1,
    PasswordReset  = 2,
    UserInvitation = 3,
    TwoFactorCode  = 4,
    SystemWarning  = 5,
}

public enum EmailStatus
{
    Sent    = 1,
    Failed  = 2,
    Retried = 3,
}

public class EmailLog
{
    public Guid Id { get; private set; }
    public EmailType Type { get; private set; }
    public string ToEmail { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public EmailStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime SentAt { get; private set; }

    private EmailLog() { }

    public static EmailLog Create(
        EmailType type, string toEmail, string subject,
        EmailStatus status, int attempts, string? errorMessage, Guid? userId)
    {
        return new EmailLog
        {
            Id           = Guid.NewGuid(),
            Type         = type,
            ToEmail      = toEmail,
            Subject      = subject,
            Status       = status,
            Attempts     = attempts,
            ErrorMessage = errorMessage,
            UserId       = userId,
            SentAt       = DateTime.UtcNow,
        };
    }
}
