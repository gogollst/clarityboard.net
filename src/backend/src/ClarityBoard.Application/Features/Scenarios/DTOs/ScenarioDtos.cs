namespace ClarityBoard.Application.Features.Scenarios.DTOs;

public record ScenarioListDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public int ProjectionMonths { get; init; }
    public int ParameterCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CalculatedAt { get; init; }
}

public record ScenarioDetailDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public int ProjectionMonths { get; init; }
    public int? Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CalculatedAt { get; init; }
    public DateOnly? BaselineDate { get; init; }
    public IReadOnlyList<ScenarioParameterDto> Parameters { get; init; } = [];
    public IReadOnlyList<ScenarioResultDto> Results { get; init; } = [];
}

public record ScenarioParameterDto
{
    public Guid Id { get; init; }
    public required string ParameterKey { get; init; }
    public decimal BaseValue { get; init; }
    public decimal AdjustedValue { get; init; }
    public string? Unit { get; init; }
    public string? Description { get; init; }
}

public record ScenarioResultDto
{
    public Guid Id { get; init; }
    public required string KpiId { get; init; }
    public int Month { get; init; }
    public decimal ProjectedValue { get; init; }
    public decimal BaselineValue { get; init; }
    public decimal DeltaValue { get; init; }
    public decimal DeltaPct { get; init; }
}

public record ScenarioParameterRequest
{
    public required string ParameterKey { get; init; }
    public decimal BaseValue { get; init; }
    public decimal AdjustedValue { get; init; }
    public string? Unit { get; init; }
    public string? Description { get; init; }
}
