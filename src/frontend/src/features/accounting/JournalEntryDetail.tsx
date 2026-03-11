import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Send, RotateCcw } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useJournalEntry, usePostJournalEntry, useReverseJournalEntry } from '@/hooks/useAccounting';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
  }).format(value);
}

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' {
  if (status === 'posted') return 'default';
  if (status === 'reversed') return 'destructive';
  return 'secondary';
}

export function Component() {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const { data: entry, isLoading } = useJournalEntry(id ?? null, selectedEntityId);
  const postMutation = usePostJournalEntry();
  const reverseMutation = useReverseJournalEntry();

  const [confirmPost, setConfirmPost] = useState(false);
  const [reverseDialog, setReverseDialog] = useState(false);
  const [reverseReason, setReverseReason] = useState('');

  const columns = [
    { key: 'lineNumber', header: t('accounting:journalEntryDetail.columns.lineNumber') },
    { key: 'accountNumber', header: t('accounting:journalEntryDetail.columns.accountNumber') },
    { key: 'accountName', header: t('accounting:journalEntryDetail.columns.accountName') },
    {
      key: 'debitAmount',
      header: t('accounting:journalEntryDetail.columns.debit'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.debitAmount ?? 0))}</span>
      ),
    },
    {
      key: 'creditAmount',
      header: t('accounting:journalEntryDetail.columns.credit'),
      render: (item: Record<string, unknown>) => (
        <span className="tabular-nums">{formatCurrency(Number(item.creditAmount ?? 0))}</span>
      ),
    },
    { key: 'currency', header: t('accounting:journalEntryDetail.columns.currency') },
    {
      key: 'vatAmount',
      header: t('accounting:journalEntryDetail.columns.vatAmount'),
      render: (item: Record<string, unknown>) => {
        const val = Number(item.vatAmount ?? 0);
        return val ? <span className="tabular-nums">{formatCurrency(val)}</span> : <span>–</span>;
      },
    },
    {
      key: 'costCenter',
      header: t('accounting:journalEntryDetail.columns.costCenter'),
      render: (item: Record<string, unknown>) => <span>{String(item.costCenter ?? '–')}</span>,
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:journalEntryDetail.title')} />
        <p className="text-muted-foreground">{t('accounting:journalEntries.noEntitySelected')}</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!entry) {
    return (
      <div>
        <PageHeader title={t('accounting:journalEntryDetail.title')} />
        <p className="text-muted-foreground">{t('accounting:journalEntries.noEntries')}</p>
      </div>
    );
  }

  const handlePost = () => {
    postMutation.mutate(
      { id: entry.id, entityId: selectedEntityId },
      {
        onSuccess: () => {
          setConfirmPost(false);
        },
      },
    );
  };

  const handleReverse = () => {
    reverseMutation.mutate(
      { id: entry.id, entityId: selectedEntityId, reason: reverseReason },
      {
        onSuccess: () => {
          setReverseDialog(false);
          setReverseReason('');
        },
      },
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${t('accounting:journalEntryDetail.title')} #${entry.entryNumber}`}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/accounting/journal-entries">
                <ArrowLeft className="mr-1 h-4 w-4" />
                {t('accounting:journalEntryDetail.backToList')}
              </Link>
            </Button>
            {entry.status === 'draft' && (
              <Button size="sm" onClick={() => setConfirmPost(true)}>
                <Send className="mr-1 h-4 w-4" />
                {t('accounting:journalEntries.actions.post')}
              </Button>
            )}
            {entry.status === 'posted' && (
              <Button size="sm" variant="destructive" onClick={() => setReverseDialog(true)}>
                <RotateCcw className="mr-1 h-4 w-4" />
                {t('accounting:journalEntries.actions.reverse')}
              </Button>
            )}
          </div>
        }
      />

      {/* Reversal notice */}
      {entry.isReversal && entry.reversalOf && (
        <Card className="border-yellow-500/50 bg-yellow-500/5">
          <CardContent className="py-3 text-sm">
            {t('accounting:journalEntryDetail.isReversal', { number: entry.reversalOf })}
          </CardContent>
        </Card>
      )}

      {/* Metadata */}
      <Card>
        <CardContent className="grid grid-cols-2 gap-4 py-4 text-sm md:grid-cols-4">
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.entryDate')}</div>
            <div>{entry.entryDate}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.postingDate')}</div>
            <div>{entry.postingDate}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.status')}</div>
            <Badge variant={statusVariant(entry.status)}>
              {t(`accounting:journalEntries.status.${entry.status}`, { defaultValue: entry.status })}
            </Badge>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.description')}</div>
            <div>{entry.description}</div>
          </div>
          {entry.sourceType && (
            <div>
              <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.sourceType')}</div>
              <div>{entry.sourceType}</div>
            </div>
          )}
          {entry.sourceRef && (
            <div>
              <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.sourceRef')}</div>
              <div>{entry.sourceRef}</div>
            </div>
          )}
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.hash')}</div>
            <div className="font-mono text-xs truncate" title={entry.hash}>
              {entry.hash ? entry.hash.slice(0, 16) + '...' : '–'}
            </div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.createdAt')}</div>
            <div>{new Date(entry.createdAt).toLocaleString('de-DE')}</div>
          </div>
        </CardContent>
      </Card>

      {/* Lines */}
      <div>
        <h3 className="mb-2 text-sm font-semibold">{t('accounting:journalEntryDetail.lines')}</h3>
        <DataTable
          columns={columns}
          data={(entry.lines ?? []) as unknown as Record<string, unknown>[]}
          isLoading={false}
          emptyMessage="–"
        />
      </div>

      {/* Post Confirmation Dialog */}
      <Dialog open={confirmPost} onOpenChange={setConfirmPost}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:journalEntries.postConfirmTitle')}</DialogTitle>
            <DialogDescription>{t('accounting:journalEntries.postConfirmDescription')}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmPost(false)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button onClick={handlePost} disabled={postMutation.isPending}>
              {t('accounting:journalEntries.postConfirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reverse Dialog */}
      <Dialog open={reverseDialog} onOpenChange={setReverseDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:journalEntryDetail.reverseConfirmTitle')}</DialogTitle>
            <DialogDescription>{t('accounting:journalEntryDetail.reverseConfirmDescription')}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1">
              <Label>{t('accounting:journalEntryDetail.reverseReasonLabel')}</Label>
              <Input
                value={reverseReason}
                onChange={(e) => setReverseReason(e.target.value)}
                placeholder={t('accounting:journalEntryDetail.reverseReasonLabel')}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReverseDialog(false)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleReverse}
              disabled={!reverseReason.trim() || reverseMutation.isPending}
            >
              {t('accounting:journalEntryDetail.reverseConfirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
