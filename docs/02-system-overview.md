# System Overview

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## High-Level Data Flow

```
External Sources          Clarity Board Core              Outputs
┌─────────────┐     ┌──────────────────────────┐     ┌──────────────┐
│ CRM         │────>│                          │────>│ Dashboards   │
│ Billing     │────>│  Webhook Receiver         │     │ (Role-based) │
│ ERP         │────>│       │                  │     ├──────────────┤
│ Bank        │────>│       v                  │────>│ DATEV Export │
│ HR Tools    │────>│  Data Processor           │     ├──────────────┤
│ Marketing   │────>│       │                  │────>│ API          │
│ Platforms   │────>│       v                  │     ├──────────────┤
│ Custom      │────>│  KPI Engine              │────>│ Alerts       │
│ Sources     │────>│       │                  │     ├──────────────┤
│             │     │       v                  │────>│ AI Insights  │
│             │     │  Scenario Engine          │     ├──────────────┤
│             │     │       │                  │────>│ Reports      │
│             │     │       v                  │     └──────────────┘
│             │     │  Storage (Postgres)       │
│             │     └──────────────────────────┘
└─────────────┘
```

---

## Core Processing Pipeline

Every data event follows this exact sequence:

| Step | Name | Description | SLA |
|------|------|-------------|-----|
| 1 | **Ingest** | Webhook receives event from external source (e.g., invoice created in billing system) | < 100ms |
| 2 | **Validate** | Payload is validated against source-specific schema, deduplicated, and logged | < 200ms |
| 3 | **Transform** | Raw data is mapped to Clarity Board's internal data model using configurable mapping rules | < 500ms |
| 4 | **Book** | Financial transactions are recorded on appropriate HGB accounts (double-entry bookkeeping) | < 1s |
| 5 | **Calculate** | Affected KPIs are recalculated based on new data | < 1s |
| 6 | **Store** | Updated values are persisted; daily KPI snapshots updated | < 500ms |
| 7 | **Notify** | If thresholds are breached, alerts are triggered via WebSocket, email, or SMS | < 2s |
| 8 | **Present** | Updated KPIs are pushed to connected dashboards via WebSocket | Real-time |

**Total pipeline latency target: < 5 seconds from event to dashboard update.**

---

## Integration Pattern

### Webhook-Based Ingestion (Primary)

All external data enters via **webhooks** (push model):

```
POST /api/v1/webhooks/{source-type}/{source-id}
Authorization: Bearer {webhook-token}
Content-Type: application/json
X-Webhook-Signature: {HMAC-SHA256 signature}
X-Idempotency-Key: {unique-event-id}
```

**Advantages:**
- Real-time updates (no polling delay)
- Source systems control when data is sent
- Clarity Board does not need credentials to external systems
- Each source has a dedicated webhook endpoint with its own authentication token
- Full event audit trail

### Scheduled Pull Adapter (Secondary)

For systems that do not support webhooks:
- Configurable polling interval (minimum 5 minutes)
- API adapter per source system
- Fetched data is converted to internal webhook format
- Processed through the same pipeline as webhook data
- Pull history tracked with last-fetch watermark

### Supported Source Types (Built-In)

| Source Type | Event Examples | Account Mapping |
|-------------|---------------|-----------------|
| `billing` | Invoice created/paid/cancelled, subscription started/cancelled | Revenue, AR, VAT |
| `crm` | Lead created, opportunity updated, customer won/lost | Pipeline metrics |
| `banking` | Balance update, transaction received/sent | Bank accounts, payments |
| `hr` | Employee hired/terminated, payroll processed, absence recorded | Personnel costs |
| `marketing` | Campaign launched, lead generated, spend recorded | Marketing costs |
| `erp` | Purchase order, inventory change, production record | COGS, inventory |
| `custom` | User-defined schema for any source | Configurable |

---

## System Components

### Backend (.NET Core 10)

| Component | Responsibility |
|-----------|---------------|
| **API Gateway** | Request routing, rate limiting, authentication |
| **Webhook Service** | Receive, validate, queue incoming events |
| **Processing Service** | Transform, book, calculate KPIs |
| **Scenario Service** | Manage scenarios, run simulations, Monte Carlo |
| **Export Service** | Generate DATEV files, PDF reports, Excel exports |
| **AI Middleware** | Route requests to appropriate AI provider |
| **Notification Service** | Alerts via WebSocket, email, SMS |
| **Background Jobs** | Daily KPI snapshots, scheduled pulls, cleanup |

### Frontend (React)

| Component | Responsibility |
|-----------|---------------|
| **Dashboard Shell** | Layout, navigation, entity/role switching |
| **KPI Cards** | Individual KPI display with sparkline and trend |
| **Chart Components** | Tremor-based visualizations (all chart types) |
| **Scenario Workbench** | Create, configure, compare scenarios |
| **Document Upload** | Drag-and-drop receipt processing |
| **Budget Planner** | Budget creation, plan vs. actual views |
| **Admin Console** | User management, entity config, webhook setup |
| **Real-time Layer** | WebSocket connection for live updates |

### Database (PostgreSQL)

| Schema Area | Tables | Purpose |
|-------------|--------|---------|
| **Core** | entities, users, roles, permissions | Organization and access |
| **Accounting** | accounts, journal_entries, ledger | HGB double-entry bookkeeping |
| **KPI** | kpi_definitions, kpi_daily_snapshots | KPI registry and history |
| **Scenario** | scenarios, scenario_parameters, simulation_results | Planning and forecasting |
| **Documents** | documents, document_fields, booking_suggestions | Receipt capture |
| **Budget** | budgets, budget_lines, budget_actuals | Budget management |
| **Integration** | webhook_configs, webhook_events, mapping_rules | Data ingestion |
| **Audit** | audit_log | Complete action history |

---

## Error Handling

### Webhook Error Handling

| Retry | Delay | Action |
|-------|-------|--------|
| 1 | 1 second | Automatic retry |
| 2 | 5 seconds | Automatic retry |
| 3 | 30 seconds | Automatic retry |
| 4 | 5 minutes | Automatic retry |
| 5 | 30 minutes | Automatic retry |
| 6 | 2 hours | Automatic retry |
| Failed | - | Move to dead-letter queue, alert Admin |

**Dead-Letter Queue:**
- All failed events stored with full error context
- Manual replay capability (individual or batch)
- Searchable by source, date, error type
- Retention: 90 days

### Application Error Handling

- All errors logged with correlation ID for tracing
- Financial calculations: fail-safe (reject invalid data, never silently produce wrong KPIs)
- AI provider failure: automatic fallback to secondary provider
- Database connection loss: read from cache, queue writes for replay

---

## Document Navigation

- Previous: [Target Users & Roles](./01-target-users-roles.md)
- Next: [Financial KPIs](./03-financial-kpis.md)
- [Back to Index](./README.md)
