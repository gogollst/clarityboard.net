# Budget Planning

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Budget Structure

### Hierarchy

```
Company Budget (Annual)
├── Department: Engineering
│   ├── Personnel (salaries, benefits, training)
│   ├── Infrastructure (servers, tools, licenses)
│   └── Other (travel, conferences)
├── Department: Sales
│   ├── Personnel
│   ├── Tools & Subscriptions
│   └── Travel & Events
├── Department: Marketing
│   ├── Personnel
│   ├── Advertising & Campaigns
│   ├── Content & Tools
│   └── Events
├── Department: HR
│   ├── Personnel
│   ├── Recruiting
│   └── Employee Benefits & Programs
├── Department: Finance
│   ├── Personnel
│   ├── External Services (audit, tax advisor)
│   └── Tools
└── Department: General & Admin
    ├── Office & Rent
    ├── Legal & Consulting
    ├── Insurance
    └── Other G&A
```

### Budget Line Item Structure

Each budget line has:

| Field | Description |
|-------|-------------|
| **Department** | Owning department |
| **Category** | Top-level category (Personnel, Infrastructure, etc.) |
| **Account** | HGB account number (maps to chart of accounts) |
| **Description** | Line item description |
| **Annual Amount** | Total budgeted for the year |
| **Monthly Breakdown** | 12 monthly values (equal or custom distribution) |
| **Type** | Fixed, Variable (% of revenue), or Step (threshold-based) |
| **Owner** | Person responsible for this budget line |
| **Approval Status** | Draft, Submitted, Approved, Locked |
| **Notes** | Justification or assumptions |

---

## 2. Budget Creation Workflow

### Process Flow

```
Budget Creation Process:

Phase 1: INITIALIZATION (CFO/Finance)
  ├── Set budget period (fiscal year)
  ├── Define total company budget envelope
  ├── Allocate department envelopes (top-down)
  └── Distribute budget templates to department heads

Phase 2: DEPARTMENT INPUT (Department Heads)
  ├── Fill in budget line items within envelope
  ├── Add justification for significant items
  ├── Flag items exceeding envelope (with reasoning)
  └── Submit for review

Phase 3: REVIEW & NEGOTIATION (Finance + Departments)
  ├── Finance reviews all submissions
  ├── Identify conflicts (total > envelope)
  ├── Negotiate adjustments with departments
  ├── Multiple rounds if needed (max 3 rounds)
  └── Finalize proposed budget

Phase 4: APPROVAL (CFO/CEO)
  ├── Review consolidated budget
  ├── Approve / Request changes
  ├── Final approval locks the budget
  └── Budget becomes active for monitoring

Phase 5: MONITORING (Ongoing)
  ├── Monthly plan vs. actual tracking
  ├── Variance alerts
  ├── Mid-year budget revision (if needed, requires approval)
  └── Year-end actual vs. budget report
```

### Budget Status Flow

```
Draft → Submitted → Under Review → Approved → Locked
                 ↗ Revision Requested ↙
```

### Approval Rules

| Budget Size | Approval Required |
|-------------|------------------|
| Department line item < 5,000 EUR | Department Head |
| Department line item 5,000-25,000 EUR | Department Head + Finance |
| Department line item > 25,000 EUR | CFO approval |
| Department total | CFO approval |
| Company total | CEO + CFO approval |
| Mid-year increase > 10% | Board/Executive approval |

---

## 3. Plan vs. Actual Analysis

### Monthly Report Structure

```
Plan vs. Actual Report
Department: Engineering
Period: January 2026

Category              Budget     Actual     Variance    Var %     Status
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Personnel
  Salaries            95,000     94,200     +800        +0.8%    ✓
  Social Security     19,000     18,840     +160        +0.8%    ✓
  Benefits            5,000      4,800      +200        +4.0%    ✓
  Training            3,000      0          +3,000      +100%    ⓘ Unused

Infrastructure
  Cloud (AWS)         8,000      8,500      -500        -6.3%    ✓
  Dev Tools           3,500      5,200      -1,700      -48.6%   ⚠ Over
  Licenses            2,000      1,800      +200        +10.0%   ✓

Other
  Travel              4,000      1,200      +2,800      +70.0%   ⓘ Under
  Conferences         2,000      0          +2,000      +100%    ⓘ Unused
  Equipment           3,000      2,800      +200        +6.7%    ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL                144,500    137,340    +7,160      +5.0%    ✓

YTD (1 month):
  Budget:    144,500 EUR
  Actual:    137,340 EUR
  Remaining: 1,589,660 EUR (of 1,734,000 annual)
```

### Variance Alert Rules

| Condition | Severity | Recipients | Channel |
|-----------|----------|-----------|---------|
| Category > 110% of monthly budget | Warning | Department Head | Dashboard |
| Category > 120% of monthly budget | Alert | Department Head + Finance | Dashboard + Email |
| Category > 150% of monthly budget | Critical | CFO | Dashboard + Email |
| Department total > 110% monthly | Warning | Department Head + Finance | Dashboard + Email |
| Department total > 120% monthly | Critical | CFO | Dashboard + Email |
| YTD department > proportional annual | Warning | Finance | Dashboard |
| Category unused for 3+ months | Info | Department Head | Dashboard |

### Cumulative Variance Tracking

```
YTD Budget Tracking (Engineering, 4 months):

Month     Budget      Actual      Monthly Var    Cumulative Var    Trend
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Jan       144,500     137,340     +7,160         +7,160           ▽
Feb       144,500     142,800     +1,700         +8,860           →
Mar       144,500     151,200     -6,700         +2,160           △
Apr       144,500     148,900     -4,400         -2,240           △
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
YTD       578,000     580,240     -              -2,240 (-0.4%)   →

Status: Within tolerance (< 5% deviation)
Projected Full Year: 1,740,720 EUR (+0.4% over budget)
```

---

## 4. Budget Forecasting

### Year-End Projection

Based on actual spending patterns, the system projects year-end actuals:

```
Year-End Budget Forecast (After 4 Months):

Department       Annual Budget   YTD Actual   Projected Year   Variance
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Engineering      1,734,000       580,240      1,740,720        +0.4%
Sales              960,000       345,000        990,000        +3.1%
Marketing          480,000       165,000        495,000        +3.1%  ⚠
HR                 360,000       108,000        324,000        -10.0%
Finance            240,000        82,000        246,000        +2.5%
General & Admin    720,000       238,000        714,000        -0.8%
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL            4,494,000     1,518,240      4,509,720        +0.4%

Confidence Interval (90%): 4,380,000 - 4,640,000 EUR

Key Findings:
  - Marketing trending 3.1% over → likely Q2 campaign overspend
  - HR trending 10% under → delayed hiring or underspent benefits
  - Overall tracking within 1% of budget
```

### Forecast Methods

| Method | Description | Used When |
|--------|-------------|-----------|
| **Linear Extrapolation** | YTD daily rate * remaining days | Default for stable categories |
| **Seasonal Adjustment** | Apply historical seasonal patterns | Categories with known seasonality |
| **Committed + Forecast** | Committed spend + trend for uncommitted | Categories with large commitments |
| **Bottom-Up** | Sum of individual planned items | Personnel (known salaries + planned hires) |

---

## 5. Person-Level Budgeting

### Personnel Budget Per Employee

```
Personnel Budget View:

Employee        Role              Monthly Cost    Annual Cost    Department
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Max M.          Sr. Engineer      7,200           86,400         Engineering
Anna K.         Engineer          6,100           73,200         Engineering
Tom S.          Jr. Engineer      4,800           57,600         Engineering
[OPEN]          Engineer          5,500           66,000         Engineering
Lisa W.         Sales Director    8,500          102,000         Sales
...

Notes:
  - [OPEN] positions included in budget but flagged as unfilled
  - Monthly cost includes salary + employer social security + benefits
  - Bonus allocations shown separately in quarterly budget
```

### Hiring Plan Integration

```
Hiring Plan (Budget Impact):

Position          Department    Start Date    Monthly Cost    FY Impact
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Engineer          Engineering   Apr 2026      5,500           44,000
Marketing Mgr    Marketing     Jun 2026      7,000           49,000
Sales Rep        Sales         Mar 2026      6,200           62,000
Finance Analyst  Finance       May 2026      5,800           46,400
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total incremental personnel cost for FY: 201,400 EUR

Including:
  - Recruiting costs: ~32,000 EUR (avg 8,000 per hire)
  - Onboarding costs: ~8,000 EUR (avg 2,000 per hire)
  - Equipment costs: ~12,000 EUR (avg 3,000 per hire)
```

---

## 6. Budget Revision Process

### Mid-Year Revision

When actual conditions deviate significantly from plan:

```
Budget Revision Request:

Requested By: Marketing Director
Date: 2026-06-15
Reason: New product launch requires additional campaign budget

Current Budget:
  Marketing Campaigns: 240,000 EUR (annual)
  YTD Spent: 130,000 EUR (54% of annual, 50% of year elapsed)

Requested Change:
  Additional: 60,000 EUR for H2 2026
  New Annual Budget: 300,000 EUR (+25%)

Justification:
  - New product launch scheduled for August 2026
  - Competitive landscape requires aggressive launch campaign
  - Expected ROI: 3:1 (180,000 EUR additional revenue)

Offset Proposal:
  - Reduce conference budget by 10,000 EUR (virtual events instead)
  - Reduce travel budget by 5,000 EUR
  - Net increase: 45,000 EUR

Approval Required: CFO (increase > 10%)
Status: Pending Approval
```

---

## Document Navigation

- Previous: [Document & Receipt Capture](./14-document-capture.md)
- Next: [Historical KPI Tracking](./16-historical-kpi-tracking.md)
- [Back to Index](./README.md)
