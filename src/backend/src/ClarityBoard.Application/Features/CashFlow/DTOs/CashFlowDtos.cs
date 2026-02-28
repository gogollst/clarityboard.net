namespace ClarityBoard.Application.Features.CashFlow.DTOs;

public record CashFlowOverviewDto
{
    public decimal OperatingInflow { get; init; }
    public decimal OperatingOutflow { get; init; }
    public decimal OperatingNet { get; init; }
    public decimal InvestingInflow { get; init; }
    public decimal InvestingOutflow { get; init; }
    public decimal InvestingNet { get; init; }
    public decimal FinancingInflow { get; init; }
    public decimal FinancingOutflow { get; init; }
    public decimal FinancingNet { get; init; }
    public decimal TotalNet { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
}

public record WeeklyForecastDto
{
    public short WeekNumber { get; init; }
    public DateOnly WeekStartDate { get; init; }
    public decimal ConfirmedInflow { get; init; }
    public decimal ProbableInflow { get; init; }
    public decimal PossibleInflow { get; init; }
    public decimal ConfirmedOutflow { get; init; }
    public decimal ProbableOutflow { get; init; }
    public decimal PossibleOutflow { get; init; }
    public decimal WeightedNetFlow { get; init; }
    public decimal CumulativeBalance { get; init; }
    public decimal ConfidenceLow { get; init; }
    public decimal ConfidenceHigh { get; init; }
}

public record CashFlowForecastDto
{
    public Guid EntityId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal OpeningBalance { get; init; }
    public IReadOnlyList<WeeklyForecastDto> Weeks { get; init; } = [];
    public DateTime CalculatedAt { get; init; }
}

public record CashFlowEntryDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public DateOnly EntryDate { get; init; }
    public required string Category { get; init; }
    public required string Subcategory { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public decimal BaseAmount { get; init; }
    public string? SourceType { get; init; }
    public string? Description { get; init; }
    public bool IsRecurring { get; init; }
    public required string Certainty { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WorkingCapitalSummaryDto
{
    public decimal? DSO { get; init; }
    public decimal? DIO { get; init; }
    public decimal? DPO { get; init; }
    public decimal? CCC { get; init; }
    public IReadOnlyList<AgingBucketDto> AgingBuckets { get; init; } = [];
}

public record AgingBucketDto
{
    public required string Label { get; init; }
    public int MinDays { get; init; }
    public int MaxDays { get; init; }
    public decimal Amount { get; init; }
}
