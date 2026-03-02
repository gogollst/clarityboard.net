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
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/useAuth';
import { useUiStore } from '@/stores/uiStore';
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

const adminNavGroup: NavGroup = {
  label: 'Admin',
  items: [
    { label: 'Users', path: '/admin/users', icon: Users },
    { label: 'Entities', path: '/admin/entities', icon: Building2 },
    { label: 'Webhooks', path: '/admin/webhooks', icon: Webhook },
    { label: 'Audit Log', path: '/admin/audit', icon: ClipboardList },
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

  const isActive = (path: string) => {
    if (path === '/') {
      return location.pathname === '/';
    }
    return location.pathname.startsWith(path);
  };

  const showAdmin = hasPermission('admin.*');

  return (
    <aside
      className={cn(
        'flex h-full flex-col border-r border-border bg-card transition-all duration-200',
        sidebarOpen ? 'w-64' : 'w-16'
      )}
    >
      {/* Logo */}
      <div className="flex h-14 items-center border-b border-border px-4">
        <Link to="/" className="flex items-center gap-2 overflow-hidden">
          <BarChart3 className="h-6 w-6 shrink-0 text-primary" />
          {sidebarOpen && (
            <span className="text-lg font-semibold tracking-tight">
              Clarity Board
            </span>
          )}
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4">
        {mainNavGroups.map((group) => (
          <NavGroupSection
            key={group.label}
            group={group}
            collapsed={!sidebarOpen}
            isActive={isActive}
          />
        ))}

        {showAdmin && (
          <>
            <div className="mx-4 my-2 border-t border-border" />
            <NavGroupSection
              group={adminNavGroup}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
            <div className="mx-4 my-2 border-t border-border" />
            <NavGroupSection
              group={aiAdminNavGroup}
              collapsed={!sidebarOpen}
              isActive={isActive}
            />
          </>
        )}
      </nav>

      {/* Collapse Toggle */}
      <div className="border-t border-border p-2">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleSidebar}
          className="w-full"
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
    <div className="mb-2">
      {!collapsed && (
        <div className="px-4 py-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">
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
                  'flex items-center gap-3 rounded-md px-2 py-2 text-sm font-medium transition-colors',
                  active
                    ? 'bg-primary/10 text-primary'
                    : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                  collapsed && 'justify-center'
                )}
                title={collapsed ? item.label : undefined}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {!collapsed && <span>{item.label}</span>}
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
