import { useMemo } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  TrendingUp,
  DollarSign,
  Users,
  Megaphone,
  Activity,
  BarChart2,
  BarChart3,
  FileText,
  PiggyBank,
  Building2,
  FileSpreadsheet,
  ChevronLeft,
  ChevronRight,
  Landmark,
  Webhook,
  ClipboardList,
  KeyRound,
  MessageSquareCode,
  ScrollText,
  Mail,
  Calendar,
  Plane,
  Star,
  Shield,
  Clock,
  UserCircle,
  ListChecks,
  BookOpen,
  CalendarRange,
  Download,
  Layers,
  Target,
  Scale,
  PieChart,
  TrendingDown,
  Receipt,
  Handshake,
  ListTree,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/useAuth';
import { useUiStore } from '@/stores/uiStore';
import { useVersion } from '@/hooks/useVersion';
import { Button } from '@/components/ui/button';

interface NavItem {
  label: string;
  path: string;
  icon: React.ElementType;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

export default function Sidebar() {
  const location = useLocation();
  const { hasPermission } = useAuth();
  const { sidebarOpen, toggleSidebar } = useUiStore();
  const { data: versionInfo } = useVersion();
  const { t } = useTranslation(['navigation', 'common']);

  const mainNavGroups = useMemo<NavGroup[]>(() => [
    {
      label: t('navigation:groups.overview'),
      items: [
        { label: t('navigation:items.dashboard'), path: '/', icon: LayoutDashboard },
      ],
    },
    {
      label: t('navigation:groups.kpis'),
      items: [
        { label: t('navigation:items.financial'), path: '/kpis/financial', icon: DollarSign },
        { label: t('navigation:items.sales'), path: '/kpis/sales', icon: TrendingUp },
        { label: t('navigation:items.marketing'), path: '/kpis/marketing', icon: Megaphone },
        { label: t('navigation:items.hrKpi'), path: '/kpis/hr', icon: Users },
      ],
    },
    {
      label: t('navigation:groups.cashFlow'),
      items: [
        { label: t('navigation:items.cashflowOverview'), path: '/cashflow', icon: Activity },
        { label: t('navigation:items.forecast'), path: '/cashflow/forecast', icon: BarChart3 },
      ],
    },
    {
      label: t('navigation:groups.planning'),
      items: [
        { label: t('navigation:items.scenarios'), path: '/scenarios', icon: Landmark },
        { label: t('navigation:items.documents'), path: '/documents', icon: FileText },
        { label: t('navigation:items.budget'), path: '/budget', icon: PiggyBank },
        { label: t('navigation:items.assets'), path: '/assets', icon: Building2 },
        { label: t('navigation:items.datevExport'), path: '/datev', icon: FileSpreadsheet },
      ],
    },
  ], [t]);

  const accountingNavGroupMemo = useMemo<NavGroup>(() => ({
    label: t('navigation:groups.accounting'),
    items: [
      { label: t('navigation:items.chartOfAccounts'), path: '/accounting/accounts', icon: ListTree },
      { label: t('navigation:items.businessPartners'), path: '/accounting/business-partners', icon: Handshake },
      { label: t('navigation:items.journalEntries'), path: '/accounting/journal-entries', icon: BookOpen },
      { label: t('navigation:items.trialBalance'), path: '/accounting/trial-balance', icon: Scale },
      { label: t('navigation:items.balanceSheet'), path: '/accounting/balance-sheet', icon: PieChart },
      { label: t('navigation:items.profitAndLoss'), path: '/accounting/profit-loss', icon: TrendingDown },
      { label: t('navigation:items.vatReconciliation'), path: '/accounting/vat', icon: Receipt },
      { label: t('navigation:items.fiscalPeriods'), path: '/accounting/fiscal-periods', icon: CalendarRange },
      { label: t('navigation:items.accountingDatev'), path: '/accounting/datev/exports', icon: Download },
      { label: t('navigation:items.costCenters'), path: '/accounting/cost-centers', icon: Layers },
      { label: t('navigation:items.accountingScenarios'), path: '/accounting/scenarios', icon: Target },
    ],
  }), [t]);

  const hrNavGroupMemo = useMemo<NavGroup>(() => ({
    label: t('navigation:groups.hr'),
    items: [
      { label: t('navigation:items.mySelf'), path: '/hr/me', icon: UserCircle },
      { label: t('navigation:items.employees'), path: '/hr/employees', icon: Users },
      { label: t('navigation:items.departments'), path: '/hr/departments', icon: Building2 },
      { label: t('navigation:items.leaveRequests'), path: '/hr/leave/requests', icon: Calendar },
      { label: t('navigation:items.worktime'), path: '/hr/worktime', icon: Clock },
      { label: t('navigation:items.travel'), path: '/hr/travel', icon: Plane },
      { label: t('navigation:items.reviews'), path: '/hr/reviews', icon: Star },
      { label: t('navigation:items.onboarding'), path: '/hr/onboarding', icon: ListChecks },
      { label: t('navigation:items.stats'), path: '/hr/stats', icon: BarChart2 },
    ],
  }), [t]);

  const hrAdminNavGroupMemo = useMemo<NavGroup>(() => ({
    label: t('navigation:groups.hrAdmin'),
    items: [
      { label: t('navigation:items.gdprManagement'), path: '/hr/admin/deletions', icon: Shield },
    ],
  }), [t]);

  const adminNavGroupMemo = useMemo<NavGroup>(() => ({
    label: t('navigation:groups.admin'),
    items: [
      { label: t('navigation:items.users'), path: '/admin/users', icon: Users },
      { label: t('navigation:items.entities'), path: '/admin/entities', icon: Building2 },
      { label: t('navigation:items.webhooks'), path: '/admin/webhooks', icon: Webhook },
      { label: t('navigation:items.auditLog'), path: '/admin/audit', icon: ClipboardList },
      { label: t('navigation:items.mailConfig'), path: '/admin/mail', icon: Mail },
    ],
  }), [t]);

  const aiAdminNavGroupMemo = useMemo<NavGroup>(() => ({
    label: t('navigation:groups.aiManagement'),
    items: [
      { label: t('navigation:items.providers'), path: '/admin/ai/providers', icon: KeyRound },
      { label: t('navigation:items.models'), path: '/admin/ai/models', icon: Layers },
      { label: t('navigation:items.prompts'), path: '/admin/ai/prompts', icon: MessageSquareCode },
      { label: t('navigation:items.callLogs'), path: '/admin/ai/logs', icon: ScrollText },
    ],
  }), [t]);

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    return location.pathname.startsWith(path);
  };

  const showAdmin = hasPermission('admin.*');
  const showAccounting = hasPermission('accounting.view');
  const showHr = hasPermission('hr.view');
  const showHrAdmin = hasPermission('hr.admin');

  return (
    <aside
      className={cn(
        'sidebar-gradient flex h-full flex-col transition-all duration-200',
        sidebarOpen ? 'w-64' : 'w-16'
      )}
    >
      {/* Logo */}
      <div
        className="flex h-14 items-center border-b px-4"
        style={{ borderColor: 'var(--color-sidebar-border)' }}
      >
        <Link to="/" className="flex items-center gap-3 overflow-hidden">
          <img src="/cblogo.svg" alt="ClarityBoard" className="h-7 w-7 shrink-0" />
          {sidebarOpen && (
            <span
              className="font-display text-sm font-medium tracking-tight"
              style={{ color: 'var(--color-sidebar-foreground-active)' }}
            >
              Clarity Board
            </span>
          )}
        </Link>
      </div>

      {/* Navigation */}
      <nav className="sidebar-scrollbar flex-1 overflow-y-auto py-3">
        {mainNavGroups.map((group) => (
          <NavGroupSection
            key={group.label}
            group={group}
            collapsed={!sidebarOpen}
            isActive={isActive}
          />
        ))}

        {showAccounting && (
          <>
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={accountingNavGroupMemo}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
          </>
        )}

        {showHr && (
          <>
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={hrNavGroupMemo}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
          </>
        )}

        {showHrAdmin && (
          <>
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={hrAdminNavGroupMemo}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
          </>
        )}

        {showAdmin && (
          <>
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={adminNavGroupMemo}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={aiAdminNavGroupMemo}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
          </>
        )}
      </nav>

      {/* Version */}
      {sidebarOpen && versionInfo && (
        <div className="px-4 py-2 text-center">
          <span
            className="text-[10px] tabular-nums"
            style={{ color: 'var(--color-sidebar-foreground)', opacity: 0.6 }}
          >
            v{versionInfo.version}
          </span>
        </div>
      )}

      {/* Collapse Toggle */}
      <div
        className="border-t p-2"
        style={{ borderColor: 'var(--color-sidebar-border)' }}
      >
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleSidebar}
          className="w-full"
          style={
            {
              color: 'var(--color-sidebar-foreground)',
              '--tw-bg-opacity': '1',
            } as React.CSSProperties
          }
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLElement).style.background =
              'var(--color-sidebar-item-hover)';
            (e.currentTarget as HTMLElement).style.color =
              'var(--color-sidebar-foreground-active)';
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLElement).style.background = '';
            (e.currentTarget as HTMLElement).style.color =
              'var(--color-sidebar-foreground)';
          }}
          aria-label={sidebarOpen ? t('common:sidebar.collapse') : t('common:sidebar.expand')}
        >
          {sidebarOpen ? (
            <ChevronLeft className="h-4 w-4" />
          ) : (
            <ChevronRight className="h-4 w-4" />
          )}
        </Button>
      </div>
    </aside>
  );
}

function NavGroupSection({
  group,
  collapsed,
  isActive,
}: {
  group: NavGroup;
  collapsed: boolean;
  isActive: (path: string) => boolean;
}) {
  return (
    <div className="mb-1">
      {!collapsed && (
        <div
          className="px-4 pb-1 pt-2 text-[10px] font-medium uppercase tracking-[0.1em]"
          style={{ color: 'var(--color-sidebar-foreground)', opacity: 0.7 }}
        >
          {group.label}
        </div>
      )}
      <ul className="space-y-0.5 px-2">
        {group.items.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.path);
          return (
            <li key={item.path}>
              <Link
                to={item.path}
                className={cn(
                  'flex items-center gap-3 rounded-md px-2 py-2 text-sm font-medium transition-all duration-150',
                  active
                    ? 'bg-[rgba(217,119,87,0.10)] [box-shadow:0_0_0_1px_rgba(217,119,87,0.18),inset_0_0_10px_rgba(217,119,87,0.04)]'
                    : '',
                  collapsed && 'justify-center'
                )}
                style={
                  active
                    ? { color: 'var(--color-sidebar-foreground-active)' }
                    : { color: 'var(--color-sidebar-foreground)' }
                }
                onMouseEnter={(e) => {
                  if (!active) {
                    (e.currentTarget as HTMLElement).style.background =
                      'var(--color-sidebar-item-hover)';
                    (e.currentTarget as HTMLElement).style.color =
                      'var(--color-sidebar-foreground-active)';
                  }
                }}
                onMouseLeave={(e) => {
                  if (!active) {
                    (e.currentTarget as HTMLElement).style.background = '';
                    (e.currentTarget as HTMLElement).style.color =
                      'var(--color-sidebar-foreground)';
                  }
                }}
                title={collapsed ? item.label : undefined}
              >
                <Icon
                  className="h-4 w-4 shrink-0"
                  style={{
                    color: active
                      ? 'var(--color-sidebar-item-active)'
                      : 'var(--color-sidebar-foreground)',
                  }}
                />
                {!collapsed && <span>{item.label}</span>}
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
