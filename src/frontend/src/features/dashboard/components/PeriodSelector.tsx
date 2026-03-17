import { useTranslation } from 'react-i18next';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import type { Period } from '../executive-config';

interface PeriodSelectorProps {
  period: Period;
  onPeriodChange: (period: Period) => void;
  compareEnabled: boolean;
  onCompareChange: (enabled: boolean) => void;
}

const PERIODS: Period[] = ['mtd', 'qtd', 'ytd'];

export default function PeriodSelector({
  period,
  onPeriodChange,
  compareEnabled,
  onCompareChange,
}: PeriodSelectorProps) {
  const { t } = useTranslation('executive');

  return (
    <div className="flex items-center gap-3">
      <div className="flex rounded-md border border-border" role="group" aria-label="Period selector">
        {PERIODS.map((p) => (
          <button
            key={p}
            onClick={() => onPeriodChange(p)}
            className={`px-3 py-1.5 text-xs font-medium transition-colors first:rounded-l-md last:rounded-r-md ${
              period === p
                ? 'bg-primary text-primary-foreground'
                : 'text-muted-foreground hover:bg-secondary/50'
            }`}
            aria-pressed={period === p}
          >
            {t(`period.${p}`)}
          </button>
        ))}
      </div>

      <div className="flex items-center gap-1.5">
        <Switch
          id="compare-toggle"
          checked={compareEnabled}
          onCheckedChange={onCompareChange}
          className="scale-75"
        />
        <Label htmlFor="compare-toggle" className="text-xs text-muted-foreground cursor-pointer">
          {t('period.vsPrior')}
        </Label>
      </div>
    </div>
  );
}
