# Data Ingestion & Event Processing

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Architecture Overview

```
External Sources                     Clarity Board
─────────────────                    ─────────────
                                     ┌──────────────────┐
  Billing ───webhook──────────────▶  │  Webhook API     │
  CRM ───webhook──────────────────▶  │  (Receive +      │
  Banking ───webhook──────────────▶  │   Validate +     │
  HR ───webhook───────────────────▶  │   Store)         │
  Marketing ───webhook────────────▶  │                  │
  ERP ───webhook──────────────────▶  │  Returns 202     │
                                     └────────┬─────────┘
                                              │
                                     ┌────────▼─────────┐
                                     │  RabbitMQ        │
                                     │  (Message Queue) │
                                     └────────┬─────────┘
                                              │
                              ┌───────────────┼───────────────┐
                              │               │               │
                     ┌────────▼──────┐ ┌──────▼───────┐ ┌────▼────────┐
                     │ Event         │ │ Event        │ │ Event       │
                     │ Processor 1   │ │ Processor 2  │ │ Processor N │
                     │ (Consumer)    │ │ (Consumer)   │ │ (Consumer)  │
                     └────────┬──────┘ └──────┬───────┘ └────┬────────┘
                              │               │               │
                     ┌────────▼───────────────▼───────────────▼────────┐
                     │                PostgreSQL                       │
                     │  (Journal Entries, KPI Recalc, Cash Flow, etc.) │
                     └─────────────────────┬───────────────────────────┘
                                           │
                                  ┌────────▼─────────┐
                                  │  SignalR Hub      │
                                  │  (Real-time push) │
                                  └──────────────────┘
```

---

## 2. Webhook Endpoint Design

### Controller

```csharp
[ApiController]
[Route("api/v1/webhooks")]
public class WebhookController : ControllerBase
{
    [HttpPost("{sourceType}/{sourceId}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReceiveWebhook(
        string sourceType,
        string sourceId,
        [FromHeader(Name = "X-Webhook-Signature")] string signature,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        [FromBody] JsonDocument payload)
    {
        // 1. Validate source exists and is active
        var config = await _configRepo.GetActiveConfig(sourceType, sourceId);
        if (config is null) return Unauthorized();

        // 2. Verify HMAC signature
        if (!VerifySignature(payload, signature, config.SharedSecret))
            return Unauthorized();

        // 3. Check idempotency (dedup)
        if (await _eventRepo.ExistsAsync(sourceType, sourceId, idempotencyKey))
            return Accepted();  // Already received, skip

        // 4. Store raw event
        var webhookEvent = new WebhookEvent
        {
            SourceType = sourceType,
            SourceId = sourceId,
            EventType = ExtractEventType(payload),
            IdempotencyKey = idempotencyKey,
            Payload = payload,
            Status = WebhookEventStatus.Pending
        };
        await _eventRepo.StoreAsync(webhookEvent);

        // 5. Publish to message queue
        await _bus.Publish(new ProcessWebhookEventMessage
        {
            EventId = webhookEvent.Id,
            SourceType = sourceType,
            SourceId = sourceId,
            Priority = config.Priority
        });

        return Accepted();
    }
}
```

### Signature Verification

```csharp
private bool VerifySignature(JsonDocument payload, string signature, string secret)
{
    var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
    var secretBytes = Encoding.UTF8.GetBytes(secret);

    using var hmac = new HMACSHA256(secretBytes);
    var computedHash = hmac.ComputeHash(payloadBytes);
    var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(computedSignature),
        Encoding.UTF8.GetBytes(signature)
    );
}
```

---

## 3. Message Queue (MassTransit + RabbitMQ)

### Queue Topology

```
Exchanges                           Queues
──────────                          ──────
webhook-events (fanout) ──────────▶ webhook-processor (competing consumers)
                        └────────▶ webhook-audit (audit logger)

kpi-updates (topic) ──────────────▶ kpi-recalculation
                    └────────────▶ alert-evaluation
                    └────────────▶ signalr-broadcast

document-processing (direct) ─────▶ document-processor
datev-export (direct) ────────────▶ datev-exporter
```

### MassTransit Configuration

```csharp
services.AddMassTransit(cfg =>
{
    cfg.SetKebabCaseEndpointNameFormatter();

    cfg.AddConsumer<WebhookEventConsumer>();
    cfg.AddConsumer<KpiRecalculationConsumer>();
    cfg.AddConsumer<AlertEvaluationConsumer>();
    cfg.AddConsumer<DocumentProcessingConsumer>();
    cfg.AddConsumer<DatevExportConsumer>();

    cfg.UsingRabbitMq((context, bus) =>
    {
        bus.Host("rabbitmq", h =>
        {
            h.Username("clarityboard");
            h.Password(Configuration["RabbitMQ:Password"]);
        });

        bus.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(5),
            intervalDelta: TimeSpan.FromSeconds(2)
        ));

        bus.UseInMemoryOutbox();  // Transactional outbox pattern

        bus.ConfigureEndpoints(context);
    });
});
```

---

## 4. Event Processing Pipeline

### Webhook Event Consumer

```csharp
public class WebhookEventConsumer : IConsumer<ProcessWebhookEventMessage>
{
    public async Task Consume(ConsumeContext<ProcessWebhookEventMessage> context)
    {
        var message = context.Message;
        var webhookEvent = await _eventRepo.GetAsync(message.EventId);

        try
        {
            // 1. Mark as processing
            webhookEvent.Status = WebhookEventStatus.Processing;
            await _eventRepo.UpdateAsync(webhookEvent);

            // 2. Load mapping rules for this source
            var mappingRules = await _mappingRepo.GetRulesAsync(
                webhookEvent.SourceType, webhookEvent.SourceId);

            // 3. Transform payload to internal format
            var transformer = _transformerFactory.Create(webhookEvent.SourceType);
            var internalEvents = transformer.Transform(webhookEvent.Payload, mappingRules);

            // 4. Process each internal event
            foreach (var evt in internalEvents)
            {
                await ProcessInternalEvent(evt);
            }

            // 5. Mark as completed
            webhookEvent.Status = WebhookEventStatus.Completed;
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            await _eventRepo.UpdateAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            webhookEvent.Status = WebhookEventStatus.Failed;
            webhookEvent.ErrorMessage = ex.Message;
            webhookEvent.RetryCount++;
            await _eventRepo.UpdateAsync(webhookEvent);

            throw;  // MassTransit handles retry
        }
    }
}
```

### Event Transformation

```csharp
// Source-specific transformers implement a common interface
public interface IWebhookTransformer
{
    IEnumerable<InternalEvent> Transform(JsonDocument payload, List<MappingRule> rules);
}

// Example: Billing webhook → Journal Entry
public class BillingTransformer : IWebhookTransformer
{
    public IEnumerable<InternalEvent> Transform(JsonDocument payload, List<MappingRule> rules)
    {
        var eventType = payload.RootElement.GetProperty("event_type").GetString();

        return eventType switch
        {
            "invoice.created" => TransformInvoiceCreated(payload, rules),
            "invoice.paid" => TransformInvoicePaid(payload, rules),
            "subscription.started" => TransformSubscriptionStarted(payload, rules),
            _ => Enumerable.Empty<InternalEvent>()
        };
    }

    private IEnumerable<InternalEvent> TransformInvoiceCreated(
        JsonDocument payload, List<MappingRule> rules)
    {
        var amount = payload.RootElement.GetProperty("amount").GetDecimal();
        var vatRate = DetermineVatRate(payload);
        var netAmount = amount / (1 + vatRate);
        var vatAmount = amount - netAmount;

        yield return new CreateJournalEntryEvent
        {
            Lines = new[]
            {
                new JournalLine { AccountNumber = "1200", Debit = amount },      // AR
                new JournalLine { AccountNumber = "8400", Credit = netAmount },   // Revenue
                new JournalLine { AccountNumber = "1776", Credit = vatAmount }    // VAT
            }
        };
    }
}
```

---

## 5. Idempotency

### Three-Layer Dedup

```
Layer 1: HTTP Level
  - X-Idempotency-Key header checked in webhook_events table
  - Duplicate key → return 202 Accepted (skip processing)

Layer 2: Message Queue Level
  - MassTransit MessageId based on webhook event ID
  - RabbitMQ dedup via consumer-side check

Layer 3: Business Level
  - Journal entry source_ref checked before creation
  - Duplicate source_ref → skip or update existing
```

### Idempotency Key Storage

```sql
-- Unique constraint prevents duplicate processing
CREATE UNIQUE INDEX idx_webhook_idempotency
    ON integration.webhook_events (source_type, source_id, idempotency_key);

-- Bloom filter in Redis for fast pre-check (optional optimization)
-- REDIS: BF.ADD webhook:idempotency:{source_type}:{source_id} {key}
```

---

## 6. Dead Letter Queue

### DLQ Configuration

```csharp
bus.ReceiveEndpoint("webhook-processor", e =>
{
    e.ConfigureConsumeTopology = false;

    // After 5 retries, move to DLQ
    e.UseMessageRetry(r => r.Exponential(5,
        TimeSpan.FromSeconds(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromSeconds(2)));

    // Dead letter to dedicated queue
    e.UseDelayedRedelivery(r => r.Intervals(
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1)));

    e.ConfigureConsumer<WebhookEventConsumer>(context);
});
```

### DLQ Dashboard

| Field | Description |
|-------|-------------|
| Event ID | Original webhook event reference |
| Source | Source type + source ID |
| Error | Last error message |
| Retry Count | Number of attempts |
| First Failed | Timestamp of first failure |
| Last Failed | Timestamp of most recent attempt |
| Actions | Retry / Discard / Manual Process |

### DLQ Processing

```csharp
// Admin endpoint to reprocess dead-lettered events
[HttpPost("admin/dlq/{eventId}/retry")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RetryDeadLetter(Guid eventId)
{
    var evt = await _eventRepo.GetAsync(eventId);
    if (evt.Status != WebhookEventStatus.DeadLetter)
        return BadRequest("Event is not in dead letter state");

    evt.Status = WebhookEventStatus.Pending;
    evt.RetryCount = 0;
    await _eventRepo.UpdateAsync(evt);

    await _bus.Publish(new ProcessWebhookEventMessage { EventId = eventId });
    return Accepted();
}
```

---

## 7. Pull-Based Adapters

### Scheduled Polling

```csharp
public class ScheduledPullService : IHostedService
{
    // Hangfire-based scheduled pulls for sources that don't support webhooks
    public void ConfigureJobs()
    {
        var adapters = _configRepo.GetActivePullAdapters();

        foreach (var adapter in adapters)
        {
            RecurringJob.AddOrUpdate(
                $"pull-{adapter.SourceType}-{adapter.SourceId}",
                () => ExecutePull(adapter.Id),
                adapter.CronExpression  // e.g. "0 */4 * * *" (every 4 hours)
            );
        }
    }
}
```

### Pull Adapter Interface

```csharp
public interface IPullAdapter
{
    string SourceType { get; }
    Task<IEnumerable<WebhookEvent>> FetchNewEventsAsync(
        PullAdapterConfig config,
        DateTimeOffset since,
        CancellationToken ct);
}

// Example: Banking API pull
public class BankingPullAdapter : IPullAdapter
{
    public string SourceType => "banking";

    public async Task<IEnumerable<WebhookEvent>> FetchNewEventsAsync(
        PullAdapterConfig config, DateTimeOffset since, CancellationToken ct)
    {
        var client = CreateBankClient(config);
        var transactions = await client.GetTransactionsSinceAsync(since, ct);

        return transactions.Select(tx => new WebhookEvent
        {
            SourceType = "banking",
            SourceId = config.SourceId,
            EventType = "transaction.received",
            IdempotencyKey = tx.TransactionId,
            Payload = JsonSerializer.SerializeToDocument(tx),
            Status = WebhookEventStatus.Pending
        });
    }
}
```

---

## 8. Mapping Rules Engine

### Configuration

```json
{
    "sourceType": "billing",
    "sourceId": "stripe-main",
    "entityId": "uuid-of-entity",
    "rules": [
        {
            "eventType": "invoice.created",
            "conditions": [
                { "field": "$.currency", "operator": "eq", "value": "EUR" },
                { "field": "$.country", "operator": "eq", "value": "DE" }
            ],
            "actions": [
                {
                    "type": "create_journal_entry",
                    "mapping": {
                        "date": "$.created_at",
                        "description": "'Invoice ' + $.invoice_number",
                        "lines": [
                            { "account": "1200", "debit": "$.total" },
                            { "account": "8400", "credit": "$.net_amount" },
                            { "account": "1776", "credit": "$.vat_amount" }
                        ]
                    }
                },
                {
                    "type": "create_cashflow_forecast",
                    "mapping": {
                        "category": "operating_inflow",
                        "subcategory": "customer_receipts",
                        "amount": "$.total",
                        "expected_date": "$.due_date"
                    }
                }
            ]
        }
    ]
}
```

### JSONPath Expression Evaluation

```csharp
public class MappingEngine
{
    public object Evaluate(string expression, JsonDocument payload)
    {
        if (expression.StartsWith("$."))
        {
            // JSONPath extraction
            return JsonPath.Evaluate(payload, expression);
        }
        else if (expression.StartsWith("'"))
        {
            // String literal with concatenation
            return EvaluateStringExpression(expression, payload);
        }
        else
        {
            // Static value
            return expression;
        }
    }
}
```

---

## 9. Monitoring & Health

| Metric | Measurement | Alert Threshold |
|--------|-------------|----------------|
| Events received/min | Counter per source | < expected rate |
| Processing latency P95 | Histogram | > 5 seconds |
| Queue depth | RabbitMQ metric | > 1000 messages |
| DLQ size | Counter | > 0 (any message) |
| Consumer lag | Queue depth trend | Growing for > 5 min |
| Source availability | Health check per source | Offline > 1 hour |
| Idempotency hit rate | Counter | Informational |
| Transformation errors | Counter per source | > 1% of events |

---

## Document Navigation

- Previous: [AI Middleware Architecture](./05-ai-middleware.md)
- Next: [Authentication & Authorization](./07-auth-architecture.md)
- [Back to Index](./README.md)
