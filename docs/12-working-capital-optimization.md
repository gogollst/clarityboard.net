# Working Capital Optimization

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Core Working Capital KPIs

| KPI | Formula | Target | Impact |
|-----|---------|--------|--------|
| **Working Capital** | Current Assets - Current Liabilities | Minimize while maintaining operations | Direct cash impact |
| **Days Sales Outstanding (DSO)** | (Accounts Receivable / Revenue) * 365 | < Industry average | Lower = faster cash collection |
| **Days Inventory Outstanding (DIO)** | (Inventory / COGS) * 365 | < Industry average | Lower = less capital tied up |
| **Days Payable Outstanding (DPO)** | (Accounts Payable / Purchases) * 365 | Maximize within terms | Higher = longer use of supplier financing |
| **Cash Conversion Cycle (CCC)** | DSO + DIO - DPO | Minimize (negative = ideal) | Lower = less working capital needed |
| **Working Capital Ratio** | Current Assets / Current Liabilities | 1.2-1.5 (healthy) | Balance between liquidity and efficiency |
| **Working Capital Turnover** | Revenue / Average Working Capital | Maximize | Higher = more efficient use of WC |

### Cash Conversion Cycle Visualization

```
CCC = DSO + DIO - DPO

Timeline:
  Day 0: Purchase raw materials / inventory
  ├── DIO (Days Inventory Outstanding) ──────────┐
  │   Inventory sits for 45 days                  │
  │                                               ▼
  │   Day 45: Goods sold, invoice issued
  │   ├── DSO (Days Sales Outstanding) ───────────┐
  │   │   Customer pays after 38 days             │
  │   │                                           ▼
  │   │   Day 83: Cash received from customer
  │   │
  DPO (Days Payable Outstanding) ─────┐
  Supplier paid after 30 days         │
                                      ▼
  Day 30: Cash paid to supplier

  CCC = 38 + 45 - 30 = 53 days
  Capital tied up for 53 days per revenue cycle

  Impact: If daily revenue = 10,000 EUR
  Working Capital required = 53 * 10,000 = 530,000 EUR
```

---

## 2. Component Analysis

### Accounts Receivable Analysis

**Aging Buckets:**

```
AR Aging Report:

Bucket          Amount      % of Total   Provision Rate   Provision
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Current         350,000     58.3%        0%               0
1-30 days       120,000     20.0%        1%               1,200
31-60 days       65,000     10.8%        5%               3,250
61-90 days       35,000      5.8%        15%              5,250
90-180 days      20,000      3.3%        30%              6,000
180+ days        10,000      1.7%        80%              8,000
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total           600,000     100%                          23,700

Bad Debt Provision: 23,700 EUR (3.95% of total AR)
```

**Top Debtors:**

| Rank | Customer | Outstanding | Past Due | DSO | Risk |
|------|----------|------------|----------|-----|------|
| 1 | Enterprise Corp | 85,000 | 15,000 | 42 | Medium |
| 2 | Global Inc | 62,000 | 8,000 | 35 | Low |
| 3 | Tech GmbH | 48,000 | 22,000 | 58 | High |
| 4 | Digital AG | 35,000 | 0 | 28 | Low |
| 5 | Smart Solutions | 28,000 | 12,000 | 65 | High |

**Payment Behavior Scoring (AI-Generated):**
- Score 0-100 based on: historical payment timeliness, current aging, company size, industry, credit data
- Score < 40: High risk → proactive collection, consider advance payment
- Score 40-70: Medium risk → standard monitoring, dunning process
- Score > 70: Low risk → standard terms

### Inventory Analysis (If Applicable)

```
Inventory Performance:

Category          Value       Turnover    DIO     Slow-Moving
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Raw Materials     120,000     8.5x        43d     5,000 EUR
Work in Progress   45,000     12.0x       30d     0 EUR
Finished Goods     85,000     6.2x        59d     12,000 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total             250,000     7.8x        47d     17,000 EUR

Slow-Moving (>90 days no movement): 17,000 EUR (6.8%)
Recommendation: Review finished goods for markdown/disposal
```

### Accounts Payable Analysis

```
AP Optimization Report:

Supplier          Amount    Terms     Current DPO   Early Disc.  Recommendation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
AWS               85,000    Net 30    28 days       None         Pay at 30 days
Office Rent       15,000    Due 1st   1 day         None         No change
Supplier A        42,000    2/10 N30  8 days        2% = 840     Take discount ✓
Supplier B        28,000    Net 60    45 days       None         Extend to 55 days
Supplier C        18,000    1/10 N45  42 days       1% = 180     Skip discount ✗
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Early Payment Discount Analysis:
  Supplier A: 2% for 20 days early = annualized 36.7% → TAKE DISCOUNT
  Supplier C: 1% for 3 days early = annualized 121.7% → but low amount, SKIP

DPO Optimization Potential:
  Extending Supplier B from 45 to 55 days:
  Cash benefit: 28,000 * (10/365) = 767 EUR/year (opportunity cost saved)
```

---

## 3. Optimization Recommendations (AI-Generated)

### Recommendation Engine

The AI analyzes WC components and generates actionable recommendations:

```
RECOMMENDATION #1: Implement Factoring for High-Value Invoices

Trigger: DSO (52 days) exceeds industry average (38 days) by 37%

Current State:
  Total AR: 600,000 EUR
  AR > 60 days: 65,000 EUR
  Estimated bad debt risk: 23,700 EUR
  Annual revenue impact of tied capital: ~30,000 EUR (at 5% cost of capital)

Proposed Action:
  Factor invoices > 10,000 EUR with specialized factor
  Typical advance rate: 85-90%
  Factor fee: 1.0-2.5% of invoice value

Projected Impact:
  DSO reduction: -14 days (to 38 days, industry average)
  Cash freed immediately: ~185,000 EUR
  Annual factoring cost: 8,000-20,000 EUR (depending on volume)
  Reduced bad debt provisions: ~5,000 EUR savings
  Net benefit: Improved liquidity at moderate cost

Tax Implications:
  Factoring fees are deductible operating expenses (§ 4 Abs. 4 EStG)
  VAT: Factoring services are VAT-exempt (§ 4 Nr. 8 UStG)
  No impact on HGB revenue recognition (true sale factoring)

Legal Considerations:
  Check customer contracts for assignment restrictions (§ 354a HGB)
  Notification vs. non-notification factoring
  Recourse vs. non-recourse (risk transfer)

Confidence: 85%
Priority: High
```

```
RECOMMENDATION #2: Negotiate Extended Payment Terms

Trigger: DPO (30 days) is below industry average (42 days)

Current State:
  Total AP: 188,000 EUR
  Average payment terms: Net 30
  Currently paying at: 28 days average

Proposed Action:
  Negotiate Net 45 with top 5 suppliers (excl. rent/salary)
  Affected AP volume: ~140,000 EUR/month

Projected Impact:
  DPO increase: +15 days (to 45 days)
  CCC reduction: 53 → 38 days
  Cash freed: Revenue/365 * 15 = ~41,000 EUR
  No additional cost (no early payment discount forfeited)

Legal Risk:
  Ensure extensions are formally agreed (avoid §286 BGB default)
  Document payment terms in supplier contracts
  Monitor supplier relationship impact

Confidence: 90%
Priority: Medium
```

---

## 4. What-If Scenarios

### Interactive WC Optimizer

Users can adjust parameters and see real-time impact:

```
Working Capital Optimizer:

                 Current    Target    Change    Cash Impact
DSO:             52 days    38 days   -14 days  +155,000 EUR
DIO:             47 days    40 days   -7 days   +55,000 EUR
DPO:             30 days    42 days   +12 days  +95,000 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
CCC:             69 days    36 days   -33 days  +305,000 EUR
WC Requirement:  690,000    360,000   -330,000

Annual Benefit at 5% cost of capital: 16,500 EUR
Implementation Cost (estimated): 25,000 EUR (one-time)
Payback Period: 18 months
```

### Sensitivity Analysis

```
CCC Sensitivity to DSO Changes:

DSO    CCC     WC Required    Change vs Current    Annual Savings
35     52d     520,000        -170,000             8,500
38     55d     550,000        -140,000             7,000
42     59d     590,000        -100,000             5,000
45     62d     620,000        -70,000              3,500
48     65d     650,000        -40,000              2,000
52     69d     690,000        0 (current)          0
55     72d     720,000        +30,000              -1,500
60     77d     770,000        +80,000              -4,000
```

---

## 5. Legal & Tax Considerations

### Legal Risks in WC Optimization

| Action | Legal Risk | Reference | Mitigation |
|--------|-----------|-----------|------------|
| Extending DPO beyond terms | Default interest (9pp above base) | § 288 BGB | Formal agreement with supplier |
| Late payments to suppliers | Breach of contract, dunning fees | § 286 BGB | Automated payment scheduling |
| Factoring | Assignment restrictions in contracts | § 354a HGB | Review all customer contracts |
| Aggressive collection | Customer relationship damage | - | Tiered dunning process |
| Inventory write-down | Must follow lower of cost or market | § 253 HGB | Regular inventory review |

### Tax Implications

| Action | Tax Treatment | Reference |
|--------|--------------|-----------|
| Bad debt write-off | Tax-deductible when permanently uncollectible | § 6 Abs. 1 Nr. 2 EStG |
| Factoring fees | Deductible operating expense | § 4 Abs. 4 EStG |
| Early payment discounts taken | Reduces cost of purchased goods/services | HGB net method |
| Inventory write-down to market value | Tax-deductible | § 6 Abs. 1 Nr. 2 EStG |
| Provisions for doubtful debts | Tax-deductible if individually assessed | § 6 Abs. 1 Nr. 3a EStG |

---

## 6. Automated Alerts

| Alert | Trigger | Action |
|-------|---------|--------|
| DSO exceeds industry benchmark | DSO > benchmark + 10 days | Recommendation to Finance |
| DPO approaches payment terms | DPO < 3 days from terms | Payment scheduling review |
| CCC increasing trend | CCC increases >5% over 3 months | WC optimization review |
| Single debtor concentration | One debtor > 25% of AR | Risk alert |
| Slow-moving inventory | Item value > threshold, no movement 90+ days | Inventory review prompt |
| Working Capital spike | WC increases >10% month-over-month | Alert to Finance |

---

## Document Navigation

- Previous: [Cash Flow Management](./11-cash-flow-management.md)
- Next: [Scenario Engine](./13-scenario-engine.md)
- [Back to Index](./README.md)
