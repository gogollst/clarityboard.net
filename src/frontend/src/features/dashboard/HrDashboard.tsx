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
// HrDashboard (named export for lazy route)
// ---------------------------------------------------------------------------

export function Component() {
  const { selectedEntityId, selectedEntity } = useEntity();
  const [dateRange, setDateRange] = useState<DateRange>('12m');

  const { startDate, endDate } = useMemo(() => computeDates(dateRange), [dateRange]);

  // Data fetching
  const { data: dashboard, isLoading } = useKpiDashboard(selectedEntityId);
  const { data: definitions } = useKpiDefinitions();

  // Headcount trend history
  const { data: headcountHistory } = useKpiHistory(
    selectedEntityId,
    'headcount',
    startDate,
    endDate,
  );

  // Turnover trend history
  const { data: turnoverHistory } = useKpiHistory(
    selectedEntityId,
    'turnover_rate',
    startDate,
    endDate,
  );

  // Retention trend history
  const { data: retentionHistory } = useKpiHistory(
    selectedEntityId,
    'retention_rate',
    startDate,
    endDate,
  );

  // -----------------------------------------------------------------------
  // KPI cards for HR domain
  // -----------------------------------------------------------------------

  const hrKpis = useMemo(() => {
    const defs = definitions?.filter((d) => d.domain === 'hr') ?? [];
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
  // Headcount trend data (area chart)
  // -----------------------------------------------------------------------

  const headcountTrendData = useMemo(() => {
    if (!headcountHistory) return [];
    const monthMap = new Map<string, number>();
    for (const snap of headcountHistory) {
      const month = snap.snapshotDate.slice(0, 7);
      monthMap.set(month, snap.value);
    }
    return Array.from(monthMap.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([month, value]) => ({ month, Headcount: value }));
  }, [headcountHistory]);

  // -----------------------------------------------------------------------
  // Turnover vs Retention bar chart data
  // -----------------------------------------------------------------------

  const turnoverVsRetentionData = useMemo(() => {
    if (!turnoverHistory && !retentionHistory) return [];

    const monthMap = new Map<string, { month: string; Turnover: number; Retention: number }>();

    for (const snap of turnoverHistory ?? []) {
      const month = snap.snapshotDate.slice(0, 7);
      if (!monthMap.has(month)) {
        monthMap.set(month, { month, Turnover: 0, Retention: 0 });
      }
      monthMap.get(month)!.Turnover = snap.value;
    }

    for (const snap of retentionHistory ?? []) {
      const month = snap.snapshotDate.slice(0, 7);
      if (!monthMap.has(month)) {
        monthMap.set(month, { month, Turnover: 0, Retention: 0 });
      }
      monthMap.get(month)!.Retention = snap.value;
    }

    return Array.from(monthMap.values()).sort((a, b) =>
      a.month.localeCompare(b.month),
    );
  }, [turnoverHistory, retentionHistory]);

  // -----------------------------------------------------------------------
  // Cost breakdown donut data (from HR cost KPI components)
  // -----------------------------------------------------------------------

  const costBreakdownData = useMemo(() => {
    const costKpi = dashboard?.kpis.find(
      (k) => k.kpiId === 'hr_cost_breakdown',
    );

    if (costKpi?.components && Object.keys(costKpi.components).length > 0) {
      const colorMap: Record<string, string> = {
        salary: '#3b82f6',
        benefits: '#10b981',
        recruiting: '#f59e0b',
        training: '#8b5cf6',
      };
      return Object.entries(costKpi.components).map(([category, value]) => ({
        name: category.charAt(0).toUpperCase() + category.slice(1),
        value,
        color: colorMap[category] ?? undefined,
      }));
    }

    // Fallback: derive from individual KPIs if available
    const getKpiValue = (kpiId: string) =>
      dashboard?.kpis.find((k) => k.kpiId === kpiId)?.value ?? 0;

    const salary = getKpiValue('salary_cost');
    const benefits = getKpiValue('benefits_cost');
    const recruiting = getKpiValue('recruiting_cost');
    const training = getKpiValue('training_cost');

    // Only show if we have at least some data
    if (salary === 0 && benefits === 0 && recruiting === 0 && training === 0) {
      return [
        { name: 'Salary', value: 0, color: '#3b82f6' },
        { name: 'Benefits', value: 0, color: '#10b981' },
        { name: 'Recruiting', value: 0, color: '#f59e0b' },
        { name: 'Training', value: 0, color: '#8b5cf6' },
      ];
    }

    return [
      { name: 'Salary', value: salary, color: '#3b82f6' },
      { name: 'Benefits', value: benefits, color: '#10b981' },
      { name: 'Recruiting', value: recruiting, color: '#f59e0b' },
      { name: 'Training', value: training, color: '#8b5cf6' },
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
        description="Select a legal entity to view HR data."
      />
    );
  }

  // -----------------------------------------------------------------------
  // Render
  // -----------------------------------------------------------------------

  return (
    <div className="space-y-6">
      <PageHeader
        title="HR Dashboard"
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
      {hrKpis.length > 0 ? (
        <KpiGrid kpis={hrKpis} />
      ) : (
        <EmptyState
          title="No HR KPIs"
          description="HR KPIs have not been configured yet."
        />
      )}

      {/* Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Headcount Trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Headcount Trend</CardTitle>
          </CardHeader>
          <CardContent>
            {headcountTrendData.length > 0 ? (
              <AreaChart
                data={headcountTrendData}
                categories={['Headcount']}
                index="month"
                valueFormatter={(v) => formatNumber(v)}
                colors={['#3b82f6']}
                showLegend={false}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No headcount history available.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Turnover vs Retention */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Turnover vs Retention</CardTitle>
          </CardHeader>
          <CardContent>
            {turnoverVsRetentionData.length > 0 ? (
              <BarChart
                data={turnoverVsRetentionData}
                categories={['Turnover', 'Retention']}
                index="month"
                valueFormatter={(v) => formatPercent(v)}
                colors={['#ef4444', '#10b981']}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No turnover/retention history available.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Cost Breakdown */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">HR Cost Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <DonutChart
              data={costBreakdownData}
              valueFormatter={(v) => formatCurrency(v)}
              showLabel
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
