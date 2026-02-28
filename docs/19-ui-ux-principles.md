# UI/UX Principles

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Design Philosophy

| Principle | Description |
|-----------|-------------|
| **Simplicity First** | Dashboard shows only role-relevant KPIs immediately on login |
| **Progressive Disclosure** | Details on demand via drill-down, not clutter |
| **Decision-Oriented** | Every visualization answers a specific business question |
| **Real-Time** | Data is live, pushed via WebSocket, not manually refreshed |
| **Mobile-Responsive** | Full functionality on desktop, core KPIs on mobile |
| **Accessible** | WCAG 2.1 Level AA compliance |
| **Consistent** | Shadcn component library ensures visual consistency |
| **Dark/Light Mode** | User preference with system detection |

---

## 2. Dashboard Layout

### Landing Dashboard (Per Role)

```
┌─────────────────────────────────────────────────────────────────┐
│ ┌───────┐  Entity: [Company A ▼]  │  Feb 2026 ▼  │  🔔 3  👤  │
│ │ Logo  │                                                       │
├─────────────────────────────────────────────────────────────────┤
│ ⚠ Alert: Cash position approaching minimum threshold (85%)      │
├──────────────────┬──────────────────┬──────────────────────────┤
│ Revenue          │ EBITDA Margin    │ Cash Position            │
│ € 2,100,000      │ 22.5%            │ € 785,000                │
│ ▲ +3.2% MoM      │ ▲ +1.5pp MoM     │ ▼ -2.1% MoM             │
│ ████████░░ 87%   │ ████████░░ 90%   │ ██████░░░░ 65%           │
├──────────────────┴──────────────────┴──────────────────────────┤
│                                                                 │
│  Revenue Trend (12 Months)                        [Line Chart]  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │         ╱─╲     ╱──╲                             ••••  │   │
│  │    ╱───╱   ╲───╱    ╲───╱──────╲           ••••••     │   │
│  │ ──╱                              ╲────••••••           │   │
│  │                                                         │   │
│  │  ── Actual   •• Forecast   -- Last Year                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
├───────────────────────────┬─────────────────────────────────────┤
│ Secondary KPIs            │ Recent Activity                     │
│                           │                                     │
│ DSO: 52 days    ▲ +3d    │ • Invoice INV-2026-1234 processed   │
│ MRR: € 175,000  ▲ +5%    │ • AWS receipt auto-booked           │
│ Churn: 4.2%     ▲ +0.3%  │ • Scenario "Q2 Hiring" created      │
│ Headcount: 45   → 0      │ • DATEV export Jan completed        │
│ NPS: +42        ▼ -3     │ • Budget alert: Marketing +15%      │
│                           │                                     │
├───────────────────────────┴─────────────────────────────────────┤
│ Quick Actions:                                                   │
│ [📄 Upload Document]  [📊 New Scenario]  [📥 Export DATEV]      │
│ [💬 Ask a Question]   [📋 Generate Report]                      │
└─────────────────────────────────────────────────────────────────┘
```

### KPI Card Design

```
┌────────────────────────────────┐
│ Net Revenue              [ⓘ]  │
│                                │
│    € 2,100,000                 │
│    ▲ +3.2% vs. last month     │
│    ▲ +25.3% vs. last year     │
│                                │
│ ████████░░ 87% of target      │
│                                │
│ [Sparkline: 30-day trend]     │
│                                │
│ [🔍 Drill Down]               │
└────────────────────────────────┘

States:
  Green:  On track (>90% of target, positive trend)
  Yellow: Attention (70-90% of target, flat/slight decline)
  Red:    Critical (<70% of target, negative trend)
```

---

## 3. Chart Types by Use Case

### Chart Selection Guide (Tremor Library)

| Use Case | Chart Type | Tremor Component | Configuration |
|----------|-----------|-----------------|---------------|
| KPI trend over time | Area / Line | `AreaChart` / `LineChart` | Daily/weekly/monthly, YoY overlay |
| Revenue breakdown | Stacked Bar | `BarChart` | By entity, product, channel |
| Budget vs. Actual | Grouped Bar | `BarChart` | Budget = outlined, Actual = solid |
| Cash flow projection | Area with bands | `AreaChart` | Confidence bands, scenario overlay |
| Department comparison | Horizontal Bar | `BarChart` | Sortable, benchmark line |
| KPI distribution | Donut | `DonutChart` | Category breakdown with legend |
| Correlation analysis | Scatter | `ScatterChart` | Two KPIs with regression line |
| Scenario comparison | Multi-line | `LineChart` | Up to 5 scenarios |
| Working Capital waterfall | Custom Bar | Custom component | DSO + DIO - DPO = CCC |
| Anomaly detection | Line with markers | `LineChart` | Highlighted anomaly points |
| Pipeline funnel | Custom | Custom component | Stage conversion rates |
| Progress/Target | Progress Bar | `ProgressBar` | Percentage of target |
| Single KPI | Number + Delta | `Metric` / `BadgeDelta` | Value + change indicator |
| Table data | Data Table | `Table` | Sortable, filterable, paginated |

### Interactive Features

| Feature | Behavior |
|---------|----------|
| **Hover** | Tooltip with exact value, date, and context |
| **Click** | Drill down to component level |
| **Drag** | Select date range for zoom |
| **Right-Click** | Context menu: Set alert, Add annotation, Export |
| **Pinch/Scroll** | Zoom in/out on time series |
| **Toggle** | Show/hide series in legend |

---

## 4. Navigation Structure

### Primary Navigation (Sidebar)

```
┌──────────────────────┐
│ 🏠 Dashboard         │  ← Role-based landing page
│                      │
│ 📊 KPIs             │
│   ├── Financial      │
│   ├── Sales          │
│   ├── Marketing      │
│   ├── HR             │
│   └── General        │
│                      │
│ 💰 Cash Flow        │
│   ├── Overview       │
│   ├── Forecast       │
│   └── Working Capital│
│                      │
│ 📐 Scenarios        │
│   ├── Active         │
│   ├── Create New     │
│   └── Compare        │
│                      │
│ 📋 Budget           │
│   ├── Plan vs Actual │
│   ├── Department     │
│   └── Forecast       │
│                      │
│ 📄 Documents        │
│   ├── Upload         │
│   ├── Processing     │
│   └── Archive        │
│                      │
│ 📦 DATEV Export     │
│                      │
│ ⚙️ Settings         │  ← Admin only
│   ├── Users & Roles  │
│   ├── Entities       │
│   ├── Webhooks       │
│   ├── Integrations   │
│   └── System         │
│                      │
│ 📝 Audit Log        │  ← Admin + Auditor
└──────────────────────┘
```

### Breadcrumb Navigation

```
Dashboard > Financial KPIs > EBITDA > Engineering Department > February 2026
```

---

## 5. Interaction Patterns

### Drill-Down Flow

```
Level 0: Dashboard KPI Card (EBITDA: € 480,000)
    │ Click "Drill Down"
    ▼
Level 1: EBITDA by Department
    │ Click "Engineering"
    ▼
Level 2: Engineering EBITDA Components
    │ Click "Personnel Costs"
    ▼
Level 3: Personnel Cost Detail (salary, social, benefits)
    │ Click specific line item
    ▼
Level 4: Source Events (individual payroll/expense events)
```

### Cross-Filter Behavior

When user selects a filter (e.g., date range, entity):
- All visible charts and KPI cards update simultaneously
- Filter state is preserved during navigation
- Filter is visible in header bar
- "Reset filters" always available

### Compare Mode

Toggle compare mode to overlay:
- **YoY**: Same metric, previous year
- **Scenario**: Same metric, different scenario
- **Entity**: Same metric, different entity
- **Budget**: Actual vs. planned

### Natural Language Search

```
┌─────────────────────────────────────────────────┐
│ 🔍 Ask anything...                              │
│                                                  │
│ Examples:                                        │
│   "What was our EBITDA margin trend last Q?"    │
│   "Compare revenue across all entities"         │
│   "Why did churn increase in February?"         │
│   "Show me cash flow forecast for next 3 months"│
│   "Which department is over budget?"            │
└─────────────────────────────────────────────────┘
```

---

## 6. Responsive Design

### Breakpoints

| Breakpoint | Width | Layout | Priority Content |
|-----------|-------|--------|-----------------|
| **Desktop XL** | > 1440px | Full layout, sidebar + 3-column grid | All features |
| **Desktop** | 1024-1440px | Sidebar + 2-column grid | All features |
| **Tablet** | 768-1024px | Collapsed sidebar + 2-column grid | Primary KPIs + charts |
| **Mobile** | < 768px | Bottom nav + single column | KPI cards + alerts only |

### Mobile-Specific Behavior

- KPI cards are swipeable (horizontal scroll)
- Charts auto-resize and simplify for mobile
- Document upload supports camera capture
- Push notifications for critical alerts
- Simplified navigation (bottom tab bar)

---

## 7. Accessibility

### WCAG 2.1 Level AA Requirements

| Requirement | Implementation |
|-------------|---------------|
| **Color Contrast** | Minimum 4.5:1 for text, 3:1 for large text |
| **Keyboard Navigation** | All interactive elements reachable via Tab/Enter/Space |
| **Screen Reader** | ARIA labels on all charts, tables, and interactive elements |
| **Focus Indicators** | Visible focus ring on all interactive elements |
| **Alt Text** | Chart descriptions available as text alternatives |
| **Motion** | Animations respect prefers-reduced-motion |
| **Zoom** | Functional up to 200% zoom without horizontal scroll |
| **Error Messages** | Associated with form fields, descriptive text |

---

## 8. Theming

### Color System

```
Primary Colors:
  Brand Primary:    #2563EB (Blue 600)
  Brand Secondary:  #7C3AED (Violet 600)

Semantic Colors:
  Success:          #16A34A (Green 600)
  Warning:          #D97706 (Amber 600)
  Error:            #DC2626 (Red 600)
  Info:             #2563EB (Blue 600)

KPI Status Colors:
  On Track:         #16A34A (Green)
  Attention:        #D97706 (Amber)
  Critical:         #DC2626 (Red)
  Neutral:          #6B7280 (Gray 500)

Chart Palette (Tremor compatible):
  Series 1:         #2563EB
  Series 2:         #7C3AED
  Series 3:         #0891B2
  Series 4:         #D97706
  Series 5:         #DC2626
  Series 6:         #16A34A
```

### Dark Mode

- Full dark mode support with Tailwind `dark:` classes
- Automatic detection via `prefers-color-scheme`
- Manual toggle in user settings
- All charts adapt to dark background

---

## Document Navigation

- Previous: [Security & Compliance](./18-security-compliance.md)
- Next: [Non-Functional Requirements](./20-non-functional-requirements.md)
- [Back to Index](./README.md)
