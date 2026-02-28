import { cn } from '@/lib/utils';

type BadgeVariant = 'default' | 'success' | 'warning' | 'destructive' | 'info';

const variantStyles: Record<BadgeVariant, string> = {
  default:
    'bg-secondary text-secondary-foreground',
  success:
    'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  warning:
    'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
  destructive:
    'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  info:
    'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
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
  const map = variantMap ?? defaultVariantMap;
  const variant = map[status.toLowerCase()] ?? 'default';

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium capitalize',
        variantStyles[variant],
        className
      )}
    >
      {status}
    </span>
  );
}
