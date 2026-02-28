namespace ClarityBoard.Domain.Entities.Entity;

public class EntityRelationship
{
    public Guid Id { get; private set; }
    public Guid ParentEntityId { get; private set; }
    public Guid ChildEntityId { get; private set; }
    public decimal OwnershipPct { get; private set; }
    public string ConsolidationType { get; private set; } = default!; // full, proportional, equity, none
    public bool HasProfitTransferAgreement { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EntityRelationship() { }

    public static EntityRelationship Create(
        Guid parentEntityId,
        Guid childEntityId,
        decimal ownershipPct,
        string consolidationType,
        bool hasProfitTransferAgreement,
        DateOnly effectiveFrom)
    {
        return new EntityRelationship
        {
            Id = Guid.NewGuid(),
            ParentEntityId = parentEntityId,
            ChildEntityId = childEntityId,
            OwnershipPct = ownershipPct,
            ConsolidationType = consolidationType,
            HasProfitTransferAgreement = hasProfitTransferAgreement,
            EffectiveFrom = effectiveFrom,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
