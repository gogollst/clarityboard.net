# Scenario Engine

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Scenario Types

| Type | Description | Use Case |
|------|-------------|----------|
| **Base Case** | Current trajectory extrapolated with existing growth rates | Default planning assumption |
| **Best Case** | Optimistic assumptions on growth, retention, efficiency | Upside potential, investment decisions |
| **Worst Case** | Pessimistic assumptions, stress testing | Risk planning, contingency preparation |
| **Custom** | User-defined parameter modifications | Specific what-if analysis |
| **AI-Generated** | ML-based prediction from historical data + market signals | Data-driven forecasting |

---

## 2. Configurable Parameters

### Revenue Parameters

| Parameter | Type | Default (Base) | Range |
|-----------|------|----------------|-------|
| Revenue Growth Rate | % per month | Historical avg | -50% to +200% |
| New Customer Acquisition Rate | Count per month | Historical avg | 0 to 100 |
| Churn Rate | % per month | Historical avg | 0% to 30% |
| ARPA Change | EUR | 0 | -50% to +100% |
| Expansion Rate | % of customers expanding | Historical avg | 0% to 50% |
| Seasonal Adjustments | Multiplier per month | 1.0 | 0.5 to 2.0 |
| One-Time Revenue Events | EUR per date | 0 | Unlimited |
| Price Change (Date + %) | Date + % | None | Any date, -50% to +100% |
| New Product Revenue | EUR per month, start date | 0 | Unlimited |

### Cost Parameters

| Parameter | Type | Default (Base) | Range |
|-----------|------|----------------|-------|
| Salary Increases | % annual | 3% | 0% to 20% |
| New Hires | Count + date + salary | From hiring plan | 0 to 50 per quarter |
| Fixed Cost Changes | EUR + account + date | Current recurring | -100% to +200% |
| Variable Cost Ratio | % of revenue | Historical ratio | 0% to 100% |
| One-Time Expenses | EUR + date + account | None | Unlimited |
| Vendor Price Changes | % per vendor, date | 0% | -30% to +50% |
| Marketing Budget | EUR per month | Current budget | 0 to unlimited |

### External Parameters

| Parameter | Type | Default (Base) | Range |
|-----------|------|----------------|-------|
| Interest Rate Changes | % + date | Current rates | -5% to +15% |
| Exchange Rate Movements | % change per currency | ECB forecast | -30% to +30% |
| Tax Rate Changes | % + type + date | Current rates | 0% to 50% |
| Inflation Adjustment | % annual | 2% | 0% to 15% |
| Regulatory Cost Impact | EUR + date | 0 | Unlimited |

---

## 3. Scenario Calculation Engine

### Processing Flow

```
1. Load base data (current actuals + configured recurring entries)
2. Apply scenario parameter modifications
3. Calculate monthly forward projections:
   For each month (1 to projection_horizon):
     a. Revenue = existing_recurring + new_revenue + growth - churn
     b. Costs = existing_recurring + new_costs + variable_costs(revenue)
     c. Working Capital changes based on DSO/DIO/DPO assumptions
     d. Tax calculation (estimated quarterly prepayments)
     e. Cash Flow = Revenue_cash - Cost_cash - Tax - WC_change - CapEx
     f. Balance Sheet = previous + changes
     g. KPI recalculation (all affected metrics)
4. Store scenario results with version and timestamp
```

### Projection Horizon

| Horizon | Resolution | Use Case |
|---------|-----------|----------|
| 13 weeks | Weekly | Short-term liquidity planning |
| 12 months | Monthly | Annual budget and planning |
| 3 years | Quarterly | Medium-term strategic planning |
| 5 years | Annually | Long-term business planning |

---

## 4. Sensitivity Analysis

### Single-Variable Sensitivity

For each parameter, calculate impact on key outputs:

```
Parameter: Monthly Churn Rate
Base Value: 5%
Range: 2% to 10% (step: 1%)

                    12-Month Revenue    12-Month EBITDA    Ending Cash
Churn 2%:           3,560,000           856,000            1,200,000
Churn 3%:           3,320,000           712,000            1,050,000
Churn 4%:           3,080,000           568,000              900,000
Churn 5% (base):    2,840,000           424,000              750,000
Churn 6%:           2,600,000           280,000              600,000
Churn 7%:           2,360,000           136,000              450,000
Churn 8%:           2,120,000            -8,000              300,000  ⚠ EBITDA negative
Churn 9%:           1,880,000          -152,000              150,000  ⚠ Cash low
Churn 10%:          1,640,000          -296,000                    0  ⚠ Cash zero
```

### Tornado Chart (Multi-Variable Ranking)

Rank parameters by absolute impact on a chosen target metric:

```
Impact on 12-Month EBITDA (Base: 424,000 EUR):

Parameter              Low Assumption    High Assumption    Range
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Churn Rate (2%-10%)    ├────────────────────────────────────┤  1,152,000
Revenue Growth (0-30%) ├──────────────────────────────┤        920,000
Headcount (+0/+15)     ├─────────────────────────┤             680,000
ARPA Change (-10/+20%) ├──────────────────────┤                540,000
Variable Cost Ratio    ├────────────────┤                      380,000
Interest Rates (+0/+3%)├──────────┤                            240,000
FX Rates (EUR/USD)     ├───────┤                               180,000
Inflation (1%-5%)      ├─────┤                                 120,000
```

### Two-Variable Sensitivity Matrix

```
Revenue Growth vs. Churn Rate → 12-Month EBITDA:

           Churn 2%    Churn 4%    Churn 6%    Churn 8%    Churn 10%
Growth 0%  │  420,000 │  180,000 │  -60,000 │ -300,000 │ -540,000 │
Growth 5%  │  580,000 │  340,000 │  100,000 │ -140,000 │ -380,000 │
Growth 10% │  740,000 │  500,000 │  260,000 │   20,000 │ -220,000 │
Growth 15% │  900,000 │  660,000 │  420,000 │  180,000 │  -60,000 │
Growth 20% │1,060,000 │  820,000 │  580,000 │  340,000 │  100,000 │

Green cells: EBITDA > 400,000 (target)
Yellow cells: EBITDA 0-400,000
Red cells: EBITDA < 0
```

---

## 5. Monte Carlo Simulation

### Distribution Definitions

For each uncertain parameter, define a probability distribution:

| Parameter | Distribution | Parameters | Rationale |
|-----------|-------------|------------|-----------|
| Revenue Growth | Normal | mean=15%, std=5% | Historical volatility |
| Churn Rate | Beta | alpha=2, beta=38 | Bounded 0-1, skewed low |
| New Customers/Month | Poisson | lambda=10 | Count data, discrete |
| Cost Inflation | Uniform | min=2%, max=5% | Equal probability within range |
| Interest Rate | Normal | mean=3.5%, std=0.5% | Central bank guidance |
| FX Rate (EUR/USD) | Lognormal | mu=0.08, sigma=0.06 | Always positive, fat tails |
| Deal Size | Lognormal | mu=9.2, sigma=0.8 | Skewed right |

### Simulation Process

```
Monte Carlo Simulation (N = 10,000 iterations):

For each iteration i = 1 to N:
  1. Sample values from each distribution
  2. Run full 12-month projection with sampled values
  3. Record key outputs:
     - 12-month Revenue
     - 12-month EBITDA
     - Ending Cash Position
     - Minimum Cash Position (month)
     - Break-even month
     - Headcount at end
  4. Store results

Output Statistics:
  Metric: 12-Month Cash Position
  Mean:       780,000 EUR
  Std Dev:    185,000 EUR
  P5:         480,000 EUR (5% chance of being lower)
  P10:        540,000 EUR
  P25:        650,000 EUR
  P50:        770,000 EUR (median)
  P75:        900,000 EUR
  P90:      1,020,000 EUR
  P95:      1,100,000 EUR

  P(Cash < 0):              0.8%
  P(Cash < 200,000):        3.2%  ← Stress threshold
  P(EBITDA < 0):            4.5%
  P(Revenue > 3,000,000):  62.3%
```

### Visualization

- **Histogram**: Distribution of outcomes with percentile markers
- **Cumulative Distribution Function (CDF)**: Probability of exceeding any value
- **Confidence Bands**: P10-P90 bands on time series projection
- **Contribution Analysis**: Which input parameters contributed most to variance

---

## 6. Scenario Comparison

### Side-by-Side View (Up to 5 Scenarios)

```
Scenario Comparison Dashboard:

Metric               Base Case    Best Case    Worst Case   Hiring +5    No Marketing
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Revenue (12m)        2,400,000    3,200,000    1,800,000    2,400,000    2,000,000
Revenue Growth       15%          30%          5%           15%          8%
EBITDA               480,000      768,000      180,000      360,000      520,000
EBITDA Margin        20%          24%          10%          15%          26%
Net Income           336,000      538,000      126,000      252,000      364,000
Ending Cash          850,000      1,300,000    200,000      650,000      950,000
Min Cash (month)     750,000      1,100,000    100,000      500,000      850,000
Headcount (end)      45           55           40           50           45
Break-Even Month     Mar          Feb          Jun          May          Feb
CLV:CAC              4.2          5.5          2.8          3.5          N/A
Rule of 40           35%          54%          15%          30%          34%
```

### Delta View (vs. Base Case)

```
Delta vs. Base Case:

Metric               Best Case    Worst Case   Hiring +5    No Marketing
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Revenue (12m)        +800,000     -600,000     0            -400,000
EBITDA               +288,000     -300,000     -120,000     +40,000
Ending Cash          +450,000     -650,000     -200,000     +100,000
Headcount            +10          -5           +5           0
```

---

## 7. Scenario Versioning & Audit

### Version Control

- Every scenario is **immutable once saved**
- Modifications create a **new version** with reference to previous
- Full version history with parameter diff view
- Each version has: creator, timestamp, parameters, results

### Audit Trail

```json
{
  "scenarioId": "scn_2026Q1_base_v3",
  "version": 3,
  "createdBy": "usr_cfo",
  "createdAt": "2026-02-27T10:00:00Z",
  "parentVersion": "scn_2026Q1_base_v2",
  "changes": [
    {
      "parameter": "churnRate",
      "oldValue": 0.05,
      "newValue": 0.04,
      "reason": "New retention program expected to reduce churn"
    }
  ],
  "approvals": [
    {
      "approvedBy": "usr_ceo",
      "approvedAt": "2026-02-27T14:00:00Z",
      "comment": "Approved for board presentation"
    }
  ]
}
```

### Export Options

- PDF report with charts and tables
- Excel with full calculation model
- DATEV-compatible format for what-if tax calculations
- API endpoint for programmatic access

---

## Document Navigation

- Previous: [Working Capital Optimization](./12-working-capital-optimization.md)
- Next: [Document & Receipt Capture](./14-document-capture.md)
- [Back to Index](./README.md)
