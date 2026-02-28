import KpiCard from './KpiCard';
import { cn } from '@/lib/utils';

type KpiUnit = 'percentage' | 'currency' | 'ratio' | 'count' | 'days';
type KpiDirection = 'higher_better' | 'lower_better' | 'target';

interface KpiGridItem {
  kpiId: string;
  name: string;
  value: number;
  previousValue?: number;
  changePct?: number;
  unit: KpiUnit;
  direction: KpiDirection;
  targetValue?: number;
  sparklineData?: number[];
}

interface KpiGridProps {
  kpis: KpiGridItem[];
  isLoading?: boolean;
  className?: string;
}

const SKELETON_COUNT = 6;

export default function KpiGrid({ kpis, isLoading, className }: KpiGridProps) {
  if (isLoading) {
    return (
      <div
        className={cn(
          'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
          className
        )}
      >
        {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
          <KpiCard
            key={i}
            kpiId=""
            name=""
            value={0}
            unit="count"
            direction="higher_better"
            isLoading
          />
        ))}
      </div>
    );
  }

  return (
    <div
      className={cn(
        'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
        className
      )}
    >
      {kpis.map((kpi) => (
        <KpiCard
          key={kpi.kpiId}
          kpiId={kpi.kpiId}
          name={kpi.name}
          value={kpi.value}
          previousValue={kpi.previousValue}
          changePct={kpi.changePct}
          unit={kpi.unit}
          direction={kpi.direction}
          targetValue={kpi.targetValue}
          sparklineData={kpi.sparklineData}
        />
      ))}
    </div>
  );
}
