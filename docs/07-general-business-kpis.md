# General Business KPIs

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Operational Efficiency

| KPI | Formula | Target | Unit |
|-----|---------|--------|------|
| **Operating Expense Ratio (OER)** | Operating Expenses / Revenue * 100 | Minimize | % |
| **SG&A as % of Revenue** | SG&A Expenses / Revenue * 100 | < 25% typical | % |
| **Rule of 40** | Revenue Growth Rate + EBITDA Margin | > 40% | % |
| **Burn Multiple** | Net Cash Burned / Net New ARR | < 1.5 | Ratio |
| **Revenue per Employee** | Total Revenue / Total Employees | Maximize | EUR |
| **Output per Employee** | Defined per industry (e.g., units, projects) | Maximize | Varies |
| **Process Cycle Efficiency** | Value-Added Time / Total Process Time * 100 | > 25% | % |
| **Overhead Ratio** | Overhead Costs / Direct Costs * 100 | Minimize | % |
| **Asset Utilization Rate** | Revenue / Total Assets | Maximize | Ratio |
| **Capacity Utilization** | Actual Output / Maximum Output * 100 | 75-90% | % |

### Rule of 40 Analysis

```
Rule of 40 = Revenue Growth Rate + EBITDA Margin

Interpretation:
  > 40%: Excellent - strong growth with profitability
  30-40%: Good - healthy balance
  20-30%: Acceptable - needs improvement in either growth or margins
  < 20%: Concerning - both growth and profitability need attention

Scenarios:
  High Growth:   Growth 50% + Margin -10% = 40% ✓ (acceptable for scaling)
  Balanced:      Growth 25% + Margin 20%  = 45% ✓ (ideal)
  Profitable:    Growth 5%  + Margin 40%  = 45% ✓ (mature business)
  Struggling:    Growth 10% + Margin 5%   = 15% ✗ (needs strategic review)
```

### Operational Efficiency Dashboard Card

```
┌─────────────────────────────────────────┐
│ Operational Efficiency Score      [?]   │
│                                         │
│         78 / 100                        │
│         ████████░░                      │
│                                         │
│ Components:                             │
│  OER: 62%          ▼ -3pp vs LQ        │
│  Rev/Employee: 185k ▲ +12% vs LQ      │
│  Rule of 40: 42%   ▲ +5pp vs LQ       │
│  Overhead: 28%     ▼ -2pp vs LQ       │
│                                         │
│ [View Breakdown →]                      │
└─────────────────────────────────────────┘
```

---

## 2. Growth KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Year-over-Year Revenue Growth** | (Revenue_current_year - Revenue_previous_year) / Revenue_previous_year * 100 | % |
| **Quarter-over-Quarter Growth** | Same logic, quarterly basis | % |
| **Month-over-Month Growth** | Same logic, monthly basis | % |
| **Compound Annual Growth Rate (CAGR)** | (Ending Value / Beginning Value)^(1/n) - 1 | % |
| **Market Share** | Company Revenue / Total Addressable Market * 100 | % |
| **Customer Growth Rate** | (Customers_end - Customers_start) / Customers_start * 100 | % |
| **Geographic Expansion Rate** | New markets entered / Target markets | % |
| **Product Adoption Rate** | Users of new product / Total addressable users * 100 | % |
| **Organic Growth Rate** | Growth excl. acquisitions and currency effects | % |
| **Net Revenue Expansion** | Revenue growth from existing customers only | % |

### Growth Decomposition

```
Revenue Growth Decomposition (YoY):

Total Revenue Growth: +800,000 EUR (+25%)

Breakdown:
  New Customer Acquisition:    +500,000 EUR (62.5%)
  Existing Customer Expansion: +200,000 EUR (25.0%)
  Price Increases:              +50,000 EUR  (6.3%)
  New Product Revenue:         +150,000 EUR (18.7%)
  Customer Churn:              -100,000 EUR (-12.5%)
  ─────────────────────────────────────────────
  Net Growth:                  +800,000 EUR (100%)

Quality Assessment:
  Organic vs. Inorganic: 100% organic ✓
  Recurring vs. One-time: 85% recurring ✓
  Concentrated vs. Diversified: Top 10 = 35% of revenue ✓
```

### CAGR Calculation

```
CAGR = (Ending Value / Beginning Value)^(1/n) - 1

Example (3-year revenue):
  Year 0: 1,000,000 EUR
  Year 1: 1,300,000 EUR (+30%)
  Year 2: 1,500,000 EUR (+15%)
  Year 3: 2,000,000 EUR (+33%)

  CAGR = (2,000,000 / 1,000,000)^(1/3) - 1 = 26.0%

  Note: CAGR smooths volatility - actual growth varied 15-33%
```

---

## 3. Quality & Risk KPIs

| KPI | Formula | Target | Unit |
|-----|---------|--------|------|
| **Customer Complaint Rate** | Complaints / Total Transactions * 100 | < 1% | % |
| **First Response Time** | Avg time to first customer response | < 4 hours | Hours |
| **Resolution Time** | Avg time to close support ticket | < 24 hours | Hours |
| **Customer Satisfaction (CSAT)** | Satisfied responses / Total survey responses * 100 | > 85% | % |
| **SLA Compliance Rate** | Transactions within SLA / Total Transactions * 100 | > 99% | % |
| **System Uptime** | Available Time / Total Time * 100 | > 99.9% | % |
| **Data Quality Score** | Valid Records / Total Records * 100 | > 98% | % |
| **Concentration Risk (Revenue)** | Revenue from Top Client / Total Revenue * 100 | < 20% | % |
| **Concentration Risk (Top 5)** | Revenue from Top 5 Clients / Total Revenue * 100 | < 40% | % |
| **Supplier Dependency** | Purchases from Top Supplier / Total Purchases * 100 | < 30% | % |
| **Regulatory Compliance Score** | Compliant Items / Total Auditable Items * 100 | 100% | % |

### Risk Heatmap Configuration

```
Risk Assessment Matrix:

                    Low Impact    Medium Impact    High Impact
High Probability   │  Medium   │    High       │  Critical   │
Medium Probability │  Low      │    Medium     │  High       │
Low Probability    │  Info     │    Low        │  Medium     │

Tracked Risk Categories:
  - Revenue Concentration (single customer > 20%)
  - Supplier Dependency (single supplier > 30%)
  - Key Person Risk (critical roles with single coverage)
  - Regulatory Risk (upcoming compliance deadlines)
  - Currency Risk (FX exposure > 10% of revenue)
  - Interest Rate Risk (variable rate debt exposure)
  - Technology Risk (end-of-life systems, security vulnerabilities)
```

### Customer Concentration Analysis

```
Revenue Concentration Report:

Rank  Customer           Revenue      Share     Cumulative
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1     Enterprise Corp    480,000      16.0%     16.0%
2     Global Inc         360,000      12.0%     28.0%
3     Tech GmbH          240,000       8.0%     36.0%
4     Digital AG          180,000       6.0%     42.0%
5     Smart Solutions     150,000       5.0%     47.0%
...
Other (45 customers)   1,590,000      53.0%    100.0%
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total                  3,000,000     100.0%

Risk Assessment:
  Top 1 customer: 16.0% → OK (< 20% threshold)
  Top 5 customers: 47.0% → WARNING (> 40% threshold)
  Herfindahl Index: 0.058 → Moderate concentration

  Recommendation: Diversify revenue by acquiring smaller accounts
```

---

## 4. Strategic KPIs (Executive View)

| KPI | Description | Visualization |
|-----|-------------|---------------|
| **Business Health Score** | Composite score (0-100) weighting financial, operational, and growth metrics | Gauge chart |
| **Strategic Goal Progress** | % completion of annual strategic objectives | Progress bars |
| **Investment Efficiency** | ROI on strategic investments (new products, markets, M&A) | Bar chart |
| **Competitive Position** | Relative performance vs. key competitors (where data available) | Radar chart |
| **Innovation Pipeline** | New product/feature development progress | Kanban/timeline |
| **Sustainability Score** | ESG-related metrics (if tracked) | Score card |

### Business Health Score Calculation

```
Business Health Score = Weighted average of category scores

Category Weights (configurable):
  Financial Health:    30%  (profitability, liquidity, debt ratios)
  Growth:             25%  (revenue growth, customer growth, market share)
  Operational:        20%  (efficiency, quality, employee metrics)
  Customer:           15%  (retention, satisfaction, CLV trends)
  Risk:               10%  (concentration, compliance, stability)

Each category scored 0-100 based on KPI performance vs. targets

Example:
  Financial:    82/100 * 0.30 = 24.6
  Growth:       75/100 * 0.25 = 18.8
  Operational:  88/100 * 0.20 = 17.6
  Customer:     70/100 * 0.15 = 10.5
  Risk:         90/100 * 0.10 =  9.0
  ─────────────────────────────────
  Business Health Score: 80.5 / 100
```

---

## Document Navigation

- Previous: [HR KPIs](./06-hr-kpis.md)
- Next: [Data Ingestion & Single Source of Truth](./08-data-ingestion.md)
- [Back to Index](./README.md)
