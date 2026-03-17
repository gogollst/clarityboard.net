import { useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useExecutiveDashboard } from '@/hooks/useExecutiveDashboard';
import { useUiStore } from '@/stores/uiStore';
import PeriodSelector from './components/PeriodSelector';
import HeadlineMetrics from './components/HeadlineMetrics';
import DomainScorecardGrid from './components/DomainScorecardGrid';
import AlertsFeed from './components/AlertsFeed';
import QuickInsights from './components/QuickInsights';
import { Separator } from '@/components/ui/separator';
import type { Period } from './executive-config';

const VALID_PERIODS: Period[] = ['mtd', 'qtd', 'ytd'];

export function Component() {
  const { t } = useTranslation('executive');
  const { selectedEntityId, selectedEntity } = useEntity();
  const [searchParams, setSearchParams] = useSearchParams();
  const connectionStatus = useUiStore((s) => s.connectionStatus);

  // URL params with validation + defaults
  const rawPeriod = searchParams.get('period');
  const period: Period = VALID_PERIODS.includes(rawPeriod as Period)
    ? (rawPeriod as Period)
    : 'mtd';
  const compareEnabled = searchParams.get('compare') === 'true';

  const setPeriod = useCallback((p: Period) => {
    setSearchParams((prev) => {
      prev.set('period', p);
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const setCompare = useCallback((enabled: boolean) => {
    setSearchParams((prev) => {
      if (enabled) prev.set('compare', 'true');
      else prev.delete('compare');
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const {
    dashboard,
    definitions,
    alertEvents,
    historyMap,
    comparisonMap,
    isLoading,
    isError,
    refetch,
  } = useExecutiveDashboard(selectedEntityId, period, compareEnabled);

  // No entity selected
  if (!selectedEntityId) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center">
          <h2 className="text-lg font-semibold">{t('noEntity.title')}</h2>
          <p className="text-sm text-muted-foreground mt-1">{t('noEntity.description')}</p>
        </div>
      </div>
    );
  }

  // Global error
  if (isError && !dashboard) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center">
          <p className="text-sm text-destructive">Failed to load dashboard data.</p>
          <button
            type="button"
            onClick={() => refetch()}
            className="mt-2 text-xs text-primary hover:underline"
          >
            {t('error.retry')}
          </button>
        </div>
      </div>
    );
  }

  const lastUpdatedTime = dashboard?.lastUpdated
    ? new Date(dashboard.lastUpdated).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : null;

  const isStale = !dashboard?.lastUpdated ||
    (Date.now() - new Date(dashboard.lastUpdated).getTime()) > 5 * 60 * 1000;

  return (
    <main className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="font-display text-2xl tracking-tight">
            {selectedEntity?.name ?? ''} — {t('title')}
          </h1>
          {lastUpdatedTime && (
            <p className="text-xs text-muted-foreground mt-1 flex items-center gap-1.5">
              {t('lastUpdated', { time: lastUpdatedTime })}
              <span
                className={`inline-block h-1.5 w-1.5 rounded-full ${
                  connectionStatus === 'connected' && !isStale
                    ? 'bg-emerald-500 animate-pulse'
                    : 'bg-amber-500'
                }`}
              />
            </p>
          )}
        </div>
        <PeriodSelector
          period={period}
          onPeriodChange={setPeriod}
          compareEnabled={compareEnabled}
          onCompareChange={setCompare}
        />
      </div>

      {/* Headline Metrics */}
      <section aria-label="Key metrics">
        <HeadlineMetrics
          kpis={dashboard?.kpis ?? []}
          definitions={definitions}
          alertEvents={alertEvents}
          historyMap={historyMap}
          comparisonMap={comparisonMap}
          compareEnabled={compareEnabled}
          isLoading={isLoading}
        />
      </section>

      <Separator className="my-6" />

      {/* Domain Scorecards */}
      <section aria-label="Domain scorecards">
        <DomainScorecardGrid
          kpis={dashboard?.kpis ?? []}
          definitions={definitions}
          alerts={alertEvents}
          historyMap={historyMap}
          isLoading={isLoading}
        />
      </section>

      <Separator className="my-6" />

      {/* Alerts Feed */}
      <section aria-label="Active alerts">
        <AlertsFeed alerts={alertEvents} />
      </section>

      {/* Quick Insights */}
      {dashboard?.kpis && dashboard.kpis.length > 0 && (
        <section aria-label="Key changes">
          <Separator className="my-4" />
          <QuickInsights kpis={dashboard.kpis} definitions={definitions} />
        </section>
      )}
    </main>
  );
}

export default { Component };
