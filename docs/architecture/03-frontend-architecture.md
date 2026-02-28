# Frontend Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Technology Decisions

| Concern | Choice | Rationale |
|---------|--------|-----------|
| **Framework** | React 19 + TypeScript 5 | Component model, ecosystem, type safety |
| **Build** | Vite 6 | Sub-second HMR, ESBuild for dev, Rollup for prod |
| **Styling** | Tailwind CSS 4 | Utility-first, treeshaking, consistent design tokens |
| **Base Components** | Shadcn/ui | Not a dependency (copy into project), accessible, composable |
| **Charts** | Tremor 3 | Built for React+Tailwind, 15+ chart types, interactive |
| **Server State** | TanStack Query 5 | Cache, dedup, auto-refetch, optimistic updates, devtools |
| **Client State** | Zustand 5 | Minimal boilerplate, TypeScript native, subscriptions |
| **Routing** | React Router 7 | Nested routes, data loaders, breadcrumbs |
| **Forms** | React Hook Form 7 + Zod 3 | Uncontrolled performance, schema validation |
| **i18n** | react-i18next 15 | Namespace-based, lazy loading, interpolation |
| **Real-time** | @microsoft/signalr 10 | SignalR client, auto-reconnect, typed hubs |
| **Date** | date-fns 4 | Tree-shakeable, immutable, locale support |
| **Icons** | Lucide React | Consistent, lightweight, Shadcn default |
| **Testing** | Vitest + Testing Library + Playwright | Fast unit, accessible integration, E2E |

---

## 2. Directory Structure

```
src/frontend/
├── public/
│   ├── favicon.ico
│   └── locales/
│       ├── de/
│       │   ├── common.json
│       │   ├── financial.json
│       │   ├── sales.json
│       │   └── ...
│       └── en/
│           └── ...
├── src/
│   ├── app/
│   │   ├── App.tsx                    # Root component
│   │   ├── router.tsx                 # Route definitions
│   │   ├── providers.tsx              # QueryClient, Auth, i18n, Theme
│   │   └── layouts/
│   │       ├── DashboardLayout.tsx    # Sidebar + header + main area
│   │       ├── AuthLayout.tsx         # Login/register pages
│   │       └── MinimalLayout.tsx      # Public pages
│   │
│   ├── components/
│   │   ├── ui/                        # Shadcn components (copied in)
│   │   │   ├── button.tsx
│   │   │   ├── card.tsx
│   │   │   ├── dialog.tsx
│   │   │   ├── dropdown-menu.tsx
│   │   │   ├── input.tsx
│   │   │   ├── select.tsx
│   │   │   ├── table.tsx
│   │   │   ├── tabs.tsx
│   │   │   ├── toast.tsx
│   │   │   └── ...
│   │   ├── charts/                    # Tremor chart wrappers
│   │   │   ├── AreaChartCard.tsx
│   │   │   ├── BarChartCard.tsx
│   │   │   ├── DonutChartCard.tsx
│   │   │   ├── LineChartCard.tsx
│   │   │   ├── WaterfallChart.tsx     # Custom: Cash flow waterfall
│   │   │   ├── FunnelChart.tsx        # Custom: Sales funnel
│   │   │   └── TornadoChart.tsx       # Custom: Sensitivity analysis
│   │   ├── kpi/
│   │   │   ├── KpiCard.tsx            # Single KPI display
│   │   │   ├── KpiGrid.tsx            # Grid of KPI cards
│   │   │   ├── KpiSparkline.tsx       # Inline trend line
│   │   │   ├── KpiDrillDown.tsx       # Drill-down modal/page
│   │   │   └── KpiCompare.tsx         # Side-by-side comparison
│   │   ├── layout/
│   │   │   ├── Sidebar.tsx
│   │   │   ├── Header.tsx
│   │   │   ├── EntitySelector.tsx     # Entity switching dropdown
│   │   │   ├── DateRangeSelector.tsx
│   │   │   ├── AlertBanner.tsx
│   │   │   └── Breadcrumbs.tsx
│   │   └── shared/
│   │       ├── DataTable.tsx          # Sortable, filterable table
│   │       ├── FileUpload.tsx         # Drag-and-drop upload
│   │       ├── ConfirmDialog.tsx
│   │       ├── LoadingSkeleton.tsx
│   │       ├── ErrorBoundary.tsx
│   │       ├── EmptyState.tsx
│   │       └── MoneyDisplay.tsx       # Formatted currency display
│   │
│   ├── features/                      # Feature-based modules
│   │   ├── dashboard/
│   │   │   ├── DashboardPage.tsx
│   │   │   ├── FinanceDashboard.tsx
│   │   │   ├── SalesDashboard.tsx
│   │   │   ├── MarketingDashboard.tsx
│   │   │   ├── HrDashboard.tsx
│   │   │   └── ExecutiveDashboard.tsx
│   │   ├── financial/
│   │   │   ├── ProfitabilityView.tsx
│   │   │   ├── LiquidityView.tsx
│   │   │   ├── ReturnsView.tsx
│   │   │   └── TaxView.tsx
│   │   ├── cashflow/
│   │   │   ├── CashFlowOverview.tsx
│   │   │   ├── ForecastView.tsx
│   │   │   ├── WorkingCapitalView.tsx
│   │   │   └── CurrencyExposure.tsx
│   │   ├── scenarios/
│   │   │   ├── ScenarioList.tsx
│   │   │   ├── ScenarioCreate.tsx
│   │   │   ├── ScenarioDetail.tsx
│   │   │   ├── ScenarioCompare.tsx
│   │   │   ├── SensitivityAnalysis.tsx
│   │   │   └── MonteCarloResults.tsx
│   │   ├── documents/
│   │   │   ├── DocumentUpload.tsx
│   │   │   ├── DocumentProcessing.tsx
│   │   │   ├── BookingSuggestion.tsx
│   │   │   └── DocumentArchive.tsx
│   │   ├── budget/
│   │   │   ├── BudgetOverview.tsx
│   │   │   ├── PlanVsActual.tsx
│   │   │   ├── DepartmentBudget.tsx
│   │   │   └── BudgetForecast.tsx
│   │   ├── datev/
│   │   │   ├── DatevExportPage.tsx
│   │   │   ├── ExportHistory.tsx
│   │   │   └── ValidationReport.tsx
│   │   ├── assets/
│   │   │   ├── AssetRegister.tsx
│   │   │   ├── DepreciationSchedule.tsx
│   │   │   └── AssetDisposal.tsx
│   │   ├── admin/
│   │   │   ├── UserManagement.tsx
│   │   │   ├── EntityConfig.tsx
│   │   │   ├── WebhookConfig.tsx
│   │   │   ├── RoleManagement.tsx
│   │   │   └── AuditLog.tsx
│   │   └── auth/
│   │       ├── LoginPage.tsx
│   │       ├── TwoFactorPage.tsx
│   │       ├── ForgotPasswordPage.tsx
│   │       └── ProfilePage.tsx
│   │
│   ├── hooks/
│   │   ├── useAuth.ts                 # Authentication state + actions
│   │   ├── useEntity.ts              # Current entity context
│   │   ├── useKpi.ts                 # KPI data fetching helpers
│   │   ├── useSignalR.ts             # SignalR connection management
│   │   ├── useAlerts.ts              # Active alerts subscription
│   │   ├── usePermission.ts          # RBAC permission checks
│   │   ├── useDateRange.ts           # Date range state
│   │   └── useDebounce.ts            # Input debouncing
│   │
│   ├── lib/
│   │   ├── api.ts                     # Axios instance + interceptors
│   │   ├── auth.ts                    # Token management
│   │   ├── signalr.ts                # SignalR connection factory
│   │   ├── format.ts                 # Number, date, currency formatting
│   │   ├── kpi-formulas.ts           # Client-side KPI calculations
│   │   └── utils.ts                  # General utilities
│   │
│   ├── stores/
│   │   ├── authStore.ts              # User session, tokens
│   │   ├── entityStore.ts            # Selected entity, entity list
│   │   ├── uiStore.ts                # Sidebar state, theme, locale
│   │   ├── alertStore.ts             # Active alerts
│   │   └── filterStore.ts            # Active filters, date range
│   │
│   ├── types/
│   │   ├── api.ts                     # API response types
│   │   ├── kpi.ts                    # KPI-related types
│   │   ├── entity.ts                 # Entity types
│   │   ├── accounting.ts             # Journal entry types
│   │   ├── scenario.ts              # Scenario types
│   │   └── user.ts                   # User, role, permission types
│   │
│   └── i18n/
│       ├── config.ts                  # i18next configuration
│       └── index.ts                  # Export
│
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.ts
└── vitest.config.ts
```

---

## 3. State Management

### Server State (TanStack Query)

```typescript
// All server data is managed through React Query
// Examples:

// KPI Dashboard data
const { data, isLoading } = useQuery({
  queryKey: ['dashboard', entityId, role],
  queryFn: () => api.getDashboard(entityId, role),
  staleTime: 30_000,        // Consider fresh for 30s
  refetchInterval: 60_000,  // Refetch every 60s as fallback
});

// KPI History (with pagination)
const { data } = useQuery({
  queryKey: ['kpi-history', entityId, kpiId, dateRange],
  queryFn: () => api.getKpiHistory(entityId, kpiId, dateRange),
  staleTime: 5 * 60_000,    // 5 min (historical data changes slowly)
});

// Mutations with optimistic updates
const createEntry = useMutation({
  mutationFn: api.createJournalEntry,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['kpi-history'] });
  },
});
```

### Client State (Zustand)

```typescript
// Minimal client-only state
interface UiStore {
  sidebarOpen: boolean;
  theme: 'light' | 'dark' | 'system';
  locale: 'de' | 'en';
  toggleSidebar: () => void;
  setTheme: (theme: UiStore['theme']) => void;
  setLocale: (locale: UiStore['locale']) => void;
}

const useUiStore = create<UiStore>((set) => ({
  sidebarOpen: true,
  theme: 'system',
  locale: 'en',
  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  setTheme: (theme) => set({ theme }),
  setLocale: (locale) => set({ locale }),
}));
```

### Real-Time Updates (SignalR)

```typescript
// SignalR hook for real-time KPI updates
function useKpiUpdates(entityId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/kpi', { accessTokenFactory: () => getAccessToken() })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connection.on('KpiUpdated', (update: KpiUpdateDto) => {
      // Update React Query cache directly (no refetch needed)
      queryClient.setQueryData(
        ['dashboard', entityId],
        (old: DashboardDto) => applyKpiUpdate(old, update)
      );
    });

    connection.on('AlertTriggered', (alert: AlertDto) => {
      // Show toast notification
      toast.warning(alert.message);
      // Update alert store
      useAlertStore.getState().addAlert(alert);
    });

    connection.start();
    return () => { connection.stop(); };
  }, [entityId]);
}
```

---

## 4. Routing Structure

```typescript
const router = createBrowserRouter([
  // Public routes
  { path: '/login', element: <LoginPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },

  // Protected routes (require auth)
  {
    path: '/',
    element: <DashboardLayout />,
    children: [
      { index: true, element: <DashboardPage /> },

      // KPI domains
      { path: 'kpis/financial', element: <FinancialKpis /> },
      { path: 'kpis/financial/:kpiId', element: <KpiDrillDown /> },
      { path: 'kpis/sales', element: <SalesKpis /> },
      { path: 'kpis/marketing', element: <MarketingKpis /> },
      { path: 'kpis/hr', element: <HrKpis /> },
      { path: 'kpis/general', element: <GeneralKpis /> },

      // Modules
      { path: 'cashflow', element: <CashFlowOverview /> },
      { path: 'cashflow/forecast', element: <ForecastView /> },
      { path: 'cashflow/working-capital', element: <WorkingCapitalView /> },
      { path: 'scenarios', element: <ScenarioList /> },
      { path: 'scenarios/new', element: <ScenarioCreate /> },
      { path: 'scenarios/:id', element: <ScenarioDetail /> },
      { path: 'scenarios/compare', element: <ScenarioCompare /> },
      { path: 'documents', element: <DocumentArchive /> },
      { path: 'documents/upload', element: <DocumentUpload /> },
      { path: 'budget', element: <BudgetOverview /> },
      { path: 'budget/:departmentId', element: <DepartmentBudget /> },
      { path: 'assets', element: <AssetRegister /> },
      { path: 'datev', element: <DatevExportPage /> },

      // Admin (role-guarded)
      { path: 'admin/users', element: <UserManagement /> },
      { path: 'admin/entities', element: <EntityConfig /> },
      { path: 'admin/webhooks', element: <WebhookConfig /> },
      { path: 'admin/audit', element: <AuditLog /> },
    ],
  },
]);
```

---

## 5. Role-Based UI Rendering

```typescript
// Permission-based component rendering
function DashboardPage() {
  const { role } = useAuth();

  return (
    <div>
      <RoleGuard roles={['finance', 'executive', 'admin']}>
        <FinanceDashboard />
      </RoleGuard>

      <RoleGuard roles={['sales', 'executive', 'admin']}>
        <SalesDashboard />
      </RoleGuard>

      <RoleGuard roles={['marketing', 'executive', 'admin']}>
        <MarketingDashboard />
      </RoleGuard>

      <RoleGuard roles={['hr', 'executive', 'admin']}>
        <HrDashboard />
      </RoleGuard>
    </div>
  );
}

// Entity-scoped data
function useEntityScopedKpis(kpiDomain: string) {
  const { selectedEntityId } = useEntityStore();

  return useQuery({
    queryKey: ['kpis', selectedEntityId, kpiDomain],
    queryFn: () => api.getKpis(selectedEntityId, kpiDomain),
    enabled: !!selectedEntityId,
  });
}
```

---

## 6. Performance Optimization

| Strategy | Implementation |
|----------|---------------|
| **Code Splitting** | `React.lazy()` per feature module, route-based |
| **Virtualization** | `@tanstack/react-virtual` for large tables (audit log, transactions) |
| **Memoization** | `React.memo` for chart components, `useMemo` for expensive calculations |
| **Stale-While-Revalidate** | React Query staleTime for KPIs (30s dashboard, 5min history) |
| **Optimistic Updates** | Immediate UI update on mutation, rollback on error |
| **Image Optimization** | Lazy loading, WebP format, responsive srcset |
| **Bundle Size** | Tree-shaking, no moment.js, date-fns per-function imports |
| **Caching** | Service worker for static assets, React Query persistence |

---

## Document Navigation

- Previous: [Backend Architecture](./02-backend-architecture.md)
- Next: [Database Architecture](./04-database-architecture.md)
- [Back to Index](./README.md)
