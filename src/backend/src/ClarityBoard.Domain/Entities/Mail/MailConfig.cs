namespace ClarityBoard.Domain.Entities.Mail;

/// <summary>
/// Singleton SMTP configuration stored in the database.
/// Password is AES-256-GCM encrypted via IEncryptionService.
/// </summary>
public class MailConfig
{
    public Guid Id { get; private set; }
    public string Host { get; private set; } = default!;
    public int Port { get; private set; }
    public string Username { get; private set; } = default!;

    /// <summary>AES-256-GCM encrypted SMTP password.</summary>
    public string EncryptedPassword { get; private set; } = default!;

    public string FromEmail { get; private set; } = default!;
    public string FromName { get; private set; } = default!;
    public bool EnableSsl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private MailConfig() { }

    public static MailConfig Create(
        string host, int port, string username, string encryptedPassword,
        string fromEmail, string fromName, bool enableSsl)
    {
        return new MailConfig
        {
            Id                = Guid.NewGuid(),
            Host              = host,
            Port              = port,
            Username          = username,
            EncryptedPassword = encryptedPassword,
            FromEmail         = fromEmail,
            FromName          = fromName,
            EnableSsl         = enableSsl,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow,
        };
    }

    public void Update(string host, int port, string username, string encryptedPassword,
        string fromEmail, string fromName, bool enableSsl)
    {
        Host              = host;
        Port              = port;
        Username          = username;
        EncryptedPassword = encryptedPassword;
        FromEmail         = fromEmail;
        FromName          = fromName;
        EnableSsl         = enableSsl;
        UpdatedAt         = DateTime.UtcNow;
    }
}
