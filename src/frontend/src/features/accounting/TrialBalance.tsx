import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { formatCurrency } from '@/lib/format';
import { useTrialBalance } from '@/hooks/useAccounting';
import { useDepartments } from '@/hooks/useHr';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const MONTHS = [
  { value: 1, label: 'Jan' }, { value: 2, label: 'Feb' }, { value: 3, label: 'Mär' },
  { value: 4, label: 'Apr' }, { value: 5, label: 'Mai' }, { value: 6, label: 'Jun' },
  { value: 7, label: 'Jul' }, { value: 8, label: 'Aug' }, { value: 9, label: 'Sep' },
  { value: 10, label: 'Okt' }, { value: 11, label: 'Nov' }, { value: 12, label: 'Dez' },
];

const now = new Date();
const currentYear = now.getFullYear();
const currentMonth = now.getMonth() + 1;

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const [year, setYear] = useState(currentYear);
  const [month, setMonth] = useState(currentMonth);
  const [departmentId, setDepartmentId] = useState('');

  const { data: departments } = useDepartments(selectedEntityId ?? undefined);
  const activeDepartments = useMemo(
    () => (departments ?? []).filter((d) => d.isActive),
    [departments],
  );

  const { data, isLoading } = useTrialBalance(selectedEntityId, year, month, departmentId || undefined);

  const yearOptions = Array.from({ length: 4 }, (_, i) => currentYear - 2 + i);

  const columns = [
    { key: 'accountNumber', header: t('accounting:trialBalance.columns.accountNumber') },
    { key: 'accountName', header: t('accounting:trialBalance.columns.accountName') },
    { key: 'accountType', header: t('accounting:trialBalance.columns.accountType') },
    {
      key: 'debitTotal',
      header: t('accounting:trialBalance.columns.debit'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.debitTotal ?? 0))}</span>
      ),
    },
    {
      key: 'creditTotal',
      header: t('accounting:trialBalance.columns.credit'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.creditTotal ?? 0))}</span>
      ),
    },
    {
      key: 'balance',
      header: t('accounting:trialBalance.columns.balance'),
      render: (item: Record<string, unknown>) => {
        const val = Number(item.balance ?? 0);
        return (
          <span className={`tabular-nums font-medium ${val < 0 ? 'text-destructive' : ''}`}>
            {formatCurrency(val)}
          </span>
        );
      },
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:trialBalance.title')} />
        <p className="text-muted-foreground">{t('accounting:trialBalance.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t('accounting:trialBalance.title')}
        actions={
          <div className="flex items-center gap-2">
            <Select value={String(year)} onValueChange={(v) => setYear(Number(v))}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {yearOptions.map((y) => (
                  <SelectItem key={y} value={String(y)}>{y}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={String(month)} onValueChange={(v) => setMonth(Number(v))}>
              <SelectTrigger className="w-24">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {MONTHS.map((m) => (
                  <SelectItem key={m.value} value={String(m.value)}>{m.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {activeDepartments.length > 0 && (
              <Select value={departmentId || 'all'} onValueChange={(v) => setDepartmentId(v === 'all' ? '' : v)}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder={t('accounting:departmentFilterAll')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('accounting:departmentFilterAll')}</SelectItem>
                  {activeDepartments.map((d) => (
                    <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
        }
      />
      <DataTable
        columns={columns}
        data={(data?.lines ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('accounting:trialBalance.noEntries')}
      />
      {data && (
        <div className="mt-4 flex justify-end gap-8 rounded-md border bg-muted/50 px-4 py-3 text-sm font-semibold">
          <span>{t('accounting:trialBalance.totals')}</span>
          <span className="tabular-nums">{formatCurrency(data.totalDebits)}</span>
          <span className="tabular-nums">{formatCurrency(data.totalCredits)}</span>
        </div>
      )}
    </div>
  );
}
