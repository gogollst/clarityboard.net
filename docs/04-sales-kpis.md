# Sales KPIs

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Revenue & Pipeline KPIs

| KPI | Formula | Data Source | Unit |
|-----|---------|-------------|------|
| **Monthly Recurring Revenue (MRR)** | Sum of all active subscription values per month | Billing | EUR |
| **Annual Recurring Revenue (ARR)** | MRR * 12 | Calculated | EUR |
| **MRR Growth Rate** | (MRR_current - MRR_previous) / MRR_previous * 100 | Calculated | % |
| **New MRR** | MRR from new customers in period | Billing | EUR |
| **Expansion MRR** | MRR increase from existing customers (upsell/cross-sell) | Billing | EUR |
| **Churned MRR** | MRR lost from cancelled subscriptions | Billing | EUR |
| **Contraction MRR** | MRR decrease from downgrades | Billing | EUR |
| **Net New MRR** | New MRR + Expansion MRR - Churned MRR - Contraction MRR | Calculated | EUR |
| **Average Revenue Per Account (ARPA)** | Total MRR / Number of Active Accounts | Calculated | EUR |
| **Pipeline Value** | Sum of all open opportunity values | CRM | EUR |
| **Weighted Pipeline** | Sum of (Opportunity Value * Win Probability) per stage | CRM | EUR |
| **Pipeline Coverage** | Weighted Pipeline / Revenue Target | Calculated | Ratio |
| **Average Deal Size** | Total Closed Revenue / Number of Closed Deals | CRM | EUR |
| **Sales Velocity** | (# Opportunities * Avg Deal Size * Win Rate) / Avg Sales Cycle Length | Calculated | EUR/day |

### MRR Waterfall Analysis

```
MRR Waterfall (Monthly):

Beginning MRR          120,000 EUR
+ New MRR               15,000 EUR  (12 new customers)
+ Expansion MRR          8,000 EUR  (23 upsells)
- Contraction MRR       -2,000 EUR  (5 downgrades)
- Churned MRR           -6,000 EUR  (8 cancellations)
─────────────────────────────────────
= Ending MRR           135,000 EUR
= Net New MRR           15,000 EUR
= MRR Growth Rate       12.5%
```

### Pipeline Stage Configuration

| Stage | Default Win Probability | Typical Duration |
|-------|------------------------|-----------------|
| Prospecting | 10% | 14 days |
| Qualification | 20% | 10 days |
| Proposal | 40% | 14 days |
| Negotiation | 60% | 10 days |
| Verbal Commit | 80% | 7 days |
| Closed Won | 100% | - |
| Closed Lost | 0% | - |

Win probabilities are configurable and can be enhanced by AI prediction models based on historical conversion data.

### Sales Velocity Formula

```
Sales Velocity = (Opportunities * Avg Deal Size * Win Rate) / Avg Cycle Length

Example:
  Opportunities in pipeline: 50
  Average Deal Size: 12,000 EUR
  Win Rate: 25%
  Average Sales Cycle: 45 days

  Velocity = (50 * 12,000 * 0.25) / 45 = 3,333 EUR/day

  Improvement levers:
  - +10 opportunities → 4,000 EUR/day (+20%)
  - +2,000 EUR avg deal → 3,889 EUR/day (+17%)
  - +5% win rate → 4,000 EUR/day (+20%)
  - -10 days cycle → 4,286 EUR/day (+29%) ← Highest impact
```

---

## 2. Conversion & Efficiency KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Lead-to-Opportunity Rate** | Qualified Opportunities / Total Leads * 100 | % |
| **Opportunity-to-Close Rate** | Closed Won / Total Opportunities * 100 | % |
| **Lead-to-Close Rate** | Closed Won / Total Leads * 100 | % |
| **Win Rate** | Closed Won / (Closed Won + Closed Lost) * 100 | % |
| **Loss Rate** | Closed Lost / (Closed Won + Closed Lost) * 100 | % |
| **Average Sales Cycle Length** | Avg days from Opportunity Created to Closed Won | Days |
| **Quota Attainment** | Actual Revenue / Revenue Quota * 100 | % |
| **Sales Efficiency** | Net New ARR / Total Sales & Marketing Spend | Ratio |
| **Magic Number** | Net New ARR / Previous Quarter Sales & Marketing Spend | Ratio |
| **Revenue per Sales Rep** | Total Revenue / Number of Sales Reps | EUR |
| **Activities per Deal** | Total Sales Activities / Number of Deals | Count |
| **Proposal-to-Close Ratio** | Proposals Sent / Deals Closed | Ratio |
| **Demo-to-Close Ratio** | Demos Conducted / Deals Closed | Ratio |

### Conversion Funnel Visualization

```
Stage               Count    Conversion    Cumulative
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Leads               1,000    100%          100%
  ↓ 30%
MQLs                  300    30%           30%
  ↓ 50%
SQLs                  150    50%           15%
  ↓ 40%
Opportunities          60    40%           6%
  ↓ 33%
Proposals Sent         20    33%           2%
  ↓ 50%
Closed Won             10    50%           1%

Overall Lead-to-Close: 1.0%
Average time: 62 days
```

### Sales Efficiency Benchmarks

| Metric | Poor | Acceptable | Good | Excellent |
|--------|------|------------|------|-----------|
| Magic Number | < 0.5 | 0.5-0.75 | 0.75-1.0 | > 1.0 |
| Sales Efficiency | < 0.5 | 0.5-1.0 | 1.0-1.5 | > 1.5 |
| Win Rate | < 15% | 15-25% | 25-35% | > 35% |
| Quota Attainment | < 60% | 60-80% | 80-100% | > 100% |
| Sales Cycle | > 90 days | 60-90 days | 30-60 days | < 30 days |

---

## 3. Customer KPIs

| KPI | Formula | Target | Unit |
|-----|---------|--------|------|
| **Customer Lifetime Value (CLV)** | ARPA * Gross Margin * (1 / Churn Rate) | Maximize | EUR |
| **Customer Acquisition Cost (CAC)** | Total Sales & Marketing Spend / New Customers Acquired | Minimize | EUR |
| **CLV:CAC Ratio** | CLV / CAC | > 3:1 | Ratio |
| **CAC Payback Period** | CAC / (ARPA * Gross Margin) | < 12 months | Months |
| **Net Revenue Retention (NRR)** | (Beginning MRR + Expansion - Contraction - Churn) / Beginning MRR * 100 | > 110% | % |
| **Gross Revenue Retention (GRR)** | (Beginning MRR - Contraction - Churn) / Beginning MRR * 100 | > 90% | % |
| **Logo Churn Rate** | Lost Customers / Beginning Customers * 100 | < 5% monthly | % |
| **Revenue Churn Rate** | Churned MRR / Beginning MRR * 100 | < 3% monthly | % |
| **Net Promoter Score (NPS)** | % Promoters - % Detractors | > 50 | Score |
| **Customer Health Score** | Composite of usage, engagement, support, payment | > 70 | Score (0-100) |

### CLV Calculation Models

**Simple CLV:**
```
CLV = ARPA * Gross Margin * Average Customer Lifetime

Where:
  Average Customer Lifetime = 1 / Monthly Churn Rate

Example:
  ARPA = 1,000 EUR/month
  Gross Margin = 80%
  Monthly Churn = 2%
  Lifetime = 1/0.02 = 50 months

  CLV = 1,000 * 0.80 * 50 = 40,000 EUR
```

**Discounted CLV (time-value adjusted):**
```
CLV = Sum over t=1 to T of: (ARPA * Gross Margin * Retention^t) / (1 + d)^t

Where:
  d = monthly discount rate
  T = projection period (typically 60 months)
```

### Customer Segmentation (for drill-down)

| Segment | Criteria | Special KPIs |
|---------|----------|-------------|
| Enterprise | ARR > 100,000 EUR | Dedicated CSM, NPS, expansion rate |
| Mid-Market | ARR 10,000-100,000 EUR | Usage trends, upsell signals |
| SMB | ARR < 10,000 EUR | Self-serve adoption, churn risk score |
| New (< 90 days) | Onboarding phase | Time to first value, activation rate |
| At-Risk | Health score < 40 | Engagement drop, support frequency |
| Champion | Health score > 80, NPS promoter | Referral potential, case study |

---

## 4. Forecasting Integration

Sales KPIs feed directly into financial forecasts:

| Sales KPI | Financial Impact |
|-----------|-----------------|
| MRR / ARR | Revenue line in P&L forecast |
| Weighted Pipeline | Probability-weighted revenue forecast |
| Churn Rate | Revenue at risk, cash flow impact |
| CAC | Marketing & sales budget requirement |
| CLV:CAC | Investment efficiency, growth sustainability |
| Sales Cycle Length | Cash flow timing (revenue recognition delay) |
| Expansion MRR | Organic growth component in forecasts |

**AI-Enhanced Forecasting:**
- Win probability per deal based on historical patterns (stage, deal size, industry, rep)
- Churn prediction based on usage patterns, support tickets, payment behavior
- Revenue forecast combining pipeline + recurring + historical conversion

---

## Document Navigation

- Previous: [Financial KPIs](./03-financial-kpis.md)
- Next: [Marketing KPIs](./05-marketing-kpis.md)
- [Back to Index](./README.md)
