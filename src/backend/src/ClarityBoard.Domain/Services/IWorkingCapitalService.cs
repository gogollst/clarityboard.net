namespace ClarityBoard.Domain.Services;

/// <summary>
/// Calculates working capital metrics (DSO, DIO, DPO, CCC) and
/// accounts receivable aging buckets for a legal entity.
/// </summary>
public interface IWorkingCapitalService
{
    Task<WorkingCapitalResult> CalculateAsync(
        Guid entityId, DateOnly asOfDate, CancellationToken ct = default);
}

/// <summary>
/// Result of a working capital calculation containing the four
/// Cash-Conversion-Cycle components and AR aging breakdown.
/// </summary>
public record WorkingCapitalResult
{
    /// <summary>Days Sales Outstanding (AR / daily revenue). Null when revenue is zero.</summary>
    public decimal? DSO { get; init; }

    /// <summary>Days Inventory Outstanding (Inventory / daily COGS). Null when COGS is zero.</summary>
    public decimal? DIO { get; init; }

    /// <summary>Days Payable Outstanding (AP / daily COGS). Null when COGS is zero.</summary>
    public decimal? DPO { get; init; }

    /// <summary>Cash Conversion Cycle (DSO + DIO - DPO). Null when any component is null.</summary>
    public decimal? CCC { get; init; }

    /// <summary>Accounts receivable grouped into aging buckets.</summary>
    public IReadOnlyList<AgingBucket> AgingBuckets { get; init; } = [];
}

/// <summary>
/// A single aging bucket representing the total outstanding AR
/// for a given age range (e.g. 0-30 days, 31-60 days).
/// </summary>
public record AgingBucket
{
    public required string Label { get; init; }
    public int MinDays { get; init; }
    public int MaxDays { get; init; }
    public decimal Amount { get; init; }
}
