namespace ClarityBoard.Application.Features.Budget.DTOs;

public record BudgetListDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string Name { get; init; }
    public short FiscalYear { get; init; }
    public required string BudgetType { get; init; }
    public required string Status { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Department { get; init; }
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public record BudgetDetailDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string Name { get; init; }
    public short FiscalYear { get; init; }
    public required string BudgetType { get; init; }
    public required string Status { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public int? Version { get; init; }
    public string? Department { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public IReadOnlyList<BudgetLineDto> Lines { get; init; } = [];
    public IReadOnlyList<BudgetRevisionDto> Revisions { get; init; } = [];
}

public record BudgetLineDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountName { get; init; }
    public string? CostCenter { get; init; }
    public short Month { get; init; }
    public decimal Amount { get; init; }
    public decimal ActualAmount { get; init; }
    public decimal Variance { get; init; }
    public decimal VariancePct { get; init; }
    public string? Notes { get; init; }
}

public record BudgetRevisionDto
{
    public Guid Id { get; init; }
    public int RevisionNumber { get; init; }
    public required string Reason { get; init; }
    public required string Changes { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record BudgetLineRequest
{
    public required Guid AccountId { get; init; }
    public short Month { get; init; }
    public decimal Amount { get; init; }
    public string? CostCenter { get; init; }
    public string? Notes { get; init; }
}

public record PlanVsActualDto
{
    public Guid BudgetId { get; init; }
    public required string BudgetName { get; init; }
    public short FiscalYear { get; init; }
    public IReadOnlyList<PlanVsActualLineDto> Lines { get; init; } = [];
    public decimal TotalPlanned { get; init; }
    public decimal TotalActual { get; init; }
    public decimal TotalVariance { get; init; }
    public decimal TotalVariancePct { get; init; }
}

public record PlanVsActualLineDto
{
    public Guid AccountId { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountName { get; init; }
    public short Month { get; init; }
    public decimal PlannedAmount { get; init; }
    public decimal ActualAmount { get; init; }
    public decimal Variance { get; init; }
    public decimal VariancePct { get; init; }
}
