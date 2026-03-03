import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTravelExpenses } from '@/hooks/useHr';
import type { TravelExpenseReport } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { Plus } from 'lucide-react';
import { formatDate } from '../utils';

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

function formatEur(totalAmountCents: number): string {
  return (totalAmountCents / 100).toLocaleString('de-DE', {
    style: 'currency',
    currency: 'EUR',
  });
}

export function Component() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');

  useEffect(() => {
    setPage(1);
  }, [status]);

  const { data, isLoading } = useTravelExpenses({
    page,
    pageSize: 25,
    status: status || undefined,
  });

  const reports: TravelExpenseReport[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 25;

  const columns = [
    {
      key: 'employeeFullName',
      header: 'Mitarbeiter',
      render: (item: Record<string, unknown>) => (
        <span className="font-medium text-foreground">
          {String(item.employeeFullName ?? '—')}
        </span>
      ),
    },
    {
      key: 'title',
      header: 'Titel',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm">{String(item.title ?? '—')}</span>
      ),
    },
    {
      key: 'tripStartDate',
      header: 'Reisezeitraum',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDate(item.tripStartDate as string | undefined)} –{' '}
          {formatDate(item.tripEndDate as string | undefined)}
        </span>
      ),
    },
    {
      key: 'destination',
      header: 'Ziel',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.destination ?? '—')}
        </span>
      ),
    },
    {
      key: 'totalAmountCents',
      header: 'Gesamtbetrag',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm font-medium tabular-nums text-right block">
          {formatEur(Number(item.totalAmountCents ?? 0))}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item: Record<string, unknown>) =>
        getTravelStatusBadge(String(item.status ?? '')),
    },
    {
      key: 'actions',
      header: '',
      render: (item: Record<string, unknown>) => (
        <Button
          size="sm"
          variant="outline"
          onClick={() => navigate(`/hr/travel/${String(item.id ?? '')}`)}
        >
          Details
        </Button>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Reisekosten"
        actions={
          <Button onClick={() => navigate('/hr/travel/new')}>
            <Plus className="mr-1 h-4 w-4" />
            Neue Abrechnung
          </Button>
        }
      />

      {/* Filter bar */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Select
          value={status || 'all'}
          onValueChange={(v) => setStatus(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Alle Status</SelectItem>
            <SelectItem value="Draft">Entwurf</SelectItem>
            <SelectItem value="Submitted">Eingereicht</SelectItem>
            <SelectItem value="Approved">Genehmigt</SelectItem>
            <SelectItem value="Reimbursed">Erstattet</SelectItem>
            <SelectItem value="Rejected">Abgelehnt</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={reports as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage="Keine Reisekostenabrechnungen gefunden."
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />
    </div>
  );
}
