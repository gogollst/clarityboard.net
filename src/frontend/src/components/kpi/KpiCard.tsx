import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import SparkLine from '@/components/charts/SparkLine';
import { cn } from '@/lib/utils';
import {
  formatCurrency,
  formatPercent,
  formatNumber,
  formatDays,
} from '@/lib/format';

type KpiUnit = 'percentage' | 'currency' | 'ratio' | 'count' | 'days';
type KpiDirection = 'higher_better' | 'lower_better' | 'target';

interface KpiCardProps {
  kpiId: string;
  name: string;
  value: number;
  previousValue?: number;
  changePct?: number;
  unit: KpiUnit;
  direction: KpiDirection;
  targetValue?: number;
  sparklineData?: number[];
  isLoading?: boolean;
}

function formatValue(value: number, unit: KpiUnit): string {
  switch (unit) {
    case 'currency':
      return formatCurrency(value);
    case 'percentage':
      return formatPercent(value);
    case 'days':
      return formatDays(value);
    case 'ratio':
      return formatNumber(value);
    case 'count':
      return formatNumber(value);
    default:
      return formatNumber(value);
  }
}

function isImprovement(
  changePct: number,
  direction: KpiDirection
): boolean | null {
  if (changePct === 0) return null;
  if (direction === 'higher_better') return changePct > 0;
  if (direction === 'lower_better') return changePct < 0;
  return null;
}

function getTrend(
  changePct: number | undefined,
  direction: KpiDirection
): 'up' | 'down' | 'neutral' {
  if (changePct === undefined || changePct === 0) return 'neutral';
  const improved = isImprovement(changePct, direction);
  if (improved === true) return 'up';
  if (improved === false) return 'down';
  return 'neutral';
}

const trendBorderColor = {
  up: 'border-t-emerald-500',
  down: 'border-t-red-500',
  neutral: 'border-t-slate-300',
};

const trendChangeColor = {
  up: 'text-emerald-600 bg-emerald-50',
  down: 'text-red-600 bg-red-50',
  neutral: 'text-muted-foreground bg-muted',
};

export default function KpiCard({
  name,
  value,
  changePct,
  unit,
  direction,
  targetValue,
  sparklineData,
  isLoading,
}: KpiCardProps) {
  if (isLoading) {
    return (
      <Card className="border-t-2 border-t-slate-200">
        <CardHeader className="pb-2">
          <Skeleton className="h-4 w-24" />
        </CardHeader>
        <CardContent>
          <Skeleton className="mb-2 h-8 w-32" />
          <Skeleton className="h-5 w-16 rounded-full" />
        </CardContent>
      </Card>
    );
  }

  const formattedValue = formatValue(value, unit);
  const trend = getTrend(changePct, direction);

  const TrendIcon =
    changePct !== undefined && changePct > 0
      ? TrendingUp
      : changePct !== undefined && changePct < 0
        ? TrendingDown
        : Minus;

  return (
    <Card className={cn('border-t-2', trendBorderColor[trend])}>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {name}
        </CardTitle>
        {sparklineData && sparklineData.length > 1 && (
          <SparkLine data={sparklineData} trend={trend} />
        )}
      </CardHeader>
      <CardContent>
        <div className="kpi-value text-2xl">{formattedValue}</div>
        <div className="mt-2 flex items-center gap-2">
          {changePct !== undefined && (
            <span
              className={cn(
                'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium',
                trendChangeColor[trend]
              )}
            >
              <TrendIcon className="h-3 w-3" />
              {changePct > 0 ? '+' : ''}
              {changePct.toFixed(1)}%
            </span>
          )}
          {targetValue !== undefined && (
            <Badge variant="outline" className="text-xs">
              Target: {formatValue(targetValue, unit)}
            </Badge>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
