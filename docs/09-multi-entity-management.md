# Multi-Entity Management

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Entity Hierarchy

Clarity Board supports complex organizational structures:

```
Holding Company (Parent)
├── Subsidiary A (100% owned)
│   ├── Sub-Subsidiary A1 (100% owned by A)
│   └── Sub-Subsidiary A2 (51% owned by A)
├── Subsidiary B (100% owned)
│   └── [Profit Transfer Agreement with Parent]
├── Subsidiary C (75% owned)
│   └── [Minority Interest: 25%]
└── Tax Unit (Organschaft)
    ├── Parent (Organtrager)
    ├── Subsidiary A (Organgesellschaft)
    └── Subsidiary B (Organgesellschaft)
```

---

## 2. Entity Configuration

Each entity is configured with the following attributes:

### Legal & Tax Information

| Field | Description | Example |
|-------|-------------|---------|
| **Legal Name** | Official registered name | "Clarity GmbH" |
| **Legal Form** | GmbH, AG, GmbH & Co. KG, UG, SE, etc. | "GmbH" |
| **Registration Number** | Handelsregisternummer | "HRB 12345" |
| **Tax Number (Steuernummer)** | For corporate/trade tax | "27/123/45678" |
| **VAT ID (USt-IdNr)** | For intra-EU transactions | "DE123456789" |
| **Jurisdiction** | Country and state | "Germany, Berlin" |
| **Trade Tax Municipality** | For Gewerbesteuer rate | "Berlin (Hebesatz: 410%)" |
| **DATEV Client Number** | For export mapping | "10001" |
| **DATEV Consultant Number** | Tax advisor number | "12345" |

### Accounting Configuration

| Field | Description | Options |
|-------|-------------|---------|
| **Chart of Accounts** | Standard account framework | SKR03, SKR04, Custom |
| **Fiscal Year Start** | Beginning of fiscal year | January (default), any month |
| **Reporting Currency** | Primary currency | EUR (default) |
| **Additional Currencies** | Secondary currencies tracked | USD, GBP, CHF, etc. |
| **VAT Filing Frequency** | Monthly or quarterly | Monthly (default) |
| **Depreciation Method** | Default depreciation approach | Linear (default), declining balance |

### Consolidation Configuration

| Field | Description | Options |
|-------|-------------|---------|
| **Parent Entity** | Owning entity reference | Entity ID or null (top-level) |
| **Ownership Percentage** | Parent's stake | 0-100% |
| **Consolidation Method** | How to include in group reports | Full, Proportional, Equity, Excluded |
| **Intercompany Partners** | Known IC transaction counterparties | List of entity IDs |
| **Elimination Rules** | Custom IC elimination configuration | Standard or custom rules |
| **Minority Interest Calculation** | How to handle non-controlling interests | Proportional (default) |

---

## 3. Consolidation Logic

### Full Consolidation (> 50% ownership)

**Step-by-step process executed during consolidated reporting:**

1. **Aggregate**: Sum all balance sheet and P&L items of parent and all fully consolidated subsidiaries
2. **Eliminate Investment**: Remove parent's investment in subsidiary against subsidiary's equity
3. **Eliminate IC Receivables/Payables**: Match and remove intercompany balances
4. **Eliminate IC Revenue/Expenses**: Match and remove intercompany transactions
5. **Eliminate IC Profit**: Remove unrealized profit in intercompany inventory/assets
6. **Calculate Minority Interest**: For subsidiaries < 100% owned, separate minority share
7. **Handle Goodwill**: Book goodwill from acquisition (investment > net assets acquired)
8. **Currency Translation**: Convert non-EUR entities to EUR (closing rate for BS, average rate for P&L)

### Intercompany Elimination Rules

```
Intercompany Transaction Matching:

For each known IC relationship (Entity A <-> Entity B):

  Balance Sheet Elimination:
    1. Match: A.Receivable_from_B == B.Payable_to_A
    2. If match: Eliminate both (net zero in consolidation)
    3. If mismatch: Flag difference for review
       - Timing differences: Auto-resolve if < 3 business days
       - Amount differences: Require manual resolution
    4. FX differences: Book to consolidation FX reserve

  P&L Elimination:
    1. Match: A.Revenue_from_B == B.Expense_to_A
    2. Eliminate matching amounts
    3. Flag and review differences

  Unrealized IC Profit:
    1. Identify IC goods/services still in inventory/assets
    2. Calculate embedded IC margin
    3. Eliminate unrealized profit
    4. Reverse in subsequent period when goods are sold/consumed

Example:
  Entity A sells goods to Entity B for 100,000 EUR (cost: 70,000 EUR)
  Entity B still holds 40,000 EUR of these goods in inventory

  Elimination:
    Revenue (A): -100,000 EUR
    COGS (A): +70,000 EUR
    Gross Profit eliminated: -30,000 EUR

    Unrealized IC profit in inventory:
    (40,000 / 100,000) * 30,000 = 12,000 EUR
    Reduce B's inventory by 12,000 EUR
    Reduce consolidated profit by 12,000 EUR
```

### Consolidation Report Structure

```
Consolidated Balance Sheet:

                          Entity A    Entity B    Elim.       Consolidated
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Assets
  Fixed Assets            500,000     200,000     -           700,000
  IC Receivables          50,000      20,000      -70,000     0
  Other Current           300,000     150,000     -12,000     438,000
  Cash                    200,000     80,000      -           280,000
Total Assets             1,050,000    450,000     -82,000    1,418,000

Liabilities & Equity
  Equity (Parent)         400,000     -           -           618,000
  Equity (Subsidiary)     -           180,000     -180,000    0
  Minority Interest       -           -           +45,000     45,000
  IC Payables             20,000      50,000      -70,000     0
  Other Liabilities       230,000     120,000     -           350,000
  Investment in B        -400,000     -           +400,000    0
  Goodwill                -           -           +5,000      5,000
Total Liab. & Equity    1,050,000    450,000     -82,000    1,418,000
```

---

## 4. Profit Transfer Agreements (Gewinnabfuhrungsvertrag)

### Configuration

When a Profit Transfer Agreement (PTA) is active:

| Field | Description |
|-------|-------------|
| **Subsidiary** | Entity transferring profit/loss |
| **Parent** | Entity receiving profit/absorbing loss |
| **Agreement Date** | Date PTA was registered |
| **Effective From** | First fiscal year under PTA |
| **Agreement Type** | Full profit transfer, partial, or profit & loss pooling |
| **Status** | Active, Pending Registration, Terminated |

### Processing Logic

```
Monthly PTA Processing:

1. Calculate subsidiary's monthly net income/loss (after own taxes if applicable)
2. Book transfer:
   - Subsidiary profit: Debit P&L (subsidiary), Credit IC Payable (subsidiary)
   - Parent receipt: Debit IC Receivable (parent), Credit Other Income (parent)

3. If subsidiary has loss:
   - Parent absorbs loss (reverse of above)
   - Check parent's capacity to absorb (equity reserves)
   - Alert if absorption exceeds defined threshold

4. Tax implications:
   - Subsidiary included in parent's tax group (Organschaft)
   - No separate corporate tax filing for subsidiary
   - Trade tax calculated at parent level for combined income

5. DATEV export:
   - PTA entries exported with specific booking keys
   - Tax filing exports reflect consolidated tax position
```

---

## 5. Tax Unit (Organschaft)

### Requirements (Automatically Monitored)

| Requirement | Description | Status Tracking |
|-------------|-------------|-----------------|
| **Financial Integration** | Majority voting rights (> 50%) held by Organtrager | Checked against ownership % |
| **Organizational Integration** | Organtrager can enforce its will in management | Configured, documented |
| **Economic Integration** | Economic activities are interrelated | Configured, documented |
| **GAV (Profit Transfer Agreement)** | Active, registered PTA for minimum 5 fiscal years | Status tracked, expiry alerts |
| **Minimum Duration** | 5 fiscal years minimum commitment | Start date + 5 years monitored |

### Tax Calculation

```
Organschaft Tax Calculation:

Organtrager (Parent) taxable income:     300,000 EUR
+ Organgesellschaft A income:            150,000 EUR
+ Organgesellschaft B income:             80,000 EUR
- Organgesellschaft C loss:              -30,000 EUR
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Combined taxable income:                 500,000 EUR

Corporate Tax (KSt):
  500,000 * 15% = 75,000 EUR
  Solidaritatszuschlag: 75,000 * 5.5% = 4,125 EUR

Trade Tax (GewSt):
  Combined Gewerbeertrag: 500,000 EUR
  + Hinzurechnungen: 10,000 EUR
  - Kurzungen: -6,000 EUR
  Adjusted: 504,000 EUR
  Steuermessbetrag: 504,000 * 3.5% = 17,640 EUR
  Hebesatz (Parent municipality): 410%
  GewSt: 17,640 * 410% = 72,324 EUR

Total: 75,000 + 4,125 + 72,324 = 151,449 EUR
Effective Rate: 151,449 / 500,000 = 30.29%
```

### Alerts & Compliance

| Alert | Trigger | Action |
|-------|---------|--------|
| GAV expiry warning | 12 months before minimum term ends | Notify Finance + Legal |
| Ownership change | Ownership % drops near 50% | Critical alert |
| New entity added | Entity added that qualifies for Organschaft | Suggest inclusion |
| Entity removed | Entity leaves consolidation scope | Review tax implications |
| Inter-entity mismatch | IC balances do not reconcile | Flag for resolution |

---

## 6. Entity Switching in UI

### Entity Selector Behavior

- Top-level selector always visible in header
- Options: Individual entities + "Consolidated" views
- "Consolidated" shows group-level data with eliminations applied
- All KPIs, charts, and reports respect the selected entity scope
- Entity switch is instant (pre-calculated data)
- Users only see entities they have access to (RBAC)

### Consolidated vs. Individual Views

| View Mode | Shows | Eliminations | Minority Interest |
|-----------|-------|-------------|-------------------|
| Individual Entity | Single entity's data only | N/A | N/A |
| Consolidated (Full Group) | All entities combined | Applied | Separated |
| Sub-Group | Selected entities combined | Applied within group | Separated |
| Side-by-Side | Multiple entities compared | Not applied | N/A |

---

## Document Navigation

- Previous: [Data Ingestion](./08-data-ingestion.md)
- Next: [HGB Accounting & DATEV Export](./10-hgb-accounting-datev.md)
- [Back to Index](./README.md)
