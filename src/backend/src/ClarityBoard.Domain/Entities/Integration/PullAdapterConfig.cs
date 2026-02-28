namespace ClarityBoard.Domain.Entities.Integration;

public class PullAdapterConfig
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string AdapterType { get; private set; } = default!; // fints, ecb_rates, datev_import
    public string Name { get; private set; } = default!;
    public string Configuration { get; private set; } = default!; // JSON: encrypted connection details
    public string Schedule { get; private set; } = default!; // Cron expression
    public bool IsActive { get; private set; } = true;
    public DateTime? LastRunAt { get; private set; }
    public string? LastRunStatus { get; private set; } // success, failed, partial
    public string? LastRunError { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PullAdapterConfig() { }

    public static PullAdapterConfig Create(
        Guid entityId, string adapterType, string name, string configuration, string schedule)
    {
        return new PullAdapterConfig
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            AdapterType = adapterType,
            Name = name,
            Configuration = configuration,
            Schedule = schedule,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void RecordRun(string status, string? error = null)
    {
        LastRunAt = DateTime.UtcNow;
        LastRunStatus = status;
        LastRunError = error;
    }
}
