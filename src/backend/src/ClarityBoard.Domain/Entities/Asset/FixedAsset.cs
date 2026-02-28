namespace ClarityBoard.Domain.Entities.Asset;

public class FixedAsset
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string AssetNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string AssetCategory { get; private set; } = default!; // land, buildings, equipment, vehicles, intangible, financial
    public Guid AssetAccountId { get; private set; }
    public Guid DepreciationAccountId { get; private set; }
    public decimal AcquisitionCost { get; private set; }
    public DateOnly AcquisitionDate { get; private set; }
    public DateOnly? InServiceDate { get; private set; }
    public string DepreciationMethod { get; private set; } = "straight_line"; // straight_line, declining_balance, units_of_production
    public int UsefulLifeMonths { get; private set; }
    public decimal ResidualValue { get; private set; }
    public decimal AccumulatedDepreciation { get; private set; }
    public decimal BookValue { get; private set; }
    public string Status { get; private set; } = "active"; // active, fully_depreciated, disposed, impaired
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string? Location { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? AfaCode { get; private set; } // German AfA-Tabelle code

    private readonly List<DepreciationSchedule> _schedules = new();
    public IReadOnlyCollection<DepreciationSchedule> Schedules => _schedules.AsReadOnly();

    private FixedAsset() { }

    public static FixedAsset Create(
        Guid entityId, string assetNumber, string name, string assetCategory,
        Guid assetAccountId, Guid depreciationAccountId,
        decimal acquisitionCost, DateOnly acquisitionDate, int usefulLifeMonths,
        Guid createdBy, string depreciationMethod = "straight_line",
        decimal residualValue = 0, string? description = null)
    {
        return new FixedAsset
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            AssetNumber = assetNumber,
            Name = name,
            Description = description,
            AssetCategory = assetCategory,
            AssetAccountId = assetAccountId,
            DepreciationAccountId = depreciationAccountId,
            AcquisitionCost = acquisitionCost,
            AcquisitionDate = acquisitionDate,
            InServiceDate = acquisitionDate,
            DepreciationMethod = depreciationMethod,
            UsefulLifeMonths = usefulLifeMonths,
            ResidualValue = residualValue,
            BookValue = acquisitionCost,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void RecordDepreciation(decimal amount)
    {
        AccumulatedDepreciation += amount;
        BookValue = AcquisitionCost - AccumulatedDepreciation;
        if (BookValue <= ResidualValue)
            Status = "fully_depreciated";
    }

    public void AddSchedule(DepreciationSchedule schedule) => _schedules.Add(schedule);
}
