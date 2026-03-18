import { useNavigate } from 'react-router-dom';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';
import { cn } from '@/lib/utils';

interface KpiChipProps {
  snapshot: KpiSnapshot | undefined;
  definition: KpiDefinition | undefined;
  route?: string | null;
  size?: 'lg' | 'md';
  comparisonValue?: number | null;
  className?: string;
}

function formatValue(value: number | null | undefined, unit: KpiDefinition['unit']): string {
  if (value == null) return '—';
  switch (unit) {
    case 'currency':
      if (Math.abs(value) >= 1_000_000) return `€${(value / 1_000_000).toFixed(1)}M`;
      if (Math.abs(value) >= 1_000) return `€${(value / 1_000).toFixed(0)}K`;
      return `€${value.toFixed(0)}`;
    case 'percentage':
      return `${value.toFixed(1)}%`;
    case 'days':
      return `${value.toFixed(0)}d`;
    case 'count':
      return value.toLocaleString('en-US', { maximumFractionDigits: 0 });
    case 'ratio':
      return value.toFixed(2);
    default:
      return String(value);
  }
}

function getTrendColor(changePct: number | null, direction: KpiDefinition['direction']): string {
  if (changePct === null || changePct === 0) return 'text-muted-foreground';
  const isPositiveChange = changePct > 0;
  const isGood =
    direction === 'higher_better' ? isPositiveChange : !isPositiveChange;
  return isGood ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400';
}

function getTrendArrow(changePct: number | null): string {
  if (changePct === null || changePct === 0) return '—';
  return changePct > 0 ? '▲' : '▼';
}

export default function KpiChip({
  snapshot,
  definition,
  route,
  size = 'md',
  comparisonValue,
  className,
}: KpiChipProps) {
  const navigate = useNavigate();

  if (!snapshot || !definition) return null;

  const isClickable = !!route;
  const valueSize = size === 'lg' ? 'text-2xl' : 'text-lg';
  const labelSize = size === 'lg' ? 'text-sm' : 'text-xs';

  return (
    <button
      type="button"
      onClick={() => isClickable && navigate(route!)}
      disabled={!isClickable}
      className={cn(
        'text-left transition-transform',
        isClickable && 'cursor-pointer active:scale-[0.98]',
        !isClickable && 'cursor-default',
        className,
      )}
      aria-label={
        `${definition.name}: ${formatValue(snapshot.value, definition.unit)}` +
        (snapshot.changePct !== null ? `, ${snapshot.changePct > 0 ? '+' : ''}${snapshot.changePct.toFixed(1)}%` : '') +
        (isClickable ? `. View ${definition.domain} KPIs` : '')
      }
    >
      <p className={cn(labelSize, 'text-muted-foreground mb-0.5')}>{definition.name}</p>
      <p className={cn(valueSize, 'font-mono tabular-nums font-semibold')}>
        {formatValue(snapshot.value, definition.unit)}
      </p>
      {snapshot.changePct !== null && (
        <p className={cn('text-xs mt-0.5', getTrendColor(snapshot.changePct, definition.direction))}>
          {getTrendArrow(snapshot.changePct)} {snapshot.changePct > 0 ? '+' : ''}{snapshot.changePct.toFixed(1)}%
        </p>
      )}
      {comparisonValue !== undefined && comparisonValue !== null && (
        <p className="text-xs text-muted-foreground/60 mt-0.5">
          {formatValue(comparisonValue, definition.unit)}
        </p>
      )}
    </button>
  );
}
