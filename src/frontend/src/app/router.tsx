import { createBrowserRouter } from 'react-router-dom';
import { lazy, Suspense } from 'react';

// Layouts
const DashboardLayout = lazy(() => import('./layouts/DashboardLayout'));
const AuthLayout = lazy(() => import('./layouts/AuthLayout'));

// Auth pages
const LoginPage = lazy(() => import('@/features/auth/LoginPage'));

// Dashboard
const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'));

// Loading fallback
function PageLoader() {
  return (
    <div className="flex h-screen items-center justify-center">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
    </div>
  );
}

function SuspenseWrapper({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageLoader />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
  // Public routes
  {
    element: (
      <SuspenseWrapper>
        <AuthLayout />
      </SuspenseWrapper>
    ),
    children: [
      { path: '/login', element: <LoginPage /> },
    ],
  },

  // Protected routes
  {
    path: '/',
    element: (
      <SuspenseWrapper>
        <DashboardLayout />
      </SuspenseWrapper>
    ),
    children: [
      { index: true, element: <DashboardPage /> },

      // KPI domains (lazy loaded)
      { path: 'kpis/financial', lazy: () => import('@/features/financial/ProfitabilityView') },
      { path: 'kpis/sales', lazy: () => import('@/features/dashboard/SalesDashboard') },
      { path: 'kpis/marketing', lazy: () => import('@/features/dashboard/MarketingDashboard') },
      { path: 'kpis/hr', lazy: () => import('@/features/dashboard/HrDashboard') },

      // Modules
      { path: 'cashflow', lazy: () => import('@/features/cashflow/CashFlowOverview') },
      { path: 'cashflow/forecast', lazy: () => import('@/features/cashflow/ForecastView') },
      { path: 'scenarios', lazy: () => import('@/features/scenarios/ScenarioList') },
      { path: 'scenarios/new', lazy: () => import('@/features/scenarios/ScenarioCreate') },
      { path: 'scenarios/:id', lazy: () => import('@/features/scenarios/ScenarioDetail') },
      { path: 'documents', lazy: () => import('@/features/documents/DocumentArchive') },
      { path: 'documents/upload', lazy: () => import('@/features/documents/DocumentUpload') },
      { path: 'budget', lazy: () => import('@/features/budget/BudgetOverview') },
      { path: 'assets', lazy: () => import('@/features/assets/AssetRegister') },
      { path: 'datev', lazy: () => import('@/features/datev/DatevExportPage') },

      // Admin
      { path: 'admin/users', lazy: () => import('@/features/admin/UserManagement') },
      { path: 'admin/entities', lazy: () => import('@/features/admin/EntityConfig') },
      { path: 'admin/webhooks', lazy: () => import('@/features/admin/WebhookConfig') },
      { path: 'admin/audit', lazy: () => import('@/features/admin/AuditLog') },
    ],
  },
]);
