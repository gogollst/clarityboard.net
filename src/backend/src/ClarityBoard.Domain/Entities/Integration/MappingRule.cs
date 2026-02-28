namespace ClarityBoard.Domain.Entities.Integration;

public class MappingRule
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string SourceType { get; private set; } = default!; // stripe, hubspot, personio, bank
    public string EventType { get; private set; } = default!; // invoice.paid, deal.closed, etc.
    public string FieldMapping { get; private set; } = default!; // JSON: source field → target field mapping
    public Guid? DebitAccountId { get; private set; }
    public Guid? CreditAccountId { get; private set; }
    public string? VatCode { get; private set; }
    public string? CostCenter { get; private set; }
    public string? Condition { get; private set; } // JSONPath expression for conditional mapping
    public int Priority { get; private set; } // Higher = evaluated first
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private MappingRule() { }

    public static MappingRule Create(
        Guid entityId, string sourceType, string eventType, string fieldMapping,
        int priority = 0, string? condition = null,
        Guid? debitAccountId = null, Guid? creditAccountId = null,
        string? vatCode = null, string? costCenter = null)
    {
        return new MappingRule
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            SourceType = sourceType,
            EventType = eventType,
            FieldMapping = fieldMapping,
            Priority = priority,
            Condition = condition,
            DebitAccountId = debitAccountId,
            CreditAccountId = creditAccountId,
            VatCode = vatCode,
            CostCenter = costCenter,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(
        string fieldMapping, int priority, string? condition,
        Guid? debitAccountId, Guid? creditAccountId,
        string? vatCode, string? costCenter, bool isActive)
    {
        FieldMapping = fieldMapping;
        Priority = priority;
        Condition = condition;
        DebitAccountId = debitAccountId;
        CreditAccountId = creditAccountId;
        VatCode = vatCode;
        CostCenter = costCenter;
        IsActive = isActive;
    }
}
