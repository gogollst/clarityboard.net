import { createBrowserRouter } from 'react-router-dom';
import { lazy, Suspense } from 'react';

// Layouts
const DashboardLayout = lazy(() => import('./layouts/DashboardLayout'));
const AuthLayout = lazy(() => import('./layouts/AuthLayout'));

// Auth pages
const LoginPage = lazy(() => import('@/features/auth/LoginPage'));
const ForgotPasswordPage = lazy(() => import('@/features/auth/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('@/features/auth/ResetPasswordPage'));
const AcceptInvitationPage = lazy(() => import('@/features/auth/AcceptInvitationPage'));

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
      { path: '/forgot-password', element: <ForgotPasswordPage /> },
      { path: '/reset-password', element: <ResetPasswordPage /> },
      { path: '/invite/accept', element: <AcceptInvitationPage /> },
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

      // Settings
      { path: 'settings', lazy: () => import('@/features/settings/SettingsPage') },

      // Admin
      { path: 'admin/users', lazy: () => import('@/features/admin/UserManagement') },
      { path: 'admin/entities', lazy: () => import('@/features/admin/EntityConfig') },
      { path: 'admin/webhooks', lazy: () => import('@/features/admin/WebhookConfig') },
      { path: 'admin/audit', lazy: () => import('@/features/admin/AuditLog') },
      { path: 'admin/mail', lazy: () => import('@/features/admin/MailConfig') },
      { path: 'admin/auth-config', lazy: () => import('@/features/admin/AuthConfig') },

      // Admin – AI Management
      { path: 'admin/ai/providers', lazy: () => import('@/features/admin/AiProviders') },
      { path: 'admin/ai/prompts', lazy: () => import('@/features/admin/AiPrompts') },
      { path: 'admin/ai/prompts/:promptKey', lazy: () => import('@/features/admin/AiPromptDetail') },
      { path: 'admin/ai/logs', lazy: () => import('@/features/admin/AiCallLogs') },

      // HR
      { path: 'hr/employees', lazy: () => import('@/features/hr/employees/EmployeeList') },
      { path: 'hr/employees/new', lazy: () => import('@/features/hr/employees/EmployeeCreate') },
      { path: 'hr/employees/:id', lazy: () => import('@/features/hr/employees/EmployeeDetail') },
      { path: 'hr/leave/requests', lazy: () => import('@/features/hr/leave/LeaveRequestList') },
      { path: 'hr/worktime/:employeeId', lazy: () => import('@/features/hr/worktime/WorkTimeOverview') },
      { path: 'hr/worktime', lazy: () => import('@/features/hr/worktime/WorkTimeOverview') },
      { path: 'hr/travel', lazy: () => import('@/features/hr/travel/TravelExpenseList') },
      { path: 'hr/travel/new', lazy: () => import('@/features/hr/travel/TravelExpenseCreate') },
      { path: 'hr/travel/:id', lazy: () => import('@/features/hr/travel/TravelExpenseDetail') },
      { path: 'hr/reviews', lazy: () => import('@/features/hr/reviews/ReviewList') },
      { path: 'hr/reviews/:id', lazy: () => import('@/features/hr/reviews/ReviewDetail') },
      { path: 'hr/employees/:employeeId/documents', lazy: () => import('@/features/hr/documents/EmployeeDocuments') },
      { path: 'hr/admin/deletions', lazy: () => import('@/features/hr/admin/DeletionRequests') },
      { path: 'hr/stats', lazy: () => import('@/features/hr/stats/HrDashboard') },
    ],
  },
]);
