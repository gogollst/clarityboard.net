import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

interface KpiPlaceholderProps {
  label: string;
  size?: 'lg' | 'md';
  className?: string;
}

export default function KpiPlaceholder({ label, size = 'md', className }: KpiPlaceholderProps) {
  const { t } = useTranslation('executive');
  const valueSize = size === 'lg' ? 'text-2xl' : 'text-lg';
  const labelSize = size === 'lg' ? 'text-sm' : 'text-xs';

  return (
    <div className={cn('text-left', className)}>
      <p className={cn(labelSize, 'text-muted-foreground mb-0.5')}>{label}</p>
      <p className={cn(valueSize, 'font-mono tabular-nums font-semibold text-muted-foreground/40')}>
        {t('comingSoon.noData')}
      </p>
    </div>
  );
}
