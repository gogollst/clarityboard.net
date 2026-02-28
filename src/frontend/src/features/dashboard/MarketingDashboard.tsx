import { useState, useMemo } from 'react';
import { useEntity } from '@/hooks/useEntity';
import { useKpiDashboard, useKpiDefinitions, useKpiHistory } from '@/hooks/useKpis';
import { formatCurrency, formatPercent, formatNumber } from '@/lib/format';
import KpiGrid from '@/components/kpi/KpiGrid';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import LineChart from '@/components/charts/LineChart';
import BarChart from '@/components/charts/BarChart';
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
// MarketingDashboard (named export for lazy route)
// ---------------------------------------------------------------------------

export function Component() {
  const { selectedEntityId, selectedEntity } = useEntity();
  const [dateRange, setDateRange] = useState<DateRange>('12m');

  const { startDate, endDate } = useMemo(() => computeDates(dateRange), [dateRange]);

  // Data fetching
  const { data: dashboard, isLoading } = useKpiDashboard(selectedEntityId);
  const { data: definitions } = useKpiDefinitions();

  // Marketing ROI trend
  const { data: roiHistory } = useKpiHistory(
    selectedEntityId,
    'marketing_roi',
    startDate,
    endDate,
  );

  // -----------------------------------------------------------------------
  // KPI cards for marketing domain
  // -----------------------------------------------------------------------

  const marketingKpis = useMemo(() => {
    const defs = definitions?.filter((d) => d.domain === 'marketing') ?? [];
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
  // Lead funnel data (from dashboard KPI snapshots)
  // -----------------------------------------------------------------------

  const leadFunnelData = useMemo(() => {
    const getKpiValue = (kpiId: string) =>
      dashboard?.kpis.find((k) => k.kpiId === kpiId)?.value ?? 0;

    return [
      { stage: 'Visitors', Count: getKpiValue('website_visitors') },
      { stage: 'Leads', Count: getKpiValue('total_leads') },
      { stage: 'MQLs', Count: getKpiValue('mqls') },
      { stage: 'SQLs', Count: getKpiValue('sqls') },
      { stage: 'Customers', Count: getKpiValue('new_customers') },
    ];
  }, [dashboard]);

  // -----------------------------------------------------------------------
  // Marketing ROI trend data
  // -----------------------------------------------------------------------

  const roiTrendData = useMemo(() => {
    if (!roiHistory) return [];
    const monthMap = new Map<string, number>();
    for (const snap of roiHistory) {
      const month = snap.snapshotDate.slice(0, 7);
      monthMap.set(month, snap.value);
    }
    return Array.from(monthMap.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([month, value]) => ({ month, 'Marketing ROI': value }));
  }, [roiHistory]);

  // -----------------------------------------------------------------------
  // Channel performance data (derived from KPI components if available)
  // -----------------------------------------------------------------------

  const channelPerformanceData = useMemo(() => {
    const channelKpi = dashboard?.kpis.find(
      (k) => k.kpiId === 'channel_performance',
    );

    if (channelKpi?.components && Object.keys(channelKpi.components).length > 0) {
      return Object.entries(channelKpi.components).map(([channel, value]) => ({
        channel,
        Spend: value,
      }));
    }

    // Placeholder structure when no data is available
    return [
      { channel: 'Organic Search', Spend: 0 },
      { channel: 'Paid Search', Spend: 0 },
      { channel: 'Social Media', Spend: 0 },
      { channel: 'Email', Spend: 0 },
      { channel: 'Referral', Spend: 0 },
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
          {Array.from({ length: 6 }).map((_, i) => (
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
        description="Select a legal entity to view marketing data."
      />
    );
  }

  // -----------------------------------------------------------------------
  // Render
  // -----------------------------------------------------------------------

  return (
    <div className="space-y-6">
      <PageHeader
        title="Marketing Dashboard"
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
      {marketingKpis.length > 0 ? (
        <KpiGrid kpis={marketingKpis} />
      ) : (
        <EmptyState
          title="No Marketing KPIs"
          description="Marketing KPIs have not been configured yet."
        />
      )}

      {/* Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Lead Funnel */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Lead Funnel</CardTitle>
          </CardHeader>
          <CardContent>
            <BarChart
              data={leadFunnelData}
              categories={['Count']}
              index="stage"
              valueFormatter={(v) => formatNumber(v)}
              colors={['#3b82f6']}
              showLegend={false}
            />
          </CardContent>
        </Card>

        {/* Marketing ROI Trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Marketing ROI Trend</CardTitle>
          </CardHeader>
          <CardContent>
            {roiTrendData.length > 0 ? (
              <LineChart
                data={roiTrendData}
                categories={['Marketing ROI']}
                index="month"
                valueFormatter={(v) => formatPercent(v)}
                colors={['#10b981']}
                showLegend={false}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No marketing ROI history available.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Channel Performance */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Channel Performance</CardTitle>
          </CardHeader>
          <CardContent>
            <BarChart
              data={channelPerformanceData}
              categories={['Spend']}
              index="channel"
              valueFormatter={(v) => formatCurrency(v)}
              colors={['#8b5cf6']}
              showLegend={false}
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
