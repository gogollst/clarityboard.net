import { useDeletionRequests } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent } from '@/components/ui/card';
import type { DeletionRequest } from '@/types/hr';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('de-DE');
}

function StatusBadge({ status }: { status: string }) {
  switch (status) {
    case 'Pending':
      return (
        <Badge className="bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
          Ausstehend
        </Badge>
      );
    case 'Completed':
      return (
        <Badge className="bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400">
          Abgeschlossen
        </Badge>
      );
    case 'Blocked':
      return (
        <Badge className="bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400">
          Blockiert
        </Badge>
      );
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}

// ---------------------------------------------------------------------------
// Main Page Component
// ---------------------------------------------------------------------------

export function Component() {
  const { data, isLoading, isError } = useDeletionRequests();

  const requests: DeletionRequest[] = data?.items ?? [];

  return (
    <div className="flex flex-col gap-6 p-6">
      <PageHeader
        title="DSGVO-Löschanträge"
        description="Verwaltung geplanter Datenlöschungen gemäß DSGVO und steuerrechtlicher Aufbewahrungspflichten"
      />

      <Card>
        <CardContent className="p-0">
          {isLoading && (
            <div className="space-y-2 p-4">
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          )}

          {isError && (
            <div className="p-8 text-center text-sm text-muted-foreground">
              Fehler beim Laden der Löschanträge.
            </div>
          )}

          {!isLoading && !isError && requests.length === 0 && (
            <div className="p-8 text-center text-sm text-muted-foreground">
              Keine Löschanträge vorhanden.
            </div>
          )}

          {!isLoading && !isError && requests.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Mitarbeiter</TableHead>
                  <TableHead>Beantragt am</TableHead>
                  <TableHead>Geplante Löschung</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Blockierungsgrund</TableHead>
                  <TableHead>Abgeschlossen am</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {requests.map((req) => (
                  <TableRow key={req.id}>
                    <TableCell className="font-medium">{req.employeeFullName}</TableCell>
                    <TableCell className="text-sm whitespace-nowrap">
                      {formatDate(req.requestedAt)}
                    </TableCell>
                    <TableCell className="text-sm whitespace-nowrap">
                      {formatDate(req.scheduledDeletionAt)}
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={req.status} />
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {req.blockReason ?? '—'}
                    </TableCell>
                    <TableCell className="text-sm whitespace-nowrap">
                      {req.completedAt ? formatDate(req.completedAt) : '—'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
