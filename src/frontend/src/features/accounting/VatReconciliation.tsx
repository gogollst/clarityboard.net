import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useVatReconciliation } from '@/hooks/useAccounting';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import type { VatCategoryAmounts } from '@/types/accounting';

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
  }).format(value);
}

function formatDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function VatCategoryCard({
  label,
  amounts,
  t,
}: {
  label: string;
  amounts: VatCategoryAmounts;
  t: (key: string) => string;
}) {
  return (
    <Card>
      <CardHeader className="pb-1">
        <CardTitle className="text-sm font-medium">{label}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">{t('accounting:vatReconciliation.netAmount')}</span>
          <span className="tabular-nums">{formatCurrency(amounts.netAmount)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">{t('accounting:vatReconciliation.vatAmount')}</span>
          <span className="tabular-nums font-medium">{formatCurrency(amounts.vatAmount)}</span>
        </div>
      </CardContent>
    </Card>
  );
}

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();

  const now = new Date();
  const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
  const lastOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);

  const [periodStart, setPeriodStart] = useState(formatDate(firstOfMonth));
  const [periodEnd, setPeriodEnd] = useState(formatDate(lastOfMonth));
  const [activeStart, setActiveStart] = useState(periodStart);
  const [activeEnd, setActiveEnd] = useState(periodEnd);

  const { data, isLoading } = useVatReconciliation(selectedEntityId, activeStart, activeEnd);

  const handleApply = () => {
    setActiveStart(periodStart);
    setActiveEnd(periodEnd);
  };

  const columns = [
    { key: 'accountNumber', header: t('accounting:vatReconciliation.columns.accountNumber') },
    { key: 'accountName', header: t('accounting:vatReconciliation.columns.accountName') },
    { key: 'vatCode', header: t('accounting:vatReconciliation.columns.vatCode') },
    {
      key: 'vatRate',
      header: t('accounting:vatReconciliation.columns.vatRate'),
      render: (item: Record<string, unknown>) => <span>{Number(item.vatRate ?? 0)}%</span>,
    },
    { key: 'vatType', header: t('accounting:vatReconciliation.columns.vatType') },
    {
      key: 'netAmount',
      header: t('accounting:vatReconciliation.columns.netAmount'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.netAmount ?? 0))}</span>
      ),
    },
    {
      key: 'vatAmount',
      header: t('accounting:vatReconciliation.columns.vatAmount'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.vatAmount ?? 0))}</span>
      ),
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:vatReconciliation.title')} />
        <p className="text-muted-foreground">{t('accounting:vatReconciliation.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('accounting:vatReconciliation.title')}
        actions={
          <div className="flex items-end gap-2">
            <div className="space-y-1">
              <Label className="text-xs">{t('accounting:vatReconciliation.periodStart')}</Label>
              <Input
                type="date"
                value={periodStart}
                onChange={(e) => setPeriodStart(e.target.value)}
                className="w-40"
              />
            </div>
            <div className="space-y-1">
              <Label className="text-xs">{t('accounting:vatReconciliation.periodEnd')}</Label>
              <Input
                type="date"
                value={periodEnd}
                onChange={(e) => setPeriodEnd(e.target.value)}
                className="w-40"
              />
            </div>
            <Button onClick={handleApply} size="sm">
              {t('common:buttons.apply') ?? 'Apply'}
            </Button>
          </div>
        }
      />

      {isLoading && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-24" />
            ))}
          </div>
          <Skeleton className="h-48 w-full" />
        </div>
      )}

      {data && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
            <VatCategoryCard label={t('accounting:vatReconciliation.outputVat19')} amounts={data.outputVat19} t={t} />
            <VatCategoryCard label={t('accounting:vatReconciliation.outputVat7')} amounts={data.outputVat7} t={t} />
            <VatCategoryCard label={t('accounting:vatReconciliation.outputVat0')} amounts={data.outputVat0} t={t} />
            <VatCategoryCard label={t('accounting:vatReconciliation.inputVat')} amounts={data.inputVat} t={t} />
            <VatCategoryCard label={t('accounting:vatReconciliation.reverseCharge')} amounts={data.reverseChargeVat} t={t} />
            <VatCategoryCard label={t('accounting:vatReconciliation.intraEu')} amounts={data.intraEuAcquisitions} t={t} />
          </div>

          {/* Totals */}
          <Card className="border-2 border-primary/20 bg-primary/5">
            <CardContent className="grid grid-cols-3 gap-4 py-4 text-center">
              <div>
                <div className="text-xs text-muted-foreground">{t('accounting:vatReconciliation.totalOutputVat')}</div>
                <div className="text-lg font-semibold tabular-nums">{formatCurrency(data.totalOutputVat)}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">{t('accounting:vatReconciliation.totalInputVat')}</div>
                <div className="text-lg font-semibold tabular-nums">{formatCurrency(data.totalInputVat)}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">{t('accounting:vatReconciliation.netPayable')}</div>
                <div className={`text-lg font-bold tabular-nums ${data.netPayable < 0 ? 'text-green-600' : 'text-destructive'}`}>
                  {formatCurrency(data.netPayable)}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Detail Table */}
          <div>
            <h3 className="mb-2 text-sm font-semibold">{t('accounting:vatReconciliation.lineDetails')}</h3>
            <DataTable
              columns={columns}
              data={(data.lineDetails ?? []) as unknown as Record<string, unknown>[]}
              isLoading={false}
              emptyMessage={t('accounting:vatReconciliation.noData')}
            />
          </div>
        </>
      )}
    </div>
  );
}
