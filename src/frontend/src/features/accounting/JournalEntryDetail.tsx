import { useState, useMemo } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Send, RotateCcw, Pencil, Plus, Trash2, Loader2 } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useJournalEntry, usePostJournalEntry, useReverseJournalEntry, useUpdateJournalEntry, useAccounts } from '@/hooks/useAccounting';
import { getLocalizedAccountName } from '@/lib/accountUtils';
import DataTable from '@/components/shared/DataTable';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
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

interface EditLineState {
  key: number;
  accountId: string;
  debitAmount: string;
  creditAmount: string;
  vatCode: string;
  vatAmount: string;
  costCenter: string;
  description: string;
}

function emptyEditLine(key: number): EditLineState {
  return { key, accountId: '', debitAmount: '', creditAmount: '', vatCode: '', vatAmount: '', costCenter: '', description: '' };
}

export function Component() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();
  const { data: entry, isLoading } = useJournalEntry(id ?? null, selectedEntityId);
  const { data: accounts } = useAccounts(selectedEntityId);
  const postMutation = usePostJournalEntry();
  const reverseMutation = useReverseJournalEntry();
  const updateMutation = useUpdateJournalEntry();

  const [confirmPost, setConfirmPost] = useState(false);
  const [reverseDialog, setReverseDialog] = useState(false);
  const [reverseReason, setReverseReason] = useState('');
  const [editOpen, setEditOpen] = useState(false);
  const [editDate, setEditDate] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editLines, setEditLines] = useState<EditLineState[]>([]);
  const [editNextKey, setEditNextKey] = useState(0);

  const openEditSheet = () => {
    if (!entry) return;
    const lines = entry.lines.map((l, i) => ({
      key: i,
      accountId: l.accountId,
      debitAmount: l.debitAmount > 0 ? String(l.debitAmount) : '',
      creditAmount: l.creditAmount > 0 ? String(l.creditAmount) : '',
      vatCode: l.vatCode ?? '',
      vatAmount: l.vatAmount > 0 ? String(l.vatAmount) : '',
      costCenter: l.costCenter ?? '',
      description: l.description ?? '',
    }));
    setEditDate(entry.entryDate);
    setEditDescription(entry.description);
    setEditLines(lines);
    setEditNextKey(lines.length);
    setEditOpen(true);
  };

  const updateEditLine = (key: number, field: keyof EditLineState, value: string) => {
    setEditLines((prev) => prev.map((l) => (l.key === key ? { ...l, [field]: value } : l)));
  };

  const addEditLine = () => {
    setEditLines((prev) => [...prev, emptyEditLine(editNextKey)]);
    setEditNextKey((k) => k + 1);
  };

  const removeEditLine = (key: number) => {
    setEditLines((prev) => prev.filter((l) => l.key !== key));
  };

  const editTotals = useMemo(() => {
    let totalDebit = 0;
    let totalCredit = 0;
    for (const line of editLines) {
      totalDebit += parseFloat(line.debitAmount) || 0;
      totalCredit += parseFloat(line.creditAmount) || 0;
    }
    return { totalDebit, totalCredit, difference: totalDebit - totalCredit };
  }, [editLines]);

  const editIsBalanced = Math.abs(editTotals.difference) < 0.005;
  const editHasLines = editLines.length > 0 && editLines.some((l) => l.accountId);
  const editHasBothDebitAndCredit = editLines.some(
    (l) => (parseFloat(l.debitAmount) || 0) > 0 && (parseFloat(l.creditAmount) || 0) > 0,
  );
  const editCanSubmit =
    editDescription.trim().length > 0 &&
    editHasLines &&
    editIsBalanced &&
    editTotals.totalDebit > 0 &&
    !editHasBothDebitAndCredit &&
    !updateMutation.isPending;

  const handleEditSubmit = () => {
    if (!selectedEntityId || !entry || !editCanSubmit) return;
    updateMutation.mutate(
      {
        id: entry.id,
        entityId: selectedEntityId,
        entryDate: editDate,
        description: editDescription.trim(),
        lines: editLines
          .filter((l) => l.accountId)
          .map((l) => ({
            accountId: l.accountId,
            debitAmount: parseFloat(l.debitAmount) || 0,
            creditAmount: parseFloat(l.creditAmount) || 0,
            vatCode: l.vatCode || undefined,
            vatAmount: l.vatAmount ? parseFloat(l.vatAmount) : undefined,
            costCenter: l.costCenter || undefined,
            description: l.description || undefined,
          })),
      },
      { onSuccess: () => setEditOpen(false) },
    );
  };

  // Calculate Brutto / Netto / USt from lines
  const totalDebit = (entry?.lines ?? []).reduce((sum, l) => sum + l.debitAmount, 0);
  const totalVat = (entry?.lines ?? []).reduce((sum, l) => sum + l.vatAmount, 0);
  const totalNet = totalDebit - totalVat;

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
              <>
                <Button size="sm" variant="outline" onClick={openEditSheet}>
                  <Pencil className="mr-1 h-4 w-4" />
                  {t('accounting:journalEntryDetail.editEntry')}
                </Button>
                <Button size="sm" onClick={() => setConfirmPost(true)}>
                  <Send className="mr-1 h-4 w-4" />
                  {t('accounting:journalEntries.actions.post')}
                </Button>
              </>
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

      {/* Amount Summary */}
      <Card>
        <CardContent className="grid grid-cols-3 gap-4 py-4 text-sm">
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.grossAmount')}</div>
            <div className="text-lg font-semibold tabular-nums">{formatCurrency(totalDebit)}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.netAmount')}</div>
            <div className="text-lg font-semibold tabular-nums">{formatCurrency(totalNet)}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.vatAmount')}</div>
            <div className="text-lg font-semibold tabular-nums">{formatCurrency(totalVat)}</div>
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

      {/* Edit Sheet */}
      <Sheet open={editOpen} onOpenChange={setEditOpen}>
        <SheetContent side="right" className="w-full overflow-y-auto sm:max-w-2xl">
          <SheetHeader>
            <SheetTitle>{t('accounting:journalEntryDetail.editTitle')}</SheetTitle>
            <SheetDescription>#{entry.entryNumber}</SheetDescription>
          </SheetHeader>

          <div className="space-y-6 py-4">
            {/* Header Fields */}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-1">
                <Label>{t('accounting:journalEntryCreate.entryDate')}</Label>
                <Input type="date" value={editDate} onChange={(e) => setEditDate(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>{t('accounting:journalEntryCreate.description')}</Label>
                <Input
                  value={editDescription}
                  onChange={(e) => setEditDescription(e.target.value)}
                  maxLength={500}
                />
              </div>
            </div>

            {/* Lines */}
            <Card>
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base">{t('accounting:journalEntryCreate.lines')}</CardTitle>
                  <Button size="sm" variant="outline" onClick={addEditLine}>
                    <Plus className="mr-1 h-3 w-3" />
                    {t('accounting:journalEntryCreate.addLine')}
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {editLines.map((line) => {
                    const hasBoth =
                      (parseFloat(line.debitAmount) || 0) > 0 &&
                      (parseFloat(line.creditAmount) || 0) > 0;
                    return (
                      <div
                        key={line.key}
                        className={`grid grid-cols-[1fr_auto] gap-2 rounded-md border p-2 ${hasBoth ? 'border-destructive/50 bg-destructive/5' : ''}`}
                      >
                        <div className="grid grid-cols-2 gap-2">
                          <div className="col-span-2">
                            <Select
                              value={line.accountId}
                              onValueChange={(v) => updateEditLine(line.key, 'accountId', v)}
                            >
                              <SelectTrigger className="h-8 text-xs">
                                <SelectValue placeholder={t('accounting:journalEntryCreate.selectAccount')} />
                              </SelectTrigger>
                              <SelectContent>
                                {(accounts ?? []).map((acc) => (
                                  <SelectItem key={acc.id} value={acc.id}>
                                    {acc.accountNumber} – {getLocalizedAccountName(acc, i18n.language)}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryCreate.debit')}</Label>
                            <Input
                              type="number" min="0" step="0.01"
                              className="h-8 text-right text-xs tabular-nums"
                              value={line.debitAmount}
                              onChange={(e) => updateEditLine(line.key, 'debitAmount', e.target.value)}
                            />
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryCreate.credit')}</Label>
                            <Input
                              type="number" min="0" step="0.01"
                              className="h-8 text-right text-xs tabular-nums"
                              value={line.creditAmount}
                              onChange={(e) => updateEditLine(line.key, 'creditAmount', e.target.value)}
                            />
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryCreate.vatCode')}</Label>
                            <Input
                              className="h-8 text-xs"
                              value={line.vatCode}
                              onChange={(e) => updateEditLine(line.key, 'vatCode', e.target.value)}
                            />
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryDetail.vatAmount')}</Label>
                            <Input
                              type="number" min="0" step="0.01"
                              className="h-8 text-right text-xs tabular-nums"
                              value={line.vatAmount}
                              onChange={(e) => updateEditLine(line.key, 'vatAmount', e.target.value)}
                            />
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryCreate.costCenter')}</Label>
                            <Input
                              className="h-8 text-xs"
                              value={line.costCenter}
                              onChange={(e) => updateEditLine(line.key, 'costCenter', e.target.value)}
                            />
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">{t('accounting:journalEntryCreate.lineDescription')}</Label>
                            <Input
                              className="h-8 text-xs"
                              value={line.description}
                              onChange={(e) => updateEditLine(line.key, 'description', e.target.value)}
                            />
                          </div>
                        </div>
                        <div className="flex items-start pt-5">
                          <Button
                            variant="ghost" size="icon" className="h-8 w-8"
                            onClick={() => removeEditLine(line.key)}
                            disabled={editLines.length <= 1}
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>

                {/* Totals */}
                <div className="mt-4 flex items-center justify-between rounded-md border bg-muted/50 px-4 py-3 text-sm">
                  <div className="flex gap-4">
                    <span>
                      <span className="text-muted-foreground">{t('accounting:journalEntryCreate.totalDebit')}: </span>
                      <span className="font-semibold tabular-nums">{formatCurrency(editTotals.totalDebit)}</span>
                    </span>
                    <span>
                      <span className="text-muted-foreground">{t('accounting:journalEntryCreate.totalCredit')}: </span>
                      <span className="font-semibold tabular-nums">{formatCurrency(editTotals.totalCredit)}</span>
                    </span>
                  </div>
                  <Badge variant={editIsBalanced && editTotals.totalDebit > 0 ? 'default' : 'destructive'}>
                    {editIsBalanced ? t('accounting:journalEntryCreate.balanced') : t('accounting:journalEntryCreate.unbalanced')}
                  </Badge>
                </div>
              </CardContent>
            </Card>

            {/* Submit */}
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setEditOpen(false)}>
                {t('common:buttons.cancel')}
              </Button>
              <Button onClick={handleEditSubmit} disabled={!editCanSubmit}>
                {updateMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t('accounting:journalEntryDetail.saveChanges')}
              </Button>
            </div>
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
