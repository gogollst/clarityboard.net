# Glossary & Appendices

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## Glossary

| Term | Definition |
|------|-----------|
| **ARR** | Annual Recurring Revenue - MRR multiplied by 12 |
| **bAV** | Betriebliche Altersvorsorge - Company pension scheme (Germany) |
| **BU-Schlussel** | Buchungsschlussel - Tax code in DATEV posting format |
| **CAC** | Customer Acquisition Cost - Total cost to acquire one new customer |
| **CAGR** | Compound Annual Growth Rate - Smoothed annualized growth rate |
| **CCC** | Cash Conversion Cycle - Days between cash outflow and cash inflow |
| **CLV** | Customer Lifetime Value - Total revenue expected from one customer |
| **COGS** | Cost of Goods Sold - Direct costs of producing goods/services |
| **CSAT** | Customer Satisfaction Score - Survey-based satisfaction metric |
| **DATEV** | German accounting software standard for tax advisors |
| **DIO** | Days Inventory Outstanding - Average days inventory is held |
| **DPO** | Days Payable Outstanding - Average days to pay suppliers |
| **DSO** | Days Sales Outstanding - Average days to collect payment |
| **EBIT** | Earnings Before Interest and Taxes |
| **EBITDA** | Earnings Before Interest, Taxes, Depreciation, and Amortization |
| **EBT** | Earnings Before Taxes |
| **eNPS** | Employee Net Promoter Score - Employee satisfaction metric |
| **EXTF** | Extended Transfer Format - DATEV file format specification |
| **FCF** | Free Cash Flow - Operating cash flow minus capital expenditures |
| **FTE** | Full-Time Equivalent - Standardized measure of employee capacity |
| **GAV** | Gewinnabfuhrungsvertrag - Profit transfer agreement (Germany) |
| **GewSt** | Gewerbesteuer - German trade tax |
| **GoBD** | Grundsatze zur ordnungsmasigen Fuhrung und Aufbewahrung von Buchern - German digital bookkeeping regulations |
| **GRR** | Gross Revenue Retention - Revenue kept excluding expansion |
| **HGB** | Handelsgesetzbuch - German Commercial Code |
| **IC** | Intercompany - Transactions between related entities |
| **KPI** | Key Performance Indicator |
| **KSt** | Korperschaftsteuer - German corporate income tax (15%) |
| **MQL** | Marketing Qualified Lead |
| **MRR** | Monthly Recurring Revenue |
| **NPS** | Net Promoter Score - Customer loyalty metric |
| **NRR** | Net Revenue Retention - Revenue kept including expansion |
| **OCF** | Operating Cash Flow |
| **Organschaft** | German tax consolidation group (fiscal unity) |
| **Organtrager** | Controlling entity in an Organschaft |
| **Organgesellschaft** | Controlled entity in an Organschaft |
| **PTA** | Profit Transfer Agreement (see GAV) |
| **RBAC** | Role-Based Access Control |
| **ROA** | Return on Assets |
| **ROCE** | Return on Capital Employed |
| **ROE** | Return on Equity |
| **ROI** | Return on Investment |
| **RPO** | Recovery Point Objective - Max acceptable data loss |
| **RTO** | Recovery Time Objective - Max acceptable downtime |
| **SKR03/04** | Standardkontenrahmen - Standard chart of accounts (Germany) |
| **SLA** | Service Level Agreement |
| **Soli** | Solidaritatszuschlag - Solidarity surcharge (5.5% of KSt) |
| **SQL** | Sales Qualified Lead (context: sales) |
| **SSOT** | Single Source of Truth |
| **TOTP** | Time-Based One-Time Password (2FA standard) |
| **USt** | Umsatzsteuer - German Value Added Tax (VAT) |
| **USt-IdNr** | Umsatzsteuer-Identifikationsnummer - VAT identification number |
| **WACC** | Weighted Average Cost of Capital |
| **WC** | Working Capital - Current Assets minus Current Liabilities |

---

## Appendix A: KPI Dependency Map

```
Revenue (Source: Billing)
├── Gross Profit = Revenue - COGS
│   ├── Gross Margin = Gross Profit / Revenue
│   ├── EBITDA = Gross Profit - OpEx (excl D&A)
│   │   ├── EBITDA Margin = EBITDA / Revenue
│   │   ├── EBIT = EBITDA - D&A
│   │   │   ├── EBT = EBIT - Net Interest
│   │   │   │   ├── Net Income = EBT - Tax
│   │   │   │   │   ├── Net Margin = Net Income / Revenue
│   │   │   │   │   ├── ROE = Net Income / Equity
│   │   │   │   │   ├── ROA = Net Income / Assets
│   │   │   │   │   └── Profit per Employee = Net Income / FTE
│   │   │   │   └── Effective Tax Rate = Tax / EBT
│   │   │   ├── ROCE = EBIT / Capital Employed
│   │   │   └── Interest Coverage = EBIT / Interest
│   │   └── Rule of 40 = Revenue Growth + EBITDA Margin
│   └── Break-Even = Fixed Costs / Contribution Margin Ratio
├── MRR / ARR (Subscription-based)
│   ├── NRR = (Start + Expansion - Contraction - Churn) / Start
│   ├── GRR = (Start - Contraction - Churn) / Start
│   ├── Net New MRR = New + Expansion - Churn - Contraction
│   └── ARPA = MRR / Active Customers
├── CLV = ARPA * Gross Margin / Churn Rate
│   └── CLV:CAC Ratio = CLV / CAC
│       └── CAC Payback = CAC / (ARPA * Gross Margin)
├── Cash Position (Source: Banking)
│   ├── Current Ratio = Current Assets / Current Liabilities
│   ├── Quick Ratio = (Current Assets - Inventory) / Current Liabilities
│   ├── OCF = Net Income + Non-Cash + WC Changes
│   │   └── FCF = OCF - CapEx
│   │       └── Cash Runway = Cash / Monthly Burn
│   └── Debt-to-Equity = Liabilities / Equity
├── DSO = Receivables / Revenue * 365
│   └── CCC = DSO + DIO - DPO
│       └── Working Capital Requirement = CCC * (Revenue / 365)
├── Revenue per Employee = Revenue / FTE
├── Personnel Cost Ratio = Personnel Cost / Revenue
└── VAT Balance = Output VAT - Input VAT
```

---

## Appendix B: Alert Configuration Matrix

| Alert | Condition | Recipients | Severity | Channel |
|-------|-----------|-----------|----------|---------|
| Cash Below Minimum | Cash < threshold | Finance, Executive | Critical | Email + Dashboard + SMS |
| Current Ratio Low | Ratio < 1.0 | Finance, Executive | Critical | Email + Dashboard |
| Current Ratio Warning | Ratio < 1.2 | Finance | Warning | Dashboard + Email |
| Budget Category Overrun | Category > 120% | Dept Head, Finance | Warning | Dashboard + Email |
| Budget Department Overrun | Dept total > 110% | Finance, CFO | Warning | Dashboard + Email |
| Unusual Transaction | Amount > 3 sigma | Finance | Info | Dashboard |
| Churn Spike | Monthly churn > 2x avg | Sales, Executive | Warning | Dashboard + Email |
| DSO Deterioration | DSO > benchmark + 10d | Finance | Warning | Dashboard |
| DATEV Export Due | 3 days before deadline | Finance | Info | Dashboard + Email |
| Payment Overdue | Invoice > 30 days past | Finance | Warning | Dashboard |
| Scenario Divergence | Actual deviates >15% | Finance, Executive | Info | Dashboard |
| System Uptime | Downtime detected | Admin | Critical | SMS + Email |
| Webhook Failure | Source offline > 1 hour | Admin | Warning | Dashboard + Email |
| Cash Runway Low | < 6 months | Executive | Warning | Dashboard + Email |
| Cash Runway Critical | < 3 months | All stakeholders | Critical | All channels |
| FCF Negative | 3 consecutive months | Finance, Executive | Warning | Dashboard + Email |
| WC Spike | WC increases > 10% MoM | Finance | Warning | Dashboard |
| VAT Filing Reminder | 5 days before due | Finance | Info | Dashboard + Email |
| Tax Prepayment Due | 7 days before due | Finance | Info | Dashboard + Email |
| AI Processing Error | Error rate > 5% | Admin | Warning | Dashboard + Email |
| Data Quality Drop | Score < 95% | Admin | Warning | Dashboard |

---

## Appendix C: German Legal References

| Reference | Description | Relevance |
|-----------|-------------|-----------|
| **HGB** | Handelsgesetzbuch (Commercial Code) | Accounting standards, chart of accounts |
| **EStG** | Einkommensteuergesetz (Income Tax Act) | Tax deductibility of expenses |
| **KStG** | Korperschaftsteuergesetz (Corporate Tax Act) | Corporate tax calculation |
| **GewStG** | Gewerbesteuergesetz (Trade Tax Act) | Trade tax calculation |
| **UStG** | Umsatzsteuergesetz (VAT Act) | VAT rates, reverse charge, filing |
| **AO** | Abgabenordnung (Tax Code) | General tax procedures, compliance |
| **GoBD** | Digital bookkeeping principles | Data retention, immutability |
| **DSGVO/GDPR** | Data Protection Regulation | Personal data handling |
| **BGB** | Burgerliches Gesetzbuch (Civil Code) | Payment terms, contracts, default |
| **ArbZG** | Arbeitszeitgesetz (Working Time Act) | Working hours limits |
| **BUrlG** | Bundesurlaubsgesetz (Federal Leave Act) | Vacation entitlements |
| **EFZG** | Entgeltfortzahlungsgesetz (Cont. Pay Act) | Sick pay obligations |
| **BetrVG** | Betriebsverfassungsgesetz (Works Constitution) | Works council requirements |

---

## Appendix D: DATEV BU-Schlussel Reference

| Code | Description | VAT Rate |
|------|-------------|----------|
| 2 | Revenue, reduced rate | 7% |
| 3 | Revenue, standard rate | 19% |
| 8 | Input VAT, reduced rate | 7% |
| 9 | Input VAT, standard rate | 19% |
| 10 | Intra-EU acquisition (input) | 19% |
| 11 | Reverse charge §13b (input) | 19% |
| 12 | Tax-free intra-EU supply | 0% |
| 13 | Tax-free export | 0% |
| 40 | No VAT | - |

---

*End of Functional Concept Documentation*

---

## Document Navigation

- Previous: [Non-Functional Requirements](./20-non-functional-requirements.md)
- Next: [GetMOSS Integration](./22-getmoss-integration.md)
- [Back to Index](./README.md)
