namespace ClarityBoard.Domain.Entities.Scenario;

public class Scenario
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Type { get; private set; } = default!; // best_case, worst_case, most_likely, custom, stress_test
    public string Status { get; private set; } = "draft"; // draft, calculating, completed, archived
    public int ProjectionMonths { get; private set; }
    public int? Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? CalculatedAt { get; private set; }
    public DateOnly? BaselineDate { get; private set; }
    public Guid? ComparedToScenarioId { get; private set; }

    private readonly List<ScenarioParameter> _parameters = new();
    public IReadOnlyCollection<ScenarioParameter> Parameters => _parameters.AsReadOnly();

    private Scenario() { }

    public static Scenario Create(
        Guid entityId, string name, string type, int projectionMonths, Guid createdBy, string? description = null)
    {
        return new Scenario
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Name = name,
            Type = type,
            ProjectionMonths = projectionMonths,
            CreatedBy = createdBy,
            Description = description,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void AddParameter(ScenarioParameter parameter) => _parameters.Add(parameter);

    public void MarkCalculating() => Status = "calculating";

    public void MarkCompleted()
    {
        Status = "completed";
        CalculatedAt = DateTime.UtcNow;
    }
}
