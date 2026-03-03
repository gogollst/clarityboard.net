namespace ClarityBoard.Domain.Entities.Hr;

public class DataAccessLog
{
    public Guid Id { get; private set; }
    public Guid AccessedEmployeeId { get; private set; }
    public Guid AccessedBy { get; private set; }
    public string AccessType { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public Guid? ResourceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime AccessedAt { get; private set; }

    private DataAccessLog() { }

    public static DataAccessLog Create(Guid accessedEmployeeId, Guid accessedBy,
        string accessType, string resourceType, Guid? resourceId = null,
        string? ipAddress = null, string? userAgent = null)
    => new()
    {
        Id                 = Guid.NewGuid(),
        AccessedEmployeeId = accessedEmployeeId,
        AccessedBy         = accessedBy,
        AccessType         = accessType,
        ResourceType       = resourceType,
        ResourceId         = resourceId,
        IpAddress          = ipAddress,
        UserAgent          = userAgent,
        AccessedAt         = DateTime.UtcNow,
    };
}
