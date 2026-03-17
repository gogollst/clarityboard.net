import { useTranslation } from 'react-i18next';
import SparkLine from '@/components/charts/SparkLine';
import { Skeleton } from '@/components/ui/skeleton';
import { HEADLINE_KPIS } from '../executive-config';
import KpiChip from './KpiChip';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';
import type { AlertDto } from '@/types/kpi';

interface HeadlineMetricsProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
  alertEvents: AlertDto[];
  historyMap: Map<string, number[]>;
  comparisonMap: Map<string, number[]>;
  compareEnabled: boolean;
  isLoading: boolean;
}

export default function HeadlineMetrics({
  kpis,
  definitions,
  alertEvents,
  historyMap,
  comparisonMap,
  compareEnabled,
  isLoading,
}: HeadlineMetricsProps) {
  const { t } = useTranslation('executive');

  if (isLoading) {
    return (
      <div className="grid grid-cols-5 gap-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="space-y-2">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-7 w-24" />
            <Skeleton className="h-3 w-12" />
            <Skeleton className="h-8 w-20 mt-1" />
          </div>
        ))}
      </div>
    );
  }

  const kpiMap = new Map(kpis.map((s) => [s.kpiId, s]));

  const criticalCount = alertEvents.filter((a) => a.severity === 'critical').length;
  const warningCount = alertEvents.filter((a) => a.severity === 'warning').length;
  const totalAlerts = criticalCount + warningCount;

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 sm:gap-6">
      {HEADLINE_KPIS.map((cfg) => {
        const snapshot = kpiMap.get(cfg.kpiId);
        const definition = definitions.get(cfg.kpiId);
        const history = historyMap.get(cfg.kpiId);
        const comparison = compareEnabled ? comparisonMap.get(cfg.kpiId) : undefined;
        const lastCompValue = comparison?.length ? comparison[comparison.length - 1] : null;

        return (
          <div key={cfg.kpiId} className="min-w-0">
            <KpiChip
              snapshot={snapshot}
              definition={definition}
              route={cfg.route}
              size="lg"
              comparisonValue={compareEnabled ? lastCompValue : null}
            />
            {history && history.length > 1 && (
              <SparkLine
                data={history}
                trend={
                  snapshot?.changePct
                    ? snapshot.changePct > 0 ? 'up' : 'down'
                    : 'neutral'
                }
                className="h-8 w-full mt-1"
              />
            )}
          </div>
        );
      })}

      {/* Alerts headline metric */}
      <button
        type="button"
        onClick={() => {
          const alertsSection = document.getElementById('alerts-feed');
          alertsSection?.scrollIntoView({ behavior: 'smooth' });
        }}
        className="text-left active:scale-[0.98] transition-transform cursor-pointer"
        aria-label={`${totalAlerts} active alerts: ${criticalCount} critical, ${warningCount} warning`}
      >
        <p className="text-sm text-muted-foreground mb-0.5">{t('headlines.alerts')}</p>
        <p className="text-2xl font-mono tabular-nums font-semibold">{totalAlerts}</p>
        {totalAlerts > 0 && (
          <p className="text-xs mt-0.5 space-x-2">
            {criticalCount > 0 && (
              <span className="text-red-600 dark:text-red-400">{criticalCount} critical</span>
            )}
            {warningCount > 0 && (
              <span className="text-amber-600 dark:text-amber-400">{warningCount} warning</span>
            )}
          </p>
        )}
      </button>
    </div>
  );
}
