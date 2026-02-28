namespace ClarityBoard.Domain.Entities.Entity;

public class TaxUnit
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid OrgantraegerId { get; private set; } // Controlling entity
    public string TaxType { get; private set; } = default!; // kst_organschaft, gewst_organschaft, ust_organschaft
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private TaxUnit() { }
}
