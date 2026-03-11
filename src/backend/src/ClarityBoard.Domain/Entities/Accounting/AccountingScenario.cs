namespace ClarityBoard.Domain.Entities.Accounting;

public enum AccountingScenarioType { Budget, Forecast, Custom }

public class AccountingScenario
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public AccountingScenarioType ScenarioType { get; private set; }
    public int Year { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsBaseline { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private AccountingScenario() { }

    public static AccountingScenario Create(
        Guid entityId, string name, AccountingScenarioType type,
        int year, Guid createdBy, string? description = null)
    {
        return new AccountingScenario
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Name = name,
            Description = description,
            ScenarioType = type,
            Year = year,
            IsLocked = false,
            IsBaseline = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;
    public void SetAsBaseline() => IsBaseline = true;
}
