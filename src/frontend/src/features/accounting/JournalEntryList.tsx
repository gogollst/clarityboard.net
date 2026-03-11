import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Plus, Send } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useJournalEntries, usePostJournalEntry } from '@/hooks/useAccounting';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';

export function Component() {
  const { t, i18n } = useTranslation('accounting');
  const { selectedEntityId } = useEntity();
  const [page, setPage] = useState(1);
  const [confirmPostId, setConfirmPostId] = useState<string | null>(null);

  const navigate = useNavigate();
  const { data, isLoading } = useJournalEntries(selectedEntityId, { page, pageSize: 50 });
  const postMutation = usePostJournalEntry();

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 50;

  const statusVariant = (status: string) => {
    if (status === 'posted') return 'default';
    if (status === 'reversed') return 'destructive';
    return 'secondary';
  };

  const columns = [
    {
      key: 'entryNumber',
      header: t('journalEntries.columns.entryNumber'),
    },
    {
      key: 'entryDate',
      header: t('journalEntries.columns.entryDate'),
      render: (item: Record<string, unknown>) => {
        const raw = item.entryDate as string | undefined;
        if (!raw) return '—';
        return new Date(raw).toLocaleDateString(i18n.language);
      },
    },
    {
      key: 'description',
      header: t('journalEntries.columns.description'),
    },
    {
      key: 'documentRef',
      header: t('journalEntries.columns.documentRef'),
      render: (item: Record<string, unknown>) => String(item.documentRef ?? '—'),
    },
    {
      key: 'sourceType',
      header: t('journalEntries.columns.sourceType'),
      render: (item: Record<string, unknown>) => String(item.sourceType ?? '—'),
    },
    {
      key: 'status',
      header: t('journalEntries.columns.status'),
      render: (item: Record<string, unknown>) => {
        const status = String(item.status ?? '');
        return (
          <Badge variant={statusVariant(status)}>
            {t(`journalEntries.status.${status}`, { defaultValue: status })}
          </Badge>
        );
      },
    },
    {
      key: 'actions',
      header: '',
      render: (item: Record<string, unknown>) => {
        if (item.status !== 'draft') return null;
        return (
          <Button
            size="sm"
            variant="outline"
            onClick={(e) => {
              e.stopPropagation();
              setConfirmPostId(String(item.id));
            }}
          >
            <Send className="mr-1 h-3 w-3" />
            {t('journalEntries.actions.post')}
          </Button>
        );
      },
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('journalEntries.title')} />
        <p className="text-muted-foreground">{t('journalEntries.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t('journalEntries.title')}
        actions={
          <Button size="sm" asChild>
            <Link to="/accounting/journal-entries/new">
              <Plus className="mr-1 h-4 w-4" />
              {t('journalEntries.newEntry')}
            </Link>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={items as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('journalEntries.noEntries')}
        onRowClick={(item) => navigate(`/accounting/journal-entries/${item.id}`)}
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />

      <Dialog open={!!confirmPostId} onOpenChange={() => setConfirmPostId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('journalEntries.postConfirmTitle')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">{t('journalEntries.postConfirmDescription')}</p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmPostId(null)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              onClick={() => {
                if (confirmPostId && selectedEntityId) {
                  postMutation.mutate({ id: confirmPostId, entityId: selectedEntityId });
                  setConfirmPostId(null);
                }
              }}
            >
              {t('journalEntries.postConfirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
