namespace ClarityBoard.Domain.Entities.Scenario;

public class SimulationRun
{
    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public int RunNumber { get; private set; }
    public string Status { get; private set; } = "pending"; // pending, running, completed, failed
    public string? InputSnapshot { get; private set; } // JSON snapshot of parameters
    public string? OutputSummary { get; private set; } // JSON summary of results
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private SimulationRun() { }

    public static SimulationRun Create(Guid scenarioId, int runNumber, string? inputSnapshot = null)
    {
        return new SimulationRun
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            RunNumber = runNumber,
            InputSnapshot = inputSnapshot,
            StartedAt = DateTime.UtcNow,
        };
    }

    public void MarkRunning()
    {
        Status = "running";
        StartedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(string outputSummary)
    {
        Status = "completed";
        OutputSummary = outputSummary;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
