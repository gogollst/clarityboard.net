# AI Middleware Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Design Principles

| Principle | Implementation |
|-----------|---------------|
| **Provider Agnostic** | All AI accessed through `IAiService` interface; no direct provider imports in business logic |
| **Hot-Swappable** | Provider config in database; change primary/fallback without redeployment |
| **Cost Controlled** | Per-provider budgets, token tracking, automatic downgrade to cheaper models |
| **PII Safe** | All requests pass through PII filter before reaching any provider |
| **Observable** | Every request logged (without PII), latency/cost metrics exported |
| **Resilient** | Circuit breaker per provider, automatic fallback chain, graceful degradation |

---

## 2. Provider Registry

```csharp
public record AiProviderConfig
{
    public string ProviderId { get; init; }          // "anthropic", "xai", "deepl", etc.
    public string DisplayName { get; init; }
    public string BaseUrl { get; init; }
    public string ApiKeyVaultRef { get; init; }      // Reference to Azure Key Vault / Secrets Manager
    public Dictionary<string, ModelConfig> Models { get; init; }
    public RateLimitConfig RateLimit { get; init; }
    public decimal MonthlyBudgetEur { get; init; }
    public bool IsEnabled { get; init; }
}

public record ModelConfig
{
    public string ModelId { get; init; }             // "claude-sonnet-4-6", "grok-3"
    public decimal InputCostPer1kTokens { get; init; }
    public decimal OutputCostPer1kTokens { get; init; }
    public int MaxContextTokens { get; init; }
    public int MaxOutputTokens { get; init; }
}
```

### Provider Capability Matrix

| Capability | Primary | Fallback | Max Latency | Max Cost/Request |
|-----------|---------|----------|-------------|-----------------|
| Document OCR | Anthropic Claude | Google Gemini | 10s | €0.05 |
| Financial Analysis | Anthropic Claude | xAI Grok | 5s | €0.03 |
| NLQ Processing | Anthropic Claude | xAI Grok | 5s | €0.02 |
| Scenario Generation | Anthropic Claude | xAI Grok | 15s | €0.10 |
| WC Optimization | Anthropic Claude | Rules Engine | 5s | €0.03 |
| Translation | DeepL | Google Translate | 3s | €0.01 |
| Text-to-Speech | ElevenLabs | Google TTS | 10s | €0.05 |
| Anomaly Detection | Statistical + Claude | Statistical Only | 2s | €0.01 |
| Forecasting | Custom ML | Anthropic Claude | 30s | €0.08 |

---

## 3. Middleware Pipeline

```
Application Request
        │
        ▼
┌─────────────────────────────────────┐
│         AI Middleware                │
│                                     │
│  1. Request Validation              │  ← Validate input, check capability
│         │                           │
│  2. PII Filter (Outbound)          │  ← Strip/mask PII before sending
│         │                           │
│  3. Cost Check                      │  ← Verify budget remaining
│         │                           │
│  4. Provider Router                 │  ← Select provider (primary → fallback)
│         │                           │
│  5. Rate Limiter                    │  ← Per-provider token bucket
│         │                           │
│  6. Circuit Breaker                 │  ← Polly circuit breaker per provider
│         │                           │
│  7. Request Adapter                 │  ← Transform to provider-specific format
│         │                           │
│  8. HTTP Call                       │  ← Actual API call with timeout
│         │                           │
│  9. Response Parser                 │  ← Normalize to internal format
│         │                           │
│  10. PII Filter (Inbound)          │  ← Verify no PII leaked back
│         │                           │
│  11. Response Cache                 │  ← Cache if applicable (NLQ, translation)
│         │                           │
│  12. Metrics + Audit               │  ← Log tokens, latency, cost, provider
│                                     │
└─────────────────────────────────────┘
        │
        ▼
Application Response (AiResult<T>)
```

---

## 4. PII Filter

### Outbound (Before Provider)

```csharp
public class PiiFilter : IPiiFilter
{
    // Patterns detected and masked before sending to AI provider
    private static readonly Dictionary<string, Regex> PiiPatterns = new()
    {
        ["email"] = new(@"\b[\w.+-]+@[\w-]+\.[\w.]+\b"),
        ["iban"] = new(@"\b[A-Z]{2}\d{2}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{0,4}\b"),
        ["tax_id"] = new(@"\b\d{2,3}/\d{3}/\d{5}\b"),             // Steuernummer
        ["vat_id"] = new(@"\bDE\d{9}\b"),                          // USt-IdNr
        ["phone"] = new(@"\b\+?\d{1,3}[\s-]?\d{3,5}[\s-]?\d{4,10}\b"),
        ["person_name"] = new(@"(?:Herr|Frau|Mr|Mrs|Ms)\s+\w+\s+\w+"),
    };

    public PiiFilterResult Filter(string input)
    {
        var masked = input;
        var replacements = new Dictionary<string, string>();

        foreach (var (type, pattern) in PiiPatterns)
        {
            masked = pattern.Replace(masked, match =>
            {
                var placeholder = $"[{type.ToUpper()}_{replacements.Count}]";
                replacements[placeholder] = match.Value;
                return placeholder;
            });
        }

        return new PiiFilterResult(masked, replacements);
    }

    public string Restore(string output, Dictionary<string, string> replacements)
    {
        var restored = output;
        foreach (var (placeholder, original) in replacements)
        {
            restored = restored.Replace(placeholder, original);
        }
        return restored;
    }
}
```

### Inbound (After Provider)

- Verify AI response does not contain PII that was not in the original request
- Flag any unexpected personal data patterns in responses
- Log violations for review

---

## 5. Provider Router & Fallback

```csharp
public class AiProviderRouter : IAiProviderRouter
{
    public async Task<AiResult<T>> RouteAsync<T>(
        AiCapability capability,
        AiRequest request,
        CancellationToken ct)
    {
        var providers = GetProviderChain(capability);  // Primary → Fallback(s)

        foreach (var provider in providers)
        {
            if (!provider.IsEnabled) continue;
            if (!HasBudgetRemaining(provider)) continue;
            if (IsCircuitOpen(provider)) continue;

            try
            {
                var result = await ExecuteWithTimeout(provider, request, ct);

                TrackUsage(provider, result.TokensUsed, result.Cost);
                return AiResult<T>.Success(result.Data, provider.ProviderId);
            }
            catch (AiProviderException ex) when (ex.IsTransient)
            {
                _logger.LogWarning(ex, "Provider {Provider} transient failure, trying fallback",
                    provider.ProviderId);
                RecordFailure(provider);
                continue;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} fatal failure", provider.ProviderId);
                RecordFailure(provider);
                continue;
            }
        }

        return AiResult<T>.Failure("All providers exhausted for capability: " + capability);
    }
}
```

### Circuit Breaker Configuration

```csharp
// Per-provider circuit breaker (Polly)
services.AddResiliencePipeline<string>("ai-provider", (builder, context) =>
{
    builder
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,                    // Open after 50% failure rate
            SamplingDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 5,                 // Min requests before evaluating
            BreakDuration = TimeSpan.FromSeconds(30),
        })
        .AddTimeout(TimeSpan.FromSeconds(30))       // Per-request timeout
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
        });
});
```

---

## 6. Cost Control

### Budget Tracking

```sql
CREATE TABLE ai.usage_log (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id     VARCHAR(50) NOT NULL,
    model_id        VARCHAR(100) NOT NULL,
    capability      VARCHAR(50) NOT NULL,
    entity_id       UUID,
    input_tokens    INT NOT NULL,
    output_tokens   INT NOT NULL,
    cost_eur        NUMERIC(10,6) NOT NULL,
    latency_ms      INT NOT NULL,
    status          VARCHAR(20) NOT NULL,       -- success, fallback, failure
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Monthly cost view
CREATE MATERIALIZED VIEW ai.mv_monthly_cost AS
SELECT
    provider_id,
    date_trunc('month', created_at) AS month,
    COUNT(*) AS request_count,
    SUM(input_tokens) AS total_input_tokens,
    SUM(output_tokens) AS total_output_tokens,
    SUM(cost_eur) AS total_cost_eur,
    AVG(latency_ms) AS avg_latency_ms
FROM ai.usage_log
GROUP BY provider_id, date_trunc('month', created_at)
WITH DATA;
```

### Budget Enforcement

```csharp
public class CostGuard : ICostGuard
{
    public async Task<bool> HasBudgetAsync(string providerId)
    {
        var config = await GetProviderConfig(providerId);
        var monthlySpend = await GetMonthlySpend(providerId);

        if (monthlySpend >= config.MonthlyBudgetEur)
        {
            _logger.LogWarning("Provider {Provider} budget exhausted: {Spend}/{Budget} EUR",
                providerId, monthlySpend, config.MonthlyBudgetEur);
            return false;
        }

        // Warn at 80% threshold
        if (monthlySpend >= config.MonthlyBudgetEur * 0.8m)
        {
            await _alertService.SendBudgetWarning(providerId, monthlySpend, config.MonthlyBudgetEur);
        }

        return true;
    }
}
```

---

## 7. Capability Implementations

### Document OCR & Extraction

```
Input: PDF/Image file stream
Pipeline:
  1. Convert to supported format (if needed)
  2. PII filter on filename/metadata
  3. Send to Anthropic Claude with structured extraction prompt
  4. Parse response into DocumentField[] (invoice number, date, amount, VAT, vendor)
  5. Apply confidence scoring (High > 0.9, Medium > 0.7, Low > 0.5)
  6. Map to chart of accounts via learned patterns
  7. Generate BookingSuggestion
Output: DocumentProcessingResult { Fields[], BookingSuggestion, Confidence }
```

### Natural Language Query

```
Input: User question in natural language (EN/DE)
Pipeline:
  1. Detect language, translate to English if needed (DeepL)
  2. PII filter
  3. Build context: entity schema, available KPIs, date range
  4. Send to Claude with system prompt defining available data
  5. Parse structured response: { query_type, filters, kpi_ids, date_range }
  6. Execute corresponding API query
  7. Format response in user's language
Output: NlqResult { Answer, DataPoints[], VisualizationType }
```

### Anomaly Detection (Hybrid)

```
Input: KPI value + historical data
Pipeline:
  1. Statistical check: Z-score > 3σ from rolling 90-day mean
  2. Seasonal adjustment: Compare to same period last year
  3. If statistical anomaly detected:
     a. Send context to Claude for explanation
     b. Claude provides: { severity, explanation, recommendation }
  4. Combine statistical + AI scores
Output: AnomalyResult { IsAnomaly, Score, Explanation, Recommendation }
```

---

## 8. Model Versioning & A/B Testing

```csharp
public record ModelVersion
{
    public string VersionId { get; init; }       // "claude-sonnet-4-6-v2"
    public string ProviderId { get; init; }
    public string ModelId { get; init; }
    public string PromptVersion { get; init; }   // Versioned prompt template
    public decimal TrafficPercentage { get; init; } // 0-100 for A/B testing
    public DateTimeOffset ActivatedAt { get; init; }
}

// Prompt versioning stored in database
public record PromptTemplate
{
    public string TemplateId { get; init; }      // "document-extraction-v3"
    public string Capability { get; init; }
    public string SystemPrompt { get; init; }
    public string UserPromptTemplate { get; init; } // With {placeholders}
    public int Version { get; init; }
    public bool IsActive { get; init; }
}
```

---

## 9. Monitoring & Alerts

| Metric | Threshold | Alert |
|--------|-----------|-------|
| Provider error rate | > 5% over 5 minutes | Warning → fallback |
| Provider latency P95 | > 10s | Warning |
| Monthly cost | > 80% budget | Warning email |
| Monthly cost | > 100% budget | Critical → disable provider |
| Circuit breaker open | Any provider | Info → dashboard |
| PII detection in response | Any occurrence | Warning → review |
| Token usage spike | > 2x daily average | Info → dashboard |

---

## Document Navigation

- Previous: [Database Architecture](./04-database-architecture.md)
- Next: [Data Ingestion & Event Processing](./06-data-ingestion.md)
- [Back to Index](./README.md)
