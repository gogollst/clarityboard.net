import { Link } from 'react-router-dom';
import {
  Banknote,
  BarChart3,
  FileSpreadsheet,
  FlaskConical,
} from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useKpiDashboard, useKpiDefinitions } from '@/hooks/useKpis';
import KpiGrid from '@/components/kpi/KpiGrid';
import AlertBanner from '@/components/kpi/AlertBanner';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import type { KpiDefinition } from '@/types/kpi';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

type Domain = KpiDefinition['domain'];

const DOMAIN_TABS: { value: Domain; label: string }[] = [
  { value: 'financial', label: 'Financial' },
  { value: 'sales', label: 'Sales' },
  { value: 'marketing', label: 'Marketing' },
  { value: 'hr', label: 'HR' },
  { value: 'general', label: 'General' },
];

function buildKpiCards(
  domain: Domain,
  definitions: KpiDefinition[] | undefined,
  snapshots: ReturnType<typeof useKpiDashboard>['data'],
) {
  const defs = definitions?.filter((d) => d.domain === domain) ?? [];
  return defs.map((def) => {
    const snapshot = snapshots?.kpis.find((k) => k.kpiId === def.id);
    return {
      kpiId: def.id,
      name: def.name,
      value: snapshot?.value ?? 0,
      previousValue: snapshot?.previousValue ?? undefined,
      changePct: snapshot?.changePct ?? undefined,
      unit: def.unit,
      direction: def.direction,
    };
  });
}

// ---------------------------------------------------------------------------
// DashboardPage
// ---------------------------------------------------------------------------

export default function DashboardPage() {
  const { selectedEntityId, selectedEntity } = useEntity();
  const { data: dashboard, isLoading } = useKpiDashboard(selectedEntityId);
  const { data: definitions } = useKpiDefinitions();

  // -----------------------------------------------------------------------
  // Loading state
  // -----------------------------------------------------------------------

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="mb-2 h-8 w-48" />
          <Skeleton className="h-4 w-64" />
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  // -----------------------------------------------------------------------
  // Empty state (no entity selected)
  // -----------------------------------------------------------------------

  if (!selectedEntityId) {
    return (
      <EmptyState
        title="No Entity Selected"
        description="Select a legal entity from the sidebar to view your KPI dashboard."
      />
    );
  }

  // -----------------------------------------------------------------------
  // Render
  // -----------------------------------------------------------------------

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description={selectedEntity?.name ?? undefined}
      />

      {/* Alerts */}
      {dashboard?.alerts && dashboard.alerts.length > 0 && (
        <AlertBanner alerts={dashboard.alerts} />
      )}

      {/* Domain tabs */}
      <Tabs defaultValue="financial">
        <TabsList>
          {DOMAIN_TABS.map((tab) => (
            <TabsTrigger key={tab.value} value={tab.value}>
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>

        {DOMAIN_TABS.map((tab) => {
          const kpis = buildKpiCards(tab.value, definitions, dashboard);
          return (
            <TabsContent key={tab.value} value={tab.value}>
              {kpis.length > 0 ? (
                <KpiGrid kpis={kpis} />
              ) : (
                <EmptyState
                  title={`No ${tab.label} KPIs`}
                  description={`No ${tab.label.toLowerCase()} KPIs have been configured yet.`}
                />
              )}
            </TabsContent>
          );
        })}
      </Tabs>

      {/* Quick actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            <Button variant="outline" asChild>
              <Link to="/cashflow">
                <Banknote className="h-4 w-4" />
                Cash Flow
              </Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to="/scenarios">
                <FlaskConical className="h-4 w-4" />
                Scenarios
              </Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to="/datev">
                <FileSpreadsheet className="h-4 w-4" />
                DATEV Export
              </Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to="/kpis/financial">
                <BarChart3 className="h-4 w-4" />
                Financial Details
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
