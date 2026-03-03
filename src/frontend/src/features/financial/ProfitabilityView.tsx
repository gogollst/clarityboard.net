import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useKpiDashboard, useKpiDefinitions, useKpiHistory } from '@/hooks/useKpis';
import { useProfitAndLoss, useBalanceSheet } from '@/hooks/useAccounting';
import { formatCurrency, formatPercent } from '@/lib/format';
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
// ProfitabilityView (named export for lazy route)
// ---------------------------------------------------------------------------

export function Component() {
  const { t } = useTranslation('cashflow');
  const { selectedEntityId, selectedEntity } = useEntity();
  const [dateRange, setDateRange] = useState<DateRange>('12m');

  const { startDate, endDate } = useMemo(() => computeDates(dateRange), [dateRange]);

  // Data fetching
  const { data: pnl, isLoading: pnlLoading } = useProfitAndLoss(
    selectedEntityId,
    startDate,
    endDate,
  );
  const { data: balanceSheet, isLoading: bsLoading } = useBalanceSheet(
    selectedEntityId,
    endDate,
  );
  const { data: dashboard, isLoading: kpiLoading } = useKpiDashboard(selectedEntityId);
  const { data: definitions } = useKpiDefinitions();

  // Margin trend history (gross margin KPI)
  const { data: grossMarginHistory } = useKpiHistory(
    selectedEntityId,
    'gross_margin',
    startDate,
    endDate,
  );
  const { data: ebitdaMarginHistory } = useKpiHistory(
    selectedEntityId,
    'ebitda_margin',
    startDate,
    endDate,
  );
  const { data: netMarginHistory } = useKpiHistory(
    selectedEntityId,
    'net_margin',
    startDate,
    endDate,
  );

  const isLoading = pnlLoading || bsLoading || kpiLoading;

  // -----------------------------------------------------------------------
  // KPI cards for financial domain
  // -----------------------------------------------------------------------

  const financialKpis = useMemo(() => {
    const defs = definitions?.filter((d) => d.domain === 'financial') ?? [];
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
  // Margin trend data (for line chart)
  // -----------------------------------------------------------------------

  const marginTrendData = useMemo(() => {
    if (!grossMarginHistory) return [];

    const dateMap = new Map<
      string,
      { month: string; 'Gross Margin': number; 'EBITDA Margin': number; 'Net Margin': number }
    >();

    for (const snap of grossMarginHistory) {
      const month = snap.snapshotDate.slice(0, 7);
      if (!dateMap.has(month)) {
        dateMap.set(month, {
          month,
          'Gross Margin': 0,
          'EBITDA Margin': 0,
          'Net Margin': 0,
        });
      }
      dateMap.get(month)!['Gross Margin'] = snap.value;
    }

    for (const snap of ebitdaMarginHistory ?? []) {
      const month = snap.snapshotDate.slice(0, 7);
      if (dateMap.has(month)) {
        dateMap.get(month)!['EBITDA Margin'] = snap.value;
      }
    }

    for (const snap of netMarginHistory ?? []) {
      const month = snap.snapshotDate.slice(0, 7);
      if (dateMap.has(month)) {
        dateMap.get(month)!['Net Margin'] = snap.value;
      }
    }

    return Array.from(dateMap.values()).sort((a, b) =>
      a.month.localeCompare(b.month),
    );
  }, [grossMarginHistory, ebitdaMarginHistory, netMarginHistory]);

  // -----------------------------------------------------------------------
  // Revenue vs Costs bar chart data
  // -----------------------------------------------------------------------

  const revenueVsCostsData = useMemo(() => {
    if (!pnl) return [];
    // Show a single-period summary when we only have aggregate P&L
    return [
      {
        period: `${startDate.slice(0, 7)} - ${endDate.slice(0, 7)}`,
        Revenue: pnl.revenue,
        COGS: pnl.cogs,
        'Operating Expenses': pnl.operatingExpenses.reduce(
          (sum, c) => sum + c.amount,
          0,
        ),
      },
    ];
  }, [pnl, startDate, endDate]);

  // -----------------------------------------------------------------------
  // Loading state
  // -----------------------------------------------------------------------

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <Skeleton className="h-64 rounded-lg" />
          <Skeleton className="h-64 rounded-lg" />
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
  // Empty state
  // -----------------------------------------------------------------------

  if (!selectedEntityId) {
    return (
      <EmptyState
        title={t('financial.noEntitySelected')}
        description={t('financial.noEntityDescription')}
      />
    );
  }

  // -----------------------------------------------------------------------
  // P&L helper rows
  // -----------------------------------------------------------------------

  const pnlRows = pnl
    ? [
        { label: t('financial.pnl.revenue'), value: pnl.revenue },
        { label: t('financial.pnl.cogs'), value: -pnl.cogs },
        { label: t('financial.pnl.grossProfit'), value: pnl.grossProfit, bold: true },
        ...pnl.operatingExpenses.map((c) => ({
          label: c.name,
          value: -c.amount,
        })),
        { label: t('financial.pnl.ebit'), value: pnl.ebit, bold: true },
        { label: t('financial.pnl.interest'), value: -pnl.interest },
        { label: t('financial.pnl.taxes'), value: -pnl.taxes },
        { label: t('financial.pnl.netIncome'), value: pnl.netIncome, bold: true },
      ]
    : [];

  // -----------------------------------------------------------------------
  // Render
  // -----------------------------------------------------------------------

  return (
    <div className="space-y-6">
      {/* Header with date range selector */}
      <PageHeader
        title={t('financial.title')}
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
                {t(`financial.dateRange.${range}`)}
              </Button>
            ))}
          </div>
        }
      />

      {/* Two-column layout: P&L + Balance Sheet */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* P&L Summary */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('financial.pnlCard')}</CardTitle>
          </CardHeader>
          <CardContent>
            {pnl ? (
              <div className="space-y-1">
                {pnlRows.map((row) => (
                  <div
                    key={row.label}
                    className={`flex items-center justify-between py-1.5 text-sm ${
                      'bold' in row && row.bold
                        ? 'border-t border-border font-semibold'
                        : ''
                    }`}
                  >
                    <span className="text-muted-foreground">{row.label}</span>
                    <span
                      className={
                        row.value < 0 ? 'text-red-600 dark:text-red-400' : ''
                      }
                    >
                      {formatCurrency(row.value)}
                    </span>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                {t('financial.noPnlData')}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Balance Sheet Summary */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('financial.balanceSheetCard')}</CardTitle>
          </CardHeader>
          <CardContent>
            {balanceSheet ? (
              <div className="space-y-1">
                <div className="flex items-center justify-between py-1.5 text-sm font-semibold">
                  <span className="text-muted-foreground">{t('financial.balanceSheet.totalAssets')}</span>
                  <span>{formatCurrency(balanceSheet.totalAssets)}</span>
                </div>
                {balanceSheet.assets.map((section) => (
                  <div
                    key={section.name}
                    className="flex items-center justify-between py-1 pl-4 text-sm"
                  >
                    <span className="text-muted-foreground">{section.name}</span>
                    <span>{formatCurrency(section.amount)}</span>
                  </div>
                ))}

                <div className="flex items-center justify-between border-t border-border py-1.5 text-sm font-semibold">
                  <span className="text-muted-foreground">
                    {t('financial.balanceSheet.totalLiabilities')}
                  </span>
                  <span>{formatCurrency(balanceSheet.totalLiabilities)}</span>
                </div>
                {balanceSheet.liabilities.map((section) => (
                  <div
                    key={section.name}
                    className="flex items-center justify-between py-1 pl-4 text-sm"
                  >
                    <span className="text-muted-foreground">{section.name}</span>
                    <span>{formatCurrency(section.amount)}</span>
                  </div>
                ))}

                <div className="flex items-center justify-between border-t border-border py-1.5 text-sm font-semibold">
                  <span className="text-muted-foreground">{t('financial.balanceSheet.equity')}</span>
                  <span>{formatCurrency(balanceSheet.equity)}</span>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                {t('financial.noBalanceSheetData')}
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* KPI cards grid */}
      {financialKpis.length > 0 && <KpiGrid kpis={financialKpis} />}

      {/* Charts section */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Margin trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('financial.marginTrendsCard')}</CardTitle>
          </CardHeader>
          <CardContent>
            {marginTrendData.length > 0 ? (
              <LineChart
                data={marginTrendData}
                categories={['Gross Margin', 'EBITDA Margin', 'Net Margin']}
                index="month"
                valueFormatter={(v) => formatPercent(v)}
                colors={['#3b82f6', '#f59e0b', '#10b981']}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                {t('financial.noMarginData')}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Revenue vs Costs */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('financial.revenueVsCostsCard')}</CardTitle>
          </CardHeader>
          <CardContent>
            {revenueVsCostsData.length > 0 ? (
              <BarChart
                data={revenueVsCostsData}
                categories={['Revenue', 'COGS', 'Operating Expenses']}
                index="period"
                valueFormatter={(v) => formatCurrency(v)}
                colors={['#10b981', '#ef4444', '#f59e0b']}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                {t('financial.noRevenueData')}
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
