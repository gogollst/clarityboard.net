namespace ClarityBoard.Application.Features.Integration.DTOs;

public record WebhookConfigDto
{
    public required Guid Id { get; init; }
    public required Guid EntityId { get; init; }
    public required string SourceType { get; init; }
    public required string Name { get; init; }
    public required string EndpointPath { get; init; }
    public string? HeaderSignatureKey { get; init; }
    public bool IsActive { get; init; }
    public string? EventFilter { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastReceivedAt { get; init; }
}

public record MappingRuleDto
{
    public required Guid Id { get; init; }
    public required Guid EntityId { get; init; }
    public required string SourceType { get; init; }
    public required string EventType { get; init; }
    public required string FieldMapping { get; init; }
    public Guid? DebitAccountId { get; init; }
    public Guid? CreditAccountId { get; init; }
    public string? VatCode { get; init; }
    public string? CostCenter { get; init; }
    public string? Condition { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WebhookEventDto
{
    public required Guid Id { get; init; }
    public required string SourceType { get; init; }
    public required string SourceId { get; init; }
    public required string EventType { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public short RetryCount { get; init; }
    public int? ProcessingDurationMs { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? MappingRuleId { get; init; }
}
