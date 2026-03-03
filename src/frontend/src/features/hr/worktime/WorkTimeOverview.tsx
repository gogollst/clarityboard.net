import { useState } from 'react';
import { useParams } from 'react-router-dom';
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

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('de-DE');
}

function formatMinutes(totalMinutes: number): string {
  const h = Math.floor(totalMinutes / 60);
  const min = totalMinutes % 60;
  return `${h}:${min.toString().padStart(2, '0')}`;
}

function getEntryTypeLabel(entryType: string): string {
  switch (entryType) {
    case 'Work':
      return 'Arbeit';
    case 'Overtime':
      return 'Überstunden';
    case 'Oncall':
      return 'Bereitschaft';
    default:
      return entryType;
  }
}

function getStatusBadge(status: string) {
  switch (status) {
    case 'Open':
      return (
        <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
          Offen
        </Badge>
      );
    case 'Locked':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          Gesperrt
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { employeeId } = useParams<{ employeeId: string }>();
  const [month, setMonth] = useState(() => new Date().toISOString().substring(0, 7));

  const { data, isLoading } = useWorkTime(employeeId ?? '', month);
  const entries: WorkTimeEntry[] = data?.items ?? [];

  const totalMonthMinutes = entries.reduce((sum, e) => sum + (e.totalMinutes ?? 0), 0);

  if (!employeeId) {
    return (
      <div>
        <PageHeader title="Arbeitszeitübersicht" />
        <p className="mt-8 text-center text-sm text-muted-foreground">
          Kein Mitarbeiter ausgewählt. Bitte rufen Sie diese Seite über
          <code className="mx-1 rounded bg-muted px-1 py-0.5 font-mono text-xs">
            /hr/worktime/:employeeId
          </code>
          auf.
        </p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader title="Arbeitszeitübersicht" />

      {/* Month selector */}
      <div className="mb-6 flex items-center gap-3">
        <div className="space-y-1">
          <Label htmlFor="monthPicker">Monat</Label>
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
          Keine Einträge für diesen Monat.
        </p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Datum</TableHead>
              <TableHead>Von</TableHead>
              <TableHead>Bis</TableHead>
              <TableHead className="text-right">Pause (min)</TableHead>
              <TableHead className="text-right">Gesamt</TableHead>
              <TableHead>Typ</TableHead>
              <TableHead>Projekt</TableHead>
              <TableHead>Status</TableHead>
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
                  {getStatusBadge(entry.status)}
                </TableCell>
              </TableRow>
            ))}

            {/* Summary row */}
            <TableRow className="border-t-2 font-semibold">
              <TableCell colSpan={4} className="text-sm">
                Gesamt
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
