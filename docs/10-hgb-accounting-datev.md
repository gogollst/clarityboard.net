# HGB Accounting & DATEV Export

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Chart of Accounts

Clarity Board uses standard German charts of accounts, configurable per entity:

### SKR03 Account Classes

| Class | Range | Description | Examples |
|-------|-------|-------------|----------|
| 0 | 0001-0999 | Fixed Assets, Financial Assets | 0200 Intangible Assets, 0400 Machinery, 0650 IT Equipment |
| 1 | 1000-1999 | Financial Accounts | 1200 Bank, 1400 Accounts Receivable, 1600 Accounts Payable |
| 2 | 2000-2999 | Liabilities, Provisions | 2000 Provisions, 2100 Bank Loans, 2900 Deferred Revenue |
| 3 | 3000-3999 | Material/Goods Procurement | 3300 Raw Materials, 3400 Purchased Goods |
| 4 | 4000-4999 | Personnel Costs, OpEx | 4100 Salaries, 4130 Social Security, 4600 Advertising |
| 7 | 7000-7999 | Other Operating Expenses | 7300 Insurance, 7600 Telephone, 7680 IT Costs |
| 8 | 8000-8999 | Revenue Accounts | 8400 Revenue 19% VAT, 8300 Revenue 7% VAT, 8120 Tax-Free Revenue |
| 9 | 9000-9999 | Statistical & Closing | 9000 Opening Balance, 9999 Closing Balance |

### SKR04 Account Classes (Alternative)

| Class | Range | Description |
|-------|-------|-------------|
| 0 | 0001-0999 | Fixed Assets |
| 1 | 1000-1999 | Current Assets |
| 2 | 2000-2999 | Equity |
| 3 | 3000-3999 | Liabilities |
| 4 | 4000-4999 | Revenue |
| 5 | 5000-5999 | Material Costs |
| 6 | 6000-6999 | Personnel Costs |
| 7 | 7000-7999 | Other Operating Expenses |
| 8 | 8000-8999 | Financial Results |
| 9 | 9000-9999 | Statistical & Closing |

### Custom Accounts

Custom accounts can be added within each class range for company-specific needs:
- Must be within the valid range for their class
- Require a name, description, and tax classification
- Mapped to standard DATEV accounts for export
- Version-controlled (changes create new version, old remains for historical data)

---

## 2. Double-Entry Booking Logic

Every financial event creates balanced journal entries. The system enforces the fundamental equation:

```
Assets = Liabilities + Equity
Debits = Credits (for every transaction)
```

### Common Booking Templates

#### Revenue Recognition (Invoice Created, 19% VAT)
```
Debit  1400 (Accounts Receivable)      1,190.00 EUR
Credit 8400 (Revenue, 19% VAT)         1,000.00 EUR
Credit 1776 (Output VAT 19%)             190.00 EUR
```

#### Payment Receipt (Bank)
```
Debit  1200 (Bank Account)             1,190.00 EUR
Credit 1400 (Accounts Receivable)      1,190.00 EUR
```

#### Vendor Invoice (Incoming, 19% VAT)
```
Debit  4964 (Cloud Infrastructure)     8,500.00 EUR
Debit  1576 (Input VAT 19%)            1,615.00 EUR
Credit 1600 (Accounts Payable)        10,115.00 EUR
```

#### Vendor Payment
```
Debit  1600 (Accounts Payable)        10,115.00 EUR
Credit 1200 (Bank Account)            10,115.00 EUR
```

#### Monthly Salary Booking
```
Debit  4100 (Gross Salaries)          50,000.00 EUR
Debit  4130 (Employer Social Security) 10,000.00 EUR
Credit 1740 (Wage Tax Payable)         8,500.00 EUR
Credit 1741 (Social Security Payable) 15,000.00 EUR
Credit 1742 (Net Salary Payable)      36,500.00 EUR
```

#### Monthly Depreciation
```
Debit  4830 (Depreciation Tangible)      500.00 EUR
Credit 0090 (Accumulated Depr. Equip.)   500.00 EUR
```

#### Prepaid Expense (Annual License, Monthly Allocation)
```
Initial booking (payment):
Debit  1500 (Prepaid Expenses)        12,000.00 EUR
Credit 1200 (Bank Account)            12,000.00 EUR

Monthly allocation:
Debit  7680 (IT Costs)                 1,000.00 EUR
Credit 1500 (Prepaid Expenses)         1,000.00 EUR
```

#### Revenue Correction (Credit Note)
```
Debit  8400 (Revenue, 19% VAT)           500.00 EUR
Debit  1776 (Output VAT 19%)              95.00 EUR
Credit 1400 (Accounts Receivable)         595.00 EUR
```

### Booking Validation Rules

| Rule | Description | Enforcement |
|------|-------------|-------------|
| **Balance Check** | Sum of debits must equal sum of credits | Hard block - cannot save |
| **Account Validity** | All accounts must exist in entity's chart | Hard block |
| **Period Check** | Booking date must be in open period | Hard block |
| **VAT Consistency** | VAT account must match revenue/expense VAT rate | Warning (overridable) |
| **Cost Center** | OpEx bookings should have cost center assignment | Warning |
| **Duplicate Check** | Same amount, date, counterparty within 24h | Warning |
| **Amount Threshold** | Bookings > 10,000 EUR require review | Approval workflow |

---

## 3. VAT Handling

### Supported VAT Rates (Germany)

| Rate | Code | Account (Output) | Account (Input) | Description |
|------|------|------------------|-----------------|-------------|
| 19% | UV19 | 1776 | 1576 | Standard rate |
| 7% | UV7 | 1771 | 1571 | Reduced rate (food, books, cultural events) |
| 0% | UV0 | - | - | Zero-rated (exports, intra-EU B2B with valid VAT ID) |
| Exempt | UVFR | - | - | VAT-exempt (insurance, financial services, medical) |
| Reverse Charge | UVRC | 1787 | 1577 | §13b UStG (construction, foreign B2B services) |
| Intra-EU Acq. | UVEU | 1774 | 1574 | Intra-community acquisition (§1a UStG) |

### Automatic VAT Determination

```
VAT Determination Logic:

1. Is transaction domestic (both parties in Germany)?
   YES → Apply standard rate (19%) or reduced rate (7%) based on goods/service type
   NO → Continue

2. Is transaction intra-EU B2B?
   YES + Valid VAT ID → Zero-rated (0%), Intra-Community Supply
   YES + No valid VAT ID → Apply domestic rate
   NO → Continue

3. Is transaction export (outside EU)?
   YES → Zero-rated (0%), Export
   NO → Continue

4. Is §13b reverse charge applicable?
   YES (construction, cleaning, foreign B2B services) → Reverse charge
   NO → Apply domestic rate

5. Is service/goods VAT-exempt?
   YES (per §4 UStG list) → Exempt
   NO → Apply standard rate
```

### Monthly VAT Reconciliation (Umsatzsteuervoranmeldung Preparation)

```
VAT Return Preparation Report:

Period: February 2026

Output VAT (Umsatzsteuer):
  Revenue 19%:  500,000 EUR → VAT: 95,000 EUR
  Revenue 7%:    50,000 EUR → VAT:  3,500 EUR
  Intra-EU supply: 80,000 EUR → VAT: 0 EUR (zero-rated)
  Exports:       120,000 EUR → VAT: 0 EUR (zero-rated)
  Total Output VAT: 98,500 EUR

Input VAT (Vorsteuer):
  Domestic purchases 19%: 300,000 EUR → VAT: 57,000 EUR
  Domestic purchases 7%:   10,000 EUR → VAT:    700 EUR
  Intra-EU acquisition:    40,000 EUR → VAT:  7,600 EUR
  Reverse charge §13b:     20,000 EUR → VAT:  3,800 EUR
  Total Input VAT: 69,100 EUR

VAT Balance:
  Output: 98,500 EUR
  Input: -69,100 EUR
  Intra-EU acquisition VAT: +7,600 EUR (output) -7,600 EUR (input) = 0
  Reverse charge: +3,800 EUR (output) -3,800 EUR (input) = 0
  ─────────────────────
  Net VAT Payable: 29,400 EUR
  Due Date: March 10, 2026
```

---

## 4. DATEV Export

### Export Package Contents

Each monthly export generates a DATEV-compatible package:

| File | Format | Description |
|------|--------|-------------|
| **EXTF_Buchungsstapel.csv** | DATEV ASCII (EXTF) | All journal entries for the period |
| **EXTF_Debitoren.csv** | DATEV ASCII (EXTF) | Customer master data (new/changed) |
| **EXTF_Kreditoren.csv** | DATEV ASCII (EXTF) | Vendor master data (new/changed) |
| **EXTF_Kontenbeschriftungen.csv** | DATEV ASCII (EXTF) | Custom account labels |
| **export_metadata.json** | JSON | Export summary, checksums, validation results |

### DATEV Buchungsstapel Format (EXTF)

**Header Record:**
```
"EXTF";700;21;"Buchungsstapel";12;20260301100000000;;"CB";"";
"10001";"12345";20260101;4;20260201;20260228;"Clarity Board Export";"";
1;0;;;"EUR";"";;""
```

**Data Records (one per journal entry line):**
```
Field Position | Field Name | Description | Example
1  | Umsatz | Amount (always positive) | 1190.00
2  | Soll/Haben | S=Debit, H=Credit | "S"
3  | WKZ | Currency code | "EUR"
4  | Kurs | Exchange rate | ""
5  | Basis-Umsatz | Base amount (foreign currency) | ""
6  | Konto | Account number | "1400"
7  | Gegenkonto | Counter account | "8400"
8  | BU-Schlussel | Tax code | "3" (=19% output VAT)
9  | Belegdatum | Document date (DDMM) | "2702"
10 | Belegfeld 1 | Document number | "INV-2026-001"
11 | Belegfeld 2 | Additional reference | ""
12 | Skonto | Discount amount | ""
13 | Buchungstext | Posting text | "Revenue Company XY"
```

### BU-Schlussel (Tax Codes)

| Code | VAT Treatment | Rate |
|------|--------------|------|
| 2 | Revenue, 7% VAT | 7% |
| 3 | Revenue, 19% VAT | 19% |
| 8 | Input VAT, 7% | 7% |
| 9 | Input VAT, 19% | 19% |
| 10 | Intra-EU acquisition, input VAT | 19% |
| 11 | Reverse charge §13b (input) | 19% |
| 12 | Tax-free intra-EU supply | 0% |
| 13 | Tax-free export | 0% |
| 40 | No VAT | - |

### Export Validation

Before export is finalized:

| Check | Description | Severity |
|-------|-------------|----------|
| **Trial Balance** | Sum of all debits = Sum of all credits | Hard block |
| **Account Existence** | All used accounts exist in DATEV account plan | Hard block |
| **Tax Code Consistency** | BU-Schlussel matches account's tax category | Hard block |
| **Date Range** | All entries within export period | Hard block |
| **Encoding** | Correct character encoding (Windows-1252 for DATEV) | Hard block |
| **Customer/Vendor Numbers** | Debtor/Creditor numbers in valid range (10000-69999 / 70000-99999) | Warning |
| **Amount Precision** | Max 2 decimal places | Auto-round with warning |
| **Missing Cost Centers** | OpEx without cost center assignment | Warning |

### Export Workflow

```
Export Process:

1. User initiates export (Finance role) for period YYYY-MM
2. System validates:
   - All webhook events for period are processed (no pending items)
   - Trial balance is balanced
   - No open validation warnings (or user acknowledges)
3. System generates DATEV files
4. System creates SHA-256 checksum per file
5. Export package is stored with audit trail:
   - Who initiated
   - Timestamp
   - Period
   - Record count
   - Checksums
6. User downloads package
7. User imports into DATEV (external step)
8. Status updated to "Exported" (prevents accidental re-export without explicit override)

Re-Export:
  - If corrections are needed after export:
  - Create correction journal entries (Stornobuchung + new entry)
  - Re-export with correction entries only (delta export)
  - Or full re-export with acknowledgment
```

---

## 5. Period Management

### Fiscal Period States

| State | Description | Allowed Actions |
|-------|-------------|-----------------|
| **Open** | Current active period | All bookings, modifications |
| **Soft Close** | Period under review | New bookings only with approval |
| **Exported** | DATEV export completed | Only correction entries with approval |
| **Closed** | Period finalized | No modifications (read-only) |

### Period Close Checklist (Automated)

```
Monthly Close Checklist:

[x] All webhook events processed (0 pending)
[x] All recurring entries booked
[x] Depreciation entries generated
[x] Prepaid expense allocations posted
[x] Accruals reviewed and posted
[x] Intercompany balances reconciled
[x] Trial balance balanced
[x] VAT reconciliation completed
[ ] Management review of P&L and Balance Sheet
[ ] DATEV export generated
[ ] Export validated and downloaded
[ ] Period status set to "Exported"
```

---

## Document Navigation

- Previous: [Multi-Entity Management](./09-multi-entity-management.md)
- Next: [Cash Flow Management](./11-cash-flow-management.md)
- [Back to Index](./README.md)
