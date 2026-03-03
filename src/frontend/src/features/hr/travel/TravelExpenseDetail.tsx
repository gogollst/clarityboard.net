import { useParams, useNavigate } from 'react-router-dom';
import {
  useTravelExpense,
  useSubmitTravelExpense,
  useApproveTravelExpense,
} from '@/hooks/useHr';
import { useAuth } from '@/hooks/useAuth';
import type { TravelExpenseItem } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { formatDate, formatEur } from '../utils';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatAmount(cents: number, currency: string): string {
  try {
    return (cents / 100).toLocaleString('de-DE', {
      style: 'currency',
      currency,
    });
  } catch {
    return `${(cents / 100).toFixed(2)} ${currency}`;
  }
}

function getTravelStatusBadge(status: string) {
  switch (status) {
    case 'Draft':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          Entwurf
        </Badge>
      );
    case 'Submitted':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          Eingereicht
        </Badge>
      );
    case 'Approved':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          Genehmigt
        </Badge>
      );
    case 'Reimbursed':
      return (
        <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
          Erstattet
        </Badge>
      );
    case 'Rejected':
      return (
        <Badge className="bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300">
          Abgelehnt
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

function expenseTypeLabel(type: string): string {
  switch (type) {
    case 'Accommodation':
      return 'Unterkunft';
    case 'Transport':
      return 'Transport';
    case 'Meal':
      return 'Verpflegung';
    case 'Other':
      return 'Sonstiges';
    default:
      return type;
  }
}

// ---------------------------------------------------------------------------
// Detail row helper
// ---------------------------------------------------------------------------

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value}</dd>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Items table
// ---------------------------------------------------------------------------

function ItemsTable({ items }: { items: TravelExpenseItem[] }) {
  if (items.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        Keine Belege vorhanden.
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Datum</TableHead>
          <TableHead>Typ</TableHead>
          <TableHead>Beschreibung</TableHead>
          <TableHead className="text-right">Betrag (Original)</TableHead>
          <TableHead className="text-right">Kurs</TableHead>
          <TableHead className="text-right">Betrag (EUR)</TableHead>
          <TableHead className="text-right">MwSt %</TableHead>
          <TableHead>Abzugsfähig</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((item) => (
          <TableRow key={item.id}>
            <TableCell className="text-sm tabular-nums">
              {formatDate(item.expenseDate)}
            </TableCell>
            <TableCell className="text-sm">
              {expenseTypeLabel(item.expenseType)}
            </TableCell>
            <TableCell className="text-sm text-muted-foreground">
              {item.description || '—'}
            </TableCell>
            <TableCell className="text-right text-sm tabular-nums">
              {formatAmount(item.originalAmountCents, item.originalCurrencyCode)}
            </TableCell>
            <TableCell className="text-right text-sm tabular-nums text-muted-foreground">
              {item.exchangeRate.toLocaleString('de-DE', { minimumFractionDigits: 4 })}
            </TableCell>
            <TableCell className="text-right text-sm font-medium tabular-nums">
              {formatEur(item.amountCents)}
            </TableCell>
            <TableCell className="text-right text-sm tabular-nums text-muted-foreground">
              {item.vatRatePercent != null
                ? `${item.vatRatePercent.toLocaleString('de-DE')} %`
                : '—'}
            </TableCell>
            <TableCell>
              {item.isDeductible ? (
                <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
                  Ja
                </Badge>
              ) : (
                <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
                  Nein
                </Badge>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const { data: report, isLoading } = useTravelExpense(id ?? '');
  const submitMutation = useSubmitTravelExpense();
  const approveMutation = useApproveTravelExpense();

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="mb-4 h-48 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!report) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        Abrechnung nicht gefunden.
      </div>
    );
  }

  const canSubmit = report.status === 'Draft';
  const canApprove = report.status === 'Submitted' && hasPermission('hr.manager');

  return (
    <div>
      <PageHeader
        title={report.title}
        actions={
          <div className="flex items-center gap-2">
            {getTravelStatusBadge(report.status)}
            {canSubmit && (
              <Button
                size="sm"
                disabled={submitMutation.isPending}
                onClick={() => submitMutation.mutate(report.id)}
              >
                {submitMutation.isPending && (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                )}
                Einreichen
              </Button>
            )}
            {canApprove && (
              <Button
                size="sm"
                disabled={approveMutation.isPending}
                onClick={() => approveMutation.mutate(report.id)}
              >
                {approveMutation.isPending && (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                )}
                Genehmigen
              </Button>
            )}
            <Button variant="outline" size="sm" onClick={() => navigate('/hr/travel')}>
              <ArrowLeft className="mr-1 h-4 w-4" />
              Zurück
            </Button>
          </div>
        }
      />

      {/* Report metadata */}
      <Card className="mb-6">
        <CardContent className="pt-6">
          <dl className="grid grid-cols-2 gap-6 sm:grid-cols-3">
            <DetailRow label="Mitarbeiter" value={report.employeeFullName} />
            <DetailRow label="Titel" value={report.title} />
            <DetailRow
              label="Reisezeitraum"
              value={`${formatDate(report.tripStartDate)} – ${formatDate(report.tripEndDate)}`}
            />
            <DetailRow label="Ziel" value={report.destination} />
            <DetailRow label="Geschäftszweck" value={report.businessPurpose} />
            <DetailRow label="Status" value={getTravelStatusBadge(report.status)} />
            <DetailRow
              label="Gesamtbetrag"
              value={
                <span className="tabular-nums">
                  {(report.totalAmountCents / 100).toLocaleString('de-DE', {
                    style: 'currency',
                    currency: 'EUR',
                  })}
                </span>
              }
            />
            <DetailRow label="Erstellt am" value={formatDate(report.createdAt)} />
          </dl>
        </CardContent>
      </Card>

      {/* Items */}
      <Card>
        <CardContent className="pt-6">
          <h3 className="mb-4 text-sm font-semibold text-foreground">Belege</h3>
          <ItemsTable items={report.items} />
        </CardContent>
      </Card>
    </div>
  );
}
