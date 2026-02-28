# Clarity Board - Documentation Index

**Project:** Clarity Board - Enterprise KPI Management Dashboard
**Language:** English
**Status:** Draft

---

## Functional Concept Documents

### Overview
| # | Document | Description |
|---|----------|-------------|
| 00 | [Executive Summary](./00-executive-summary.md) | Product overview, vision, and goals |
| 01 | [Target Users & Roles](./01-target-users-roles.md) | Role definitions, personas, access patterns |
| 02 | [System Overview](./02-system-overview.md) | High-level data flow, processing pipeline, integration pattern |

### KPI Domains
| # | Document | Description |
|---|----------|-------------|
| 03 | [Financial KPIs](./03-financial-kpis.md) | Profitability, liquidity, returns, tax-relevant KPIs |
| 04 | [Sales KPIs](./04-sales-kpis.md) | Revenue, pipeline, conversion, customer metrics |
| 05 | [Marketing KPIs](./05-marketing-kpis.md) | Acquisition, engagement, attribution models |
| 06 | [HR KPIs](./06-hr-kpis.md) | Workforce, recruitment, retention, compensation |
| 07 | [General Business KPIs](./07-general-business-kpis.md) | Operational efficiency, growth, quality & risk |

### Core Modules
| # | Document | Description |
|---|----------|-------------|
| 08 | [Data Ingestion & Single Source of Truth](./08-data-ingestion.md) | Webhooks, mapping, transformation, SSOT principles |
| 09 | [Multi-Entity Management](./09-multi-entity-management.md) | Holdings, consolidation, Organschaft, profit transfer |
| 10 | [HGB Accounting & DATEV Export](./10-hgb-accounting-datev.md) | Chart of accounts, double-entry, VAT, DATEV format |
| 11 | [Cash Flow Management](./11-cash-flow-management.md) | Cash flow statement, invoice splitting, liquidity planning, multi-currency |
| 12 | [Working Capital Optimization](./12-working-capital-optimization.md) | DSO, DIO, DPO, CCC, optimization strategies |
| 13 | [Scenario Engine](./13-scenario-engine.md) | Scenarios, Monte Carlo, sensitivity analysis, comparison |
| 14 | [Document & Receipt Capture](./14-document-capture.md) | Upload, AI processing, booking suggestions, GoBD |
| 15 | [Budget Planning](./15-budget-planning.md) | Budget structure, workflow, plan vs. actual |
| 16 | [Historical KPI Tracking](./16-historical-kpi-tracking.md) | Daily snapshots, visualizations, retention |

### Technical & Cross-Cutting
| # | Document | Description |
|---|----------|-------------|
| 17 | [AI Integrations](./17-ai-integrations.md) | Middleware architecture, features, guardrails |
| 18 | [Security & Compliance](./18-security-compliance.md) | Authentication, RBAC, GDPR, OWASP, audit logging |
| 19 | [UI/UX Principles](./19-ui-ux-principles.md) | Design philosophy, layouts, chart types, interactions |
| 20 | [Non-Functional Requirements](./20-non-functional-requirements.md) | Performance, scalability, availability, compatibility |
| 21 | [Glossary & Appendices](./21-glossary-appendices.md) | Terms, KPI dependency map, alert matrix |

### Erweiterte Module (v1.1)
| # | Document | Description |
|---|----------|-------------|
| 22 | [GetMOSS Integration](./22-getmoss-integration.md) | Virtual credit card import, receipt matching, auto-booking |
| 23 | [Fixed Asset & Depreciation Management](./23-fixed-asset-management.md) | Asset lifecycle, AfA, asset register, DATEV Anlagenspiegel |
| 24 | [Regulatory Reporting](./24-regulatory-reporting.md) | E-Bilanz, Jahresabschluss, steuerliche Meldungen, Offenlegung |
| 25 | [Onboarding & Initial Setup](./25-onboarding-setup.md) | Ersteinrichtung, Eroeffnungsbilanz-Import, Go-Live-Checkliste |

---

## Architecture & Technical Documents
*(To be created after functional concept approval)*

| Document | Description |
|----------|-------------|
| Architecture Overview | System architecture, tech stack decisions, deployment |
| Database Schema | Complete Postgres schema design |
| API Specification | REST API endpoints, contracts, versioning |
| Integration Guide | Webhook setup, source configuration |

---

*Last updated: 2026-02-27 (v1.1 - Review & Erweiterung)*
