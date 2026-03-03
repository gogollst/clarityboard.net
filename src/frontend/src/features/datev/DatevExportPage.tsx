import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useTriggerExport, useDatevExports } from '@/hooks/useDatev';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Download, Loader2 } from 'lucide-react';

const MONTH_VALUES = ['01', '02', '03', '04', '05', '06', '07', '08', '09', '10', '11', '12'] as const;

const YEARS = Array.from({ length: 5 }, (_, i) => {
  const year = new Date().getFullYear() - i;
  return { value: String(year), label: String(year) };
});

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  pending: 'warning',
  completed: 'success',
  failed: 'destructive',
};

export function Component() {
  const { t, i18n } = useTranslation('datev');
  const { selectedEntityId } = useEntity();
  const { data: exports, isLoading } = useDatevExports(selectedEntityId);
  const triggerExport = useTriggerExport();

  const [startMonth, setStartMonth] = useState('01');
  const [startYear, setStartYear] = useState(String(new Date().getFullYear()));
  const [endMonth, setEndMonth] = useState(
    String(new Date().getMonth() + 1).padStart(2, '0'),
  );
  const [endYear, setEndYear] = useState(String(new Date().getFullYear()));

  const handleExport = () => {
    if (!selectedEntityId) return;
    triggerExport.mutate({
      entityId: selectedEntityId,
      startDate: `${startYear}-${startMonth}-01`,
      endDate: `${endYear}-${endMonth}-01`,
    });
  };

  return (
    <div>
      <PageHeader
        title={t('title')}
        description={t('description')}
      />

      {/* Export Form */}
      <Card>
        <CardHeader>
          <CardTitle>{t('newExport')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <div className="flex gap-2">
              <div>
                <Label>{t('startMonth')}</Label>
                <Select value={startMonth} onValueChange={setStartMonth}>
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MONTH_VALUES.map((m) => (
                      <SelectItem key={m} value={m}>
                        {t(`months.${m}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>{t('startYear')}</Label>
                <Select value={startYear} onValueChange={setStartYear}>
                  <SelectTrigger className="w-[100px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {YEARS.map((y) => (
                      <SelectItem key={y.value} value={y.value}>
                        {y.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="flex gap-2">
              <div>
                <Label>{t('endMonth')}</Label>
                <Select value={endMonth} onValueChange={setEndMonth}>
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MONTH_VALUES.map((m) => (
                      <SelectItem key={m} value={m}>
                        {t(`months.${m}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>{t('endYear')}</Label>
                <Select value={endYear} onValueChange={setEndYear}>
                  <SelectTrigger className="w-[100px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {YEARS.map((y) => (
                      <SelectItem key={y.value} value={y.value}>
                        {y.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <Button
              onClick={handleExport}
              disabled={triggerExport.isPending}
            >
              {triggerExport.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('export')}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Export History */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>{t('exportHistory')}</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('columns.period')}</TableHead>
                  <TableHead>{t('columns.status')}</TableHead>
                  <TableHead>{t('columns.fileName')}</TableHead>
                  <TableHead>{t('columns.createdAt')}</TableHead>
                  <TableHead>{t('columns.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {exports && exports.length > 0 ? (
                  exports.map((exp) => (
                    <TableRow key={exp.id}>
                      <TableCell className="font-medium">
                        {exp.startDate} - {exp.endDate}
                      </TableCell>
                      <TableCell>
                        <StatusBadge
                          status={exp.status}
                          variantMap={STATUS_VARIANT_MAP}
                        />
                      </TableCell>
                      <TableCell>{exp.fileName ?? '-'}</TableCell>
                      <TableCell>
                        {new Date(exp.createdAt).toLocaleDateString(i18n.language)}
                      </TableCell>
                      <TableCell>
                        {exp.status === 'completed' && (
                          <Button variant="ghost" size="sm" title={t('actions.download')}>
                            <Download className="h-4 w-4" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell
                      colSpan={5}
                      className="text-center text-muted-foreground"
                    >
                      {t('noExports')}
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
