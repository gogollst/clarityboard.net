import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useBalanceSheet } from '@/hooks/useAccounting';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { BalanceSheetSection as BSSection } from '@/types/accounting';

const MONTHS = [
  { value: 1, label: 'Jan' }, { value: 2, label: 'Feb' }, { value: 3, label: 'Mär' },
  { value: 4, label: 'Apr' }, { value: 5, label: 'Mai' }, { value: 6, label: 'Jun' },
  { value: 7, label: 'Jul' }, { value: 8, label: 'Aug' }, { value: 9, label: 'Sep' },
  { value: 10, label: 'Okt' }, { value: 11, label: 'Nov' }, { value: 12, label: 'Dez' },
];

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
  }).format(value);
}

const now = new Date();
const currentYear = now.getFullYear();
const currentMonth = now.getMonth() + 1;

function SectionTable({
  sections,
  showPrior,
  amountLabel,
  priorLabel,
}: {
  sections: BSSection[];
  showPrior: boolean;
  amountLabel: string;
  priorLabel: string;
}) {
  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="border-b text-muted-foreground">
          <th className="py-2 text-left font-medium"></th>
          <th className="py-2 text-right font-medium">{amountLabel}</th>
          {showPrior && <th className="py-2 text-right font-medium">{priorLabel}</th>}
        </tr>
      </thead>
      <tbody>
        {sections.map((section) => (
          <SectionRows key={section.name} section={section} showPrior={showPrior} />
        ))}
      </tbody>
    </table>
  );
}

function SectionRows({ section, showPrior }: { section: BSSection; showPrior: boolean }) {
  return (
    <>
      <tr>
        <td colSpan={showPrior ? 3 : 2} className="pt-3 pb-1 font-semibold text-muted-foreground text-xs uppercase tracking-wide">
          {section.name}
        </td>
      </tr>
      {section.items.map((item) => (
        <tr key={item.label} className="border-b border-border/50">
          <td className="py-1.5 pl-4">{item.label}</td>
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
      <tr className="font-semibold border-b">
        <td className="py-2 pl-4">{section.name}</td>
        <td className={`py-2 text-right tabular-nums ${section.subtotal < 0 ? 'text-destructive' : ''}`}>
          {formatCurrency(section.subtotal)}
        </td>
        {showPrior && (
          <td className={`py-2 text-right tabular-nums ${(section.priorSubtotal ?? 0) < 0 ? 'text-destructive' : ''}`}>
            {section.priorSubtotal != null ? formatCurrency(section.priorSubtotal) : '–'}
          </td>
        )}
      </tr>
    </>
  );
}

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const [year, setYear] = useState(currentYear);
  const [month, setMonth] = useState(currentMonth);
  const [compare, setCompare] = useState(false);
  const [compareYear, setCompareYear] = useState(currentYear - 1);
  const [compareMonth, setCompareMonth] = useState(currentMonth);

  const { data, isLoading } = useBalanceSheet(
    selectedEntityId,
    year,
    month,
    compare ? compareYear : undefined,
    compare ? compareMonth : undefined,
  );

  const yearOptions = Array.from({ length: 4 }, (_, i) => currentYear - 2 + i);
  const showPrior = compare && data?.priorDate != null;

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:balanceSheet.title')} />
        <p className="text-muted-foreground">{t('accounting:balanceSheet.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('accounting:balanceSheet.title')}
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
                {MONTHS.map((m) => (
                  <SelectItem key={m.value} value={String(m.value)}>{m.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="flex items-center gap-2 ml-4">
              <Checkbox
                id="bs-compare"
                checked={compare}
                onCheckedChange={(v) => setCompare(v === true)}
              />
              <Label htmlFor="bs-compare">{t('accounting:balanceSheet.compareWith')}</Label>
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
                    {MONTHS.map((m) => (
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
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      )}

      {data && (
        <>
          {/* AKTIVA */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle>{t('accounting:balanceSheet.assets')}</CardTitle>
            </CardHeader>
            <CardContent>
              <SectionTable
                sections={data.assets}
                showPrior={showPrior}
                amountLabel={t('accounting:balanceSheet.amount')}
                priorLabel={t('accounting:balanceSheet.priorAmount')}
              />
              <div className="mt-2 flex justify-between border-t-2 pt-2 font-bold">
                <span>{t('accounting:balanceSheet.totalAssets')}</span>
                <div className="flex gap-8">
                  <span className="tabular-nums">{formatCurrency(data.totalAssets)}</span>
                  {showPrior && data.priorTotalAssets != null && (
                    <span className="tabular-nums text-muted-foreground">{formatCurrency(data.priorTotalAssets)}</span>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* PASSIVA */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle>{t('accounting:balanceSheet.liabilitiesAndEquity')}</CardTitle>
            </CardHeader>
            <CardContent>
              <SectionTable
                sections={data.liabilitiesAndEquity}
                showPrior={showPrior}
                amountLabel={t('accounting:balanceSheet.amount')}
                priorLabel={t('accounting:balanceSheet.priorAmount')}
              />
              <div className="mt-2 flex justify-between border-t-2 pt-2 font-bold">
                <span>{t('accounting:balanceSheet.totalLiabilitiesAndEquity')}</span>
                <div className="flex gap-8">
                  <span className="tabular-nums">{formatCurrency(data.totalLiabilitiesAndEquity)}</span>
                  {showPrior && data.priorTotalLiabilitiesAndEquity != null && (
                    <span className="tabular-nums text-muted-foreground">{formatCurrency(data.priorTotalLiabilitiesAndEquity)}</span>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Balance Check */}
          <div className="flex justify-center">
            {Math.abs(data.totalAssets - data.totalLiabilitiesAndEquity) < 0.01 ? (
              <Badge variant="default" className="text-sm px-4 py-1">
                {t('accounting:balanceSheet.balanced')}
              </Badge>
            ) : (
              <Badge variant="destructive" className="text-sm px-4 py-1">
                {t('accounting:balanceSheet.imbalanced')}
              </Badge>
            )}
          </div>
        </>
      )}
    </div>
  );
}
