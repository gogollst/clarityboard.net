# HR KPIs

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Workforce Composition

| KPI | Formula | Unit |
|-----|---------|------|
| **Total Headcount** | Count of active employees (FTE and part-time) | Count |
| **Full-Time Equivalents (FTE)** | Sum of (individual work hours / full-time hours) | FTE |
| **Headcount by Department** | Breakdown per org unit | Count |
| **Headcount by Location** | Breakdown per office/remote | Count |
| **Contractor Ratio** | Contractors / (Employees + Contractors) * 100 | % |
| **Diversity Index** | Configurable demographic breakdowns (gender, age bands) | Index |
| **Average Tenure** | Sum of all employee tenures / Headcount | Years |
| **Span of Control** | Direct Reports / Managers | Ratio |
| **Management Ratio** | Managers / Total Employees * 100 | % |
| **New Hire Ratio** | Employees < 12 months / Total Employees * 100 | % |

### Headcount Trend Visualization

```
Headcount Development (12 months):

Month    Start    Hired    Left    End     Net     Growth%
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Jan      40       5        2       43      +3      +7.5%
Feb      43       3        1       45      +2      +4.7%
Mar      45       4        3       46      +1      +2.2%
...
Dec      58       2        1       59      +1      +1.7%
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
YTD      40       42       23      59      +19     +47.5%
```

---

## 2. Recruitment KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Open Positions** | Count of unfilled approved roles | Count |
| **Time to Hire** | Days from job posting to offer acceptance | Days |
| **Time to Fill** | Days from requisition approval to start date | Days |
| **Cost per Hire** | (Internal Recruiting Costs + External Costs) / Hires | EUR |
| **Offer Acceptance Rate** | Accepted Offers / Total Offers * 100 | % |
| **Quality of Hire** | Avg performance rating of new hires at 6/12 months | Score |
| **Source Effectiveness** | Hires per Source / Cost per Source | Ratio |
| **Application-to-Hire Ratio** | Hires / Applications * 100 | % |
| **Pipeline Diversity** | Demographic mix at each hiring stage | % |
| **Recruiter Efficiency** | Hires / Recruiter FTE | Hires/FTE |
| **Hiring Manager Satisfaction** | Survey score from hiring managers | Score (1-5) |

### Recruitment Funnel

```
Hiring Funnel (Average per Position):

Stage               Count    Conversion    Avg Days
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Applications        150      100%          -
  ↓ 20%
Screened            30       20%           3 days
  ↓ 40%
Phone Interview     12       8%            5 days
  ↓ 50%
On-site Interview   6        4%            10 days
  ↓ 50%
Offer Extended      3        2%            5 days
  ↓ 67%
Offer Accepted      2        1.3%          3 days
  ↓ 100%
Started             2        1.3%          14 days (notice period)

Total Time to Hire: ~40 days
Total Time to Fill: ~54 days (incl. notice period)
```

### Cost per Hire Breakdown

```
Cost per Hire Calculation:

Internal Costs:
  Recruiter salary (prorated)           2,500 EUR
  Hiring manager time (interviews)        800 EUR
  Team interview time                     600 EUR
  Administrative/onboarding               300 EUR

External Costs:
  Job board postings                      500 EUR
  Agency fee (if applicable)            3,000 EUR
  Background check                        150 EUR
  Relocation (if applicable)                0 EUR
  Employer branding (prorated)            200 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total Cost per Hire                     8,050 EUR

Benchmark: 3,000-8,000 EUR (industry-dependent)
```

---

## 3. Retention & Engagement KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Voluntary Turnover Rate** | Voluntary Departures / Avg Headcount * 100 | % (annualized) |
| **Involuntary Turnover Rate** | Involuntary Departures / Avg Headcount * 100 | % (annualized) |
| **Total Turnover Rate** | All Departures / Avg Headcount * 100 | % (annualized) |
| **Retention Rate** | (Headcount_start - Departures) / Headcount_start * 100 | % |
| **90-Day Retention Rate** | New hires still employed at 90 days / New hires * 100 | % |
| **First-Year Retention Rate** | New hires still employed at 12 months / New hires * 100 | % |
| **Employee Satisfaction Score (eNPS)** | % Promoters - % Detractors | Score (-100 to +100) |
| **Absenteeism Rate** | Absent Days / Available Work Days * 100 | % |
| **Regrettable Turnover Rate** | High-performer departures / High-performer headcount * 100 | % |
| **Internal Mobility Rate** | Internal Transfers / Total Headcount * 100 | % |

### Turnover Analysis

```
Turnover Analysis (Annual):

By Reason:
  Better opportunity          35%
  Compensation               20%
  Management issues           15%
  Work-life balance          12%
  Career development          10%
  Relocation                  5%
  Other                       3%

By Tenure:
  < 6 months                 25%  ← Onboarding issue signal
  6-12 months                20%
  1-2 years                  25%
  2-5 years                  20%
  > 5 years                  10%

By Department:
  Engineering                18%  (industry avg: 15%)  ⚠
  Sales                      22%  (industry avg: 25%)  ✓
  Marketing                  12%  (industry avg: 14%)  ✓
  HR                          8%  (industry avg: 10%)  ✓
  Operations                 10%  (industry avg: 12%)  ✓
```

### Employee Satisfaction Tracking

```
eNPS Calculation:
  Survey Question: "How likely are you to recommend this company
                    as a place to work?" (0-10 scale)

  Promoters (9-10):    45%
  Passives (7-8):      35%
  Detractors (0-6):    20%

  eNPS = 45% - 20% = +25

  Benchmark:
    < 0:    Needs immediate attention
    0-10:   Below average
    10-30:  Good
    30-50:  Excellent
    > 50:   World-class
```

---

## 4. Cost & Compensation KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Total Personnel Cost** | Salaries + Social Security + Benefits + Bonuses | EUR |
| **Personnel Cost Ratio** | Total Personnel Cost / Revenue * 100 | % |
| **Average Salary** | Total Salary Expense / FTE | EUR |
| **Revenue per Employee** | Total Revenue / FTE | EUR |
| **Profit per Employee** | Net Income / FTE | EUR |
| **Training Cost per Employee** | Total Training Budget / FTE | EUR |
| **Overtime Rate** | Overtime Hours / Regular Hours * 100 | % |
| **Benefits Utilization Rate** | Benefits Used / Benefits Available * 100 | % |
| **Compensation Competitiveness** | Company Avg Salary / Market Avg Salary * 100 | % |
| **Total Cost of Workforce (TCOW)** | All workforce-related costs (incl. contractors, tools) | EUR |
| **Cost per FTE** | TCOW / Total FTE | EUR |
| **Payroll Accuracy** | Correct Payroll Runs / Total Payroll Runs * 100 | % |

### Personnel Cost Structure

```
Personnel Cost Breakdown (Monthly, per FTE average):

Base Salary                              5,000 EUR
Employer Social Security (~20%)          1,000 EUR
  - Health Insurance                       370 EUR
  - Pension Insurance                      465 EUR
  - Unemployment Insurance                  60 EUR
  - Long-term Care Insurance                85 EUR
  - Accident Insurance                      20 EUR
Benefits                                   300 EUR
  - Company pension (bAV)                  150 EUR
  - Transit allowance                       50 EUR
  - Meal allowance                          50 EUR
  - Other benefits                          50 EUR
Bonus (prorated monthly)                   400 EUR
Training & Development                     100 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total Cost per FTE (monthly)             6,800 EUR
Total Cost per FTE (annual)             81,600 EUR
Multiplier over base salary:             1.36x
```

### Financial Impact Integration

HR KPIs are directly linked to financial reporting:

| HR KPI | Financial Impact | P&L Line |
|--------|-----------------|----------|
| Headcount change | Personnel cost change | Operating Expenses |
| Turnover | Replacement cost (1.5-2x annual salary) | Operating Expenses |
| Overtime rate | Additional labor cost (25-50% premium) | Operating Expenses |
| Hiring pipeline | Future personnel cost commitment | Cash Flow Forecast |
| Training spend | Development investment | Operating Expenses |
| Compensation increases | Budget impact | Operating Expenses |

---

## 5. Compliance & Legal KPIs

| KPI | Description | Legal Basis |
|-----|-------------|-------------|
| **Working Time Compliance** | % employees within legal working hours (max 48h/week avg) | ArbZG § 3 |
| **Vacation Utilization** | Used vacation days / Entitled vacation days | BUrlG § 7 |
| **Vacation Liability** | Financial value of unused vacation days | HGB accrual |
| **Sick Leave Rate** | Sick days / Available work days | EFZG |
| **Continued Pay Liability** | Cost of sick pay (6 weeks per illness) | EFZG § 3 |
| **Works Council Compliance** | Required consultations completed | BetrVG |
| **Training Compliance** | Mandatory training completion rate | Various regulations |

---

## Document Navigation

- Previous: [Marketing KPIs](./05-marketing-kpis.md)
- Next: [General Business KPIs](./07-general-business-kpis.md)
- [Back to Index](./README.md)
