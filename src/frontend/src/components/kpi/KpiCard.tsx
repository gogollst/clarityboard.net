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
  // For 'target' direction, we cannot determine improvement without more context
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
      <Card>
        <CardHeader className="pb-2">
          <Skeleton className="h-4 w-24" />
        </CardHeader>
        <CardContent>
          <Skeleton className="mb-2 h-8 w-32" />
          <Skeleton className="h-4 w-16" />
        </CardContent>
      </Card>
    );
  }

  const formattedValue = formatValue(value, unit);
  const trend = getTrend(changePct, direction);
  const improved = changePct !== undefined ? isImprovement(changePct, direction) : null;

  const changeColor =
    improved === true
      ? 'text-green-600'
      : improved === false
        ? 'text-red-600'
        : 'text-muted-foreground';

  const TrendIcon =
    changePct !== undefined && changePct > 0
      ? TrendingUp
      : changePct !== undefined && changePct < 0
        ? TrendingDown
        : Minus;

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          {name}
        </CardTitle>
        {sparklineData && sparklineData.length > 1 && (
          <SparkLine data={sparklineData} trend={trend} />
        )}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{formattedValue}</div>
        <div className="mt-1 flex items-center gap-2">
          {changePct !== undefined && (
            <div className={cn('flex items-center gap-1 text-sm', changeColor)}>
              <TrendIcon className="h-3.5 w-3.5" />
              <span>
                {changePct > 0 ? '+' : ''}
                {changePct.toFixed(1)}%
              </span>
            </div>
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
