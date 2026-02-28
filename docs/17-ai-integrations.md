# AI Integrations

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. AI Middleware Architecture

All AI capabilities are abstracted behind a middleware layer enabling provider hot-swapping, fallback chains, and unified monitoring.

### Provider Registry

| Capability | Primary Provider | Fallback | Use Case |
|-----------|-----------------|----------|----------|
| Document OCR & Extraction | Anthropic Claude | Google Gemini | Receipt/invoice processing |
| Financial Analysis | Anthropic Claude | xAI Grok | KPI anomaly detection, recommendations |
| Forecasting (ML) | Custom ML Pipeline | Anthropic Claude | Revenue, cash flow, churn prediction |
| Natural Language Query | Anthropic Claude | xAI Grok | "What was our EBITDA margin last quarter?" |
| Translation | DeepL | Google Translate | Multi-language document processing |
| Text-to-Speech | ElevenLabs | Google TTS | Audio reports, accessibility |
| Scenario Generation | Anthropic Claude | xAI Grok | AI-suggested scenarios based on data |
| Working Capital Optimization | Anthropic Claude | Custom rules engine | DSO/DPO/DIO recommendations |
| Anomaly Detection | Statistical + AI hybrid | Pure statistical | KPI outlier detection |

### Middleware Request Flow

```
Application Layer
       │
       ▼
┌──────────────────────┐
│   AI Middleware       │
│   ┌────────────────┐ │
│   │ Request Router  │ │  ← Selects provider based on capability + load + cost
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Rate Limiter   │ │  ← Enforces per-provider rate limits
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Request Logger  │ │  ← Logs request (without PII) for audit
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Provider Adapter│ │  ← Translates to provider-specific API
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Response Parser │ │  ← Normalizes provider response
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Fallback Handler│ │  ← On failure: retry or switch provider
│   └───────┬────────┘ │
│           │          │
│   ┌───────▼────────┐ │
│   │ Result Cache    │ │  ← Cache deterministic results
│   └────────────────┘ │
└──────────────────────┘
       │
       ▼
Application Layer (receives normalized result)
```

### Provider Configuration

```json
{
  "providers": {
    "anthropic": {
      "apiEndpoint": "${ANTHROPIC_API_URL}",
      "model": "claude-sonnet-4-6",
      "maxTokens": 4096,
      "rateLimit": 100,
      "rateLimitWindow": "1m",
      "timeout": 30000,
      "retries": 2,
      "capabilities": ["document_processing", "financial_analysis", "nlq", "scenario_generation"],
      "costPerToken": { "input": 0.003, "output": 0.015 },
      "priority": 1
    },
    "xai": {
      "apiEndpoint": "${XAI_API_URL}",
      "model": "grok-3",
      "capabilities": ["financial_analysis", "nlq", "scenario_generation"],
      "priority": 2
    },
    "deepl": {
      "apiEndpoint": "https://api.deepl.com/v2",
      "capabilities": ["translation"],
      "priority": 1
    },
    "elevenlabs": {
      "apiEndpoint": "https://api.elevenlabs.io/v1",
      "capabilities": ["text_to_speech"],
      "priority": 1
    }
  }
}
```

---

## 2. AI-Powered Features

### 2.1 Anomaly Detection

**Hybrid approach: Statistical + AI contextual analysis**

```
Statistical Layer:
  1. Z-Score: Flag values > 3 standard deviations from rolling 90-day mean
  2. IQR: Flag values outside 1.5 * interquartile range
  3. Isolation Forest: ML-based anomaly detection on multi-dimensional KPI space
  4. Seasonal STL: Decompose trend/seasonal/residual, flag residual anomalies

AI Contextual Layer:
  When statistical anomaly detected:
  1. Gather context: recent events, market data, related KPIs
  2. Send to AI: "Revenue dropped 15% on 2026-02-25. Recent events: [list].
     Related KPIs: [list]. Explain possible cause."
  3. AI response: "Revenue dropped 15% - correlated with:
     - Website downtime from 14:00-18:00 UTC (4 hours during peak)
     - 3 large invoices delayed from billing system (processing backlog)
     - No organic trend change detected
     Recommendation: Investigate billing delay, verify website uptime SLA"

Severity Classification:
  INFO:     Unusual but within expected range (1.5-2.5 sigma)
  WARNING:  Significant deviation (2.5-3.5 sigma) or negative trend
  CRITICAL: Extreme deviation (>3.5 sigma) or KPI breaches threshold
```

### 2.2 Natural Language Queries

Users can ask questions in natural language:

```
User: "How did our cash position change after the AWS price increase in January?"

AI Processing:
  1. Parse intent: cash position change analysis
  2. Identify entities: AWS, cash position, January
  3. Query data:
     - AWS expense: Dec 7,200 → Jan 7,800 → Feb 8,500
     - Cash position: Dec 850,000 → Jan 820,000 → Feb 785,000
     - Operating Cash Flow: Dec 45,000 → Jan 38,000 → Feb 32,000
  4. Correlate and explain

Response: "Your monthly AWS spend increased from 7,200 EUR to 8,500 EUR
(+18.1%) between December and February. This reduced your Operating Cash
Flow by approximately 1,300 EUR/month. Your cash position decreased from
850,000 EUR to 785,000 EUR over the same period, though AWS cost increase
accounts for only ~20% of this decline. Other factors include:
- Seasonal revenue dip in January (-8% vs December)
- Two large vendor payments in February (total 25,000 EUR)
Your cash runway decreased from 18.2 months to 16.8 months."
```

**Supported Query Types:**

| Category | Example Queries |
|----------|----------------|
| **Trend** | "What's the revenue trend for Q4?" |
| **Comparison** | "Compare EBITDA margin across all entities" |
| **Cause Analysis** | "Why did churn spike in March?" |
| **Forecast** | "What will our cash position be in 6 months?" |
| **Recommendation** | "How can we improve our DSO?" |
| **Drill-Down** | "Break down marketing spend by channel for last quarter" |
| **What-If** | "What happens to cash if we hire 5 more engineers?" |

### 2.3 Predictive Analytics

| Prediction | Model | Inputs | Output | Accuracy Target |
|-----------|-------|--------|--------|----------------|
| Revenue Forecast | Time series + pipeline | Historical revenue, pipeline data, seasonality | Monthly revenue (3-12 months) | < 10% MAPE at 3 months |
| Churn Prediction | Classification | Usage patterns, support tickets, payment behavior | Churn probability per customer | > 80% AUC |
| Cash Flow Forecast | Ensemble (time series + causal) | Inflows, outflows, seasonality, WC | Weekly cash position (13 weeks) | < 5% deviation at 4 weeks |
| Headcount Needs | Regression | Revenue targets, productivity ratios | FTE needed per department | +-2 FTE at 6 months |
| Budget Forecast | Time series + regression | YTD actuals, seasonal patterns | Year-end actuals per category | < 8% deviation at 6 months |

### 2.4 Document Processing (Detail)

See [Document & Receipt Capture](./14-document-capture.md) for full specification.

AI role in document processing:
- OCR and text extraction
- Field identification and extraction
- Document classification
- Account suggestion based on content analysis
- Learning from user corrections

### 2.5 Text-to-Speech Reports

```
Audio Report Generation:

Input: KPI dashboard state for selected period
Processing:
  1. AI generates natural language summary of key KPIs
  2. Highlights significant changes and anomalies
  3. Includes recommendations
  4. Text sent to ElevenLabs for voice synthesis

Output: "Good morning. Here's your daily financial briefing for
February 27th. Revenue is tracking at 2.1 million euros,
up 3.2 percent month-over-month. EBITDA margin improved to
22.5 percent. Cash position is stable at 785,000 euros.
One alert: your DSO has increased to 54 days, exceeding
the industry benchmark by 16 days. I recommend reviewing
the accounts receivable aging report. Your next DATEV
export is due in 3 days."

Delivery: In-app audio player, optional email with MP3 attachment
Language: German and English (configurable per user)
Voice: Professional, configurable (male/female, speed)
Schedule: Configurable (e.g., daily at 8:00 AM)
```

---

## 3. AI Guardrails

### Safety Principles

| Principle | Implementation |
|-----------|---------------|
| **Human-in-the-loop** | No autonomous booking. AI suggests, human confirms. |
| **Transparency** | All AI outputs labeled as AI-generated with confidence scores |
| **Explainability** | Every prediction includes top contributing factors |
| **Auditability** | Full log of AI inputs, outputs, model versions, and user actions |
| **Fallback** | If AI confidence < threshold, present manual workflow instead |
| **No hallucination in numbers** | AI never fabricates financial figures; always references stored data |
| **Bias monitoring** | Track prediction accuracy across customer segments, entity sizes |

### Confidence Thresholds

| Confidence | Action | Display |
|-----------|--------|---------|
| > 95% | Auto-suggest, user confirms with one click | Green indicator |
| 80-95% | Suggest with highlighted uncertain fields | Yellow indicator |
| 60-80% | Present as "best guess," require review | Orange indicator |
| < 60% | Do not suggest, present manual workflow | Red indicator / no suggestion |

### Model Versioning

```
AI Model Registry:

Model: document_classifier_v3
  Version: 3.2.1
  Deployed: 2026-02-15
  Accuracy: 94.2% (on test set)
  Used by: Document processing pipeline
  Previous: v3.1.0 (accuracy: 92.8%)
  Rollback: Available (one-click)

Model: revenue_forecast_v2
  Version: 2.0.3
  Deployed: 2026-01-20
  MAPE: 7.8% (3-month horizon)
  Used by: Scenario engine, cash flow forecasting
  Previous: v1.5.2 (MAPE: 9.1%)
```

### Cost Monitoring

```
AI Usage Report (Monthly):

Provider        Requests    Tokens In    Tokens Out    Cost
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Anthropic       12,450      2.1M         850K          78.75 EUR
xAI Grok        1,200       180K         95K           12.30 EUR
DeepL           450         120K         115K          8.50 EUR
ElevenLabs      180         -            45K chars     15.00 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total                                                  114.55 EUR

Budget: 200.00 EUR/month
Utilization: 57.3%
Alert threshold: 80% (160 EUR)
```

---

## 4. Data Privacy in AI Processing

### PII Handling

| Data Type | Treatment | Sent to AI? |
|-----------|-----------|-------------|
| Financial amounts | Sent as-is (not PII) | Yes |
| KPI values | Sent as-is | Yes |
| Company names | Pseudonymized unless needed for context | Minimal |
| Employee names | Never sent | No |
| Employee salaries | Aggregated only | Aggregated |
| Customer names | Pseudonymized for analysis | Pseudonymized |
| Bank details | Never sent | No |
| Tax IDs | Never sent | No |
| Document images | Sent for OCR only, not stored by provider | Yes (processing only) |

### Data Processing Agreements

Required for each AI provider:
- Standard Contractual Clauses (EU)
- Data processing agreement per GDPR Art. 28
- No training on customer data clause
- Data deletion after processing confirmation
- Audit rights

---

## Document Navigation

- Previous: [Historical KPI Tracking](./16-historical-kpi-tracking.md)
- Next: [Security & Compliance](./18-security-compliance.md)
- [Back to Index](./README.md)
