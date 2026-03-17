import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { getDomainRoute } from '../executive-config';
import type { AlertDto } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface AlertsFeedProps {
  alerts: AlertDto[];
}

export default function AlertsFeed({ alerts }: AlertsFeedProps) {
  const { t } = useTranslation('executive');
  const navigate = useNavigate();

  // Filter to critical + warning, sort by severity then recency
  const filtered = alerts
    .filter((a) => a.severity === 'critical' || a.severity === 'warning')
    .sort((a, b) => {
      if (a.severity === 'critical' && b.severity !== 'critical') return -1;
      if (b.severity === 'critical' && a.severity !== 'critical') return 1;
      return new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime();
    });

  const visible = filtered.slice(0, 5);
  const hasMore = filtered.length > 5;

  if (filtered.length === 0) {
    return (
      <div id="alerts-feed" className="py-6 text-center">
        <p className="text-sm text-muted-foreground">
          <span className="text-emerald-500 mr-1.5">✓</span>
          {t('alerts.allClear')}
        </p>
      </div>
    );
  }

  return (
    <div id="alerts-feed" className="py-4">
      <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
        {t('alerts.title')}
      </h2>
      <div className="space-y-2">
        {visible.map((alert) => (
          <div key={alert.id} className="flex items-center gap-2 text-sm">
            <span className={cn(
              'flex-shrink-0 text-xs',
              alert.severity === 'critical' ? 'text-red-500' : 'text-amber-500',
            )}>
              {alert.severity === 'critical' ? '●' : '▲'}
            </span>
            <span className="flex-1 min-w-0 truncate text-foreground/80">
              {alert.title}: {alert.message}
            </span>
            <button
              type="button"
              onClick={() => {
                const route = alert.kpiId ? getDomainRoute(alert.kpiId) : '/kpis/financial';
                const highlight = alert.kpiId ? `?highlight=${alert.kpiId}` : '';
                navigate(`${route}${highlight}`);
              }}
              className="flex-shrink-0 text-xs text-primary hover:underline"
            >
              {t('alerts.view')}
            </button>
          </div>
        ))}
      </div>
      {hasMore && (
        <button
          type="button"
          onClick={() => navigate('/kpis/financial')} // TODO: dedicated alerts page
          className="mt-2 text-xs text-primary hover:underline"
        >
          {t('alerts.viewAll')}
        </button>
      )}
    </div>
  );
}
