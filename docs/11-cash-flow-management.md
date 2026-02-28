# Cash Flow Management

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Cash Flow Statement (Direct Method)

```
Operating Cash Flow (OCF)
  + Cash received from customers
  - Cash paid to suppliers
  - Cash paid to employees
  - Cash paid for operating expenses
  - Income tax paid
  - Interest paid
  = Net OCF

Investing Cash Flow (ICF)
  - Purchase of fixed assets (CapEx)
  + Sale of fixed assets
  - Purchase of financial investments
  + Proceeds from financial investments
  = Net ICF

Financing Cash Flow (FinCF)
  + Proceeds from loans
  - Loan repayments
  + Capital contributions
  - Dividends paid
  - Share buybacks
  = Net FinCF

Net Change in Cash = OCF + ICF + FinCF
Ending Cash = Beginning Cash + Net Change in Cash

Free Cash Flow (FCF) = OCF - CapEx
  (Eigenstaendige KPI, nicht mit Financing Cash Flow verwechseln)
```

### Cash Flow vs. P&L Reconciliation

```
Net Income (from P&L)                    150,000 EUR
+ Depreciation & Amortization             50,000 EUR
+ Provisions increase                     10,000 EUR
- Provisions decrease                     -5,000 EUR
+ Loss on asset disposal                   5,000 EUR
+ Decrease in Accounts Receivable         10,000 EUR
- Increase in Inventory                   -8,000 EUR
+ Increase in Accounts Payable            15,000 EUR
+ Increase in Accrued Liabilities          8,000 EUR
- Decrease in Deferred Revenue            -3,000 EUR
- Increase in Prepaid Expenses            -2,000 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
= Operating Cash Flow                    230,000 EUR
```

---

## 2. Invoice Splitting & Period Allocation

### Automatic Period Allocation

Invoices covering multiple periods are automatically split for P&L accuracy while maintaining cash flow reality:

**Example: Annual software license**
```
Invoice Date:     2026-01-15
Coverage Period:  2026-01-01 to 2026-12-31
Total Amount:     12,000 EUR (net) + 2,280 EUR VAT = 14,280 EUR
Payment Terms:    Net 30

Cash Flow Impact:
  February 2026: -14,280 EUR (payment date)

P&L Impact (monthly allocation):
  January:   1,000 EUR expense
  February:  1,000 EUR expense
  March:     1,000 EUR expense
  ...
  December:  1,000 EUR expense

Balance Sheet (Prepaid Expenses):
  January:   11,000 EUR (12,000 - 1,000)
  February:  10,000 EUR
  March:      9,000 EUR
  ...
  November:   1,000 EUR
  December:       0 EUR
```

### Split Methods

| Method | Description | Use Case |
|--------|-------------|----------|
| **Equal Monthly** | Total / number of months | Standard subscriptions |
| **Daily Pro-Rata** | Total / days * days in month | Partial month coverage |
| **Custom Schedule** | User-defined per-period amounts | Seasonal or variable pricing |
| **Front-Loaded** | Higher allocation in early months | Implementation fees + subscription |
| **Back-Loaded** | Higher allocation in later months | Milestone-based payments |

### Split Configuration

```json
{
  "invoiceId": "inv_001",
  "splitConfig": {
    "method": "equal_monthly",
    "startDate": "2026-01-01",
    "endDate": "2026-12-31",
    "totalNetAmount": 12000.00,
    "cashFlowDate": "2026-02-14",
    "expenseAccount": "7680",
    "prepaidAccount": "1500"
  }
}
```

---

## 3. Recurring Revenue & Cost Configuration

### Recurring Entry Definition

```json
{
  "name": "AWS Infrastructure",
  "type": "recurring_expense",
  "category": "infrastructure",
  "amount": 8500.00,
  "currency": "EUR",
  "frequency": "monthly",
  "startDate": "2026-01-01",
  "endDate": null,
  "growthRate": 0.05,
  "growthType": "monthly_compound",
  "account": "4964",
  "counterAccount": "1600",
  "vatRate": 19,
  "costCenter": "IT",
  "tags": ["cloud", "infrastructure"],
  "notes": "Expected 5% monthly growth due to scaling",
  "scenarioLinks": {
    "base": { "amount": 8500, "growthRate": 0.05 },
    "optimistic": { "amount": 8500, "growthRate": 0.03 },
    "pessimistic": { "amount": 8500, "growthRate": 0.08 }
  }
}
```

### Recurring Entry Types

| Type | Description | Examples |
|------|-------------|---------|
| **Recurring Revenue** | Regular income streams | Subscriptions, retainers, rent income |
| **Recurring Expense** | Regular cost items | SaaS tools, rent, insurance, salaries |
| **Recurring Transfer** | Inter-entity payments | Management fees, royalties, IC charges |
| **Seasonal** | Recurring but only in specific months | Annual insurance, quarterly bonuses |

### Growth Models

| Model | Formula | Use Case |
|-------|---------|----------|
| **Fixed** | amount (constant) | Stable costs (rent, insurance) |
| **Linear Growth** | amount + (growthRate * month_index) | Gradual increase |
| **Compound Growth** | amount * (1 + growthRate)^month_index | Usage-based scaling |
| **Step Function** | amount until trigger, then new_amount | Price tier changes |
| **Seasonal Pattern** | amount * seasonal_factor[month] | Seasonal business |

### Variance Detection

When actual values arrive for a recurring entry:
- Compare actual vs. expected
- If deviation > 10%: Flag for review
- If deviation > 25%: Alert to Finance
- System learns and adjusts future projections based on actuals

---

## 4. Rolling 13-Week Cash Flow Forecast

### Forecast Structure

```
13-Week Cash Flow Forecast

Week of     Beginning   Confirmed   Probable    Planned     Outflows     Net       Ending
            Cash        Inflows     Inflows     Inflows     (Committed)  Change    Cash
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Mar 03      500,000     80,000      20,000      5,000       -95,000      10,000    510,000
Mar 10      510,000     60,000      25,000      10,000      -85,000      10,000    520,000
Mar 17      520,000     120,000     15,000      5,000       -110,000     30,000    550,000
Mar 24      550,000     40,000      30,000      8,000       -90,000      -12,000   538,000
...
May 26      485,000     70,000      35,000      12,000      -88,000      29,000    514,000
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Minimum Cash Position: 472,000 EUR (Week of Apr 14)
Minimum Cash Buffer: 72,000 EUR above minimum threshold (400,000 EUR)
```

### Inflow Categories

| Category | Certainty | Weight in Forecast | Source |
|----------|-----------|-------------------|--------|
| **Confirmed** | Invoiced, payment terms known | 100% | Billing system |
| **Recurring** | Configured recurring revenue | 95% | Configuration |
| **Probable** | Pipeline with probability > 60% | Probability-weighted | CRM |
| **Planned** | Budget-based expectation | 70% | Budget |
| **Speculative** | Early-stage pipeline | 20-50% | CRM |

### Outflow Categories

| Category | Certainty | Deferrable | Source |
|----------|-----------|-----------|--------|
| **Committed** | Invoices received, contracts | 100% | Billing/ERP |
| **Payroll** | Salary obligations | 100% | HR system |
| **Tax** | Tax prepayments, VAT | 100% | Tax calendar |
| **Recurring** | Configured recurring costs | 95% | Configuration |
| **Planned** | Budget allocations | 80% | Budget |
| **Discretionary** | Deferrable spending | Variable | Manual |

### Liquidity Alert Thresholds

| Level | Condition | Recipients | Response |
|-------|-----------|-----------|----------|
| **Info** | Cash projected below 150% of minimum within 8 weeks | Finance | Dashboard notification |
| **Warning** | Cash projected below 120% of minimum within 4 weeks | Finance + Executive | Email + dashboard |
| **Critical** | Cash projected below minimum within 2 weeks | Finance + Executive | Email + SMS + dashboard |
| **Emergency** | Cash projected to go negative at any point | All stakeholders | Immediate SMS + email + dashboard |

---

## 5. Multi-Currency Handling

### Currency Management

| Feature | Implementation |
|---------|---------------|
| **Storage** | All transactions stored in original currency + EUR equivalent |
| **Exchange Rates** | ECB reference rates, updated daily via API |
| **Historical Rates** | Stored per day for retrospective analysis |
| **Booking Rate** | Transaction date rate (or custom rate if specified) |
| **Reporting Rate** | End-of-period rate for balance sheet, average rate for P&L |
| **Rate Source** | ECB (default), configurable for other providers |

### FX Gain/Loss Calculation

```
Unrealized FX Gain/Loss (Open Positions):

Example: USD invoice of $10,000 booked at 1 EUR = 1.08 USD

  Original booking: 10,000 / 1.08 = 9,259.26 EUR
  Current rate: 1 EUR = 1.12 USD
  Current value: 10,000 / 1.12 = 8,928.57 EUR
  Unrealized FX loss: 9,259.26 - 8,928.57 = 330.69 EUR

  Booking (month-end revaluation):
  Debit  2380 (Unrealized FX Loss)     330.69 EUR
  Credit 1400 (Accounts Receivable)    330.69 EUR

Realized FX Gain/Loss (On Payment):

  Payment received: $10,000 at rate 1 EUR = 1.10 USD
  Received: 10,000 / 1.10 = 9,090.91 EUR
  Original booking: 9,259.26 EUR
  Realized FX loss: 9,259.26 - 9,090.91 = 168.35 EUR

  Booking:
  Debit  1200 (Bank)                  9,090.91 EUR
  Debit  2381 (Realized FX Loss)        168.35 EUR
  Credit 1400 (Accounts Receivable)   9,259.26 EUR
```

### Currency Exposure Report

```
Currency Exposure Summary:

Currency  Receivables  Payables   Net Exposure  % of Revenue  Risk Level
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
EUR       850,000      620,000    +230,000      -             Domestic
USD       120,000      180,000    -60,000       4.0%          Medium
GBP       45,000       10,000     +35,000       1.5%          Low
CHF       80,000       30,000     +50,000       2.7%          Medium
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Hedging Recommendation:
  USD net short position of 60,000 EUR → Consider forward contract
  CHF net long position of 50,000 EUR → Monitor, hedge if > 5% revenue
```

---

## 6. Cash Flow Visualization

### Primary Chart: Rolling Cash Flow Projection
- X-axis: Weeks (13 weeks forward)
- Y-axis: EUR
- Series: Confirmed + Probable + Planned (stacked area)
- Overlay: Minimum cash threshold line
- Color: Green (above threshold), Orange (near threshold), Red (below threshold)

### Secondary Charts
- Waterfall: OCF → ICF → FCF → Net Change
- Trend: Monthly actual cash flow (12 months) with forecast overlay
- Comparison: Actual vs. forecast accuracy (forecast from 4 weeks ago vs. actual)

---

## Document Navigation

- Previous: [HGB Accounting & DATEV Export](./10-hgb-accounting-datev.md)
- Next: [Working Capital Optimization](./12-working-capital-optimization.md)
- [Back to Index](./README.md)
