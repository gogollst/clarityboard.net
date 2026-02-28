# Financial KPIs

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Profitability KPIs

| KPI | Formula | Data Source | Frequency | Unit |
|-----|---------|-------------|-----------|------|
| **Gross Revenue** | Sum of all invoiced amounts (gross, incl. VAT) | Billing system | Real-time | EUR |
| **Net Revenue** | Gross Revenue - VAT - Discounts - Returns | Billing system | Real-time | EUR |
| **Cost of Goods Sold (COGS)** | Direct costs attributable to revenue generation | ERP / Billing | Real-time | EUR |
| **Gross Profit** | Net Revenue - COGS | Calculated | Real-time | EUR |
| **Gross Margin** | Gross Profit / Net Revenue * 100 | Calculated | Real-time | % |
| **EBITDA** | Operating Income + Depreciation + Amortization | Calculated | Monthly | EUR |
| **EBITDA Margin** | EBITDA / Net Revenue * 100 | Calculated | Monthly | % |
| **EBIT** | EBITDA - Depreciation - Amortization | Calculated | Monthly | EUR |
| **EBT** | EBIT - Interest Expense + Interest Income | Calculated | Monthly | EUR |
| **Net Income** | EBT - Income Tax Expense | Calculated | Monthly | EUR |
| **Net Margin** | Net Income / Net Revenue * 100 | Calculated | Monthly | % |
| **Contribution Margin (per product/service)** | Revenue - Variable Costs per unit | Billing + ERP | Real-time | EUR |
| **Contribution Margin Ratio** | Contribution Margin / Revenue * 100 | Calculated | Real-time | % |
| **Break-Even Revenue** | Fixed Costs / Contribution Margin Ratio | Calculated | Monthly | EUR |
| **Break-Even Units** | Fixed Costs / Contribution Margin per Unit | Calculated | Monthly | Units |

### EBITDA Calculation Detail

```
EBITDA = Net Revenue
       - COGS
       - Operating Expenses (excl. D&A)
       + Other Operating Income
       - Other Operating Expenses

Where Operating Expenses include:
  - Personnel costs (salaries, social security, pensions)
  - Rent and lease payments
  - Marketing and advertising spend
  - IT and infrastructure costs
  - Professional services
  - Travel and entertainment
  - Insurance
  - Other administrative expenses

Excluded from EBITDA:
  - Depreciation of tangible assets
  - Amortization of intangible assets
  - Interest income/expense
  - Tax expense
  - Extraordinary items
```

### Drill-Down Levels

1. **Company-wide** - Total EBITDA across all operations
2. **Per entity** - Individual subsidiary (in multi-entity setup)
3. **Per department / cost center** - Engineering, Sales, Marketing, etc.
4. **Per product line / service** - Individual product profitability
5. **Monthly trend** - With year-over-year comparison overlay

### Contribution Margin Analysis

```
Contribution Margin Statement:
  Revenue (per product/service)        100,000 EUR
  - Variable Costs
    - Direct materials                  -25,000 EUR
    - Direct labor                      -15,000 EUR
    - Variable overhead                  -5,000 EUR
    - Sales commission                   -8,000 EUR
  ─────────────────────────────────────────────────
  = Contribution Margin                  47,000 EUR
  = Contribution Margin Ratio            47.0%

  Break-Even Analysis:
    Fixed Costs (monthly)               200,000 EUR
    Break-Even Revenue = 200,000 / 0.47 = 425,532 EUR
    Current Revenue                     500,000 EUR
    Safety Margin = (500,000 - 425,532) / 500,000 = 14.9%
```

---

## 2. Liquidity KPIs

| KPI | Formula | Threshold | Unit |
|-----|---------|-----------|------|
| **Cash Position** | Sum of all bank account balances | Configurable minimum | EUR |
| **Current Ratio** | Current Assets / Current Liabilities | > 1.5 healthy, < 1.0 critical | Ratio |
| **Quick Ratio (Acid Test)** | (Current Assets - Inventory) / Current Liabilities | > 1.0 healthy | Ratio |
| **Cash Ratio** | Cash & Equivalents / Current Liabilities | > 0.2 | Ratio |
| **Operating Cash Flow (OCF)** | Net Income + Non-Cash Items + Changes in Working Capital | Positive | EUR |
| **Free Cash Flow (FCF)** | OCF - Capital Expenditures | Positive | EUR |
| **Cash Burn Rate** | (Starting Cash - Ending Cash) / Number of Months | N/A (startups) | EUR/month |
| **Cash Runway** | Current Cash / Monthly Cash Burn Rate | > 12 months | Months |
| **Debt-to-Equity** | Total Liabilities / Total Equity | Industry-dependent | Ratio |
| **Interest Coverage Ratio** | EBIT / Interest Expense | > 3.0 | Ratio |

### Automated Alert Configuration

| Condition | Severity | Recipients | Channel |
|-----------|----------|-----------|---------|
| Cash Position drops below defined minimum | **Critical** | Finance + Executive | Email + Dashboard + SMS |
| Current Ratio < 1.2 | **Warning** | Finance | Dashboard + Email |
| Current Ratio < 1.0 | **Critical** | Finance + Executive | Email + Dashboard + SMS |
| Cash Runway < 6 months | **Warning** | Executive | Dashboard + Email |
| Cash Runway < 3 months | **Critical** | All stakeholders | Email + Dashboard + SMS |
| FCF negative for 3 consecutive months | **Warning** | Finance + Executive | Dashboard + Email |
| Debt-to-Equity > 3.0 | **Warning** | Finance | Dashboard |

### OCF Calculation (Indirect Method)

```
Net Income                              150,000 EUR
+ Depreciation & Amortization            50,000 EUR
+ Loss on asset disposal                  5,000 EUR
- Gain on asset disposal                      0 EUR
+ Decrease in Accounts Receivable        10,000 EUR
- Increase in Accounts Receivable             0 EUR
+ Decrease in Inventory                   5,000 EUR
- Increase in Inventory                       0 EUR
+ Increase in Accounts Payable           15,000 EUR
- Decrease in Accounts Payable                0 EUR
+ Increase in Accrued Liabilities         8,000 EUR
- Decrease in Prepaid Expenses            2,000 EUR
─────────────────────────────────────────────────
= Operating Cash Flow                   245,000 EUR
- Capital Expenditures                  -80,000 EUR
─────────────────────────────────────────────────
= Free Cash Flow                        165,000 EUR
```

---

## 3. Return KPIs

| KPI | Formula | Benchmark | Unit |
|-----|---------|-----------|------|
| **Return on Equity (ROE)** | Net Income / Average Shareholders' Equity * 100 | > 15% | % |
| **Return on Assets (ROA)** | Net Income / Average Total Assets * 100 | > 5% | % |
| **Return on Investment (ROI)** | (Gain from Investment - Cost of Investment) / Cost of Investment * 100 | > Cost of Capital | % |
| **Return on Capital Employed (ROCE)** | EBIT / (Total Assets - Current Liabilities) * 100 | > WACC | % |
| **Weighted Average Cost of Capital (WACC)** | (E/V * Re) + (D/V * Rd * (1-T)) | Reference rate | % |

### DuPont Analysis (ROE Decomposition)

```
ROE = Net Margin * Asset Turnover * Equity Multiplier

Where:
  Net Margin        = Net Income / Revenue
  Asset Turnover    = Revenue / Total Assets
  Equity Multiplier = Total Assets / Shareholders' Equity

Decomposition identifies ROE drivers:
  - Profitability improvement → Net Margin
  - Efficiency improvement → Asset Turnover
  - Leverage optimization → Equity Multiplier

Example:
  ROE = 8% * 1.5 * 2.5 = 30%

  Scenario: What if Net Margin improves to 10%?
  ROE = 10% * 1.5 * 2.5 = 37.5% (+7.5pp)
```

### WACC Calculation Detail

```
WACC = (E/V * Re) + (D/V * Rd * (1-T))

Where:
  E  = Market value of equity
  D  = Market value of debt
  V  = E + D (total firm value)
  Re = Cost of equity (CAPM: Rf + Beta * (Rm - Rf))
  Rd = Cost of debt (weighted average interest rate)
  T  = Corporate tax rate

Example:
  E = 5,000,000 EUR, D = 2,000,000 EUR, V = 7,000,000 EUR
  Re = 12% (Rf=3%, Beta=1.2, Market Premium=7.5%)
  Rd = 4%, T = 30% (KSt 15% + Soli 0.825% + GewSt ~14%)

  WACC = (5M/7M * 12%) + (2M/7M * 4% * 0.7)
       = 8.57% + 0.80%
       = 9.37%
```

---

## 4. Tax-Relevant KPIs

| KPI | Formula | Notes |
|-----|---------|-------|
| **Effective Tax Rate** | Total Tax Expense / EBT * 100 | Compare against statutory rate (~30%) |
| **Tax Shield (Depreciation)** | Depreciation * Marginal Tax Rate | Benefit from D&A deductions |
| **VAT Balance** | Output VAT Collected - Input VAT Paid | Monthly settlement obligation |
| **Trade Tax Base (Gewerbesteuer)** | EBIT + 25% of financing costs > 200k EUR | Municipal rate varies |
| **Corporate Tax Base (KSt)** | EBT adjusted per KStG | 15% + 5.5% Solidaritatszuschlag |
| **Deductible Expenses Ratio** | Tax-Deductible OpEx / Total OpEx | Optimization indicator |

### German Tax Calculation

```
Corporate Income Tax (Korperschaftsteuer):
  Taxable Income                        500,000 EUR
  KSt Rate: 15%                          75,000 EUR
  Solidaritatszuschlag: 5.5% of KSt       4,125 EUR
  Subtotal                                79,125 EUR

Trade Tax (Gewerbesteuer):
  Gewerbeertrag (trade income)          500,000 EUR
  + Hinzurechnungen (add-backs):
    25% of interest > 200k EUR           10,000 EUR
  - Kurzungen (deductions):
    1.2% of real estate value            -6,000 EUR
  Adjusted Gewerbeertrag                504,000 EUR
  Steuermessbetrag: 3.5%                 17,640 EUR
  Hebesatz (municipal): 400%             70,560 EUR

  Total Tax Burden:
  KSt + Soli + GewSt = 79,125 + 70,560 = 149,685 EUR
  Effective Tax Rate = 149,685 / 500,000 = 29.94%
```

### Compliance Warnings (Automated)

| Trigger | Alert | Action |
|---------|-------|--------|
| Transfer pricing deviations > 10% from arm's length | Warning | Documentation reminder |
| Related-party transactions exceeding thresholds | Info | Compliance review prompt |
| VAT filing deadline approaching (10th of following month) | Reminder | Email to Finance |
| Corporate tax prepayment due dates | Reminder | Calendar alert |
| Effective tax rate deviates >5pp from statutory rate | Info | Analysis prompt |
| Tax-deductible expenses ratio drops >10% YoY | Warning | Review recommendation |

---

## 5. KPI Dependency Map (Financial)

```
Revenue (Source: Billing)
├── Gross Profit = Revenue - COGS
│   ├── Gross Margin = Gross Profit / Revenue
│   ├── EBITDA = Gross Profit - OpEx (excl D&A)
│   │   ├── EBITDA Margin = EBITDA / Revenue
│   │   ├── EBIT = EBITDA - D&A
│   │   │   ├── EBT = EBIT - Net Interest
│   │   │   │   ├── Net Income = EBT - Tax
│   │   │   │   │   ├── Net Margin
│   │   │   │   │   ├── ROE = Net Income / Equity
│   │   │   │   │   ├── ROA = Net Income / Assets
│   │   │   │   │   └── EPS = Net Income / Shares
│   │   │   │   └── Effective Tax Rate = Tax / EBT
│   │   │   ├── ROCE = EBIT / Capital Employed
│   │   │   └── Interest Coverage = EBIT / Interest
│   │   └── Rule of 40 = Growth + EBITDA Margin
│   └── Break-Even = Fixed Costs / CM Ratio
├── Cash Position (Source: Banking)
│   ├── Current Ratio = Current Assets / Current Liabilities
│   ├── Quick Ratio = (Current Assets - Inventory) / Current Liabilities
│   ├── OCF = Net Income + Non-Cash + WC Changes
│   │   └── FCF = OCF - CapEx
│   │       └── Cash Runway = Cash / Monthly Burn
│   └── Debt-to-Equity = Liabilities / Equity
└── VAT Balance = Output VAT - Input VAT
```

---

## Document Navigation

- Previous: [System Overview](./02-system-overview.md)
- Next: [Sales KPIs](./04-sales-kpis.md)
- [Back to Index](./README.md)
