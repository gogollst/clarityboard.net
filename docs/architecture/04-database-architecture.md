# Database Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Technology Decisions

| Concern | Choice | Rationale |
|---------|--------|-----------|
| **RDBMS** | PostgreSQL 18 (local install) | JSONB, partitioning, CTE, full-text search, mature ecosystem |
| **Connection Pooling** | PgBouncer | Lightweight, transaction-mode pooling, reduces PG connection overhead |
| **ORM** | Entity Framework Core 10 | Code-first migrations, LINQ, interceptors, bulk operations (EFCore.BulkExtensions) |
| **Migrations** | EF Core Migrations | Versioned, idempotent, rollback support |
| **Audit** | Custom + EF interceptors | SaveChanges interceptor writes to audit table |
| **Multi-Tenancy** | Schema per entity group + Row-Level Security | Isolation + query scoping via `entity_id` |

---

## 2. Schema Strategy

### Schema Organization

```
clarityboard (database)
├── public                    # Shared infrastructure tables
│   ├── users
│   ├── roles
│   ├── permissions
│   ├── user_roles
│   ├── refresh_tokens
│   ├── audit_logs
│   └── system_config
│
├── accounting                # Double-entry bookkeeping
│   ├── accounts
│   ├── journal_entries
│   ├── journal_entry_lines
│   ├── fiscal_periods
│   ├── recurring_entries
│   └── vat_records
│
├── kpi                       # KPI engine
│   ├── kpi_definitions
│   ├── kpi_snapshots         # Partitioned by date
│   ├── kpi_alerts
│   └── kpi_alert_events
│
├── entity                    # Multi-entity management
│   ├── legal_entities
│   ├── entity_relationships
│   ├── tax_units
│   └── intercompany_rules
│
├── cashflow                  # Cash flow management
│   ├── cash_flow_entries     # Partitioned by date
│   ├── cash_flow_forecasts
│   └── liquidity_alerts
│
├── scenario                  # Scenario engine
│   ├── scenarios
│   ├── scenario_parameters
│   ├── scenario_results
│   └── simulation_runs
│
├── document                  # Document capture
│   ├── documents
│   ├── document_fields
│   ├── booking_suggestions
│   └── recurring_patterns
│
├── budget                    # Budget planning
│   ├── budgets
│   ├── budget_lines
│   └── budget_revisions
│
├── asset                     # Fixed assets
│   ├── fixed_assets
│   ├── depreciation_schedules
│   └── asset_disposals
│
└── integration               # External integrations
    ├── webhook_configs
    ├── webhook_events        # Partitioned by date
    ├── mapping_rules
    └── pull_adapter_configs
```

---

## 3. Core Table Designs

### Base Entity Pattern

All business tables inherit a common column set:

```sql
-- Common columns on every business table
id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
entity_id       UUID NOT NULL REFERENCES entity.legal_entities(id),
created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
created_by      UUID NOT NULL REFERENCES public.users(id),
version         INT NOT NULL DEFAULT 1      -- Optimistic concurrency
```

### Accounting Schema

```sql
CREATE TABLE accounting.accounts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id       UUID NOT NULL,
    account_number  VARCHAR(10) NOT NULL,       -- SKR03/SKR04 number
    name            VARCHAR(200) NOT NULL,
    account_type    VARCHAR(20) NOT NULL,       -- asset, liability, equity, revenue, expense
    account_class   SMALLINT NOT NULL,          -- HGB class (0-9)
    parent_id       UUID REFERENCES accounting.accounts(id),
    is_active       BOOLEAN NOT NULL DEFAULT true,
    vat_default     VARCHAR(10),                -- Default VAT code
    datev_auto      VARCHAR(10),                -- DATEV automatic account
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    UNIQUE (entity_id, account_number)
);

CREATE TABLE accounting.journal_entries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id       UUID NOT NULL,
    entry_number    BIGINT NOT NULL,            -- Sequential per entity
    entry_date      DATE NOT NULL,
    posting_date    DATE NOT NULL,
    description     VARCHAR(500) NOT NULL,
    document_id     UUID,                       -- Link to source document
    fiscal_period_id UUID NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'draft',  -- draft, posted, reversed
    is_reversal     BOOLEAN NOT NULL DEFAULT false,
    reversal_of     UUID,
    source_type     VARCHAR(50),                -- manual, webhook, recurring, ai-suggestion
    source_ref      VARCHAR(200),               -- External reference
    hash            VARCHAR(64) NOT NULL,       -- SHA-256 for GoBD immutability
    previous_hash   VARCHAR(64),                -- Chain link for tamper detection
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID NOT NULL,
    version         INT NOT NULL DEFAULT 1,

    UNIQUE (entity_id, entry_number)
);

CREATE TABLE accounting.journal_entry_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id UUID NOT NULL REFERENCES accounting.journal_entries(id),
    line_number     SMALLINT NOT NULL,
    account_id      UUID NOT NULL REFERENCES accounting.accounts(id),
    debit_amount    NUMERIC(18,2) NOT NULL DEFAULT 0,
    credit_amount   NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency        CHAR(3) NOT NULL DEFAULT 'EUR',
    exchange_rate   NUMERIC(12,6) DEFAULT 1.0,
    base_amount     NUMERIC(18,2) NOT NULL,     -- Amount in EUR
    vat_code        VARCHAR(10),                -- BU-Schlüssel
    vat_amount      NUMERIC(18,2) DEFAULT 0,
    cost_center     VARCHAR(50),
    description     VARCHAR(300),

    UNIQUE (journal_entry_id, line_number),
    CHECK (debit_amount >= 0 AND credit_amount >= 0),
    CHECK (NOT (debit_amount > 0 AND credit_amount > 0))  -- Either debit or credit
);

CREATE TABLE accounting.fiscal_periods (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id       UUID NOT NULL,
    year            SMALLINT NOT NULL,
    month           SMALLINT NOT NULL,
    start_date      DATE NOT NULL,
    end_date        DATE NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'open',  -- open, soft_closed, hard_closed
    closed_at       TIMESTAMPTZ,
    closed_by       UUID,

    UNIQUE (entity_id, year, month)
);
```

### KPI Schema

```sql
CREATE TABLE kpi.kpi_definitions (
    id              VARCHAR(100) PRIMARY KEY,   -- e.g. 'financial.gross_margin'
    domain          VARCHAR(50) NOT NULL,       -- financial, sales, marketing, hr, general
    name            VARCHAR(200) NOT NULL,
    description     TEXT,
    formula         TEXT NOT NULL,              -- Expression or reference to calculator
    unit            VARCHAR(20) NOT NULL,       -- percentage, currency, ratio, count, days
    direction       VARCHAR(10) NOT NULL,       -- higher_better, lower_better, target
    dependencies    JSONB DEFAULT '[]',         -- List of KPI IDs this depends on
    default_target  JSONB,                      -- Default threshold values
    is_active       BOOLEAN NOT NULL DEFAULT true,
    display_order   INT NOT NULL DEFAULT 0
);

CREATE TABLE kpi.kpi_snapshots (
    id              UUID NOT NULL DEFAULT gen_random_uuid(),
    entity_id       UUID NOT NULL,
    kpi_id          VARCHAR(100) NOT NULL REFERENCES kpi.kpi_definitions(id),
    snapshot_date   DATE NOT NULL,
    value           NUMERIC(18,6),
    previous_value  NUMERIC(18,6),
    change_pct      NUMERIC(8,4),
    target_value    NUMERIC(18,6),
    components      JSONB,                     -- Breakdown of calculation inputs
    is_provisional  BOOLEAN NOT NULL DEFAULT false,
    calculated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),

    PRIMARY KEY (entity_id, kpi_id, snapshot_date)
) PARTITION BY RANGE (snapshot_date);

-- Monthly partitions
CREATE TABLE kpi.kpi_snapshots_2026_01 PARTITION OF kpi.kpi_snapshots
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
CREATE TABLE kpi.kpi_snapshots_2026_02 PARTITION OF kpi.kpi_snapshots
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');
-- ... auto-created by maintenance job
```

### Cash Flow Schema

```sql
CREATE TABLE cashflow.cash_flow_entries (
    id              UUID NOT NULL DEFAULT gen_random_uuid(),
    entity_id       UUID NOT NULL,
    entry_date      DATE NOT NULL,
    category        VARCHAR(50) NOT NULL,       -- operating_inflow, operating_outflow,
                                                -- investing, financing
    subcategory     VARCHAR(100) NOT NULL,      -- e.g. customer_receipts, payroll
    amount          NUMERIC(18,2) NOT NULL,     -- Positive = inflow, negative = outflow
    currency        CHAR(3) NOT NULL DEFAULT 'EUR',
    base_amount     NUMERIC(18,2) NOT NULL,
    source_type     VARCHAR(50),                -- journal_entry, forecast, manual
    source_ref      UUID,
    description     VARCHAR(500),
    is_recurring    BOOLEAN NOT NULL DEFAULT false,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    PRIMARY KEY (entity_id, id, entry_date)
) PARTITION BY RANGE (entry_date);
```

### Webhook Event Store

```sql
CREATE TABLE integration.webhook_events (
    id              UUID NOT NULL DEFAULT gen_random_uuid(),
    source_type     VARCHAR(50) NOT NULL,
    source_id       VARCHAR(100) NOT NULL,
    event_type      VARCHAR(100) NOT NULL,
    idempotency_key VARCHAR(200) NOT NULL,
    payload         JSONB NOT NULL,
    received_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    processed_at    TIMESTAMPTZ,
    status          VARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending, processing, completed, failed, dead_letter
    error_message   TEXT,
    retry_count     SMALLINT NOT NULL DEFAULT 0,
    next_retry_at   TIMESTAMPTZ,

    PRIMARY KEY (id, received_at),
    UNIQUE (source_type, source_id, idempotency_key)
) PARTITION BY RANGE (received_at);
```

---

## 4. Indexing Strategy

### Composite Indexes (Entity + Time)

```sql
-- All time-series queries filter by entity_id first, then date range
CREATE INDEX idx_journal_entries_entity_date
    ON accounting.journal_entries (entity_id, entry_date DESC);

CREATE INDEX idx_journal_entries_entity_period
    ON accounting.journal_entries (entity_id, fiscal_period_id);

CREATE INDEX idx_journal_entry_lines_account
    ON accounting.journal_entry_lines (account_id, journal_entry_id);

CREATE INDEX idx_kpi_snapshots_entity_kpi
    ON kpi.kpi_snapshots (entity_id, kpi_id, snapshot_date DESC);

CREATE INDEX idx_cashflow_entity_category
    ON cashflow.cash_flow_entries (entity_id, category, entry_date DESC);

CREATE INDEX idx_webhook_events_status
    ON integration.webhook_events (status, next_retry_at)
    WHERE status IN ('pending', 'failed');

CREATE INDEX idx_webhook_events_source
    ON integration.webhook_events (source_type, source_id, received_at DESC);
```

### Partial Indexes

```sql
-- Only index active/open records for frequently accessed subsets
CREATE INDEX idx_fiscal_periods_open
    ON accounting.fiscal_periods (entity_id, year, month)
    WHERE status = 'open';

CREATE INDEX idx_alerts_active
    ON kpi.kpi_alert_events (entity_id, created_at DESC)
    WHERE status = 'active';

CREATE INDEX idx_documents_pending
    ON document.documents (entity_id, created_at)
    WHERE status IN ('uploaded', 'processing');
```

### GIN Indexes (JSONB)

```sql
-- For JSONB queries on webhook payloads and KPI components
CREATE INDEX idx_webhook_payload
    ON integration.webhook_events USING GIN (payload jsonb_path_ops);

CREATE INDEX idx_kpi_components
    ON kpi.kpi_snapshots USING GIN (components jsonb_path_ops);
```

---

## 5. Partitioning Strategy

### Time-Based Partitioning

| Table | Partition Key | Interval | Retention |
|-------|--------------|----------|-----------|
| `kpi.kpi_snapshots` | `snapshot_date` | Monthly | 3 years daily, then yearly aggregates |
| `cashflow.cash_flow_entries` | `entry_date` | Monthly | Permanent |
| `integration.webhook_events` | `received_at` | Monthly | 12 months, then archived |
| `public.audit_logs` | `created_at` | Monthly | 7 years (GoBD compliance) |

### Partition Management

```sql
-- Automated partition creation (run monthly via pg_cron)
CREATE OR REPLACE FUNCTION create_monthly_partitions()
RETURNS void AS $$
DECLARE
    next_month DATE := date_trunc('month', now()) + interval '2 months';
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    start_date := next_month;
    end_date := next_month + interval '1 month';

    -- KPI snapshots
    partition_name := 'kpi_snapshots_' || to_char(next_month, 'YYYY_MM');
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS kpi.%I PARTITION OF kpi.kpi_snapshots
         FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );

    -- Repeat for other partitioned tables...
END;
$$ LANGUAGE plpgsql;

-- Schedule with pg_cron
SELECT cron.schedule('create-partitions', '0 0 15 * *', 'SELECT create_monthly_partitions()');
```

### Partition Archival

```sql
-- Archive old webhook events (older than 12 months)
-- Detach partition → dump to compressed file → drop
ALTER TABLE integration.webhook_events
    DETACH PARTITION integration.webhook_events_2025_01;

-- Export to compressed format for long-term storage
-- Then drop the detached partition
DROP TABLE integration.webhook_events_2025_01;
```

---

## 6. Connection Pooling (PgBouncer)

### Configuration

```ini
[databases]
clarityboard = host=postgres port=5432 dbname=clarityboard

[pgbouncer]
pool_mode = transaction          ; Release connection after each transaction
max_client_conn = 500            ; Max total client connections
default_pool_size = 25           ; Connections per user/database pair
min_pool_size = 5                ; Minimum idle connections
reserve_pool_size = 5            ; Emergency overflow pool
reserve_pool_timeout = 3         ; Seconds before using reserve pool
max_db_connections = 50          ; Max connections to actual PostgreSQL
server_idle_timeout = 300        ; Close idle server connections after 5 min
client_idle_timeout = 600        ; Close idle client connections after 10 min
query_timeout = 30               ; Kill queries exceeding 30 seconds
```

### Connection Flow

```
Application (500 connections)
        │
        ▼
    PgBouncer (transaction pooling)
        │
        ▼
    PostgreSQL (50 actual connections)
```

### EF Core Configuration

```csharp
services.AddDbContext<ClarityBoardContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
        npgsql.CommandTimeout(30);
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});
```

---

## 7. Materialized Views

### Dashboard Aggregations

```sql
-- Pre-calculated trial balance per entity and period
CREATE MATERIALIZED VIEW accounting.mv_trial_balance AS
SELECT
    je.entity_id,
    fp.year,
    fp.month,
    jel.account_id,
    a.account_number,
    a.name AS account_name,
    a.account_type,
    SUM(jel.debit_amount) AS total_debit,
    SUM(jel.credit_amount) AS total_credit,
    SUM(jel.debit_amount) - SUM(jel.credit_amount) AS balance
FROM accounting.journal_entries je
JOIN accounting.journal_entry_lines jel ON je.id = jel.journal_entry_id
JOIN accounting.accounts a ON jel.account_id = a.id
JOIN accounting.fiscal_periods fp ON je.fiscal_period_id = fp.id
WHERE je.status = 'posted'
GROUP BY je.entity_id, fp.year, fp.month, jel.account_id,
         a.account_number, a.name, a.account_type
WITH DATA;

CREATE UNIQUE INDEX idx_mv_trial_balance
    ON accounting.mv_trial_balance (entity_id, year, month, account_id);

-- Refresh after journal entry mutations
-- Triggered via application event handler
REFRESH MATERIALIZED VIEW CONCURRENTLY accounting.mv_trial_balance;
```

### KPI Summary View

```sql
CREATE MATERIALIZED VIEW kpi.mv_latest_kpis AS
SELECT DISTINCT ON (entity_id, kpi_id)
    entity_id,
    kpi_id,
    snapshot_date,
    value,
    previous_value,
    change_pct,
    target_value,
    components,
    calculated_at
FROM kpi.kpi_snapshots
ORDER BY entity_id, kpi_id, snapshot_date DESC
WITH DATA;

CREATE UNIQUE INDEX idx_mv_latest_kpis
    ON kpi.mv_latest_kpis (entity_id, kpi_id);
```

---

## 8. Row-Level Security

```sql
-- Enable RLS on all business tables
ALTER TABLE accounting.journal_entries ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see data for entities they have access to
CREATE POLICY entity_isolation ON accounting.journal_entries
    USING (entity_id IN (
        SELECT ue.entity_id
        FROM public.user_entities ue
        WHERE ue.user_id = current_setting('app.current_user_id')::UUID
    ));

-- Set user context on each request (via EF interceptor)
-- SET LOCAL app.current_user_id = '{userId}';
-- SET LOCAL app.current_entity_id = '{entityId}';
```

---

## 9. Backup & Recovery

| Strategy | Configuration |
|----------|--------------|
| **Continuous WAL Archiving** | WAL segments archived to S3/MinIO every 60 seconds |
| **Base Backups** | Full backup daily at 02:00 UTC via `pg_basebackup` |
| **Point-in-Time Recovery** | Any point within last 7 days via WAL replay |
| **Logical Backups** | Weekly `pg_dump` for cross-version portability |
| **RPO** | < 1 minute (WAL shipping) |
| **RTO** | < 15 minutes (restore from latest base backup + WAL replay) |
| **Retention** | 7 daily + 4 weekly + 12 monthly base backups |
| **Encryption** | Backups encrypted at rest (AES-256) |
| **Verification** | Automated restore test weekly on staging |

---

## 10. Data Lifecycle

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Hot Data   │───▶│  Warm Data   │───▶│  Cold Data   │───▶│  Archived    │
│  (Current)   │    │ (3-12 months)│    │ (1-3 years)  │    │  (3-7 years) │
├──────────────┤    ├──────────────┤    ├──────────────┤    ├──────────────┤
│ PostgreSQL   │    │ PostgreSQL   │    │ PostgreSQL   │    │ S3/MinIO     │
│ SSD storage  │    │ SSD storage  │    │ HDD/cheaper  │    │ Compressed   │
│ Full indexes │    │ Full indexes │    │ Reduced idx  │    │ Parquet fmt  │
│ In memory    │    │ Partial cache│    │ No cache     │    │ Query on     │
│ cache (Redis)│    │              │    │              │    │ demand only  │
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘

Retention: GoBD requires 10 years for tax-relevant records.
Audit logs: 7 years minimum.
KPI snapshots: 3 years daily, then yearly aggregates permanent.
```

---

## Document Navigation

- Previous: [Frontend Architecture](./03-frontend-architecture.md)
- Next: [AI Middleware Architecture](./05-ai-middleware.md)
- [Back to Index](./README.md)
