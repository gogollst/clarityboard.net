import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useEntity } from '@/hooks/useEntity';
import { formatCurrency } from '@/lib/format';
import {
  useDeferredRevenueOverview,
  usePendingRevenueEntries,
  usePostRevenueEntry,
  usePostAllDueRevenueEntries,
} from '@/hooks/useDocuments';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { RevenueScheduleEntry } from '@/types/document';

function buildMonthOptions(): { value: string; label: string }[] {
  const now = new Date();
  return Array.from({ length: 4 }, (_, i) => {
    const d = new Date(now.getFullYear(), now.getMonth() + i, 1);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    return { value: `${yyyy}-${mm}-01`, label: `${yyyy}-${mm}` };
  });
}

function statusVariant(status: RevenueScheduleEntry['status']) {
  switch (status) {
    case 'booked':
      return 'default' as const;
    case 'cancelled':
      return 'destructive' as const;
    default:
      return 'secondary' as const;
  }
}

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();

  const monthOptions = useMemo(() => buildMonthOptions(), []);
  const [selectedMonth, setSelectedMonth] = useState(monthOptions[0].value);

  const { data: overview, isLoading: overviewLoading } =
    useDeferredRevenueOverview(selectedEntityId);
  const { data: pending, isLoading: pendingLoading } =
    usePendingRevenueEntries(selectedEntityId, selectedMonth);

  const postEntry = usePostRevenueEntry();
  const postAll = usePostAllDueRevenueEntries();

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:deferredRevenue.title')} />
        <p className="text-muted-foreground">
          {t('accounting:deferredRevenue.noEntitySelected')}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('accounting:deferredRevenue.title')}
        description={t('accounting:deferredRevenue.description')}
      />

      {/* Summary Cards */}
      {overviewLoading ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24" />
          ))}
        </div>
      ) : overview ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-sm font-medium">
                {t('accounting:deferredRevenue.praBalance')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tabular-nums">
                {formatCurrency(overview.totalPraBalance)}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-sm font-medium">
                {t('accounting:deferredRevenue.dueThisMonth')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tabular-nums">
                {formatCurrency(overview.dueThisMonth)}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-sm font-medium">
                {t('accounting:deferredRevenue.dueNextMonth')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tabular-nums">
                {formatCurrency(overview.dueNextMonth)}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-sm font-medium">
                {t('accounting:deferredRevenue.entries')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tabular-nums">
                {overview.totalPlannedEntries}{' '}
                <span className="text-sm font-normal text-muted-foreground">
                  {t('accounting:deferredRevenue.planned')}
                </span>{' '}
                / {overview.totalBookedEntries}{' '}
                <span className="text-sm font-normal text-muted-foreground">
                  {t('accounting:deferredRevenue.booked')}
                </span>
              </div>
            </CardContent>
          </Card>
        </div>
      ) : null}

      {/* Month selector + Post All */}
      <div className="flex items-center justify-between">
        <Select value={selectedMonth} onValueChange={setSelectedMonth}>
          <SelectTrigger className="w-48">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {monthOptions.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Button
          onClick={() =>
            postAll.mutate({ entityId: selectedEntityId, upToMonth: selectedMonth })
          }
          disabled={postAll.isPending || !pending?.items?.length}
        >
          {postAll.isPending
            ? t('common:buttons.processing')
            : t('accounting:deferredRevenue.postAll')}
        </Button>
      </div>

      {/* Pending entries table */}
      {pendingLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : !pending?.items?.length ? (
        <EmptyState
          title={t('accounting:deferredRevenue.emptyTitle')}
          description={t('accounting:deferredRevenue.emptyDescription')}
        />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">
                  {t('accounting:deferredRevenue.columns.document')}
                </th>
                <th className="px-4 py-3 text-left font-medium">
                  {t('accounting:deferredRevenue.columns.period')}
                </th>
                <th className="px-4 py-3 text-right font-medium">
                  {t('accounting:deferredRevenue.columns.amount')}
                </th>
                <th className="px-4 py-3 text-left font-medium">
                  {t('accounting:deferredRevenue.columns.account')}
                </th>
                <th className="px-4 py-3 text-left font-medium">
                  {t('accounting:deferredRevenue.columns.status')}
                </th>
                <th className="px-4 py-3 text-right font-medium">
                  {t('accounting:deferredRevenue.columns.action')}
                </th>
              </tr>
            </thead>
            <tbody>
              {pending.items.map((entry: RevenueScheduleEntry) => (
                <tr key={entry.id} className="border-b">
                  <td className="px-4 py-3">
                    <Link
                      to={`/documents/${entry.documentId}`}
                      className="text-primary underline-offset-4 hover:underline"
                    >
                      {entry.documentId.slice(0, 8)}...
                    </Link>
                  </td>
                  <td className="px-4 py-3">{entry.periodDate.slice(0, 7)}</td>
                  <td className="px-4 py-3 text-right tabular-nums">
                    {formatCurrency(entry.amount)}
                  </td>
                  <td className="px-4 py-3">{entry.revenueAccountNumber}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(entry.status)}>
                      {t(`accounting:deferredRevenue.status.${entry.status}`)}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {entry.status === 'planned' && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={postEntry.isPending}
                        onClick={() =>
                          postEntry.mutate({
                            entityId: selectedEntityId,
                            entryId: entry.id,
                          })
                        }
                      >
                        {t('accounting:deferredRevenue.post')}
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
