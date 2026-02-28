import { useState } from 'react';
import {
  AlertTriangle,
  AlertCircle,
  Info,
  ChevronDown,
  ChevronUp,
  Check,
  X,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import type { AlertDto } from '@/types/kpi';

interface AlertBannerProps {
  alerts: AlertDto[];
  onAcknowledge?: (id: string) => void;
  onDismiss?: (id: string) => void;
}

const INITIAL_DISPLAY_COUNT = 3;

const severityConfig = {
  critical: {
    icon: AlertTriangle,
    bg: 'bg-red-50 border-red-200 dark:bg-red-950/20 dark:border-red-900',
    text: 'text-red-800 dark:text-red-200',
    iconColor: 'text-red-600 dark:text-red-400',
    badge: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  },
  warning: {
    icon: AlertCircle,
    bg: 'bg-amber-50 border-amber-200 dark:bg-amber-950/20 dark:border-amber-900',
    text: 'text-amber-800 dark:text-amber-200',
    iconColor: 'text-amber-600 dark:text-amber-400',
    badge: 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
  },
  info: {
    icon: Info,
    bg: 'bg-blue-50 border-blue-200 dark:bg-blue-950/20 dark:border-blue-900',
    text: 'text-blue-800 dark:text-blue-200',
    iconColor: 'text-blue-600 dark:text-blue-400',
    badge: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  },
};

export default function AlertBanner({
  alerts,
  onAcknowledge,
  onDismiss,
}: AlertBannerProps) {
  const [expanded, setExpanded] = useState(false);

  // Only show active alerts
  const activeAlerts = alerts.filter((a) => a.status === 'active');

  if (activeAlerts.length === 0) return null;

  const displayedAlerts = expanded
    ? activeAlerts
    : activeAlerts.slice(0, INITIAL_DISPLAY_COUNT);

  const hasMore = activeAlerts.length > INITIAL_DISPLAY_COUNT;

  return (
    <div className="space-y-2">
      {displayedAlerts.map((alert) => {
        const config = severityConfig[alert.severity];
        const Icon = config.icon;

        return (
          <div
            key={alert.id}
            className={cn(
              'flex items-start gap-3 rounded-lg border p-3',
              config.bg
            )}
          >
            <Icon className={cn('mt-0.5 h-4 w-4 shrink-0', config.iconColor)} />
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className={cn('text-sm font-medium', config.text)}>
                  {alert.title}
                </span>
                <span
                  className={cn(
                    'inline-flex rounded-full px-2 py-0.5 text-xs font-medium',
                    config.badge
                  )}
                >
                  {alert.severity}
                </span>
              </div>
              <p className={cn('mt-0.5 text-sm', config.text)}>
                {alert.message}
              </p>
            </div>
            <div className="flex shrink-0 items-center gap-1">
              {onAcknowledge && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  onClick={() => onAcknowledge(alert.id)}
                  title="Acknowledge"
                >
                  <Check className="h-3.5 w-3.5" />
                </Button>
              )}
              {onDismiss && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  onClick={() => onDismiss(alert.id)}
                  title="Dismiss"
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              )}
            </div>
          </div>
        );
      })}

      {hasMore && (
        <Button
          variant="ghost"
          size="sm"
          className="w-full text-xs"
          onClick={() => setExpanded(!expanded)}
        >
          {expanded ? (
            <>
              Show less <ChevronUp className="ml-1 h-3 w-3" />
            </>
          ) : (
            <>
              Show {activeAlerts.length - INITIAL_DISPLAY_COUNT} more alerts{' '}
              <ChevronDown className="ml-1 h-3 w-3" />
            </>
          )}
        </Button>
      )}
    </div>
  );
}
