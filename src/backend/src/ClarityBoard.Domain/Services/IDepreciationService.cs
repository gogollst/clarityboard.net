using ClarityBoard.Domain.Entities.Asset;

namespace ClarityBoard.Domain.Services;

/// <summary>
/// Calculates depreciation schedules for fixed assets using the specified method
/// (straight-line, declining balance) with pro-rata first-year support.
/// </summary>
public interface IDepreciationService
{
    /// <summary>
    /// Generates the full depreciation schedule for a fixed asset from its in-service date
    /// through the end of its useful life.
    /// </summary>
    IReadOnlyList<DepreciationSchedule> CalculateSchedule(FixedAsset asset);
}
