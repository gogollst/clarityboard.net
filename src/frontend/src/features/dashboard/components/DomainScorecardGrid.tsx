import { Skeleton } from '@/components/ui/skeleton';
import { DOMAIN_CONFIGS } from '../executive-config';
import DomainScorecard from './DomainScorecard';
import type { KpiSnapshot, KpiDefinition, AlertDto } from '@/types/kpi';

interface DomainScorecardGridProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
  alerts: AlertDto[];
  historyMap: Map<string, number[]>;
  isLoading: boolean;
}

export default function DomainScorecardGrid({
  kpis,
  definitions,
  alerts,
  historyMap,
  isLoading,
}: DomainScorecardGridProps) {
  if (isLoading) {
    return (
      <div className="space-y-6">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="space-y-3">
            <Skeleton className="h-3 w-20" />
            <div className="flex gap-6">
              {Array.from({ length: 4 }).map((_, j) => (
                <div key={j} className="space-y-2">
                  <Skeleton className="h-3 w-14" />
                  <Skeleton className="h-5 w-20" />
                  <Skeleton className="h-3 w-10" />
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    );
  }

  const kpiMap = new Map(kpis.map((s) => [s.kpiId, s]));

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8">
      {DOMAIN_CONFIGS.map((config) => (
        <DomainScorecard
          key={config.domain}
          config={config}
          kpis={kpiMap}
          definitions={definitions}
          alerts={alerts}
          historyMap={historyMap}
        />
      ))}
    </div>
  );
}
