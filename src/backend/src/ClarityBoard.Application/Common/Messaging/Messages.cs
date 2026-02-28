namespace ClarityBoard.Application.Common.Messaging;

public record ProcessWebhookEvent(Guid WebhookEventId, Guid EntityId, string SourceType, string EventType);
public record ProcessDocument(Guid DocumentId, Guid EntityId);
public record RecalculateKpis(Guid EntityId, DateOnly SnapshotDate);
public record EvaluateAlerts(Guid EntityId, string KpiId, decimal Value, decimal? PreviousValue);
