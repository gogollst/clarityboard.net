# Target Users & Roles

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## Role Definitions

| Role | Access Scope | Primary Use Cases |
|------|-------------|-------------------|
| **Admin** | Full system access, configuration, user management | System setup, entity configuration, integration management, audit review |
| **Finance** | All financial KPIs, accounting, cash flow, scenarios, budgets, documents | Financial reporting, cash flow planning, scenario analysis, DATEV export, receipt processing |
| **Sales** | Sales KPIs, pipeline, revenue metrics, customer data | Pipeline management, conversion tracking, revenue forecasting, target monitoring |
| **Marketing** | Marketing KPIs, campaign metrics, CAC, channel performance | Campaign ROI analysis, channel optimization, lead quality assessment |
| **HR** | HR KPIs, headcount, turnover, costs, satisfaction | Workforce planning, retention analysis, cost per hire, satisfaction tracking |
| **Executive** | Cross-domain read access, consolidated views, scenarios | Strategic overview, group reporting, scenario comparison, investment decisions |
| **Auditor** | Read-only access to all financial data and audit trails | Compliance verification, data integrity checks, export validation |

---

## Role-Based Dashboard Behavior

Each role sees **only their relevant KPIs** upon login. No navigation required to find key data - it is immediately visible on the landing dashboard. Users can drill down into details but the primary view is optimized for at-a-glance decision making.

### Cross-Role Visibility Rules

- **Finance** sees all monetary KPIs across domains
- **Executives** see aggregated KPIs from all domains
- **Sales/Marketing/HR** see only their domain-specific KPIs plus budget-related financial data
- **Auditors** see financial data with full audit trails but cannot modify anything
- **Admin** has unrestricted access to all data and configuration

### Entity-Level Access

Users can be restricted to specific entities within the organizational hierarchy. Consolidation views are only available to users with access to all entities included in the consolidation scope.

---

## User Personas

### Persona 1: CFO (Finance/Executive)

| Attribute | Detail |
|-----------|--------|
| **Role** | Finance + Executive |
| **Primary Needs** | Consolidated P&L across entities, cash flow forecast, liquidity position, DATEV export status |
| **Pain Points** | Manual consolidation in Excel, inconsistent data from subsidiaries, delayed reporting |
| **Success Criteria** | Morning dashboard shows real-time group financial position with no manual work |
| **Key Actions** | Review consolidated reports, approve scenarios, trigger DATEV export, review alerts |
| **Frequency** | Daily (dashboard), weekly (deep dives), monthly (DATEV + scenarios) |

### Persona 2: Controller (Finance)

| Attribute | Detail |
|-----------|--------|
| **Role** | Finance |
| **Primary Needs** | Plan vs. actual comparisons, variance analysis, budget monitoring, receipt processing |
| **Pain Points** | Chasing department heads for budget updates, manual receipt entry, reconciliation errors |
| **Success Criteria** | Automated variance alerts, AI-processed receipts with one-click booking |
| **Key Actions** | Process receipts, analyze variances, manage budgets, prepare DATEV exports |
| **Frequency** | Daily (receipts + monitoring), weekly (variance), monthly (closing + DATEV) |

### Persona 3: Sales Director (Sales)

| Attribute | Detail |
|-----------|--------|
| **Role** | Sales |
| **Primary Needs** | Pipeline health, conversion rates, revenue forecast, team performance |
| **Pain Points** | CRM data is unreliable, forecast accuracy is poor, no single view of sales performance |
| **Success Criteria** | Real-time pipeline with AI-enhanced win probability and revenue prediction |
| **Key Actions** | Monitor pipeline, track quota attainment, review conversion funnel, forecast revenue |
| **Frequency** | Daily (pipeline + activity), weekly (team review), monthly (forecast + targets) |

### Persona 4: Marketing Manager (Marketing)

| Attribute | Detail |
|-----------|--------|
| **Role** | Marketing |
| **Primary Needs** | Campaign ROI, channel performance, lead quality, marketing spend efficiency |
| **Pain Points** | Cannot attribute revenue to campaigns, data scattered across ad platforms |
| **Success Criteria** | Unified view of marketing performance with clear revenue attribution |
| **Key Actions** | Analyze campaign ROI, optimize channel spend, track MQL-to-SQL conversion |
| **Frequency** | Daily (campaign monitoring), weekly (channel analysis), monthly (budget review) |

### Persona 5: HR Manager (HR)

| Attribute | Detail |
|-----------|--------|
| **Role** | HR |
| **Primary Needs** | Headcount trends, turnover rates, cost per hire, engagement scores |
| **Pain Points** | Data scattered across HR tools, no connection between HR metrics and financial impact |
| **Success Criteria** | Integrated view showing HR KPIs with their financial impact |
| **Key Actions** | Monitor headcount, track hiring pipeline, analyze retention, review personnel costs |
| **Frequency** | Daily (hiring pipeline), weekly (headcount), monthly (full HR report) |

### Persona 6: Managing Director / CEO (Executive)

| Attribute | Detail |
|-----------|--------|
| **Role** | Executive |
| **Primary Needs** | High-level business health, growth trajectory, strategic KPIs, scenario comparison |
| **Pain Points** | Reports arrive late, numbers do not match across departments, no forward-looking view |
| **Success Criteria** | Single dashboard with business health score and actionable strategic insights |
| **Key Actions** | Review business health, compare scenarios, approve budgets, strategic planning |
| **Frequency** | Daily (health check), weekly (deep dive), quarterly (strategy + scenarios) |

---

## Document Navigation

- Previous: [Executive Summary](./00-executive-summary.md)
- Next: [System Overview](./02-system-overview.md)
- [Back to Index](./README.md)
