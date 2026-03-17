import { useTranslation } from 'react-i18next';
import type { KpiSnapshot, KpiDefinition } from '@/types/kpi';

interface QuickInsightsProps {
  kpis: KpiSnapshot[];
  definitions: Map<string, KpiDefinition>;
}

export default function QuickInsights({ kpis, definitions }: QuickInsightsProps) {
  const { t } = useTranslation('executive');

  // Top 3 most notable changes
  const notable = kpis
    .filter((s) => s.changePct !== null && s.changePct !== 0)
    .sort((a, b) => Math.abs(b.changePct!) - Math.abs(a.changePct!))
    .slice(0, 3);

  if (notable.length === 0) return null;

  const sentences = notable.map((s) => {
    const def = definitions.get(s.kpiId);
    const name = def?.name ?? s.kpiId;
    const pct = Math.abs(s.changePct!).toFixed(1);
    const isPositive = s.changePct! > 0;

    if (def?.direction === 'lower_better') {
      return isPositive
        ? t('insights.worsened', { name, pct })
        : t('insights.improved', { name, pct });
    }
    return isPositive
      ? t('insights.up', { name, pct })
      : t('insights.down', { name, pct });
  });

  return (
    <div className="py-4">
      <p className="text-sm text-muted-foreground">
        {sentences.join('. ')}.
      </p>
    </div>
  );
}
