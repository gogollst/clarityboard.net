import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { formatCurrency } from '@/lib/format';
import { useProfitAndLoss } from '@/hooks/useAccounting';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

function buildMonths(locale: string) {
  const fmt = new Intl.DateTimeFormat(locale, { month: 'short' });
  return Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: fmt.format(new Date(2024, i, 1)),
  }));
}

const now = new Date();
const currentYear = now.getFullYear();
const currentMonth = now.getMonth() + 1;

export function Component() {
  const { t, i18n } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const months = useMemo(() => buildMonths(i18n.language), [i18n.language]);
  const [year, setYear] = useState(currentYear);
  const [month, setMonth] = useState(currentMonth);
  const [compare, setCompare] = useState(false);
  const [compareYear, setCompareYear] = useState(currentYear - 1);
  const [compareMonth, setCompareMonth] = useState(currentMonth);

  const { data, isLoading } = useProfitAndLoss(
    selectedEntityId,
    year,
    month,
    compare ? compareYear : undefined,
    compare ? compareMonth : undefined,
  );

  const yearOptions = Array.from({ length: 4 }, (_, i) => currentYear - 2 + i);
  const showPrior = compare && data?.compareYear != null;

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:profitAndLoss.title')} />
        <p className="text-muted-foreground">{t('accounting:profitAndLoss.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('accounting:profitAndLoss.title')}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <Select value={String(year)} onValueChange={(v) => setYear(Number(v))}>
              <SelectTrigger className="w-24"><SelectValue /></SelectTrigger>
              <SelectContent>
                {yearOptions.map((y) => (
                  <SelectItem key={y} value={String(y)}>{y}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={String(month)} onValueChange={(v) => setMonth(Number(v))}>
              <SelectTrigger className="w-24"><SelectValue /></SelectTrigger>
              <SelectContent>
                {months.map((m) => (
                  <SelectItem key={m.value} value={String(m.value)}>{m.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="flex items-center gap-2 ml-4">
              <Checkbox
                id="compare"
                checked={compare}
                onCheckedChange={(v) => setCompare(v === true)}
              />
              <Label htmlFor="compare">{t('accounting:profitAndLoss.compareWith')}</Label>
            </div>
            {compare && (
              <>
                <Select value={String(compareYear)} onValueChange={(v) => setCompareYear(Number(v))}>
                  <SelectTrigger className="w-24"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {yearOptions.map((y) => (
                      <SelectItem key={y} value={String(y)}>{y}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Select value={String(compareMonth)} onValueChange={(v) => setCompareMonth(Number(v))}>
                  <SelectTrigger className="w-24"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {months.map((m) => (
                      <SelectItem key={m.value} value={String(m.value)}>{m.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </>
            )}
          </div>
        }
      />

      {isLoading && (
        <div className="space-y-4">
          <Skeleton className="h-48 w-full" />
          <Skeleton className="h-48 w-full" />
        </div>
      )}

      {data && (
        <>
          {data.sections.map((section) => (
            <Card key={section.name}>
              <CardHeader className="pb-2">
                <CardTitle className="text-base">{t(`accounting:profitAndLoss.sections.${section.name}`, section.name)}</CardTitle>
              </CardHeader>
              <CardContent>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-muted-foreground">
                      <th className="py-2 text-left font-medium">{t('accounting:profitAndLoss.position')}</th>
                      <th className="py-2 text-right font-medium">{t('accounting:profitAndLoss.amount')}</th>
                      {showPrior && (
                        <th className="py-2 text-right font-medium">{t('accounting:profitAndLoss.priorAmount')}</th>
                      )}
                    </tr>
                  </thead>
                  <tbody>
                    {section.items.map((item) => (
                      <tr key={item.label} className="border-b border-border/50">
                        <td className="py-1.5">{t(`accounting:profitAndLoss.items.${item.label}`, item.label)}</td>
                        <td className={`py-1.5 text-right tabular-nums ${item.amount < 0 ? 'text-destructive' : ''}`}>
                          {formatCurrency(item.amount)}
                        </td>
                        {showPrior && (
                          <td className={`py-1.5 text-right tabular-nums ${(item.priorAmount ?? 0) < 0 ? 'text-destructive' : ''}`}>
                            {item.priorAmount != null ? formatCurrency(item.priorAmount) : '–'}
                          </td>
                        )}
                      </tr>
                    ))}
                    <tr className="font-semibold">
                      <td className="py-2">{t('accounting:profitAndLoss.subtotal')}</td>
                      <td className={`py-2 text-right tabular-nums ${section.subtotal < 0 ? 'text-destructive' : ''}`}>
                        {formatCurrency(section.subtotal)}
                      </td>
                      {showPrior && (
                        <td className={`py-2 text-right tabular-nums ${(section.priorSubtotal ?? 0) < 0 ? 'text-destructive' : ''}`}>
                          {section.priorSubtotal != null ? formatCurrency(section.priorSubtotal) : '–'}
                        </td>
                      )}
                    </tr>
                  </tbody>
                </table>
              </CardContent>
            </Card>
          ))}

          <Card className="border-2 border-primary/20 bg-primary/5">
            <CardContent className="flex items-center justify-between py-4">
              <span className="text-lg font-bold">{t('accounting:profitAndLoss.netIncome')}</span>
              <div className="flex gap-8">
                <span className={`text-lg font-bold tabular-nums ${data.netIncome < 0 ? 'text-destructive' : ''}`}>
                  {formatCurrency(data.netIncome)}
                </span>
                {showPrior && data.priorNetIncome != null && (
                  <span className={`text-lg tabular-nums text-muted-foreground ${data.priorNetIncome < 0 ? 'text-destructive' : ''}`}>
                    {formatCurrency(data.priorNetIncome)}
                  </span>
                )}
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
