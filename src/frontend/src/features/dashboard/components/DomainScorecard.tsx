import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import SparkLine from '@/components/charts/SparkLine';
import KpiChip from './KpiChip';
import KpiPlaceholder from './KpiPlaceholder';
import type { DomainConfig } from '../executive-config';
import type { KpiSnapshot, KpiDefinition, AlertDto } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface DomainScorecardProps {
  config: DomainConfig;
  kpis: Map<string, KpiSnapshot>;
  definitions: Map<string, KpiDefinition>;
  alerts: AlertDto[];
  historyMap: Map<string, number[]>;
}

export default function DomainScorecard({
  config,
  kpis,
  definitions,
  alerts,
  historyMap,
}: DomainScorecardProps) {
  const { t } = useTranslation(['executive', 'dashboard']);
  const navigate = useNavigate();

  const isClickable = !!config.route && !config.comingSoon;
  const chartHistory = config.chartKpiId ? historyMap.get(config.chartKpiId) : null;
  const domainAlerts = alerts.filter((a) =>
    a.kpiId && config.kpiIds.some((id) => a.kpiId === id || a.kpiId?.startsWith(config.domain + '.')),
  );

  // Determine sparkline trend from the chart KPI
  const chartSnapshot = config.chartKpiId ? kpis.get(config.chartKpiId) : null;
  const chartTrend = chartSnapshot?.changePct
    ? chartSnapshot.changePct > 0 ? 'up' as const : 'down' as const
    : 'neutral' as const;

  return (
    <div
      role={isClickable ? 'button' : undefined}
      tabIndex={isClickable ? 0 : undefined}
      onClick={() => isClickable && navigate(config.route!)}
      onKeyDown={(e) => {
        if (isClickable && (e.key === 'Enter' || e.key === ' ')) {
          e.preventDefault();
          navigate(config.route!);
        }
      }}
      className={cn(
        'py-5 px-1 border-b border-border',
        isClickable && 'cursor-pointer hover:bg-secondary/50 transition-colors duration-200 rounded-md',
        config.comingSoon && 'opacity-60',
      )}
      aria-label={isClickable ? `${t(config.labelKey)} — click to view details` : undefined}
    >
      <div className="flex items-start justify-between gap-4">
        {/* Left: domain label + KPI chips */}
        <div className="flex-1 min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
            {t(config.labelKey)}
          </p>
          <div className="flex flex-wrap gap-x-6 gap-y-3">
            {config.kpiIds.map((kpiId) => {
              if (config.comingSoon) {
                const label = definitions.get(kpiId)?.name ?? kpiId.split('.').pop() ?? kpiId;
                return <KpiPlaceholder key={kpiId} label={label} />;
              }
              return (
                <KpiChip
                  key={kpiId}
                  snapshot={kpis.get(kpiId)}
                  definition={definitions.get(kpiId)}
                  size="md"
                />
              );
            })}
          </div>
        </div>

        {/* Right: sparkline */}
        <div className="hidden lg:block flex-shrink-0 w-24">
          {chartHistory && chartHistory.length > 1 ? (
            <SparkLine data={chartHistory} trend={chartTrend} className="h-12 w-24" />
          ) : config.comingSoon ? (
            <p className="text-xs text-muted-foreground/50 text-center pt-3">
              {t('executive:comingSoon.noChart')}
            </p>
          ) : null}
        </div>
      </div>

      {/* Inline alerts */}
      {domainAlerts.length > 0 && !config.comingSoon && (
        <div className="mt-3 space-y-1">
          {domainAlerts.slice(0, 2).map((alert) => (
            <p key={alert.id} className="text-xs text-muted-foreground">
              <span className={alert.severity === 'critical' ? 'text-red-500' : 'text-amber-500'}>
                {alert.severity === 'critical' ? '●' : '▲'}
              </span>
              {' '}{alert.message}
            </p>
          ))}
        </div>
      )}

      {/* Coming soon label */}
      {config.comingSoon && (
        <p className="text-xs text-muted-foreground/50 mt-3">
          {t('executive:comingSoon.inProgress')}
        </p>
      )}
    </div>
  );
}
