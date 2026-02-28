# Data Ingestion & Single Source of Truth

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Webhook Architecture

Each external data source connects to Clarity Board through a dedicated webhook endpoint:

```
POST /api/v1/webhooks/{source-type}/{source-id}
Authorization: Bearer {webhook-token}
Content-Type: application/json
X-Webhook-Signature: {HMAC-SHA256 signature}
X-Idempotency-Key: {unique-event-id}
```

### Source Types (Built-In)

| Source Type | Event Examples | Default Account Mapping |
|-------------|---------------|------------------------|
| `billing` | Invoice created/paid/cancelled, subscription started/cancelled | Revenue (8xxx), AR (1200), VAT (17xx) |
| `crm` | Lead created, opportunity updated, customer won/lost | Pipeline metrics only |
| `banking` | Balance update, transaction received/sent | Bank (1200), various |
| `hr` | Employee hired/terminated, payroll processed, absence recorded | Personnel (4xxx) |
| `marketing` | Campaign launched, lead generated, spend recorded | Marketing costs (4600-4699) |
| `erp` | Purchase order, inventory change, production record | COGS (3xxx), Inventory (1xxx) |
| `custom` | User-defined schema for any source | Configurable per mapping |

### Webhook Security

| Layer | Implementation |
|-------|---------------|
| **Authentication** | Bearer token per source (unique, revocable) |
| **Signature Verification** | HMAC-SHA256 of payload with shared secret |
| **IP Allowlisting** | Optional: restrict to known source IPs |
| **Rate Limiting** | Per-source rate limits (default: 1000/minute) |
| **Payload Size** | Maximum 5 MB per request |
| **TLS** | Minimum TLS 1.2, preferred TLS 1.3 |

---

## 2. Webhook Processing Pipeline

### Step-by-Step Processing

| Step | Action | Failure Behavior |
|------|--------|-----------------|
| **1. Receive** | Accept HTTP request, return 202 Accepted | 401 if auth fails, 429 if rate limited |
| **2. Authenticate** | Verify bearer token + HMAC signature | Reject with 401, log attempt |
| **3. Deduplicate** | Check X-Idempotency-Key against processed events | Return 200 (already processed) |
| **4. Validate** | Validate payload against registered source schema | Reject with 422, store in error queue |
| **5. Queue** | Place in processing queue (prioritized by event type) | Queue overflow → backpressure response |
| **6. Transform** | Map source fields to internal data model | Dead-letter queue on mapping error |
| **7. Book** | Create balanced journal entries (financial events) | Reject if entries do not balance |
| **8. Calculate** | Recalculate affected KPIs | Fallback to previous value, alert |
| **9. Store** | Persist to PostgreSQL | Retry with exponential backoff |
| **10. Notify** | Push updates via WebSocket, trigger alerts | Queue notifications for retry |

### Event Priority

| Priority | Event Types | Processing SLA |
|----------|------------|----------------|
| **Critical** | Payment received, payment failed, bank balance change | < 2 seconds |
| **High** | Invoice created, subscription change, employee payroll | < 5 seconds |
| **Normal** | Lead created, campaign metrics, inventory change | < 30 seconds |
| **Low** | Engagement metrics, content analytics, bulk imports | < 5 minutes |

---

## 3. Data Mapping & Transformation

### Mapping Configuration

Each source has a configurable mapping definition:

```json
{
  "sourceType": "billing",
  "sourceId": "stripe-main",
  "entityId": "ent_company_a",
  "version": 3,
  "mappings": {
    "events": {
      "invoice.paid": {
        "type": "financial_transaction",
        "fieldMappings": {
          "grossAmount": {
            "sourceField": "amount_paid",
            "transform": "divide_by_100"
          },
          "currency": {
            "sourceField": "currency",
            "transform": "uppercase"
          },
          "invoiceNumber": {
            "sourceField": "number"
          },
          "customerId": {
            "sourceField": "customer",
            "lookup": "customer_mapping"
          },
          "invoiceDate": {
            "sourceField": "created",
            "transform": "unix_to_iso"
          },
          "lineItems": {
            "sourceField": "lines.data[*]",
            "itemMappings": {
              "description": "description",
              "amount": {
                "field": "amount",
                "transform": "divide_by_100"
              },
              "quantity": "quantity",
              "taxRate": {
                "field": "tax_rates[0].percentage",
                "default": 19
              }
            }
          }
        },
        "accountMapping": {
          "debit": "1200",
          "credit": "8400",
          "taxAccount": "1776",
          "taxRate": "from:lineItems[*].taxRate"
        }
      },
      "invoice.payment_succeeded": {
        "type": "payment_receipt",
        "accountMapping": {
          "debit": "1200",
          "credit": "1400"
        }
      }
    }
  }
}
```

### Available Transforms

| Transform | Description | Example |
|-----------|-------------|---------|
| `divide_by_100` | For APIs using cents | 12500 → 125.00 |
| `multiply_by_100` | Reverse of above | 125.00 → 12500 |
| `uppercase` | Uppercase string | "eur" → "EUR" |
| `lowercase` | Lowercase string | "EUR" → "eur" |
| `unix_to_iso` | Unix timestamp to ISO 8601 | 1709000000 → "2024-02-27T..." |
| `iso_to_date` | ISO 8601 to date only | "2024-02-27T14:00:00Z" → "2024-02-27" |
| `lookup` | Map value using lookup table | "cus_abc" → internal customer ID |
| `regex_extract` | Extract via regex | "INV-2024-001" → "2024-001" |
| `default` | Use default if source is null | null → 19 (VAT rate) |
| `currency_convert` | Convert to entity currency | 100 USD → 92.50 EUR |
| `round` | Round to N decimal places | 125.456 → 125.46 |
| `negate` | Reverse sign | 500 → -500 |

---

## 4. Scheduled Pull Adapter

For systems that do not support webhooks:

### Configuration

```json
{
  "sourceType": "erp",
  "sourceId": "sap-pull",
  "adapter": "rest_api",
  "schedule": "*/15 * * * *",
  "connection": {
    "baseUrl": "https://erp.company.com/api/v2",
    "authType": "oauth2",
    "tokenEndpoint": "/oauth/token"
  },
  "endpoints": [
    {
      "path": "/invoices",
      "method": "GET",
      "params": {
        "modified_since": "{last_fetch_timestamp}",
        "status": "approved",
        "limit": 100
      },
      "pagination": {
        "type": "cursor",
        "cursorField": "next_cursor"
      },
      "mapToEvent": "invoice.created"
    }
  ],
  "watermark": {
    "field": "modified_at",
    "storage": "database"
  }
}
```

### Pull Adapter Behavior

- Fetches data at configured interval (minimum 5 minutes)
- Uses watermark to only fetch new/modified records since last run
- Converts fetched data into internal webhook event format
- Processes through the same pipeline as webhook data
- Handles pagination automatically
- Tracks fetch history with success/failure status
- Alert on 3 consecutive failures

---

## 5. Single Source of Truth Principles

### Principle 1: No Manual Data Entry for KPIs

All KPI data flows from source systems via webhooks or pull adapters. Manual entries are strictly limited to:
- **Adjustments**: Corrections with mandatory reason and approval workflow
- **Opening balances**: Initial setup only
- **Journal adjustments**: Month-end closing entries (e.g., accruals, provisions)

Every manual entry requires audit trail documentation.

### Principle 2: Immutable Event Log

```
Every data event is stored as an immutable record:

┌─────────────────────────────────────────┐
│ Event ID: evt_2026022714230001          │
│ Source: billing/stripe-main             │
│ Type: invoice.paid                      │
│ Received: 2026-02-27T14:23:00Z         │
│ Processed: 2026-02-27T14:23:01Z        │
│ Status: PROCESSED                       │
│ Entity: ent_company_a                   │
│ Raw Payload: {...}                      │
│ Mapped Data: {...}                      │
│ Journal Entries: [je_001, je_002]       │
│ KPIs Affected: [revenue, gross_margin]  │
│ Checksum: sha256:abc123...              │
└─────────────────────────────────────────┘

Corrections create NEW events with reference to original:
  Event ID: evt_2026022715000001
  Type: invoice.corrected
  References: evt_2026022714230001
  Reason: "Amount was incorrect, corrected from 1000 to 1200"
  Reversal entries + new entries created
```

### Principle 3: Deterministic Calculations

Given the same input data, KPI calculations always produce the same result:
- No hidden state or side effects
- Calculation formulas versioned and documented
- Recalculation from raw events produces identical results
- Monthly verification: full recalculation vs. incremental = same values

### Principle 4: Full Traceability

Every KPI value can be traced to source events:

```
KPI: Gross Margin = 42.5% (2026-02-27)
  ├── Gross Profit: 425,000 EUR
  │   ├── Net Revenue: 1,000,000 EUR
  │   │   └── 847 invoice events (evt_001 through evt_847)
  │   └── COGS: 575,000 EUR
  │       └── 312 cost events (evt_848 through evt_1159)
  └── Calculation: 425,000 / 1,000,000 * 100 = 42.5%
```

### Principle 5: Conflict Resolution

When multiple sources provide conflicting data:

| Priority | Rule | Example |
|----------|------|---------|
| 1 (Highest) | Banking source for cash data | Bank balance overrides calculated balance |
| 2 | Billing source for revenue data | Billing system is authoritative for invoices |
| 3 | ERP source for cost/inventory data | ERP overrides manual cost entries |
| 4 | CRM for customer/pipeline data | CRM is authoritative for pipeline |
| 5 (Lowest) | Manual entries | Only used when no automated source exists |

Conflicts are logged, flagged for review, and displayed in the Admin dashboard.

---

## 6. Data Quality Monitoring

### Automated Checks

| Check | Frequency | Action on Failure |
|-------|-----------|-------------------|
| Schema validation | Every event | Reject event, alert |
| Duplicate detection | Every event | Skip processing, log |
| Balance verification | Daily | Alert Finance team |
| Completeness check | Daily | Report missing expected events |
| Cross-source reconciliation | Weekly | Flag discrepancies for review |
| Full recalculation verification | Monthly | Compare incremental vs. full rebuild |

### Data Quality Dashboard (Admin)

```
Data Quality Score: 99.2%

Events (Last 24h):
  Received: 1,247
  Processed: 1,240 (99.4%)
  Rejected: 5 (0.4%)
  In Dead-Letter: 2 (0.2%)

Sources Health:
  billing/stripe    ████████████ 100%  Last event: 2 min ago
  crm/hubspot       ████████████ 100%  Last event: 15 min ago
  banking/api       ████████████ 100%  Last event: 1 hour ago
  hr/personio       ██████████░░  83%  Last event: 3 hours ago ⚠
  erp/sap-pull      ████████████ 100%  Last pull: 10 min ago
```

---

## Document Navigation

- Previous: [General Business KPIs](./07-general-business-kpis.md)
- Next: [Multi-Entity Management](./09-multi-entity-management.md)
- [Back to Index](./README.md)
