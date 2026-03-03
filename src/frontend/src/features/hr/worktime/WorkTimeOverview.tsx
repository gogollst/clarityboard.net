import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useWorkTime } from '@/hooks/useHr';
import type { WorkTimeEntry } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatMinutes(totalMinutes: number): string {
  const h = Math.floor(totalMinutes / 60);
  const min = totalMinutes % 60;
  return `${h}:${min.toString().padStart(2, '0')}`;
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const { employeeId } = useParams<{ employeeId: string }>();
  const [month, setMonth] = useState(() => new Date().toISOString().substring(0, 7));

  const { data, isLoading } = useWorkTime(employeeId ?? '', month);

  // Guard MUST come after all hooks but before derived state
  if (!employeeId) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('worktime.noEmployee')}
      </div>
    );
  }

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function getEntryTypeLabel(entryType: string): string {
    switch (entryType) {
      case 'Work':     return t('worktime.entryType.Work');
      case 'Overtime': return t('worktime.entryType.Overtime');
      case 'Oncall':   return t('worktime.entryType.Oncall');
      default:         return entryType;
    }
  }

  function getWorkTimeStatusBadge(status: string) {
    switch (status) {
      case 'Open':
        return (
          <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
            {t('worktime.status.Open')}
          </Badge>
        );
      case 'Locked':
        return (
          <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
            {t('worktime.status.Locked')}
          </Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  }

  const entries: WorkTimeEntry[] = data?.items ?? [];
  const totalMonthMinutes = entries.reduce((sum, e) => sum + (e.totalMinutes ?? 0), 0);

  return (
    <div>
      <PageHeader title={t('worktime.title')} />

      {/* Month selector */}
      <div className="mb-6 flex items-center gap-3">
        <div className="space-y-1">
          <Label htmlFor="monthPicker">{t('worktime.month')}</Label>
          <Input
            id="monthPicker"
            type="month"
            value={month}
            onChange={(e) => setMonth(e.target.value)}
            className="w-44"
          />
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      ) : entries.length === 0 ? (
        <p className="py-12 text-center text-sm text-muted-foreground">
          {t('worktime.noEntries')}
        </p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('worktime.columns.date')}</TableHead>
              <TableHead>{t('worktime.columns.from')}</TableHead>
              <TableHead>{t('worktime.columns.to')}</TableHead>
              <TableHead className="text-right">{t('worktime.columns.breakMin')}</TableHead>
              <TableHead className="text-right">{t('worktime.columns.total')}</TableHead>
              <TableHead>{t('worktime.columns.type')}</TableHead>
              <TableHead>{t('worktime.columns.project')}</TableHead>
              <TableHead>{t('worktime.columns.status')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {entries.map((entry) => (
              <TableRow key={entry.id}>
                <TableCell className="text-sm tabular-nums">
                  {formatDate(entry.date)}
                </TableCell>
                <TableCell className="text-sm tabular-nums text-muted-foreground">
                  {entry.startTime ?? '—'}
                </TableCell>
                <TableCell className="text-sm tabular-nums text-muted-foreground">
                  {entry.endTime ?? '—'}
                </TableCell>
                <TableCell className="text-right text-sm tabular-nums text-muted-foreground">
                  {entry.breakMinutes}
                </TableCell>
                <TableCell className="text-right font-medium tabular-nums">
                  {formatMinutes(entry.totalMinutes)}
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {getEntryTypeLabel(entry.entryType)}
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {entry.projectCode ?? '—'}
                </TableCell>
                <TableCell>
                  {getWorkTimeStatusBadge(entry.status)}
                </TableCell>
              </TableRow>
            ))}

            {/* Summary row */}
            <TableRow className="border-t-2 font-semibold">
              <TableCell colSpan={4} className="text-sm">
                {t('worktime.total')}
              </TableCell>
              <TableCell className="text-right tabular-nums">
                {formatMinutes(totalMonthMinutes)}
              </TableCell>
              <TableCell colSpan={3} />
            </TableRow>
          </TableBody>
        </Table>
      )}
    </div>
  );
}
