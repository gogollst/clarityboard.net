import { useState, useMemo } from 'react';
import { useEntity } from '@/hooks/useEntity';
import { useKpiDashboard, useKpiDefinitions, useKpiHistory } from '@/hooks/useKpis';
import { formatCurrency, formatPercent, formatNumber } from '@/lib/format';
import KpiGrid from '@/components/kpi/KpiGrid';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import AreaChart from '@/components/charts/AreaChart';
import BarChart from '@/components/charts/BarChart';
import DonutChart from '@/components/charts/DonutChart';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';

// ---------------------------------------------------------------------------
// Date range helpers
// ---------------------------------------------------------------------------

type DateRange = '3m' | '6m' | '12m';

function computeDates(range: DateRange) {
  const end = new Date();
  const start = new Date();
  start.setMonth(start.getMonth() - (range === '3m' ? 3 : range === '6m' ? 6 : 12));
  return {
    startDate: start.toISOString().split('T')[0],
    endDate: end.toISOString().split('T')[0],
  };
}

// ---------------------------------------------------------------------------
// SalesDashboard (named export for lazy route)
// ---------------------------------------------------------------------------

export function Component() {
  const { selectedEntityId, selectedEntity } = useEntity();
  const [dateRange, setDateRange] = useState<DateRange>('12m');

  const { startDate, endDate } = useMemo(() => computeDates(dateRange), [dateRange]);

  // Data fetching
  const { data: dashboard, isLoading } = useKpiDashboard(selectedEntityId);
  const { data: definitions } = useKpiDefinitions();

  // MRR trend history
  const { data: mrrHistory } = useKpiHistory(
    selectedEntityId,
    'mrr',
    startDate,
    endDate,
  );

  // -----------------------------------------------------------------------
  // KPI cards for sales domain
  // -----------------------------------------------------------------------

  const salesKpis = useMemo(() => {
    const defs = definitions?.filter((d) => d.domain === 'sales') ?? [];
    return defs.map((def) => {
      const snapshot = dashboard?.kpis.find((k) => k.kpiId === def.id);
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
  }, [definitions, dashboard]);

  // -----------------------------------------------------------------------
  // MRR trend data (for area chart)
  // -----------------------------------------------------------------------

  const mrrTrendData = useMemo(() => {
    if (!mrrHistory) return [];
    const monthMap = new Map<string, number>();
    for (const snap of mrrHistory) {
      const month = snap.snapshotDate.slice(0, 7);
      monthMap.set(month, snap.value);
    }
    return Array.from(monthMap.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([month, value]) => ({ month, MRR: value }));
  }, [mrrHistory]);

  // -----------------------------------------------------------------------
  // Pipeline funnel data (from dashboard KPI snapshots)
  // -----------------------------------------------------------------------

  const pipelineFunnelData = useMemo(() => {
    const getKpiValue = (kpiId: string) =>
      dashboard?.kpis.find((k) => k.kpiId === kpiId)?.value ?? 0;

    return [
      { stage: 'Leads', Count: getKpiValue('pipeline_leads') },
      { stage: 'Qualified', Count: getKpiValue('pipeline_qualified') },
      { stage: 'Proposal', Count: getKpiValue('pipeline_proposal') },
      { stage: 'Won', Count: getKpiValue('pipeline_won') },
    ];
  }, [dashboard]);

  // -----------------------------------------------------------------------
  // Churn vs Retention donut data
  // -----------------------------------------------------------------------

  const churnRetentionData = useMemo(() => {
    const churn = dashboard?.kpis.find((k) => k.kpiId === 'churn_rate')?.value ?? 0;
    const retention = 100 - churn;
    return [
      { name: 'Retained', value: retention, color: '#10b981' },
      { name: 'Churned', value: churn, color: '#ef4444' },
    ];
  }, [dashboard]);

  // -----------------------------------------------------------------------
  // Loading state
  // -----------------------------------------------------------------------

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  // -----------------------------------------------------------------------
  // Empty state
  // -----------------------------------------------------------------------

  if (!selectedEntityId) {
    return (
      <EmptyState
        title="No Entity Selected"
        description="Select a legal entity to view sales data."
      />
    );
  }

  // -----------------------------------------------------------------------
  // Render
  // -----------------------------------------------------------------------

  return (
    <div className="space-y-6">
      <PageHeader
        title="Sales Dashboard"
        description={selectedEntity?.name ?? undefined}
        actions={
          <div className="flex gap-1 rounded-lg bg-muted p-1">
            {(['3m', '6m', '12m'] as DateRange[]).map((range) => (
              <Button
                key={range}
                variant={dateRange === range ? 'default' : 'ghost'}
                size="sm"
                onClick={() => setDateRange(range)}
              >
                {range === '3m' ? '3M' : range === '6m' ? '6M' : '12M'}
              </Button>
            ))}
          </div>
        }
      />

      {/* KPI cards */}
      {salesKpis.length > 0 ? (
        <KpiGrid kpis={salesKpis} />
      ) : (
        <EmptyState
          title="No Sales KPIs"
          description="Sales KPIs have not been configured yet."
        />
      )}

      {/* Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* MRR Trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">MRR Trend</CardTitle>
          </CardHeader>
          <CardContent>
            {mrrTrendData.length > 0 ? (
              <AreaChart
                data={mrrTrendData}
                categories={['MRR']}
                index="month"
                valueFormatter={(v) => formatCurrency(v)}
                colors={['#3b82f6']}
                showLegend={false}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No MRR history data available.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Pipeline Funnel */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Pipeline Funnel</CardTitle>
          </CardHeader>
          <CardContent>
            <BarChart
              data={pipelineFunnelData}
              categories={['Count']}
              index="stage"
              valueFormatter={(v) => formatNumber(v)}
              colors={['#8b5cf6']}
              showLegend={false}
            />
          </CardContent>
        </Card>

        {/* Churn vs Retention */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Churn vs Retention</CardTitle>
          </CardHeader>
          <CardContent>
            <DonutChart
              data={churnRetentionData}
              valueFormatter={(v) => formatPercent(v)}
              showLabel
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
