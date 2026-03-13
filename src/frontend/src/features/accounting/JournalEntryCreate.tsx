import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ArrowLeft, Loader2 } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useCreateJournalEntry, useAccounts } from '@/hooks/useAccounting';
import { getLocalizedAccountName } from '@/lib/accountUtils';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface LineState {
  key: number;
  accountId: string;
  side: 'debit' | 'credit';
  netAmount: string;
  vatCode: string;
  vatAmount: string;
  costCenter: string;
  description: string;
}

function emptyLine(key: number): LineState {
  return {
    key,
    accountId: '',
    side: 'debit',
    netAmount: '',
    vatCode: '',
    vatAmount: '',
    costCenter: '',
    description: '',
  };
}

function lineGross(line: LineState): number {
  return (parseFloat(line.netAmount) || 0) + (parseFloat(line.vatAmount) || 0);
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
  }).format(value);
}

export function Component() {
  const { t, i18n } = useTranslation(['accounting', 'common']);
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const createMutation = useCreateJournalEntry();
  const { data: accounts } = useAccounts(selectedEntityId);

  const today = new Date().toISOString().slice(0, 10);
  const [entryDate, setEntryDate] = useState(today);
  const [description, setDescription] = useState('');
  const [nextKey, setNextKey] = useState(2);
  const [lines, setLines] = useState<LineState[]>([emptyLine(0), emptyLine(1)]);

  const updateLine = (key: number, field: keyof LineState, value: string) => {
    setLines((prev) =>
      prev.map((l) => (l.key === key ? { ...l, [field]: value } : l)),
    );
  };

  const addLine = () => {
    setLines((prev) => [...prev, emptyLine(nextKey)]);
    setNextKey((k) => k + 1);
  };

  const removeLine = (key: number) => {
    setLines((prev) => prev.filter((l) => l.key !== key));
  };

  const totals = useMemo(() => {
    let totalDebit = 0;
    let totalCredit = 0;
    for (const line of lines) {
      const gross = lineGross(line);
      if (line.side === 'debit') totalDebit += gross;
      else totalCredit += gross;
    }
    return { totalDebit, totalCredit, difference: totalDebit - totalCredit };
  }, [lines]);

  const isBalanced = Math.abs(totals.difference) < 0.005;
  const hasLines = lines.length > 0 && lines.some((l) => l.accountId);
  const hasDescription = description.trim().length > 0;
  const canSubmit =
    hasDescription &&
    hasLines &&
    isBalanced &&
    totals.totalDebit > 0 &&
    !createMutation.isPending;

  const handleSubmit = () => {
    if (!selectedEntityId || !canSubmit) return;

    createMutation.mutate(
      {
        entityId: selectedEntityId,
        entryDate,
        description: description.trim(),
        lines: lines
          .filter((l) => l.accountId)
          .map((l) => {
            const gross = lineGross(l);
            return {
              accountId: l.accountId,
              debitAmount: l.side === 'debit' ? gross : 0,
              creditAmount: l.side === 'credit' ? gross : 0,
              vatCode: l.vatCode || undefined,
              vatAmount: l.vatAmount ? parseFloat(l.vatAmount) : undefined,
              costCenter: l.costCenter || undefined,
              description: l.description || undefined,
            };
          }),
      },
      {
        onSuccess: () => {
          navigate('/accounting/journal-entries');
        },
      },
    );
  };

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:journalEntryCreate.title')} />
        <p className="text-muted-foreground">{t('accounting:journalEntries.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('accounting:journalEntryCreate.title')}
        actions={
          <Button variant="outline" size="sm" onClick={() => navigate('/accounting/journal-entries')}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            {t('accounting:journalEntryDetail.backToList')}
          </Button>
        }
      />

      {/* Header Fields */}
      <Card>
        <CardContent className="grid grid-cols-1 gap-4 py-4 md:grid-cols-2">
          <div className="space-y-1">
            <Label>{t('accounting:journalEntryCreate.entryDate')}</Label>
            <Input
              type="date"
              value={entryDate}
              onChange={(e) => setEntryDate(e.target.value)}
            />
          </div>
          <div className="space-y-1">
            <Label>{t('accounting:journalEntryCreate.description')}</Label>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={t('accounting:journalEntryCreate.descriptionPlaceholder')}
              maxLength={500}
            />
            {description.length > 0 && !hasDescription && (
              <p className="text-xs text-destructive">{t('accounting:journalEntryCreate.validation.descriptionRequired')}</p>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Lines */}
      <Card>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">{t('accounting:journalEntryCreate.lines')}</CardTitle>
            <Button size="sm" variant="outline" onClick={addLine}>
              <Plus className="mr-1 h-3 w-3" />
              {t('accounting:journalEntryCreate.addLine')}
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="py-2 text-left font-medium min-w-[200px]">{t('accounting:journalEntryCreate.account')}</th>
                  <th className="py-2 text-left font-medium w-24">{t('accounting:journalEntryCreate.side')}</th>
                  <th className="py-2 text-right font-medium w-28">{t('accounting:journalEntryCreate.netAmount')}</th>
                  <th className="py-2 text-left font-medium w-24">{t('accounting:journalEntryCreate.vatCode')}</th>
                  <th className="py-2 text-right font-medium w-28">{t('accounting:journalEntryCreate.vatAmount')}</th>
                  <th className="py-2 text-right font-medium w-28">{t('accounting:journalEntryCreate.grossCalculated')}</th>
                  <th className="py-2 text-left font-medium w-28">{t('accounting:journalEntryCreate.costCenter')}</th>
                  <th className="py-2 text-left font-medium min-w-[120px]">{t('accounting:journalEntryCreate.lineDescription')}</th>
                  <th className="py-2 w-10"></th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line) => (
                  <tr key={line.key} className="border-b">
                    <td className="py-1.5 pr-2">
                      <Select
                        value={line.accountId}
                        onValueChange={(v) => updateLine(line.key, 'accountId', v)}
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
                    </td>
                    <td className="py-1.5 pr-2">
                      <Select
                        value={line.side}
                        onValueChange={(v) => updateLine(line.key, 'side', v)}
                      >
                        <SelectTrigger className="h-8 text-xs">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="debit">{t('accounting:journalEntryCreate.sideDebit')}</SelectItem>
                          <SelectItem value="credit">{t('accounting:journalEntryCreate.sideCredit')}</SelectItem>
                        </SelectContent>
                      </Select>
                    </td>
                    <td className="py-1.5 pr-2">
                      <Input
                        type="number"
                        min="0"
                        step="0.01"
                        className="h-8 text-right text-xs tabular-nums"
                        value={line.netAmount}
                        onChange={(e) => updateLine(line.key, 'netAmount', e.target.value)}
                      />
                    </td>
                    <td className="py-1.5 pr-2">
                      <Input
                        className="h-8 text-xs"
                        value={line.vatCode}
                        onChange={(e) => updateLine(line.key, 'vatCode', e.target.value)}
                      />
                    </td>
                    <td className="py-1.5 pr-2">
                      <Input
                        type="number"
                        min="0"
                        step="0.01"
                        className="h-8 text-right text-xs tabular-nums"
                        value={line.vatAmount}
                        onChange={(e) => updateLine(line.key, 'vatAmount', e.target.value)}
                      />
                    </td>
                    <td className="py-1.5 pr-2">
                      <span className="inline-block h-8 leading-8 text-right text-xs tabular-nums text-muted-foreground w-full">
                        {formatCurrency(lineGross(line))}
                      </span>
                    </td>
                    <td className="py-1.5 pr-2">
                      <Input
                        className="h-8 text-xs"
                        value={line.costCenter}
                        onChange={(e) => updateLine(line.key, 'costCenter', e.target.value)}
                      />
                    </td>
                    <td className="py-1.5 pr-2">
                      <Input
                        className="h-8 text-xs"
                        value={line.description}
                        onChange={(e) => updateLine(line.key, 'description', e.target.value)}
                      />
                    </td>
                    <td className="py-1.5">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8"
                        onClick={() => removeLine(line.key)}
                        disabled={lines.length <= 1}
                      >
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Totals */}
          <div className="mt-4 flex items-center justify-between rounded-md border bg-muted/50 px-4 py-3 text-sm">
            <div className="flex gap-6">
              <span>
                <span className="text-muted-foreground">{t('accounting:journalEntryCreate.totalDebit')}: </span>
                <span className="font-semibold tabular-nums">{formatCurrency(totals.totalDebit)}</span>
              </span>
              <span>
                <span className="text-muted-foreground">{t('accounting:journalEntryCreate.totalCredit')}: </span>
                <span className="font-semibold tabular-nums">{formatCurrency(totals.totalCredit)}</span>
              </span>
              <span>
                <span className="text-muted-foreground">{t('accounting:journalEntryCreate.difference')}: </span>
                <span className={`font-semibold tabular-nums ${isBalanced ? '' : 'text-destructive'}`}>
                  {formatCurrency(totals.difference)}
                </span>
              </span>
            </div>
            <Badge variant={isBalanced && totals.totalDebit > 0 ? 'default' : 'destructive'}>
              {isBalanced ? t('accounting:journalEntryCreate.balanced') : t('accounting:journalEntryCreate.unbalanced')}
            </Badge>
          </div>

        </CardContent>
      </Card>

      {/* Submit */}
      <div className="flex justify-end">
        <Button onClick={handleSubmit} disabled={!canSubmit}>
          {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {t('accounting:journalEntryCreate.submit')}
        </Button>
      </div>
    </div>
  );
}
