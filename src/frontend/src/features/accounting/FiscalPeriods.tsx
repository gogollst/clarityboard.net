import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useFiscalPeriods, useUpdateFiscalPeriodStatus } from '@/hooks/useAccounting';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

const MONTH_NAMES: Record<string, string> = {
  '1': 'Jan', '2': 'Feb', '3': 'Mär', '4': 'Apr', '5': 'Mai', '6': 'Jun',
  '7': 'Jul', '8': 'Aug', '9': 'Sep', '10': 'Okt', '11': 'Nov', '12': 'Dez',
};

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  if (status === 'open') return 'default';
  if (status === 'hard_closed') return 'destructive';
  if (status === 'exported') return 'secondary';
  return 'outline';
}

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const { data: periods, isLoading } = useFiscalPeriods(selectedEntityId);
  const updateStatus = useUpdateFiscalPeriodStatus();

  const columns = [
    { key: 'year', header: t('accounting:fiscalPeriods.columns.year') },
    {
      key: 'month',
      header: t('accounting:fiscalPeriods.columns.month'),
      render: (item: Record<string, unknown>) =>
        MONTH_NAMES[String(item.month ?? '')] ?? String(item.month ?? ''),
    },
    {
      key: 'status',
      header: t('accounting:fiscalPeriods.columns.status'),
      render: (item: Record<string, unknown>) => {
        const status = String(item.status ?? '');
        return (
          <Badge variant={statusVariant(status)}>
            {t(`accounting:fiscalPeriods.status.${status}`, { defaultValue: status })}
          </Badge>
        );
      },
    },
    { key: 'exportCount', header: t('accounting:fiscalPeriods.columns.exportCount') },
    {
      key: 'actions',
      header: '',
      render: (item: Record<string, unknown>) => {
        const status = String(item.status ?? '');
        if (status === 'hard_closed') return null;
        if (status === 'open') {
          return (
            <Button
              size="sm"
              variant="outline"
              onClick={(e) => {
                e.stopPropagation();
                updateStatus.mutate({
                  id: String(item.id),
                  status: 'soft_close',
                  entityId: selectedEntityId ?? '',
                });
              }}
            >
              {t('accounting:fiscalPeriods.actions.close')}
            </Button>
          );
        }
        return (
          <Button
            size="sm"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              updateStatus.mutate({
                id: String(item.id),
                status: 'open',
                entityId: selectedEntityId ?? '',
              });
            }}
          >
            {t('accounting:fiscalPeriods.actions.reopen')}
          </Button>
        );
      },
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:fiscalPeriods.title')} />
        <p className="text-muted-foreground">{t('accounting:fiscalPeriods.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader title={t('accounting:fiscalPeriods.title')} />
      <DataTable
        columns={columns}
        data={(periods ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('accounting:fiscalPeriods.noEntries')}
      />
    </div>
  );
}
