import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  TrendingUp,
  DollarSign,
  Users,
  Megaphone,
  Activity,
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
} from 'lucide-react';
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

const mainNavGroups: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      { label: 'Dashboard', path: '/', icon: LayoutDashboard },
    ],
  },
  {
    label: 'KPIs',
    items: [
      { label: 'Financial', path: '/kpis/financial', icon: DollarSign },
      { label: 'Sales', path: '/kpis/sales', icon: TrendingUp },
      { label: 'Marketing', path: '/kpis/marketing', icon: Megaphone },
      { label: 'HR', path: '/kpis/hr', icon: Users },
    ],
  },
  {
    label: 'Cash Flow',
    items: [
      { label: 'Overview', path: '/cashflow', icon: Activity },
      { label: 'Forecast', path: '/cashflow/forecast', icon: BarChart3 },
    ],
  },
  {
    label: 'Planning',
    items: [
      { label: 'Scenarios', path: '/scenarios', icon: Landmark },
      { label: 'Documents', path: '/documents', icon: FileText },
      { label: 'Budget', path: '/budget', icon: PiggyBank },
      { label: 'Assets', path: '/assets', icon: Building2 },
      { label: 'DATEV Export', path: '/datev', icon: FileSpreadsheet },
    ],
  },
];

const hrNavGroup: NavGroup = {
  label: 'HR',
  items: [
    { label: 'Mitarbeiter', path: '/hr/employees', icon: Users },
    { label: 'Urlaubsanträge', path: '/hr/leave/requests', icon: Calendar },
    { label: 'Reisekosten', path: '/hr/travel', icon: Plane },
    { label: 'Beurteilungen', path: '/hr/reviews', icon: Star },
  ],
};

const adminNavGroup: NavGroup = {
  label: 'Admin',
  items: [
    { label: 'Users', path: '/admin/users', icon: Users },
    { label: 'Entities', path: '/admin/entities', icon: Building2 },
    { label: 'Webhooks', path: '/admin/webhooks', icon: Webhook },
    { label: 'Audit Log', path: '/admin/audit', icon: ClipboardList },
    { label: 'Mail Config', path: '/admin/mail', icon: Mail },
  ],
};

const aiAdminNavGroup: NavGroup = {
  label: 'AI Management',
  items: [
    { label: 'Providers', path: '/admin/ai/providers', icon: KeyRound },
    { label: 'Prompts', path: '/admin/ai/prompts', icon: MessageSquareCode },
    { label: 'Call Logs', path: '/admin/ai/logs', icon: ScrollText },
  ],
};

export default function Sidebar() {
  const location = useLocation();
  const { hasPermission } = useAuth();
  const { sidebarOpen, toggleSidebar } = useUiStore();
  const { data: versionInfo } = useVersion();

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    return location.pathname.startsWith(path);
  };

  const showAdmin = hasPermission('admin.*');
  const showHr = hasPermission('hr.view');

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

        {showHr && (
          <>
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={hrNavGroup}
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
              group={adminNavGroup}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
            <div
              className="mx-3 my-2 border-t"
              style={{ borderColor: 'var(--color-sidebar-border)' }}
            />
            <NavGroupSection
              group={aiAdminNavGroup}
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
          aria-label={sidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
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
