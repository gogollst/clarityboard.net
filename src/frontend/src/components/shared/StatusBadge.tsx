import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

type BadgeVariant = 'default' | 'success' | 'warning' | 'destructive' | 'info';

const variantStyles: Record<BadgeVariant, string> = {
  default:
    'bg-secondary text-secondary-foreground border border-border',
  success:
    'bg-emerald-100 text-emerald-700 border border-emerald-200',
  warning:
    'bg-amber-100 text-amber-700 border border-amber-200',
  destructive:
    'bg-red-100 text-red-700 border border-red-200',
  info:
    'bg-blue-100 text-blue-700 border border-blue-200',
};

const dotColor: Record<BadgeVariant, string> = {
  default: 'bg-slate-400',
  success: 'bg-emerald-500',
  warning: 'bg-amber-500',
  destructive: 'bg-red-500',
  info: 'bg-blue-500',
};

interface StatusBadgeProps {
  status: string;
  variantMap?: Record<string, BadgeVariant>;
  className?: string;
}

const defaultVariantMap: Record<string, BadgeVariant> = {
  active: 'success',
  connected: 'success',
  resolved: 'success',
  healthy: 'success',
  warning: 'warning',
  pending: 'warning',
  reconnecting: 'warning',
  critical: 'destructive',
  error: 'destructive',
  inactive: 'destructive',
  disconnected: 'destructive',
  info: 'info',
  acknowledged: 'info',
};

export default function StatusBadge({
  status,
  variantMap,
  className,
}: StatusBadgeProps) {
  const { t } = useTranslation('common');
  const map = variantMap ?? defaultVariantMap;
  const variant = map[status.toLowerCase()] ?? 'default';

  // Try to get translated label for known status keys; fall back to raw status
  const translationKey = `status.${status.toLowerCase()}`;
  const translated = t(translationKey, { defaultValue: '' });
  const label = translated || status;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium capitalize',
        variantStyles[variant],
        className
      )}
    >
      <span className={cn('h-1.5 w-1.5 rounded-full', dotColor[variant])} />
      {label}
    </span>
  );
}
