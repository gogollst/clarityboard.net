# Executive Dashboard Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a role-aware executive dashboard at `/` with 5 headline KPIs, domain scorecards, alerts feed, period comparison, and a simplified executive sidebar — using only existing backend endpoints.

**Architecture:** New `ExecutiveDashboard.tsx` page composes `HeadlineMetrics`, `DomainScorecardGrid`, `AlertsFeed`, `QuickInsights`, and `PeriodSelector` components. Data flows through a single `useExecutiveDashboard` hook that wraps existing `useKpiDashboard` + `useQueries()` for batched KPI history. The current operational dashboard moves to `/dashboard/ops`; the sidebar renders an executive variant when the user has executive roles.

**Tech Stack:** React 19, TypeScript 5, TanStack Query (`useQueries`), Zustand, React Router 7, Tailwind CSS 4, shadcn/ui (ToggleGroup, Switch, Badge, Separator, Tooltip), Recharts (SparkLine), i18next, sonner (toasts)

**Spec:** `docs/plans/2026-03-17-executive-dashboard-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `src/frontend/src/features/dashboard/ExecutiveDashboard.tsx` | Page component — composes all sections, reads URL params |
| `src/frontend/src/features/dashboard/components/PeriodSelector.tsx` | MTD/QTD/YTD toggle + "vs Prior" switch |
| `src/frontend/src/features/dashboard/components/HeadlineMetrics.tsx` | 5-KPI strip with sparklines |
| `src/frontend/src/features/dashboard/components/DomainScorecard.tsx` | Single domain row (KPI chips + sparkline + alerts) |
| `src/frontend/src/features/dashboard/components/DomainScorecardGrid.tsx` | Grid of all 5 domain rows |
| `src/frontend/src/features/dashboard/components/AlertsFeed.tsx` | Condensed alert list (max 5) |
| `src/frontend/src/features/dashboard/components/QuickInsights.tsx` | Auto-generated comparison sentences |
| `src/frontend/src/features/dashboard/components/KpiChip.tsx` | Single KPI value display (reused in headline + scorecard) |
| `src/frontend/src/features/dashboard/components/KpiPlaceholder.tsx` | Coming soon placeholder chip |
| `src/frontend/src/hooks/useExecutiveDashboard.ts` | Combined data hook — dashboard + alert events + batched histories |
| `src/frontend/src/features/dashboard/executive-config.ts` | Static config: KPI IDs per domain, headline KPI IDs, domain-to-route map |
| `src/frontend/src/locales/en/executive.json` | English translations |
| `src/frontend/src/locales/de/executive.json` | German translations |
| `src/frontend/src/locales/ru/executive.json` | Russian translations |

### Modified Files
| File | Change |
|------|--------|
| `src/frontend/src/lib/queryKeys.ts` | Add `entityAlertEvents` key factory |
| `src/frontend/src/hooks/useKpis.ts` | Add `useEntityAlertEvents()` hook |
| `src/frontend/src/hooks/useAuth.ts` | Add `isExecutive` computed flag |
| `src/frontend/src/stores/uiStore.ts` | Add `showFullNav` boolean + `toggleFullNav` action |
| `src/frontend/src/app/router.tsx` | Add `/` → ExecutiveDashboard, `/dashboard/ops` → DashboardPage |
| `src/frontend/src/components/layout/Sidebar.tsx` | Add executive sidebar variant; update operational link `/` → `/dashboard/ops` |
| `src/frontend/src/locales/en/navigation.json` | Add executive nav group + item labels |
| `src/frontend/src/locales/de/navigation.json` | Add executive nav group + item labels |
| `src/frontend/src/locales/ru/navigation.json` | Add executive nav group + item labels |

---

## Task 1: Infrastructure — Query Keys, Alert Events Hook, Types

**Files:**
- Modify: `src/frontend/src/lib/queryKeys.ts`
- Modify: `src/frontend/src/hooks/useKpis.ts`

- [ ] **Step 1: Add `entityAlertEvents` to queryKeys**

In `src/frontend/src/lib/queryKeys.ts`, add to the `kpi` object after `alertEvents`:

```ts
entityAlertEvents: (entityId: string, status?: string) =>
  ['kpi', 'entity-alert-events', entityId, status ?? 'all'] as const,
```

- [ ] **Step 2: Add `useEntityAlertEvents` hook to useKpis.ts**

In `src/frontend/src/hooks/useKpis.ts`, add after the existing `useAlertEvents` function:

```ts
export function useEntityAlertEvents(
  entityId: string | null,
  status?: 'active' | 'acknowledged' | 'resolved',
) {
  return useQuery({
    queryKey: queryKeys.kpi.entityAlertEvents(entityId ?? '', status),
    queryFn: async () => {
      const { data } = await api.get<AlertDto[]>(
        '/kpi/alert-events',
        { params: { entityId, status } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}
```

Note: The alert-events endpoint response uses the same `AlertDto` type (with `severity`, `title`, `message`, `kpiId`, `triggeredAt`, `status` fields). If the actual response shape differs, add a separate `AlertEventDto` to `src/frontend/src/types/kpi.ts` and use that instead.

- [ ] **Step 3: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds with no TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/lib/queryKeys.ts src/frontend/src/hooks/useKpis.ts
git commit -m "feat(kpi): add useEntityAlertEvents hook for executive dashboard alerts"
```

---

## Task 2: Auth — `isExecutive` Flag

**Files:**
- Modify: `src/frontend/src/hooks/useAuth.ts`

- [ ] **Step 1: Add `isExecutive` to useAuth return**

In `src/frontend/src/hooks/useAuth.ts`, add before the `return` statement (line ~84):

```ts
const EXECUTIVE_ROLES = ['ceo', 'cfo', 'cso', 'chro', 'coo'];

const isExecutive =
  user?.roles?.some((r) => EXECUTIVE_ROLES.includes(r.toLowerCase())) ||
  user?.permissions?.includes('executive.view') ||
  false;
```

Add `isExecutive` to the return object:

```ts
return {
  user,
  isAuthenticated,
  login,
  verify2FA,
  logout,
  forgotPassword,
  resetPasswordViaToken,
  hasPermission,
  role: user?.roles?.[0] ?? null,
  isExecutive,
};
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/hooks/useAuth.ts
git commit -m "feat(auth): add isExecutive flag for role-aware UI"
```

---

## Task 3: UI Store — `showFullNav` Toggle

**Files:**
- Modify: `src/frontend/src/stores/uiStore.ts`

- [ ] **Step 1: Add `showFullNav` to UiState interface and store**

In `src/frontend/src/stores/uiStore.ts`, add to the `UiState` interface:

```ts
showFullNav: boolean;
toggleFullNav: () => void;
```

Add to the store initial state:

```ts
showFullNav: false,
toggleFullNav: () => set((s) => ({ showFullNav: !s.showFullNav })),
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/stores/uiStore.ts
git commit -m "feat(ui): add showFullNav toggle for executive sidebar"
```

---

## Task 4: Executive Config — Static KPI Domain Mapping

**Files:**
- Create: `src/frontend/src/features/dashboard/executive-config.ts`

- [ ] **Step 1: Create the config file**

```ts
// Static configuration for the executive dashboard.
// Maps domains to their primary KPI IDs and navigation routes.

export const HEADLINE_KPIS = [
  { kpiId: 'financial.revenue', labelKey: 'executive:headlines.revenue', route: '/kpis/financial' },
  { kpiId: 'financial.ebitda', labelKey: 'executive:headlines.ebitda', route: '/kpis/financial' },
  { kpiId: 'financial.free_cash_flow', labelKey: 'executive:headlines.freeCashFlow', route: '/kpis/financial' },
  { kpiId: 'hr.headcount', labelKey: 'executive:headlines.headcount', route: '/kpis/hr' },
] as const;

export type Period = 'mtd' | 'qtd' | 'ytd';

export interface DomainConfig {
  domain: string;
  labelKey: string;
  route: string | null; // null = coming soon, not clickable
  kpiIds: string[];
  chartKpiId: string | null; // primary KPI for the sparkline
  comingSoon?: boolean;
}

export const DOMAIN_CONFIGS: DomainConfig[] = [
  {
    domain: 'financial',
    labelKey: 'dashboard:domains.financial',
    route: '/kpis/financial',
    kpiIds: ['financial.revenue', 'financial.ebitda_margin', 'financial.net_margin', 'financial.operating_cash_flow'],
    chartKpiId: 'financial.revenue',
  },
  {
    domain: 'sales',
    labelKey: 'dashboard:domains.sales',
    route: '/kpis/sales',
    kpiIds: ['sales.mrr', 'sales.pipeline_value', 'sales.win_rate', 'sales.churn_rate'],
    chartKpiId: 'sales.mrr',
  },
  {
    domain: 'marketing',
    labelKey: 'dashboard:domains.marketing',
    route: '/kpis/marketing',
    kpiIds: ['marketing.cpl', 'marketing.marketing_roi', 'marketing.lead_conversion_rate', 'marketing.website_conversion'],
    chartKpiId: 'marketing.lead_conversion_rate',
  },
  {
    domain: 'hr',
    labelKey: 'dashboard:domains.hr',
    route: '/kpis/hr',
    kpiIds: ['hr.headcount', 'hr.turnover_rate', 'hr.time_to_hire', 'hr.absence_rate'],
    chartKpiId: 'hr.headcount',
  },
  {
    domain: 'operations',
    labelKey: 'executive:domains.operations',
    route: null,
    kpiIds: ['ops.document_processing_rate', 'ops.sla_compliance', 'ops.process_throughput'],
    chartKpiId: null,
    comingSoon: true,
  },
];

// All unique KPI IDs that need history data for sparklines
export function getAllSparklineKpiIds(): string[] {
  const headlineIds = HEADLINE_KPIS.map((h) => h.kpiId);
  const domainChartIds = DOMAIN_CONFIGS
    .filter((d) => d.chartKpiId && !d.comingSoon)
    .map((d) => d.chartKpiId!);
  return [...new Set([...headlineIds, ...domainChartIds])];
}

// Map KPI ID to its domain route for "View" links
export function getDomainRoute(kpiId: string): string {
  const domain = kpiId.split('.')[0];
  const config = DOMAIN_CONFIGS.find((d) => d.domain === domain);
  return config?.route ?? '/kpis/financial'; // fallback per spec
}

export function getPeriodDates(period: Period): { from: string; to: string } {
  const now = new Date();
  const y = now.getFullYear();
  const m = now.getMonth(); // 0-indexed
  const today = formatDate(now);

  switch (period) {
    case 'mtd':
      return { from: formatDate(new Date(y, m, 1)), to: today };
    case 'qtd': {
      const quarterStart = new Date(y, Math.floor(m / 3) * 3, 1);
      return { from: formatDate(quarterStart), to: today };
    }
    case 'ytd':
      return { from: `${y}-01-01`, to: today };
  }
}

export function getPriorPeriodDates(period: Period): { from: string; to: string } {
  const { from, to } = getPeriodDates(period);
  return {
    from: shiftYearBack(from),
    to: shiftYearBack(to),
  };
}

function formatDate(d: Date): string {
  return d.toISOString().slice(0, 10); // YYYY-MM-DD
}

function shiftYearBack(dateStr: string): string {
  const [y, m, d] = dateStr.split('-');
  return `${Number(y) - 1}-${m}-${d}`;
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/executive-config.ts
git commit -m "feat(executive): add static KPI domain config and period helpers"
```

---

## Task 5: Data Hook — `useExecutiveDashboard`

**Files:**
- Create: `src/frontend/src/hooks/useExecutiveDashboard.ts`

**Dependencies:** Tasks 1, 4

- [ ] **Step 1: Create the hook**

```ts
import { useQueries } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { useKpiDashboard, useKpiDefinitions, useEntityAlertEvents } from './useKpis';
import {
  getAllSparklineKpiIds,
  getPeriodDates,
  getPriorPeriodDates,
  type Period,
} from '@/features/dashboard/executive-config';
import type { KpiSnapshot } from '@/types/kpi';

export function useExecutiveDashboard(
  entityId: string | null,
  period: Period,
  compareEnabled: boolean,
) {
  const dashboard = useKpiDashboard(entityId);
  const definitions = useKpiDefinitions();
  const alertEvents = useEntityAlertEvents(entityId, 'active');

  const { from, to } = getPeriodDates(period);
  const sparklineKpiIds = getAllSparklineKpiIds();

  // Batch-fetch sparkline history for all KPIs
  const historyQueries = useQueries({
    queries: entityId
      ? sparklineKpiIds.map((kpiId) => ({
          queryKey: [...queryKeys.kpi.history(entityId, kpiId), from, to],
          queryFn: async () => {
            const { data } = await api.get<KpiSnapshot[]>(
              `/kpi/${kpiId}/history`,
              { params: { from, to } },
            );
            return { kpiId, data };
          },
        }))
      : [],
  });

  // Prior-year comparison (lazy — only when compareEnabled)
  const prior = compareEnabled ? getPriorPeriodDates(period) : null;

  const comparisonQueries = useQueries({
    queries: entityId && prior
      ? sparklineKpiIds.map((kpiId) => ({
          queryKey: [...queryKeys.kpi.history(entityId, kpiId), prior.from, prior.to],
          queryFn: async () => {
            const { data } = await api.get<KpiSnapshot[]>(
              `/kpi/${kpiId}/history`,
              { params: { from: prior.from, to: prior.to } },
            );
            return { kpiId, data };
          },
          enabled: compareEnabled,
        }))
      : [],
  });

  // Build lookup: kpiId → number[]
  const historyMap = new Map<string, number[]>();
  for (const q of historyQueries) {
    if (q.data) {
      historyMap.set(q.data.kpiId, q.data.data.map((s) => s.value));
    }
  }

  const comparisonMap = new Map<string, number[]>();
  for (const q of comparisonQueries) {
    if (q.data) {
      comparisonMap.set(q.data.kpiId, q.data.data.map((s) => s.value));
    }
  }

  // Build KPI definition lookup
  const definitionMap = new Map(
    definitions.data?.map((d) => [d.id, d]) ?? [],
  );

  const isLoading =
    dashboard.isLoading ||
    definitions.isLoading ||
    historyQueries.some((q) => q.isLoading);

  const isError = dashboard.isError;

  return {
    dashboard: dashboard.data,
    definitions: definitionMap,
    alertEvents: alertEvents.data ?? [],
    historyMap,
    comparisonMap,
    isLoading,
    isError,
    refetch: dashboard.refetch,
  };
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/hooks/useExecutiveDashboard.ts
git commit -m "feat(executive): add useExecutiveDashboard composite data hook"
```

---

## Task 6: i18n — English Translations

**Files:**
- Create: `src/frontend/src/locales/en/executive.json`
- Modify: `src/frontend/src/locales/en/navigation.json`

- [ ] **Step 1: Create English executive translations**

```json
{
  "title": "Executive Summary",
  "lastUpdated": "Last updated: {{time}}",
  "period": {
    "mtd": "MTD",
    "qtd": "QTD",
    "ytd": "YTD",
    "vsPrior": "vs Prior"
  },
  "headlines": {
    "revenue": "Revenue",
    "ebitda": "EBITDA",
    "freeCashFlow": "Free Cash Flow",
    "headcount": "Headcount",
    "alerts": "Alerts"
  },
  "domains": {
    "operations": "Operations"
  },
  "alerts": {
    "title": "Alerts & Exceptions",
    "viewAll": "View all alerts",
    "allClear": "All metrics within target ranges",
    "view": "View"
  },
  "insights": {
    "title": "Key Changes",
    "up": "{{name}} up {{pct}}% vs last period",
    "down": "{{name}} down {{pct}}% vs last period",
    "improved": "{{name}} improved by {{pct}}%",
    "worsened": "{{name}} worsened by {{pct}}%",
    "unchanged": "{{name}} unchanged"
  },
  "comingSoon": {
    "noData": "--",
    "noChart": "No data yet",
    "inProgress": "Data collection in progress"
  },
  "sidebar": {
    "executiveSummary": "Executive Summary",
    "domains": "Domains",
    "tools": "Tools",
    "fullDashboard": "Full Dashboard",
    "showFullNav": "Show full navigation",
    "hideFullNav": "Hide full navigation",
    "operations": "Operations",
    "comingSoon": "Coming soon"
  },
  "noEntity": {
    "title": "No Entity Selected",
    "description": "Select a legal entity to view the executive summary."
  },
  "error": {
    "domainFailed": "Unable to load {{domain}} data.",
    "retry": "Retry"
  }
}
```

- [ ] **Step 2: Add executive nav labels to navigation.json**

In `src/frontend/src/locales/en/navigation.json`, add to `groups`:

```json
"executive": "Executive",
"executiveTools": "Tools"
```

Add to `items`:

```json
"executiveSummary": "Executive Summary",
"fullDashboard": "Full Dashboard",
"operations": "Operations"
```

- [ ] **Step 3: Register the `executive` namespace in i18n config**

Check `src/frontend/src/lib/i18n.ts` (or wherever i18next is configured) and ensure the `executive` namespace is included in the resources/ns list. The namespace should auto-load from the JSON file.

- [ ] **Step 4: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/locales/en/executive.json src/frontend/src/locales/en/navigation.json
git commit -m "feat(i18n): add English executive dashboard translations"
```

---

## Task 7: PeriodSelector Component

**Files:**
- Create: `src/frontend/src/features/dashboard/components/PeriodSelector.tsx`

- [ ] **Step 1: Create the component**

```tsx
import { useTranslation } from 'react-i18next';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import type { Period } from '../executive-config';

interface PeriodSelectorProps {
  period: Period;
  onPeriodChange: (period: Period) => void;
  compareEnabled: boolean;
  onCompareChange: (enabled: boolean) => void;
}

const PERIODS: Period[] = ['mtd', 'qtd', 'ytd'];

export default function PeriodSelector({
  period,
  onPeriodChange,
  compareEnabled,
  onCompareChange,
}: PeriodSelectorProps) {
  const { t } = useTranslation('executive');

  return (
    <div className="flex items-center gap-3">
      <div className="flex rounded-md border border-border" role="group" aria-label="Period selector">
        {PERIODS.map((p) => (
          <button
            key={p}
            onClick={() => onPeriodChange(p)}
            className={`px-3 py-1.5 text-xs font-medium transition-colors first:rounded-l-md last:rounded-r-md ${
              period === p
                ? 'bg-primary text-primary-foreground'
                : 'text-muted-foreground hover:bg-secondary/50'
            }`}
            aria-pressed={period === p}
          >
            {t(`period.${p}`)}
          </button>
        ))}
      </div>

      <div className="flex items-center gap-1.5">
        <Switch
          id="compare-toggle"
          checked={compareEnabled}
          onCheckedChange={onCompareChange}
          className="scale-75"
        />
        <Label htmlFor="compare-toggle" className="text-xs text-muted-foreground cursor-pointer">
          {t('period.vsPrior')}
        </Label>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/components/PeriodSelector.tsx
git commit -m "feat(executive): add PeriodSelector component"
```

---

## Task 8: KpiChip + KpiPlaceholder Components

**Files:**
- Create: `src/frontend/src/features/dashboard/components/KpiChip.tsx`
- Create: `src/frontend/src/features/dashboard/components/KpiPlaceholder.tsx`

- [ ] **Step 1: Create KpiChip — reusable KPI value display**

```tsx
import { useNavigate } from 'react-router-dom';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface KpiChipProps {
  snapshot: KpiSnapshot | undefined;
  definition: KpiDefinition | undefined;
  route?: string | null;
  size?: 'lg' | 'md'; // lg = headline, md = scorecard
  comparisonValue?: number | null;
  className?: string;
}

function formatValue(value: number, unit: KpiDefinition['unit']): string {
  switch (unit) {
    case 'currency':
      if (Math.abs(value) >= 1_000_000) return `€${(value / 1_000_000).toFixed(1)}M`;
      if (Math.abs(value) >= 1_000) return `€${(value / 1_000).toFixed(0)}K`;
      return `€${value.toFixed(0)}`;
    case 'percentage':
      return `${value.toFixed(1)}%`;
    case 'days':
      return `${value.toFixed(0)}d`;
    case 'count':
      return value.toLocaleString('en-US', { maximumFractionDigits: 0 });
    case 'ratio':
      return value.toFixed(2);
    default:
      return String(value);
  }
}

function getTrendColor(changePct: number | null, direction: KpiDefinition['direction']): string {
  if (changePct === null || changePct === 0) return 'text-muted-foreground';
  const isPositiveChange = changePct > 0;
  const isGood =
    direction === 'higher_better' ? isPositiveChange : !isPositiveChange;
  return isGood ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400';
}

function getTrendArrow(changePct: number | null): string {
  if (changePct === null || changePct === 0) return '—';
  return changePct > 0 ? '▲' : '▼';
}

export default function KpiChip({
  snapshot,
  definition,
  route,
  size = 'md',
  comparisonValue,
  className,
}: KpiChipProps) {
  const navigate = useNavigate();

  if (!snapshot || !definition) return null;

  const isClickable = !!route;
  const valueSize = size === 'lg' ? 'text-2xl' : 'text-lg';
  const labelSize = size === 'lg' ? 'text-sm' : 'text-xs';

  return (
    <button
      type="button"
      onClick={() => isClickable && navigate(route!)}
      disabled={!isClickable}
      className={cn(
        'text-left transition-transform',
        isClickable && 'cursor-pointer active:scale-[0.98]',
        !isClickable && 'cursor-default',
        className,
      )}
      aria-label={
        `${definition.name}: ${formatValue(snapshot.value, definition.unit)}` +
        (snapshot.changePct !== null ? `, ${snapshot.changePct > 0 ? '+' : ''}${snapshot.changePct.toFixed(1)}%` : '') +
        (isClickable ? `. View ${definition.domain} KPIs` : '')
      }
    >
      <p className={cn(labelSize, 'text-muted-foreground mb-0.5')}>{definition.name}</p>
      <p className={cn(valueSize, 'font-mono tabular-nums font-semibold')}>
        {formatValue(snapshot.value, definition.unit)}
      </p>
      {snapshot.changePct !== null && (
        <p className={cn('text-xs mt-0.5', getTrendColor(snapshot.changePct, definition.direction))}>
          {getTrendArrow(snapshot.changePct)} {snapshot.changePct > 0 ? '+' : ''}{snapshot.changePct.toFixed(1)}%
        </p>
      )}
      {comparisonValue !== undefined && comparisonValue !== null && (
        <p className="text-xs text-muted-foreground/60 mt-0.5">
          {formatValue(comparisonValue, definition.unit)}
        </p>
      )}
    </button>
  );
}
```

- [ ] **Step 2: Create KpiPlaceholder**

```tsx
import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

interface KpiPlaceholderProps {
  label: string;
  size?: 'lg' | 'md';
  className?: string;
}

export default function KpiPlaceholder({ label, size = 'md', className }: KpiPlaceholderProps) {
  const { t } = useTranslation('executive');
  const valueSize = size === 'lg' ? 'text-2xl' : 'text-lg';
  const labelSize = size === 'lg' ? 'text-sm' : 'text-xs';

  return (
    <div className={cn('text-left', className)}>
      <p className={cn(labelSize, 'text-muted-foreground mb-0.5')}>{label}</p>
      <p className={cn(valueSize, 'font-mono tabular-nums font-semibold text-muted-foreground/40')}>
        {t('comingSoon.noData')}
      </p>
    </div>
  );
}
```

- [ ] **Step 3: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/dashboard/components/KpiChip.tsx src/frontend/src/features/dashboard/components/KpiPlaceholder.tsx
git commit -m "feat(executive): add KpiChip and KpiPlaceholder components"
```

---

## Task 9: HeadlineMetrics Component

**Files:**
- Create: `src/frontend/src/features/dashboard/components/HeadlineMetrics.tsx`

**Dependencies:** Tasks 5, 8

- [ ] **Step 1: Create the component**

```tsx
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import SparkLine from '@/components/charts/SparkLine';
import { Skeleton } from '@/components/ui/skeleton';
import { HEADLINE_KPIS } from '../executive-config';
import KpiChip from './KpiChip';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';
import type { AlertDto } from '@/types/kpi';

interface HeadlineMetricsProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
  alertEvents: AlertDto[];
  historyMap: Map<string, number[]>;
  comparisonMap: Map<string, number[]>;
  compareEnabled: boolean;
  isLoading: boolean;
}

export default function HeadlineMetrics({
  kpis,
  definitions,
  alertEvents,
  historyMap,
  comparisonMap,
  compareEnabled,
  isLoading,
}: HeadlineMetricsProps) {
  const { t } = useTranslation('executive');
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <div className="grid grid-cols-5 gap-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="space-y-2">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-7 w-24" />
            <Skeleton className="h-3 w-12" />
            <Skeleton className="h-8 w-20 mt-1" />
          </div>
        ))}
      </div>
    );
  }

  const kpiMap = new Map(kpis.map((s) => [s.kpiId, s]));

  const criticalCount = alertEvents.filter((a) => a.severity === 'critical').length;
  const warningCount = alertEvents.filter((a) => a.severity === 'warning').length;
  const totalAlerts = criticalCount + warningCount;

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 sm:gap-6">
      {HEADLINE_KPIS.map((cfg) => {
        const snapshot = kpiMap.get(cfg.kpiId);
        const definition = definitions.get(cfg.kpiId);
        const history = historyMap.get(cfg.kpiId);
        const comparison = compareEnabled ? comparisonMap.get(cfg.kpiId) : undefined;
        const lastCompValue = comparison?.length ? comparison[comparison.length - 1] : null;

        return (
          <div key={cfg.kpiId} className="min-w-0">
            <KpiChip
              snapshot={snapshot}
              definition={definition}
              route={cfg.route}
              size="lg"
              comparisonValue={compareEnabled ? lastCompValue : null}
            />
            {history && history.length > 1 && (
              <SparkLine
                data={history}
                trend={
                  snapshot?.changePct
                    ? snapshot.changePct > 0 ? 'up' : 'down'
                    : 'neutral'
                }
                className="h-8 w-full mt-1"
              />
            )}
          </div>
        );
      })}

      {/* Alerts headline metric */}
      <button
        type="button"
        onClick={() => {
          const alertsSection = document.getElementById('alerts-feed');
          alertsSection?.scrollIntoView({ behavior: 'smooth' });
        }}
        className="text-left active:scale-[0.98] transition-transform cursor-pointer"
        aria-label={`${totalAlerts} active alerts: ${criticalCount} critical, ${warningCount} warning`}
      >
        <p className="text-sm text-muted-foreground mb-0.5">{t('headlines.alerts')}</p>
        <p className="text-2xl font-mono tabular-nums font-semibold">{totalAlerts}</p>
        {totalAlerts > 0 && (
          <p className="text-xs mt-0.5 space-x-2">
            {criticalCount > 0 && (
              <span className="text-red-600 dark:text-red-400">{criticalCount} critical</span>
            )}
            {warningCount > 0 && (
              <span className="text-amber-600 dark:text-amber-400">{warningCount} warning</span>
            )}
          </p>
        )}
      </button>
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/components/HeadlineMetrics.tsx
git commit -m "feat(executive): add HeadlineMetrics component"
```

---

## Task 10: DomainScorecard + DomainScorecardGrid Components

**Files:**
- Create: `src/frontend/src/features/dashboard/components/DomainScorecard.tsx`
- Create: `src/frontend/src/features/dashboard/components/DomainScorecardGrid.tsx`

**Dependencies:** Tasks 5, 8

- [ ] **Step 1: Create DomainScorecard**

```tsx
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import SparkLine from '@/components/charts/SparkLine';
import KpiChip from './KpiChip';
import KpiPlaceholder from './KpiPlaceholder';
import type { DomainConfig } from '../executive-config';
import type { KpiSnapshot, KpiDefinition, AlertDto } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface DomainScorecardProps {
  config: DomainConfig;
  kpis: Map<string, KpiSnapshot>;
  definitions: Map<string, KpiDefinition>;
  alerts: AlertDto[];
  historyMap: Map<string, number[]>;
}

export default function DomainScorecard({
  config,
  kpis,
  definitions,
  alerts,
  historyMap,
}: DomainScorecardProps) {
  const { t } = useTranslation(['executive', 'dashboard']);
  const navigate = useNavigate();

  const isClickable = !!config.route && !config.comingSoon;
  const chartHistory = config.chartKpiId ? historyMap.get(config.chartKpiId) : null;
  const domainAlerts = alerts.filter((a) =>
    a.kpiId && config.kpiIds.some((id) => a.kpiId === id || a.kpiId?.startsWith(config.domain + '.')),
  );

  // Determine sparkline trend from the chart KPI
  const chartSnapshot = config.chartKpiId ? kpis.get(config.chartKpiId) : null;
  const chartTrend = chartSnapshot?.changePct
    ? chartSnapshot.changePct > 0 ? 'up' as const : 'down' as const
    : 'neutral' as const;

  return (
    <div
      role={isClickable ? 'button' : undefined}
      tabIndex={isClickable ? 0 : undefined}
      onClick={() => isClickable && navigate(config.route!)}
      onKeyDown={(e) => {
        if (isClickable && (e.key === 'Enter' || e.key === ' ')) {
          e.preventDefault();
          navigate(config.route!);
        }
      }}
      className={cn(
        'py-5 px-1 border-b border-border',
        isClickable && 'cursor-pointer hover:bg-secondary/50 transition-colors duration-200 rounded-md',
        config.comingSoon && 'opacity-60',
      )}
      aria-label={isClickable ? `${t(config.labelKey)} — click to view details` : undefined}
    >
      <div className="flex items-start justify-between gap-4">
        {/* Left: domain label + KPI chips */}
        <div className="flex-1 min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
            {t(config.labelKey)}
          </p>
          <div className="flex flex-wrap gap-x-6 gap-y-3">
            {config.kpiIds.map((kpiId) => {
              if (config.comingSoon) {
                const label = definitions.get(kpiId)?.name ?? kpiId.split('.').pop() ?? kpiId;
                return <KpiPlaceholder key={kpiId} label={label} />;
              }
              return (
                <KpiChip
                  key={kpiId}
                  snapshot={kpis.get(kpiId)}
                  definition={definitions.get(kpiId)}
                  size="md"
                />
              );
            })}
          </div>
        </div>

        {/* Right: sparkline */}
        <div className="hidden lg:block flex-shrink-0 w-24">
          {chartHistory && chartHistory.length > 1 ? (
            <SparkLine data={chartHistory} trend={chartTrend} className="h-12 w-24" />
          ) : config.comingSoon ? (
            <p className="text-xs text-muted-foreground/50 text-center pt-3">
              {t('executive:comingSoon.noChart')}
            </p>
          ) : null}
        </div>
      </div>

      {/* Inline alerts */}
      {domainAlerts.length > 0 && !config.comingSoon && (
        <div className="mt-3 space-y-1">
          {domainAlerts.slice(0, 2).map((alert) => (
            <p key={alert.id} className="text-xs text-muted-foreground">
              <span className={alert.severity === 'critical' ? 'text-red-500' : 'text-amber-500'}>
                {alert.severity === 'critical' ? '●' : '▲'}
              </span>
              {' '}{alert.message}
            </p>
          ))}
        </div>
      )}

      {/* Coming soon label */}
      {config.comingSoon && (
        <p className="text-xs text-muted-foreground/50 mt-3">
          {t('executive:comingSoon.inProgress')}
        </p>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Create DomainScorecardGrid**

```tsx
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { DOMAIN_CONFIGS } from '../executive-config';
import DomainScorecard from './DomainScorecard';
import type { KpiSnapshot, KpiDefinition, AlertDto } from '@/types/kpi';

interface DomainScorecardGridProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
  alerts: AlertDto[];
  historyMap: Map<string, number[]>;
  isLoading: boolean;
}

export default function DomainScorecardGrid({
  kpis,
  definitions,
  alerts,
  historyMap,
  isLoading,
}: DomainScorecardGridProps) {
  if (isLoading) {
    return (
      <div className="space-y-6">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="space-y-3">
            <Skeleton className="h-3 w-20" />
            <div className="flex gap-6">
              {Array.from({ length: 4 }).map((_, j) => (
                <div key={j} className="space-y-2">
                  <Skeleton className="h-3 w-14" />
                  <Skeleton className="h-5 w-20" />
                  <Skeleton className="h-3 w-10" />
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    );
  }

  const kpiMap = new Map(kpis.map((s) => [s.kpiId, s]));

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8">
      {DOMAIN_CONFIGS.map((config) => (
        <DomainScorecard
          key={config.domain}
          config={config}
          kpis={kpiMap}
          definitions={definitions}
          alerts={alerts}
          historyMap={historyMap}
        />
      ))}
    </div>
  );
}
```

- [ ] **Step 3: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/dashboard/components/DomainScorecard.tsx src/frontend/src/features/dashboard/components/DomainScorecardGrid.tsx
git commit -m "feat(executive): add DomainScorecard and DomainScorecardGrid components"
```

---

## Task 11: AlertsFeed Component

**Files:**
- Create: `src/frontend/src/features/dashboard/components/AlertsFeed.tsx`

- [ ] **Step 1: Create the component**

```tsx
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { getDomainRoute } from '../executive-config';
import type { AlertDto } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface AlertsFeedProps {
  alerts: AlertDto[];
}

export default function AlertsFeed({ alerts }: AlertsFeedProps) {
  const { t } = useTranslation('executive');
  const navigate = useNavigate();

  // Filter to critical + warning, sort by severity then recency
  const filtered = alerts
    .filter((a) => a.severity === 'critical' || a.severity === 'warning')
    .sort((a, b) => {
      if (a.severity === 'critical' && b.severity !== 'critical') return -1;
      if (b.severity === 'critical' && a.severity !== 'critical') return 1;
      return new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime();
    });

  const visible = filtered.slice(0, 5);
  const hasMore = filtered.length > 5;

  if (filtered.length === 0) {
    return (
      <div id="alerts-feed" className="py-6 text-center">
        <p className="text-sm text-muted-foreground">
          <span className="text-emerald-500 mr-1.5">✓</span>
          {t('alerts.allClear')}
        </p>
      </div>
    );
  }

  return (
    <div id="alerts-feed" className="py-4">
      <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
        {t('alerts.title')}
      </h2>
      <div className="space-y-2">
        {visible.map((alert) => (
          <div key={alert.id} className="flex items-center gap-2 text-sm">
            <span className={cn(
              'flex-shrink-0 text-xs',
              alert.severity === 'critical' ? 'text-red-500' : 'text-amber-500',
            )}>
              {alert.severity === 'critical' ? '●' : '▲'}
            </span>
            <span className="flex-1 min-w-0 truncate text-foreground/80">
              {alert.title}: {alert.message}
            </span>
            <button
              type="button"
              onClick={() => {
                const route = alert.kpiId ? getDomainRoute(alert.kpiId) : '/kpis/financial';
                const highlight = alert.kpiId ? `?highlight=${alert.kpiId}` : '';
                navigate(`${route}${highlight}`);
              }}
              className="flex-shrink-0 text-xs text-primary hover:underline"
            >
              {t('alerts.view')}
            </button>
          </div>
        ))}
      </div>
      {hasMore && (
        <button
          type="button"
          onClick={() => navigate('/kpis/financial')} // TODO: dedicated alerts page
          className="mt-2 text-xs text-primary hover:underline"
        >
          {t('alerts.viewAll')}
        </button>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/components/AlertsFeed.tsx
git commit -m "feat(executive): add AlertsFeed component"
```

---

## Task 12: QuickInsights Component

**Files:**
- Create: `src/frontend/src/features/dashboard/components/QuickInsights.tsx`

- [ ] **Step 1: Create the component**

```tsx
import { useTranslation } from 'react-i18next';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';

interface QuickInsightsProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
}

export default function QuickInsights({ kpis, definitions }: QuickInsightsProps) {
  const { t } = useTranslation('executive');

  // Top 3 most notable changes
  const notable = kpis
    .filter((s) => s.changePct !== null && s.changePct !== 0)
    .sort((a, b) => Math.abs(b.changePct!) - Math.abs(a.changePct!))
    .slice(0, 3);

  if (notable.length === 0) return null;

  const sentences = notable.map((s) => {
    const def = definitions.get(s.kpiId);
    const name = def?.name ?? s.kpiId;
    const pct = Math.abs(s.changePct!).toFixed(1);
    const isPositive = s.changePct! > 0;

    if (def?.direction === 'lower_better') {
      return isPositive
        ? t('insights.worsened', { name, pct })
        : t('insights.improved', { name, pct });
    }
    return isPositive
      ? t('insights.up', { name, pct })
      : t('insights.down', { name, pct });
  });

  return (
    <div className="py-4">
      <p className="text-sm text-muted-foreground">
        {sentences.join('. ')}.
      </p>
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/components/QuickInsights.tsx
git commit -m "feat(executive): add QuickInsights component"
```

---

## Task 13: ExecutiveDashboard Page

**Files:**
- Create: `src/frontend/src/features/dashboard/ExecutiveDashboard.tsx`

**Dependencies:** Tasks 5–12

- [ ] **Step 1: Create the page component**

```tsx
import { useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useExecutiveDashboard } from '@/hooks/useExecutiveDashboard';
import { useUiStore } from '@/stores/uiStore';
import PeriodSelector from './components/PeriodSelector';
import HeadlineMetrics from './components/HeadlineMetrics';
import DomainScorecardGrid from './components/DomainScorecardGrid';
import AlertsFeed from './components/AlertsFeed';
import QuickInsights from './components/QuickInsights';
import { Separator } from '@/components/ui/separator';
import type { Period } from './executive-config';

const VALID_PERIODS: Period[] = ['mtd', 'qtd', 'ytd'];

export function Component() {
  const { t } = useTranslation('executive');
  const { selectedEntityId, selectedEntity } = useEntity();
  const [searchParams, setSearchParams] = useSearchParams();
  const connectionStatus = useUiStore((s) => s.connectionStatus);

  // URL params with validation + defaults
  const rawPeriod = searchParams.get('period');
  const period: Period = VALID_PERIODS.includes(rawPeriod as Period)
    ? (rawPeriod as Period)
    : 'mtd';
  const compareEnabled = searchParams.get('compare') === 'true';

  const setPeriod = useCallback((p: Period) => {
    setSearchParams((prev) => {
      prev.set('period', p);
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const setCompare = useCallback((enabled: boolean) => {
    setSearchParams((prev) => {
      if (enabled) prev.set('compare', 'true');
      else prev.delete('compare');
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const {
    dashboard,
    definitions,
    alertEvents,
    historyMap,
    comparisonMap,
    isLoading,
    isError,
    refetch,
  } = useExecutiveDashboard(selectedEntityId, period, compareEnabled);

  // No entity selected
  if (!selectedEntityId) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center">
          <h2 className="text-lg font-semibold">{t('noEntity.title')}</h2>
          <p className="text-sm text-muted-foreground mt-1">{t('noEntity.description')}</p>
        </div>
      </div>
    );
  }

  // Global error
  if (isError && !dashboard) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center">
          <p className="text-sm text-destructive">Failed to load dashboard data.</p>
          <button
            type="button"
            onClick={() => refetch()}
            className="mt-2 text-xs text-primary hover:underline"
          >
            {t('error.retry')}
          </button>
        </div>
      </div>
    );
  }

  const lastUpdatedTime = dashboard?.lastUpdated
    ? new Date(dashboard.lastUpdated).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : null;

  const isStale = !dashboard?.lastUpdated ||
    (Date.now() - new Date(dashboard.lastUpdated).getTime()) > 5 * 60 * 1000;

  return (
    <main className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="font-display text-2xl tracking-tight">
            {selectedEntity?.name ?? ''} — {t('title')}
          </h1>
          {lastUpdatedTime && (
            <p className="text-xs text-muted-foreground mt-1 flex items-center gap-1.5">
              {t('lastUpdated', { time: lastUpdatedTime })}
              <span
                className={`inline-block h-1.5 w-1.5 rounded-full ${
                  connectionStatus === 'connected' && !isStale
                    ? 'bg-emerald-500 animate-pulse'
                    : 'bg-amber-500'
                }`}
              />
            </p>
          )}
        </div>
        <PeriodSelector
          period={period}
          onPeriodChange={setPeriod}
          compareEnabled={compareEnabled}
          onCompareChange={setCompare}
        />
      </div>

      {/* Headline Metrics */}
      <section aria-label="Key metrics">
        <HeadlineMetrics
          kpis={dashboard?.kpis ?? []}
          definitions={definitions}
          alertEvents={alertEvents}
          historyMap={historyMap}
          comparisonMap={comparisonMap}
          compareEnabled={compareEnabled}
          isLoading={isLoading}
        />
      </section>

      <Separator className="my-6" />

      {/* Domain Scorecards */}
      <section aria-label="Domain scorecards">
        <DomainScorecardGrid
          kpis={dashboard?.kpis ?? []}
          definitions={definitions}
          alerts={alertEvents}
          historyMap={historyMap}
          isLoading={isLoading}
        />
      </section>

      <Separator className="my-6" />

      {/* Alerts Feed */}
      <section aria-label="Active alerts">
        <AlertsFeed alerts={alertEvents} />
      </section>

      {/* Quick Insights */}
      {dashboard?.kpis && dashboard.kpis.length > 0 && (
        <section aria-label="Key changes">
          <Separator className="my-4" />
          <QuickInsights kpis={dashboard.kpis} definitions={definitions} />
        </section>
      )}
    </main>
  );
}

export default { Component };
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/dashboard/ExecutiveDashboard.tsx
git commit -m "feat(executive): add ExecutiveDashboard page component"
```

---

## Task 14: Router Changes

**Files:**
- Modify: `src/frontend/src/app/router.tsx`

**Dependencies:** Task 13

- [ ] **Step 1: Update router**

In `src/frontend/src/app/router.tsx`:

1. Keep the existing `DashboardPage` lazy import.
2. Add a new lazy import for `ExecutiveDashboard`.
3. Change the `index` route to point to `ExecutiveDashboard`.
4. Add a new route for `/dashboard/ops` pointing to `DashboardPage`.

Change the children array of the protected route block:

Replace:
```ts
{ index: true, element: <DashboardPage /> },
```

With:
```ts
{ index: true, lazy: () => import('@/features/dashboard/ExecutiveDashboard') },
{ path: 'dashboard/ops', lazy: () => import('@/features/dashboard/DashboardPage') },
```

Also remove the now-unused top-level import:
```ts
// DELETE this line:
const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'));
```

Note: `DashboardPage` already uses `export function Component()` (React Router lazy convention), so the `lazy:` pattern works directly.

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 3: Verify routes manually**

Start dev server: `cd src/frontend && npm run dev`
- Navigate to `http://localhost:3000/` → should show ExecutiveDashboard
- Navigate to `http://localhost:3000/dashboard/ops` → should show the original DashboardPage
- All other routes should work as before

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/app/router.tsx
git commit -m "feat(router): add executive dashboard at / and move operational to /dashboard/ops"
```

---

## Task 15: Sidebar — Role-Aware Navigation

**Files:**
- Modify: `src/frontend/src/components/layout/Sidebar.tsx`
- Modify: `src/frontend/src/locales/en/navigation.json`

**Dependencies:** Tasks 2, 3

This is the most complex modification — read the full Sidebar.tsx first and understand its structure before making changes.

- [ ] **Step 1: Read the current Sidebar.tsx**

Read: `src/frontend/src/components/layout/Sidebar.tsx`
Understand:
- How `navGroups` are defined (array of group objects with label + items)
- How `isActive(path)` works
- How permission checks filter groups
- The collapsed/expanded state logic

- [ ] **Step 2: Add executive sidebar variant**

At the top of the Sidebar component, import the new hooks:

```ts
import { useAuth } from '@/hooks/useAuth';
import { useUiStore } from '@/stores/uiStore';
```

Create a separate `executiveNavGroups` constant defined alongside the existing `navGroups`. This should follow the exact same structure as `navGroups` but with the simplified executive items from spec Section 5.

Add a conditional that decides which nav groups to render:

```ts
const { isExecutive } = useAuth();
const showFullNav = useUiStore((s) => s.showFullNav);
const toggleFullNav = useUiStore((s) => s.toggleFullNav);

const activeGroups = isExecutive && !showFullNav ? executiveNavGroups : operationalNavGroups;
```

Where `operationalNavGroups` is the existing `navGroups` with the Dashboard link updated from `/` to `/dashboard/ops`.

At the bottom of the executive sidebar (before the version display), add:

```tsx
{isExecutive && (
  <button
    onClick={toggleFullNav}
    className="w-full px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors text-left"
  >
    {showFullNav ? t('navigation:items.hideFullNav') : t('navigation:items.showFullNav')}
  </button>
)}
```

**Critical:** Also update the operational sidebar's Dashboard link from `path: '/'` to `path: '/dashboard/ops'` so operational users navigate correctly.

- [ ] **Step 3: Update navigation.json translations**

Add the missing translation keys to `src/frontend/src/locales/en/navigation.json` (from Task 6 Step 2) if not already done.

- [ ] **Step 4: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 5: Manual verification**

Start dev server and verify:
1. User without executive role → sees original sidebar with "Dashboard" linking to `/dashboard/ops`
2. User with executive role → sees simplified sidebar with "Executive Summary" linking to `/`
3. "Show full navigation" toggle works and resets on page refresh
4. All sidebar links navigate correctly

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/components/layout/Sidebar.tsx src/frontend/src/locales/en/navigation.json
git commit -m "feat(sidebar): add role-aware executive sidebar with full nav toggle"
```

---

## Task 16: i18n — German and Russian Translations

**Files:**
- Create: `src/frontend/src/locales/de/executive.json`
- Create: `src/frontend/src/locales/ru/executive.json`
- Modify: `src/frontend/src/locales/de/navigation.json`
- Modify: `src/frontend/src/locales/ru/navigation.json`

- [ ] **Step 1: Create German executive translations**

Copy `src/frontend/src/locales/en/executive.json` and translate all values to German. Key translations:
- "Executive Summary" → "Geschäftsführungs-Übersicht"
- "Free Cash Flow" → "Freier Cashflow"
- "Headcount" → "Mitarbeiterzahl"
- "vs Prior" → "gg. Vorjahr"
- "All metrics within target ranges" → "Alle Kennzahlen im Zielbereich"
- "Data collection in progress" → "Datenerfassung läuft"
- "No Entity Selected" → "Keine Gesellschaft ausgewählt"

- [ ] **Step 2: Create Russian executive translations**

Copy `src/frontend/src/locales/en/executive.json` and translate all values to Russian.

- [ ] **Step 3: Update German and Russian navigation.json**

Add the same `groups` and `items` keys as added to the English file in Task 6.

- [ ] **Step 4: Verify build**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/locales/de/ src/frontend/src/locales/ru/
git commit -m "feat(i18n): add German and Russian executive dashboard translations"
```

---

## Task 17: Final Integration Verification

- [ ] **Step 1: Full build check**

Run: `cd src/frontend && npm run build`
Expected: Build succeeds with zero errors.

- [ ] **Step 2: Lint check**

Run: `cd src/frontend && npm run lint`
Expected: No new lint errors.

- [ ] **Step 3: Manual smoke test**

Start dev server: `cd src/frontend && npm run dev`

Verify:
1. `/` renders ExecutiveDashboard with headline metrics, domain scorecards, alerts, insights
2. `/dashboard/ops` renders the original operational dashboard
3. Period selector (MTD/QTD/YTD) updates URL params and refetches data
4. "vs Prior" toggle shows comparison values (if data exists)
5. Operations domain row shows coming soon placeholders
6. Clicking a KPI/domain row navigates to the correct domain page
7. Alert feed shows critical + warning alerts with "View" links
8. Sidebar shows executive variant for executive roles, operational for others
9. "Show full navigation" toggle expands to full sidebar
10. Theme toggle (light/dark) works on executive dashboard
11. Language switch works (en/de/ru)
12. Mobile responsive: headline strip scrolls horizontally, scorecard stacks
13. Empty state shown when no entity selected

- [ ] **Step 4: Commit any fixes**

If any fixes were needed, commit them.

```bash
git add -A
git commit -m "fix(executive): address integration issues found during smoke test"
```

---

## Dependency Graph

```
Task 1 (queryKeys + alert hook) ─┐
Task 2 (isExecutive)             │
Task 3 (showFullNav)             │
Task 4 (executive-config) ───────┼── Task 5 (useExecutiveDashboard)
                                 │
Task 6 (i18n en) ────────────────┤
                                 │
Task 7 (PeriodSelector) ─────────┤
Task 8 (KpiChip + Placeholder) ──┤
                                 ├── Task 9 (HeadlineMetrics)
                                 ├── Task 10 (DomainScorecard + Grid)
                                 ├── Task 11 (AlertsFeed)
                                 ├── Task 12 (QuickInsights)
                                 │
                                 └── Task 13 (ExecutiveDashboard page)
                                         │
                                         ├── Task 14 (Router)
                                         │
Task 2 + Task 3 ─────────────────────── Task 15 (Sidebar)
                                         │
                                         ├── Task 16 (i18n de/ru)
                                         │
                                         └── Task 17 (Integration verification)
```

Tasks 1–4 and 6–8 can all run in parallel. Tasks 9–12 can run in parallel after 5 and 8. Task 13 needs 5–12. Tasks 14–16 need 13. Task 17 is last.
