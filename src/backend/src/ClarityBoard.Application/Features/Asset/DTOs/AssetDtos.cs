namespace ClarityBoard.Application.Features.Asset.DTOs;

public record AssetListDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string AssetNumber { get; init; }
    public required string Name { get; init; }
    public required string AssetCategory { get; init; }
    public decimal AcquisitionCost { get; init; }
    public DateOnly AcquisitionDate { get; init; }
    public required string DepreciationMethod { get; init; }
    public decimal BookValue { get; init; }
    public required string Status { get; init; }
    public string? Location { get; init; }
}

public record AssetDetailDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string AssetNumber { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AssetCategory { get; init; }
    public decimal AcquisitionCost { get; init; }
    public DateOnly AcquisitionDate { get; init; }
    public DateOnly? InServiceDate { get; init; }
    public required string DepreciationMethod { get; init; }
    public int UsefulLifeMonths { get; init; }
    public decimal ResidualValue { get; init; }
    public decimal AccumulatedDepreciation { get; init; }
    public decimal BookValue { get; init; }
    public required string Status { get; init; }
    public string? Location { get; init; }
    public string? SerialNumber { get; init; }
    public string? AfaCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<DepreciationScheduleDto> Schedules { get; init; } = [];
    public AssetDisposalDto? Disposal { get; init; }
}

public record DepreciationScheduleDto
{
    public Guid Id { get; init; }
    public DateOnly PeriodDate { get; init; }
    public decimal DepreciationAmount { get; init; }
    public decimal AccumulatedAmount { get; init; }
    public decimal BookValueAfter { get; init; }
    public bool IsPosted { get; init; }
    public DateTime? PostedAt { get; init; }
}

public record AssetDisposalDto
{
    public Guid Id { get; init; }
    public DateOnly DisposalDate { get; init; }
    public required string DisposalType { get; init; }
    public decimal DisposalProceeds { get; init; }
    public decimal BookValueAtDisposal { get; init; }
    public decimal GainLoss { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Anlagenspiegel (asset movement report) per category.
/// </summary>
public record AnlagenspiegelDto
{
    public Guid EntityId { get; init; }
    public short FiscalYear { get; init; }
    public IReadOnlyList<AnlagenspiegelCategoryDto> Categories { get; init; } = [];
    public decimal TotalOpeningCost { get; init; }
    public decimal TotalAdditions { get; init; }
    public decimal TotalDisposals { get; init; }
    public decimal TotalDepreciation { get; init; }
    public decimal TotalClosingBookValue { get; init; }
}

public record AnlagenspiegelCategoryDto
{
    public required string Category { get; init; }
    public decimal OpeningCost { get; init; }
    public decimal Additions { get; init; }
    public decimal Disposals { get; init; }
    public decimal DepreciationCharge { get; init; }
    public decimal AccumulatedDepreciation { get; init; }
    public decimal ClosingBookValue { get; init; }
    public int AssetCount { get; init; }
}
