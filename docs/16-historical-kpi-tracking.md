# Historical KPI Tracking

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Daily Snapshot Architecture

Every KPI is recalculated and stored as a daily snapshot:

### Snapshot Data Model

```
kpi_daily_snapshots:
  id                  UUID (PK)
  kpi_id              FK → kpi_definitions
  entity_id           FK → entities
  snapshot_date       DATE
  value               DECIMAL(18,4)
  previous_value      DECIMAL(18,4)     -- previous day's value
  change_absolute     DECIMAL(18,4)     -- value - previous_value
  change_percentage   DECIMAL(8,4)      -- change / previous * 100
  target_value        DECIMAL(18,4)     -- configured target for this date
  target_attainment   DECIMAL(8,4)      -- value / target * 100
  components          JSONB             -- breakdown of calculation inputs
  metadata            JSONB             -- additional context (data sources, quality)
  created_at          TIMESTAMP
```

### Component Storage Example

```json
{
  "kpi": "ebitda_margin",
  "date": "2026-02-27",
  "value": 22.5,
  "components": {
    "ebitda": 450000,
    "net_revenue": 2000000,
    "ebitda_breakdown": {
      "gross_profit": 1200000,
      "personnel_costs": -500000,
      "rent": -50000,
      "marketing": -80000,
      "it_costs": -45000,
      "other_opex": -75000
    }
  }
}
```

### Daily Recalculation Process

```
Daily KPI Recalculation (scheduled: 02:00 UTC):

1. Lock calculation window (prevent concurrent modifications)
2. For each entity:
   a. Collect all events processed since last snapshot
   b. Recalculate all affected KPIs
   c. Compare with previous day's snapshot
   d. Store new snapshot
   e. Check alert thresholds
   f. Log recalculation result
3. Generate delta report (what changed and why)
4. Release lock

Duration target: < 30 minutes for 100k transactions
Verification: Spot-check 10% of KPIs against manual calculation
```

---

## 2. Available Visualizations

### Time Series Views

| View | Description | Configuration |
|------|-------------|---------------|
| **Daily Trend** | Day-by-day values for selected period | 30/60/90/365 days |
| **Weekly Aggregate** | Weekly sum/average | Current quarter or custom |
| **Monthly Aggregate** | Monthly sum/average | Current year or custom |
| **Quarterly Aggregate** | Quarterly sum/average | Last 8 quarters |
| **Annual Aggregate** | Annual sum/average | All available years |

### Comparison Views

| View | Description | Use Case |
|------|-------------|----------|
| **Year-over-Year (YoY)** | Current period overlaid with same period last year | Seasonal comparison |
| **Quarter-over-Quarter (QoQ)** | Current quarter vs. previous quarter | Growth tracking |
| **Month-over-Month (MoM)** | Current month vs. previous month | Short-term trends |
| **Budget vs. Actual** | Planned values overlaid with actual | Budget monitoring |
| **Multi-Entity** | Same KPI across multiple entities | Entity comparison |
| **Multi-Scenario** | Same KPI across different scenarios | Planning |

### Statistical Analysis

| Analysis | Description | Visualization |
|----------|-------------|---------------|
| **Rolling Average** | 7-day, 30-day, 90-day moving averages | Line overlay on trend chart |
| **Trend Line** | Linear regression with R-squared value | Dashed line with confidence band |
| **Seasonality Decomposition** | Trend + seasonal + residual components | Three-panel decomposition chart |
| **Anomaly Detection** | AI-detected outliers marked on chart | Highlighted points with tooltip |
| **Volatility** | Standard deviation over rolling window | Bollinger-band style envelope |
| **Correlation** | Two KPIs plotted against each other | Scatter plot with regression line |

### Chart Configurations

```
Example: Revenue Trend with Analysis

┌─────────────────────────────────────────────────────────────┐
│ Net Revenue (Monthly)                            [Controls] │
│                                                              │
│ EUR                                                          │
│ 300k ┤                                            ••••     │
│      │                                       •••••         │
│ 250k ┤                                  •••••              │
│      │                            ••••••                    │
│ 200k ┤                     ••••••                           │
│      │               •••••                                  │
│ 150k ┤         ••••••                                       │
│      │   •••••   ─ ─ ─ ─ ─ Last Year                       │
│ 100k ┤•••                                                   │
│      ├────┬────┬────┬────┬────┬────┬────┬────┬────┬────┤   │
│      Jan  Feb  Mar  Apr  May  Jun  Jul  Aug  Sep  Oct       │
│                                                              │
│ ● Actual  ─ ─ YoY  ── 90d MA  ▓ Anomaly                   │
│ YoY Growth: +25.3%  |  90d MA: 245,000  |  R²: 0.94       │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Data Retention Policy

| Resolution | Retention | Storage | Aggregation |
|------------|-----------|---------|-------------|
| **Daily** | 3 years | Full detail (all components) | Raw snapshot |
| **Weekly** | 7 years | Aggregated (value, min, max, avg) | Week ending Sunday |
| **Monthly** | 10 years | Aggregated (value, min, max, avg) | Calendar month |
| **Yearly** | Permanent | Aggregated (value, min, max, avg) | Fiscal year |

### GoBD Retention Rules

- **Tax-relevant KPIs** (revenue, costs, profit, tax): 10-year retention minimum
- **Non-tax KPIs** (HR, marketing engagement): 3-year retention default
- **Audit logs**: 10-year retention minimum
- **Source events**: 10-year retention (immutable)

### Aggregation Logic

```
Weekly Aggregation (from daily snapshots):

  For each KPI, for each week:
    value_end    = last daily value in the week
    value_start  = first daily value in the week
    value_min    = minimum daily value
    value_max    = maximum daily value
    value_avg    = average of daily values
    change_week  = value_end - value_start
    data_points  = count of daily snapshots (7 for full week)
```

---

## 4. Data Export & Reporting

### Export Formats

| Format | Content | Use Case |
|--------|---------|----------|
| **PDF Report** | Charts + tables + commentary | Management presentations |
| **Excel** | Raw data + pivot-ready format | Custom analysis |
| **CSV** | Raw data, machine-readable | Data integration |
| **API (JSON)** | Programmatic access to historical data | External tools, dashboards |

### Scheduled Reports

Users can configure automated report generation:

```json
{
  "reportName": "Weekly Financial KPI Summary",
  "schedule": "every Monday at 08:00",
  "recipients": ["cfo@company.com", "controller@company.com"],
  "format": "pdf",
  "content": {
    "kpis": ["net_revenue", "ebitda", "ebitda_margin", "cash_position", "current_ratio"],
    "period": "last_7_days",
    "comparison": "previous_week",
    "includeCharts": true,
    "includeAlerts": true,
    "includeAiInsights": true
  }
}
```

---

## 5. Audit & Compliance

### Historical Data Integrity

- Every snapshot is checksummed (SHA-256)
- Checksums are hash-chained (each includes previous checksum)
- Tampering detection: any modification breaks the chain
- Independent verification: recalculation from source events must match stored snapshots

### Audit Access

```
Audit Query: "Show me how EBITDA was calculated on 2026-01-31"

Response:
  KPI: EBITDA
  Entity: Company A
  Date: 2026-01-31
  Value: 480,000 EUR

  Calculation:
    Net Revenue:              2,000,000 EUR (from 1,247 invoicing events)
    - COGS:                    -800,000 EUR (from 412 cost events)
    = Gross Profit:           1,200,000 EUR
    - Personnel:               -500,000 EUR (from 45 payroll events)
    - Rent:                     -50,000 EUR (from 1 recurring entry)
    - Marketing:                -80,000 EUR (from 23 expense events)
    - IT:                       -45,000 EUR (from 8 expense events)
    - Other OpEx:               -45,000 EUR (from 31 expense events)
    = EBITDA:                   480,000 EUR

  Source events: [link to event list]
  Snapshot checksum: sha256:a1b2c3...
  Previous checksum: sha256:d4e5f6...
  Chain valid: Yes
```

---

## Document Navigation

- Previous: [Budget Planning](./15-budget-planning.md)
- Next: [AI Integrations](./17-ai-integrations.md)
- [Back to Index](./README.md)
