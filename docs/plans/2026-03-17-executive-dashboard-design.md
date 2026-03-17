# Executive Dashboard & C-Level Experience — Design Spec

**Date:** 2026-03-17
**Status:** Draft
**Scope:** Frontend-only (one minor backend addition: executive role seeding; missing KPIs shown as placeholders)

---

## 1. Goal

Transform ClarityBoard's landing experience for C-level executives (CEO, CFO, CSO, CHRO, COO) so they can assess business health within 5 seconds of opening the app. Operational users retain their current detailed interface unchanged.

Apply taste-skill design principles tuned for a financial dashboard:
- Design Variance: **5** (structured grids with offset accents)
- Motion Intensity: **3** (subtle transitions on hover/click only, no perpetual animations)
- Visual Density: **7** (information-dense, well-grouped, monospace numbers)

---

## 2. Requirements

### Functional
- Executive users see simplified sidebar and land on executive dashboard at `/`
- Operational users land on operational dashboard at `/dashboard/ops`
- Period selector (MTD/QTD/YTD) with URL persistence
- "vs Prior" comparison (same period prior year)
- 5 headline KPIs + domain scorecards + alerts feed + quick insights
- Coming soon placeholders for Operations domain and missing KPIs

### Non-Functional
- **Performance:** Page interactive within 5 seconds; consider batch API if >30 parallel KPI history requests cause issues
- **i18n:** Full support for de, en, ru (executive.json per locale)
- **Accessibility:** EN 301 549 / WCAG 2.1 AA (see Section 11)
- **Responsive:** Usable on desktop, tablet, mobile (breakpoints in Section 12)

---

## 3. User Roles

**Executive users:** CEO, CFO, CSO, CHRO, COO
- Land on new executive dashboard at `/`
- See simplified sidebar focused on high-level navigation
- Can drill down into operational views via clickable KPIs or "Show full navigation" toggle

**Operational users:** Accounting/Finance, HR, Sales staff
- Land on current operational dashboard (moved to `/dashboard/ops`)
- See current sidebar unchanged
- Can access executive dashboard at `/` if they navigate there manually

---

## 4. Route Changes

| Route | Before | After |
|-------|--------|-------|
| `/` | `DashboardPage.tsx` (operational) | `ExecutiveDashboard.tsx` (new) |
| `/dashboard/ops` | *(does not exist)* | `DashboardPage.tsx` (current operational dashboard, relocated) |

All other routes remain unchanged. The executive dashboard is a new page; the operational dashboard is preserved at a new path.

**Important:** The operational sidebar's "Dashboard" link (currently pointing to `/`) must be updated to `/dashboard/ops` so operational users land on their familiar operational view, not the executive dashboard.

---

## 5. Sidebar — Role-Aware Navigation

### Executive Sidebar (shown when `isExecutive === true`)

```
Executive Summary        →  /
─────────────────────────
Domains
  Financial              →  /kpis/financial
  Sales                  →  /kpis/sales
  Marketing              →  /kpis/marketing
  HR                     →  /kpis/hr
  Operations             →  (coming soon, disabled link)
─────────────────────────
Tools
  Cashflow Forecast      →  /cashflow/forecast
  Scenarios              →  /scenarios
  Budget                 →  /budget
─────────────────────────
Full Dashboard           →  /dashboard/ops
Admin                    →  /admin/users (only if admin permissions)
AI Management            →  /admin/ai/providers (only if admin permissions)
```

### Role Detection

The user model already has `roles: string[]` and `permissions: string[]` arrays. The `useAuth()` hook exposes `role: user?.roles?.[0] ?? null`.

**Required backend addition:** Seed executive role values (`ceo`, `cfo`, `cso`, `chro`, `coo`) into the role system and add an `executive.view` permission. This is a minor seed data change — no schema migration needed. Admins assign executive roles to users via the existing admin user management UI.

**Frontend detection logic (in priority order):**
1. User's `roles` array contains one of `ceo|cfo|cso|chro|coo`
2. User's `permissions` array contains `executive.view`
3. Fallback: default to operational sidebar if neither condition is met

Implementation: Add `isExecutive` computed getter to `useAuth()` hook.

**Note:** Until executive roles are assigned to users in the backend, all users will see the operational sidebar (safe fallback). The executive dashboard page at `/` is accessible to all authenticated users regardless of role — only the sidebar simplification is role-gated.

### Sidebar Toggle

Bottom of executive sidebar: "Show full navigation" link (text-only, muted). Clicking it expands to the full operational sidebar for the current session. State stored as a non-persisted boolean `showFullNav: boolean` in `uiStore` — must NOT use `zustand/middleware/persist`. Resets to `false` on page refresh.

---

## 6. Executive Dashboard — Page Layout

### 6.1 Header Bar (inline, not a separate component)

```
┌──────────────────────────────────────────────────────────────┐
│  {Entity Name} — Executive Summary       [MTD|QTD|YTD] [⇄ vs Prior]  │
│                                          Last updated: 14:32  ●       │
└──────────────────────────────────────────────────────────────┘
```

- Left: Entity name (from `useEntity()`) + "Executive Summary" in Fraunces
- Right: Period selector (MTD/QTD/YTD toggle buttons) + comparison switch
- Below right: "Last updated" timestamp with subtle green pulse dot (connected via SignalR) or amber (stale). **Stale:** >5 minutes since last KPI update, or SignalR disconnected. When SignalR is unavailable, use polling/refetch as fallback.
- Period + comparison state stored in URL search params: `?period=mtd&compare=true`
- **URL param validation:** Invalid values (e.g. `?period=invalid`) fall back to defaults: `period=mtd`, `compare=false`

### 6.2 Headline Metrics Strip — 5 KPIs in a single row

```
┌──────────┬──────────┬──────────┬──────────┬──────────┐
│ Revenue  │  EBITDA  │Free Cash │Headcount │  Alerts  │
│          │         │  Flow    │          │          │
│ €2.4M    │  €380K   │  €1.1M   │   147    │    3     │
│ ▲ +8.2%  │ ▲ +3.1%  │ ▼ -2.4%  │  — 0%    │ 2⚠ 1🔴  │
│ ~~~~~~~~ │ ~~~~~~~~ │ ~~~~~~~~ │ ~~~~~~~~ │          │
└──────────┴──────────┴──────────┴──────────┴──────────┘
```

- Large monospace value (`.kpi-value` / `font-mono tabular-nums`)
- Change % badge: **higher_better** KPIs (Revenue, EBITDA, etc.): emerald = positive, red = negative. **lower_better** KPIs (Churn, CPL, etc.): emerald = negative change (improvement), red = positive change (worsening). Slate for neutral (0%).
- Mini sparkline below value (90 days, reusing existing SparkLine component — uses Recharts LineChart)
- Alerts count shows severity breakdown (warning count + critical count)
- Each metric clickable → navigates to relevant domain page
- When "vs Prior" enabled: smaller muted comparison value shown below main value

### 6.3 Domain Scorecard Grid — main content

**Layout:** 2-column grid on desktop, single column on mobile. NOT cards — horizontal scorecard rows separated by `border-b` and whitespace (taste-skill anti-card-overuse for density > 5).

Each domain row structure:

```
┌─────────────────────────────────────────────────────────────┐
│  Financial                                                   │
│                                                              │
│  Revenue    EBITDA Margin   Net Margin    Op. Cash Flow     │
│  €2.4M     15.8%           9.2%          €420K              │
│  ▲ +8.2%   ▲ +1.2pp        ▼ -0.3pp      ▲ +15%           │  [~~~~~~~~]
│                                                              │
│  ⚠ EBITDA margin below target (threshold: 18%)              │
└─────────────────────────────────────────────────────────────┘
```

- Left: Domain label (DM Sans semibold, uppercase tracking) + 3-4 primary KPI chips inline
- Right: Mini area chart (sparkline, 90 days of the domain's primary metric)
- Bottom: Inline alert indicators if any domain KPIs are warning/critical (muted text, not a banner)
- **Interactive rows (Financial, Sales, Marketing, HR):** Entire row is hoverable (`bg-secondary/50` transition) and clickable → domain dashboard
- **Operations row (coming soon):** Not hoverable, not clickable — no drill-down target
- Rows separated by `divide-y divide-border` + generous vertical padding

**Domain rows and their KPIs:**

| Domain | KPI 1 | KPI 2 | KPI 3 | KPI 4 | Chart Metric |
|--------|-------|-------|-------|-------|-------------|
| Financial | Revenue | EBITDA Margin | Net Margin | Operating Cash Flow | Revenue trend |
| Sales | MRR | Pipeline Value | Win Rate | Churn Rate | MRR trend |
| Marketing | CPL | Marketing ROI | Lead Conversion | Website Conversion | Lead conversion trend |
| HR | Headcount | Turnover Rate | Time-to-Hire | Absence Rate | Headcount trend |
| Operations | Doc Processing Rate* | SLA Compliance* | Process Throughput* | — | *(no chart)* |

*Asterisked items = coming soon placeholders (see Section 10)*

### 6.4 Alerts & Exceptions Panel

Below the scorecard grid. Not a separate alert-only view — a condensed feed.

- Shows only `critical` and `warning` severity alert events, sorted by severity (critical first) then recency (newest first)
- Each row: severity icon (red circle / amber triangle) + KPI name + condition text + "View" link
- **"View" link target:** Navigates to the KPI's domain page (e.g. `/kpis/financial`) with optional `?highlight={kpiId}` for scroll/focus. If KPI has no domain mapping, link to `/kpis/financial` as fallback.
- Max 5 shown, with "View all alerts" link if more exist
- If no alerts: clean empty state with checkmark icon: "All metrics within target ranges"
- **Data source:** `useEntityAlertEvents(entityId, 'active')` — calls `GET /api/kpi/alert-events?status=active`. The dashboard endpoint returns only `ActiveAlerts` count, not the full alert list. Map `AlertEventDto` to display format (title, message, severity, kpiId).

### 6.5 Quick Insights Row

Bottom of page. Auto-generated comparison sentences from `changePct` data:

```
"Revenue up 12.3% vs last month. Headcount unchanged. Churn rate improved by 0.8pp."
```

- Template-driven, not AI — string interpolation from KPI data
- **"Top 3 most notable":** Sort all KPIs by `Math.abs(changePct)` descending, take top 3. Skip KPIs with `changePct === null` or `0`.
- Muted text, small font — supplementary context, not a headline

---

## 7. Period Comparison

### Period Selector
Three toggle buttons (shadcn ToggleGroup): **MTD** | **QTD** | **YTD**

- MTD = 1st of current month → today
- QTD = 1st of current quarter → today
- YTD = Jan 1 → today
- Default: MTD

### Comparison Toggle
Switch component next to period selector: "vs Prior"

**"vs Prior" = same period prior year.** Example: MTD March 2025 vs MTD March 2024; QTD Q1 2025 vs QTD Q1 2024.

When enabled:
- Headline metrics show comparison value in smaller muted text below current value
- `changePct` formula: `(current - prior) / prior * 100` where prior = same period prior year
- Sparklines: two lines in same chart — solid line = current period, dashed line = prior year
- Domain scorecard KPI chips show delta vs prior period

### Data Fetching
New hook: `useExecutiveDashboard(entityId, period, compareEnabled)`

- Wraps `useKpiDashboard(entityId)` for current KPI values (note: `entityId` is used only for TanStack Query cache keying — the backend reads entity from the JWT `entity_id` claim, not from a query param)
- Alerts: separate `useEntityAlertEvents(entityId, 'active')` for `GET /api/kpi/alert-events?status=active`
- Uses TanStack Query's `useQueries()` API (NOT individual `useKpiHistory` hooks in a loop — that would violate Rules of Hooks) to batch-fetch sparkline histories for all ~25 KPIs
- When compare enabled: additional `useQueries` batch with offset dates (lazy, only fetched on toggle)
- Period + compare state in URL search params for bookmarkability
- **Date format contract:** All date params sent to `/api/kpi/{kpiId}/history` must be `YYYY-MM-DD` strings (not ISO datetime) to match the backend's `DateOnly` binding

### Performance
- KPI history calls parallelized via `useQueries()` (TanStack Query batches them automatically)
- Comparison data fetched lazily (not on page load)
- Sparkline charts use the existing SparkLine component (Recharts LineChart). If >30 parallel requests cause performance issues, consider a backend batch endpoint `GET /api/kpi/history/batch?kpiIds=...&from=&to=`.
- **Target:** Page interactive within 5 seconds; <30 parallel requests preferred

---

## 8. Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| `useQueries()` for sparkline batch | Avoids Rules of Hooks violation; single batch instead of N hooks in loop |
| Period + compare in URL params | Bookmarkability, shareable links, back/forward navigation |
| `showFullNav` non-persisted | Executive sidebar toggle resets on refresh — avoids stale UX |
| Alerts from `alert-events` endpoint | Dashboard returns only `ActiveAlerts` count; full list from `/api/kpi/alert-events` |
| Operations as placeholder row | `KpiDefinition.domain` has no `operations` yet; use `domain: 'general'` or hardcoded row until backend adds Operations KPIs |

---

## 9. Data Flow

```
useExecutiveDashboard(entityId, period, compareEnabled)
├── useKpiDashboard(entityId)           → GET /api/kpi/dashboard (KPI values, entity from JWT)
├── useEntityAlertEvents(entityId, 'active') → GET /api/kpi/alert-events?status=active
├── useQueries([...])                   → GET /api/kpi/{kpiId}/history?from=&to= (× ~25 KPIs)
└── useQueries([...]) [if compareEnabled] → same, with prior-year date range

ExecutiveDashboard.tsx
├── PeriodSelector (URL params)
├── HeadlineMetrics (5 KPIs + sparklines)
├── DomainScorecardGrid (Financial, Sales, Marketing, HR, Operations)
├── AlertsFeed (from useEntityAlertEvents)
└── QuickInsights (template from changePct)
```

---

## 10. Coming Soon Placeholder Pattern

For KPIs that don't have backend data yet:

**Individual KPI chip:**
- Value shows `--` in `text-muted-foreground`
- Label is visible (shows what's planned)
- No change badge, no sparkline
- Not clickable (no drill-down target)

**Full domain row (Operations):**
- All KPI chips show `--` placeholder
- Chart area shows "No data yet" in muted text
- Bottom of row: "Data collection in progress" label
- Row is not clickable/hoverable (no drill-down target)
- Visually muted but same layout structure — feels like a normal row awaiting data, not a marketing teaser

**Operations domain:** `KpiDefinition.domain` currently has no `operations` value. The Operations row is rendered as a hardcoded placeholder. When backend adds Operations KPIs, extend `KpiDefinition.domain` to include `'operations'` or use a domain mapping in the frontend.

**KPIs marked as coming soon:**
- Operations: Document Processing Rate, SLA Compliance, Process Throughput (entire domain)
- HR: Open Positions (single chip within an otherwise populated row)
- Sales: Top Deals ranking (not shown as chip — no space in the 4-chip layout)
- HR: Review Completion Rate, Onboarding Completion Rate (not shown — 4-chip limit per domain)

---

## 11. Design Treatment (Taste-Skill Applied)

### Typography
- **Page title:** Fraunces (existing display font), `text-2xl tracking-tight`
- **Domain labels:** DM Sans semibold, `text-xs uppercase tracking-wider text-muted-foreground`
- **KPI values:** `font-mono tabular-nums text-2xl` (headline) / `text-lg` (domain chips)
- **KPI labels:** DM Sans, `text-sm text-muted-foreground`
- **No Inter.** Existing Fraunces + DM Sans + JetBrains Mono pairing is already high-quality.

### Color
- Existing coral accent (`#d97757`) for primary actions and active states
- Zinc/stone neutrals for backgrounds and borders
- No purple. No gradients on text. No neon glows.
- Semantic colors for KPI trends: emerald (positive), red (negative), slate (neutral)
- Alert severity: red for critical, amber for warning
- Shadows tinted to background hue (existing pattern)

### Layout
- Max width container: `max-w-7xl mx-auto`
- Headline strip: CSS Grid `grid-cols-5` (desktop) → horizontal scroll on mobile
- Domain scorecard: 2-column grid (desktop) → single column (tablet/mobile)
- Anti-card: scorecard rows use `divide-y` borders, not card containers
- Generous padding: `py-6` between sections, `py-4` within domain rows

### Interactive States
- **Loading:** Skeleton loaders matching exact layout shape (headline strip skeleton, scorecard row skeleton)
- **Empty:** Per-domain empty handling — if an entire domain has no data, show coming soon pattern. No entity selected: same EmptyState as dashboard ("Select an entity").
- **Error:** Per-domain error boundary — one domain failing doesn't break others. Inline error: "Unable to load financial data. Retry." with retry button. Global fetch failure: toast + optional full-page error with retry.
- **Hover:** Scorecard rows (except Operations) get `bg-secondary/50` with `transition-colors duration-200`
- **Active/Press:** KPI chips get `scale-[0.98]` on `:active` (tactile feedback)
- **Focus:** Visible focus ring on all interactive elements for keyboard nav

### No Perpetual Animations
- No auto-sorting lists, no typewriter effects, no infinite carousels
- Sparklines render once with a subtle fade-in (`opacity 0→1, 300ms`)
- Pulse dot on "Last updated" is the only continuous animation (CSS `animate-pulse`, tiny, decorative)

### Accessibility (EN 301 549 / WCAG 2.1 AA)
- **Focus:** Visible focus ring on all interactive elements (buttons, links, toggles). Period selector ToggleGroup must be fully keyboard-navigable.
- **Labels:** Icon-only elements (e.g. severity icons) get `aria-label`. KPI chips that are links need descriptive `aria-label` (e.g. "Revenue: €2.4M, +8.2%. View financial KPIs").
- **Semantics:** Use `<main>`, `<section>`, headings hierarchy (h1 for page title). Tables if tabular data; otherwise semantic divs with `role` where needed.
- **Contrast:** Emerald/red/slate trend colors meet 4.5:1 for text. Coral accent meets 3:1 for UI.
- **Skip link:** Ensure "Skip to main content" works if layout has one.

---

## 12. Responsive Behavior

| Breakpoint | Headline Strip | Scorecard Grid | Sparklines | Period Selector |
|-----------|---------------|---------------|------------|-----------------|
| XL (1280+) | 5-col grid | 2-column | Full width | Inline |
| LG (1024-1279) | 5-col grid | 2-column | Smaller | Inline |
| MD (768-1023) | 5-col grid, compact | Single column | Hidden | Inline |
| SM (<768) | Horizontal scroll strip | Single column | Hidden | Stacked below title |

---

## 13. Error Handling, Security, Performance

### Error Handling
- **Per-domain:** Error boundary around each DomainScorecard. If one domain fails, show inline "Unable to load {domain} data. Retry." with retry button; others unaffected.
- **Global:** If `useKpiDashboard` fails, show toast; optional full-page error with retry.
- **Entity switch:** When `entityId` changes mid-load, cancel in-flight queries (TanStack Query handles via query key change). Avoid stale data display.

### Security
- Entity scoping: All KPI/alert endpoints use JWT `entity_id` claim. No entityId in URL for sensitive data.
- `executive.view` permission: Optional; only affects sidebar. Dashboard page at `/` is accessible to all authenticated users.
- No new sensitive endpoints; no PII in executive view.

### Performance Targets
- Page interactive within 5 seconds (LCP).
- Prefer <30 parallel KPI history requests. If exceeded, consider batch endpoint.
- Lazy-load comparison data only when "vs Prior" toggle is enabled.

---

## 14. Files to Create/Modify

### New Files
| File | Purpose |
|------|---------|
| `src/frontend/src/features/dashboard/ExecutiveDashboard.tsx` | Main executive dashboard page |
| `src/frontend/src/features/dashboard/components/HeadlineMetrics.tsx` | 5-KPI headline strip |
| `src/frontend/src/features/dashboard/components/DomainScorecard.tsx` | Single domain scorecard row |
| `src/frontend/src/features/dashboard/components/DomainScorecardGrid.tsx` | Grid of all domain rows |
| `src/frontend/src/features/dashboard/components/AlertsFeed.tsx` | Condensed alerts panel |
| `src/frontend/src/features/dashboard/components/QuickInsights.tsx` | Auto-generated comparison sentences |
| `src/frontend/src/features/dashboard/components/PeriodSelector.tsx` | MTD/QTD/YTD toggle + comparison switch |
| `src/frontend/src/features/dashboard/components/KpiPlaceholder.tsx` | Coming soon placeholder chip |
| `src/frontend/src/hooks/useExecutiveDashboard.ts` | Combined data hook for executive view |
| `src/frontend/src/locales/de/executive.json` | German translations |
| `src/frontend/src/locales/en/executive.json` | English translations |
| `src/frontend/src/locales/ru/executive.json` | Russian translations |

### Modified Files
| File | Change |
|------|--------|
| `src/frontend/src/app/router.tsx` | Add `/` → ExecutiveDashboard, move current dashboard to `/dashboard/ops` |
| `src/frontend/src/hooks/useKpis.ts` | Add `useEntityAlertEvents(entityId, status?)` for `GET /api/kpi/alert-events?status=active` |
| `src/frontend/src/components/layout/Sidebar.tsx` | Add executive sidebar variant with role detection; update operational sidebar "Dashboard" link from `/` to `/dashboard/ops` |
| `src/frontend/src/hooks/useAuth.ts` or `src/frontend/src/stores/authStore.ts` | Add `isExecutive` computed flag |
| `src/frontend/src/stores/uiStore.ts` | Add non-persisted `showFullNav: boolean` flag |
| `src/frontend/src/lib/queryKeys.ts` | Add executive dashboard query keys; add `entityAlertEvents: (entityId, status?) => [...]` (distinct from existing `alertEvents(alertId)`) |
| `src/frontend/src/locales/*/navigation.json` | Add executive nav labels |

### Minimal Backend Change
- Seed executive role values (`ceo`, `cfo`, `cso`, `chro`, `coo`) and `executive.view` permission into the role/permission system. No schema migration — seed data only.

### Data from Existing Endpoints (no API changes)
- `GET /api/kpi/dashboard` — current KPIs (entity from JWT). Returns `ActiveAlerts` count only, not full alert list.
- `GET /api/kpi/alert-events?status=active` — full alert events for AlertsFeed (entity from JWT)
- `GET /api/kpi/{kpiId}/history?from=YYYY-MM-DD&to=YYYY-MM-DD` — sparkline data
- `GET /api/kpi/definitions` — KPI metadata
- `GET /api/kpi/working-capital` — DSO/DIO/DPO/CCC

---

## 15. KPI-to-Executive-Role Mapping (Validated)

### Headline Metrics (visible to all executives)
| Metric | KPI ID | Source | Status |
|--------|--------|--------|--------|
| Revenue | `financial.revenue` | KpiSummaryDto (dashboard) | ✅ |
| EBITDA | `financial.ebitda` | KpiSummaryDto (dashboard) | ✅ |
| Free Cash Flow | `financial.free_cash_flow` | KpiSummaryDto (dashboard) | ✅ |
| Headcount | `hr.headcount` | KpiSummaryDto (dashboard) | ✅ |
| Active Alerts | Count from `useEntityAlertEvents` or `KpiDashboardDto.ActiveAlerts` | Alert events / Dashboard | ✅ |

### Domain Scorecard KPIs
| Domain | KPI 1 | KPI 2 | KPI 3 | KPI 4 | Status |
|--------|-------|-------|-------|-------|--------|
| Financial | `financial.revenue` ✅ | `financial.ebitda_margin` ✅ | `financial.net_margin` ✅ | `financial.operating_cash_flow` ✅ | ✅ All exist |
| Sales | `sales.mrr` ✅ | `sales.pipeline_value` ✅ | `sales.win_rate` ✅ | `sales.churn_rate` ✅ | ✅ All exist |
| Marketing | `marketing.cpl` ✅ | `marketing.marketing_roi` ✅ | `marketing.lead_conversion_rate` ✅ | `marketing.website_conversion` ✅ | ✅ All exist |
| HR | `hr.headcount` ✅ | `hr.turnover_rate` ✅ | `hr.time_to_hire` ✅ | `hr.absence_rate` ✅ | ✅ All exist |
| Operations | Doc Processing Rate ❌ | SLA Compliance ❌ | Process Throughput ❌ | — | ❌ Coming soon |

### Coming Soon KPIs (placeholder only, no backend)
- `ops.document_processing_rate` — entire Operations domain
- `ops.sla_compliance` — entire Operations domain
- `ops.process_throughput` — entire Operations domain
- `hr.open_positions` — not shown (4-chip limit, lower priority than existing HR KPIs)
- `sales.top_deals` — not shown as chip (would need a different widget format)
- `hr.review_completion_rate` — not shown (4-chip limit)
- `hr.onboarding_completion_rate` — not shown (4-chip limit)

---

## 16. Test Strategy

- **Unit:** `useExecutiveDashboard` — mock API, verify query keys, period/compare params
- **Unit:** Quick Insights — sort by `|changePct|`, top 3 logic
- **Integration:** Period selector updates URL; data refetches on param change
- **Integration:** Entity switch invalidates queries, no stale data
- **E2E (optional):** Route `/` shows ExecutiveDashboard; `/dashboard/ops` shows operational dashboard

---

## 17. Potential Risks & Edge Cases

| Risk | Mitigation |
|------|-------------|
| ~25 parallel KPI history requests | Monitor; add batch endpoint if needed |
| Entity switch during load | TanStack Query key includes entityId; old queries discarded |
| No entity selected | EmptyState like DashboardPage |
| Alert has no domain mapping | "View" link falls back to `/kpis/financial` |
| SignalR disconnected | "Last updated" shows amber; refetch interval as fallback |
| Invalid URL params | Fallback to `period=mtd`, `compare=false` |

---

## 18. Out of Scope

- Backend KPI creation for Operations domain
- PDF export of executive dashboard
- Role-specific KPI customization (CEO sees different metrics than CFO)
- Drag-and-drop dashboard customization
- Scheduled email reports
- Mobile app / PWA optimizations beyond responsive web
- AI-generated insights (beyond template string Quick Insights)
