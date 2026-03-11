import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, RefreshCw } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useDatevExports, useGenerateDatevExport, useFiscalPeriods, useSyncTravelCosts } from '@/hooks/useAccounting';
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
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const API_BASE = '/api';

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  if (status === 'Ready') return 'default';
  if (status === 'Failed') return 'destructive';
  if (status === 'Generating') return 'outline';
  return 'secondary';
}

export function Component() {
  const { t, i18n } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();

  const { data: exports, isLoading } = useDatevExports(selectedEntityId);
  const { data: periods } = useFiscalPeriods(selectedEntityId);
  const generateMutation = useGenerateDatevExport();
  const syncMutation = useSyncTravelCosts();

  const [generateOpen, setGenerateOpen] = useState(false);
  const [syncOpen, setSyncOpen] = useState(false);
  const [selectedPeriodId, setSelectedPeriodId] = useState('');
  const [exportType, setExportType] = useState('Buchungsstapel');
  const [syncFromDate, setSyncFromDate] = useState('');
  const [syncToDate, setSyncToDate] = useState('');

  const columns = [
    {
      key: 'exportType',
      header: t('accounting:datevExports.columns.exportType'),
      render: (item: Record<string, unknown>) =>
        t(`accounting:datevExports.exportType.${item.exportType}`, {
          defaultValue: String(item.exportType ?? ''),
        }),
    },
    {
      key: 'status',
      header: t('accounting:datevExports.columns.status'),
      render: (item: Record<string, unknown>) => {
        const status = String(item.status ?? '');
        return (
          <Badge variant={statusVariant(status)}>
            {t(`accounting:datevExports.status.${status}`, { defaultValue: status })}
          </Badge>
        );
      },
    },
    { key: 'recordCount', header: t('accounting:datevExports.columns.recordCount') },
    {
      key: 'createdAt',
      header: t('accounting:datevExports.columns.createdAt'),
      render: (item: Record<string, unknown>) => {
        const raw = item.createdAt as string | undefined;
        if (!raw) return '—';
        return new Date(raw).toLocaleDateString(i18n.language);
      },
    },
    {
      key: 'download',
      header: '',
      render: (item: Record<string, unknown>) => {
        if (item.status !== 'Ready') return null;
        return (
          <a
            href={`${API_BASE}/accounting/datev/exports/${item.id}/download`}
            target="_blank"
            rel="noreferrer"
            onClick={(e) => e.stopPropagation()}
          >
            <Button size="sm" variant="outline">
              <Download className="mr-1 h-3 w-3" />
              {t('accounting:datevExports.download')}
            </Button>
          </a>
        );
      },
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:datevExports.title')} />
        <p className="text-muted-foreground">{t('accounting:datevExports.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t('accounting:datevExports.title')}
        actions={
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => setSyncOpen(true)}>
              <RefreshCw className="mr-1 h-4 w-4" />
              {t('accounting:datevExports.travelSync')}
            </Button>
            <Button onClick={() => setGenerateOpen(true)}>
              {t('accounting:datevExports.generate')}
            </Button>
          </div>
        }
      />

      <DataTable
        columns={columns}
        data={(exports ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('accounting:datevExports.noEntries')}
      />

      {/* Generate Export Dialog */}
      <Dialog open={generateOpen} onOpenChange={setGenerateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:datevExports.dialogTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1">
              <Label>{t('accounting:datevExports.fiscalPeriodLabel')}</Label>
              <Select value={selectedPeriodId} onValueChange={setSelectedPeriodId}>
                <SelectTrigger>
                  <SelectValue placeholder="—" />
                </SelectTrigger>
                <SelectContent>
                  {(periods ?? []).map((p) => (
                    <SelectItem key={p.id} value={p.id}>
                      {p.year}/{String(p.month).padStart(2, '0')}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:datevExports.exportTypeLabel')}</Label>
              <Select value={exportType} onValueChange={setExportType}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Buchungsstapel">
                    {t('accounting:datevExports.exportType.Buchungsstapel')}
                  </SelectItem>
                  <SelectItem value="Stammdaten">
                    {t('accounting:datevExports.exportType.Stammdaten')}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setGenerateOpen(false)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              disabled={!selectedPeriodId || generateMutation.isPending}
              onClick={() => {
                if (!selectedEntityId || !selectedPeriodId) return;
                generateMutation.mutate({
                  entityId: selectedEntityId,
                  fiscalPeriodId: selectedPeriodId,
                  exportType,
                });
                setGenerateOpen(false);
              }}
            >
              {t('accounting:datevExports.generate')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Travel Sync Dialog */}
      <Dialog open={syncOpen} onOpenChange={setSyncOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:datevExports.travelSyncDialogTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1">
              <Label>{t('accounting:datevExports.travelSyncFromDate')}</Label>
              <Input type="date" value={syncFromDate} onChange={(e) => setSyncFromDate(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:datevExports.travelSyncToDate')}</Label>
              <Input type="date" value={syncToDate} onChange={(e) => setSyncToDate(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSyncOpen(false)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              disabled={!syncFromDate || !syncToDate || syncMutation.isPending}
              onClick={() => {
                if (!selectedEntityId || !syncFromDate || !syncToDate) return;
                syncMutation.mutate({
                  entityId: selectedEntityId,
                  fromDate: syncFromDate,
                  toDate: syncToDate,
                });
                setSyncOpen(false);
              }}
            >
              {t('accounting:datevExports.travelSyncButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
