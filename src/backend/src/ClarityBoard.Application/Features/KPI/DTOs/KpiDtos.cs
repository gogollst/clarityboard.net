using ClarityBoard.Domain.Services;

namespace ClarityBoard.Application.Features.KPI.DTOs;

public record KpiSummaryDto
{
    public required string KpiId { get; init; }
    public required string Name { get; init; }
    public required string Domain { get; init; }
    public required string Unit { get; init; }
    public required string Direction { get; init; }
    public decimal? Value { get; init; }
    public decimal? PreviousValue { get; init; }
    public decimal? ChangePct { get; init; }
    public decimal? TargetValue { get; init; }
    public DateOnly? SnapshotDate { get; init; }
    public string? TrendDirection { get; init; } // up, down, flat
}

public record KpiSnapshotDto
{
    public required string KpiId { get; init; }
    public required DateOnly SnapshotDate { get; init; }
    public decimal? Value { get; init; }
    public decimal? PreviousValue { get; init; }
    public decimal? ChangePct { get; init; }
    public decimal? TargetValue { get; init; }
    public bool IsProvisional { get; init; }
}

public record KpiDashboardDto
{
    public IReadOnlyList<KpiSummaryDto> Kpis { get; init; } = [];
    public int ActiveAlerts { get; init; }
    public DateTime LastUpdated { get; init; }
}

public record KpiDefinitionDto
{
    public required string Id { get; init; }
    public required string Domain { get; init; }
    public required string Name { get; init; }
    public required string Formula { get; init; }
    public required string Unit { get; init; }
    public required string Direction { get; init; }
    public string? Category { get; init; }
    public int DisplayOrder { get; init; }
}

public record KpiAlertDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string KpiId { get; init; }
    public required string Name { get; init; }
    public required string Condition { get; init; }
    public decimal ThresholdValue { get; init; }
    public required string Severity { get; init; }
    public string TargetRoles { get; init; } = "[]";
    public string Channels { get; init; } = "[]";
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AlertEventDto
{
    public Guid Id { get; init; }
    public Guid AlertId { get; init; }
    public Guid EntityId { get; init; }
    public required string KpiId { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal ThresholdValue { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Status { get; init; }
    public Guid? AcknowledgedBy { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record KpiDrillDownDto
{
    public required string KpiId { get; init; }
    public required string Name { get; init; }
    public decimal? Value { get; init; }
    public DateOnly SnapshotDate { get; init; }
    public Dictionary<string, object?> Components { get; init; } = new();
}

public record WorkingCapitalDto
{
    public decimal? DSO { get; init; }
    public decimal? DIO { get; init; }
    public decimal? DPO { get; init; }
    public decimal? CCC { get; init; }
    public IReadOnlyList<AgingBucket> AgingBuckets { get; init; } = [];
}
