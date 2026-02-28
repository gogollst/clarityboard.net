namespace ClarityBoard.Domain.Entities.Asset;

public class AssetDisposal
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid EntityId { get; private set; }
    public DateOnly DisposalDate { get; private set; }
    public string DisposalType { get; private set; } = default!; // sale, scrap, transfer, write_off
    public decimal DisposalProceeds { get; private set; }
    public decimal BookValueAtDisposal { get; private set; }
    public decimal GainLoss { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private AssetDisposal() { }

    public static AssetDisposal Create(
        Guid assetId, Guid entityId, DateOnly disposalDate, string disposalType,
        decimal disposalProceeds, decimal bookValueAtDisposal, Guid createdBy, string? notes = null)
    {
        return new AssetDisposal
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            EntityId = entityId,
            DisposalDate = disposalDate,
            DisposalType = disposalType,
            DisposalProceeds = disposalProceeds,
            BookValueAtDisposal = bookValueAtDisposal,
            GainLoss = disposalProceeds - bookValueAtDisposal,
            CreatedBy = createdBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
