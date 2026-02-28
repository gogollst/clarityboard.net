namespace ClarityBoard.Domain.Entities.Identity;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? EntityId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = default!; // create, update, delete, login, export, etc.
    public string TableName { get; private set; } = default!;
    public string? RecordId { get; private set; }
    public string? OldValues { get; private set; } // JSON
    public string? NewValues { get; private set; } // JSON
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string Hash { get; private set; } = default!; // SHA-256 for GoBD tamper detection
    public string? PreviousHash { get; private set; } // Chain link
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action, string tableName, string? recordId = null,
        Guid? entityId = null, Guid? userId = null,
        string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetHash(string hash, string? previousHash)
    {
        Hash = hash;
        PreviousHash = previousHash;
    }
}
