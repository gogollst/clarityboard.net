import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useDocuments, useReprocessDocument, useDeleteDocument, useDeleteDocumentPreflight } from '@/hooks/useDocuments';
import { api } from '@/lib/api';
import type { DocumentStatus } from '@/types/document';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import StatusBadge from '@/components/shared/StatusBadge';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { formatCurrency } from '@/lib/format';
import { Upload, Search, Download, Eye, RefreshCw, Loader2, Trash2, AlertTriangle } from 'lucide-react';

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  uploaded: 'info',
  processing: 'warning',
  extracted: 'success',
  review: 'warning',
  booked: 'success',
  failed: 'destructive',
};

export function Component() {
  const { t } = useTranslation('documents');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<DocumentStatus | 'all'>('all');
  const [search, setSearch] = useState('');
  const pageSize = 20;
  const reprocessDocument = useReprocessDocument();
  const deleteDocument = useDeleteDocument();
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; fileName: string } | null>(null);
  const {
    data: preflight,
    isFetching: isPreflightLoading,
    refetch: fetchPreflight,
  } = useDeleteDocumentPreflight(selectedEntityId, deleteTarget?.id ?? null);

  const { data, isLoading } = useDocuments({
    entityId: selectedEntityId ?? '',
    page,
    pageSize,
    status: status === 'all' ? undefined : status,
    search: search || undefined,
  });

  const handleDownload = async (id: string) => {
    if (!selectedEntityId) return;
    try {
      const { data: result } = await api.get<{ url: string }>(
        `/documents/${id}/download`,
        { params: { entityId: selectedEntityId } },
      );
      window.open(result.url, '_blank');
    } catch {
      // Error handled by interceptor
    }
  };

  const handleReprocess = (id: string) => {
    if (!selectedEntityId) return;
    reprocessDocument.mutate({ documentId: id, entityId: selectedEntityId });
  };

  const handleOpenDeleteDialog = (id: string, fileName: string) => {
    setDeleteTarget({ id, fileName });
  };

  // Fetch preflight when a new deleteTarget is selected
  useEffect(() => {
    if (deleteTarget) fetchPreflight();
  }, [deleteTarget?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleConfirmDelete = () => {
    if (!selectedEntityId || !deleteTarget) return;
    deleteDocument.mutate(
      { documentId: deleteTarget.id, entityId: selectedEntityId },
      { onSuccess: () => setDeleteTarget(null) },
    );
  };

  // Count items by status for quick filters
  const items = data?.items ?? [];
  const reviewCount = items.filter((d) => d.status === 'review').length;
  const failedCount = items.filter((d) => d.status === 'failed').length;
  const processingCount = items.filter((d) => d.status === 'processing').length;

  const columns = [
    {
      key: 'fileName',
      header: t('columns.fileName'),
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.fileName ?? '')}</span>
      ),
    },
    {
      key: 'vendorName',
      header: t('columns.vendor'),
      render: (item: Record<string, unknown>) =>
        String(item.vendorName ?? '-'),
    },
    {
      key: 'invoiceNumber',
      header: t('columns.invoiceNumber'),
      render: (item: Record<string, unknown>) =>
        String(item.invoiceNumber ?? '-'),
    },
    {
      key: 'invoiceDate',
      header: t('columns.date'),
      render: (item: Record<string, unknown>) =>
        String(item.invoiceDate ?? '-'),
    },
    {
      key: 'totalAmount',
      header: t('columns.amount'),
      render: (item: Record<string, unknown>) => {
        const amount = item.totalAmount as number | undefined;
        return amount != null ? formatCurrency(amount) : '-';
      },
    },
    {
      key: 'status',
      header: t('columns.status'),
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={String(item.status ?? '')}
          variantMap={STATUS_VARIANT_MAP}
        />
      ),
    },
    {
      key: 'actions',
      header: t('columns.actions'),
      render: (item: Record<string, unknown>) => (
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            title={t('actions.view')}
            onClick={() => navigate(`/documents/${String(item.id ?? '')}`)}
          >
            <Eye className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            title={t('actions.download')}
            onClick={() => handleDownload(String(item.id ?? ''))}
          >
            <Download className="h-4 w-4" />
          </Button>
          {(String(item.status) === 'failed' || String(item.status) === 'processing') && (
            <Button
              variant="ghost"
              size="sm"
              title={t('actions.reprocess')}
              onClick={() => handleReprocess(String(item.id ?? ''))}
              disabled={reprocessDocument.isPending}
            >
              {reprocessDocument.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <RefreshCw className="h-4 w-4" />
              )}
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            title={t('actions.delete')}
            className="text-red-500 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
            onClick={() => handleOpenDeleteDialog(String(item.id ?? ''), String(item.fileName ?? ''))}
            disabled={deleteDocument.isPending}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title={t('title')}
        actions={
          <Link to="/documents/upload">
            <Button>
              <Upload className="mr-1 h-4 w-4" />
              {t('upload')}
            </Button>
          </Link>
        }
      />

      {/* Quick Filter Badges */}
      <div className="mb-3 flex flex-wrap gap-2">
        {reviewCount > 0 && (
          <button onClick={() => { setStatus('review'); setPage(1); }}>
            <Badge variant="secondary" className="cursor-pointer hover:bg-amber-100 dark:hover:bg-amber-900">
              {t('archive.quickFilters.review')} ({reviewCount})
            </Badge>
          </button>
        )}
        {failedCount > 0 && (
          <button onClick={() => { setStatus('failed'); setPage(1); }}>
            <Badge variant="destructive" className="cursor-pointer">
              {t('archive.quickFilters.failed')} ({failedCount})
            </Badge>
          </button>
        )}
        {processingCount > 0 && (
          <button onClick={() => { setStatus('processing'); setPage(1); }}>
            <Badge variant="secondary" className="cursor-pointer">
              {t('archive.quickFilters.processing')} ({processingCount})
            </Badge>
          </button>
        )}
        {status !== 'all' && (
          <button onClick={() => { setStatus('all'); setPage(1); }}>
            <Badge variant="outline" className="cursor-pointer">
              {t('allStatuses')} &times;
            </Badge>
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
        <Select
          value={status}
          onValueChange={(v) => {
            setStatus(v as DocumentStatus | 'all');
            setPage(1);
          }}
        >
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder={t('allStatuses')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('allStatuses')}</SelectItem>
            <SelectItem value="uploaded">{t('statuses.uploaded')}</SelectItem>
            <SelectItem value="processing">{t('statuses.processing')}</SelectItem>
            <SelectItem value="extracted">{t('statuses.extracted')}</SelectItem>
            <SelectItem value="review">{t('statuses.review')}</SelectItem>
            <SelectItem value="booked">{t('statuses.booked')}</SelectItem>
            <SelectItem value="failed">{t('statuses.failed')}</SelectItem>
          </SelectContent>
        </Select>
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={t('searchPlaceholder')}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="pl-9"
          />
        </div>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={(data?.items ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('noDocuments')}
        pagination={{
          page,
          pageSize,
          total: data?.totalCount ?? 0,
          onPageChange: setPage,
        }}
      />

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('delete.title')}</DialogTitle>
            <DialogDescription>
              {t('delete.confirm', { fileName: deleteTarget?.fileName })}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4 space-y-3">
            {isPreflightLoading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                {t('delete.loading')}
              </div>
            ) : preflight ? (
              <>
                {!preflight.canDelete && preflight.blockReason && (
                  <div className="rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-800 dark:bg-red-950">
                    <p className="text-sm font-medium text-red-700 dark:text-red-300">
                      {t('delete.blocked')}
                    </p>
                    <p className="text-sm text-red-600 dark:text-red-400">
                      {preflight.blockReason.startsWith('closed_period:')
                        ? t('delete.closedPeriod', { period: preflight.blockReason.split(':')[1] })
                        : preflight.blockReason}
                    </p>
                  </div>
                )}
                {preflight.canDelete && (
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center gap-2">
                      <span className="text-muted-foreground">•</span>
                      {t('delete.willDeleteDocument')}
                    </li>
                    {preflight.fieldCount > 0 && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.willDeleteFields', { count: preflight.fieldCount })}
                      </li>
                    )}
                    {preflight.hasBookingSuggestion && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.willDeleteBookingSuggestion')}
                      </li>
                    )}
                    {preflight.hasJournalEntry && preflight.journalEntryWillBeReversed && (
                      <li className="flex items-center gap-2 text-amber-600 dark:text-amber-400">
                        <AlertTriangle className="h-4 w-4 shrink-0" />
                        {t('delete.willReverseJournalEntry')}
                      </li>
                    )}
                    {preflight.hasJournalEntry && !preflight.journalEntryWillBeReversed && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.journalEntryAlreadyReversed')}
                      </li>
                    )}
                  </ul>
                )}
                {preflight.canDelete && (
                  <p className="text-xs text-muted-foreground italic">{t('delete.irreversible')}</p>
                )}
              </>
            ) : null}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              {t('actions.cancel', { ns: 'common', defaultValue: 'Cancel' })}
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirmDelete}
              disabled={deleteDocument.isPending || !preflight?.canDelete}
            >
              {deleteDocument.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              <Trash2 className="mr-1 h-4 w-4" />
              {t('actions.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
