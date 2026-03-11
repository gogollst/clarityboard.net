import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useAccountingScenarios, useCreateAccountingScenario } from '@/hooks/useAccounting';
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

export function Component() {
  const { t } = useTranslation(['accounting', 'common']);
  const { selectedEntityId } = useEntity();

  const { data: scenarios, isLoading } = useAccountingScenarios(selectedEntityId);
  const createMutation = useCreateAccountingScenario();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [scenarioType, setScenarioType] = useState('Budget');
  const [year, setYear] = useState(String(new Date().getFullYear()));

  const resetForm = () => {
    setName('');
    setDescription('');
    setScenarioType('Budget');
    setYear(String(new Date().getFullYear()));
  };

  const columns = [
    { key: 'name', header: t('accounting:accountingScenarios.columns.name') },
    {
      key: 'scenarioType',
      header: t('accounting:accountingScenarios.columns.scenarioType'),
      render: (item: Record<string, unknown>) =>
        t(`accounting:accountingScenarios.type.${item.scenarioType}`, {
          defaultValue: String(item.scenarioType ?? ''),
        }),
    },
    { key: 'year', header: t('accounting:accountingScenarios.columns.year') },
    {
      key: 'isBaseline',
      header: t('accounting:accountingScenarios.columns.isBaseline'),
      render: (item: Record<string, unknown>) =>
        item.isBaseline ? <Badge variant="default">Baseline</Badge> : null,
    },
    {
      key: 'isLocked',
      header: t('accounting:accountingScenarios.columns.isLocked'),
      render: (item: Record<string, unknown>) =>
        item.isLocked ? <Badge variant="destructive">{t('accounting:accountingScenarios.columns.isLocked')}</Badge> : null,
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:accountingScenarios.title')} />
        <p className="text-muted-foreground">{t('accounting:accountingScenarios.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t('accounting:accountingScenarios.title')}
        actions={
          <Button onClick={() => setDialogOpen(true)}>
            {t('accounting:accountingScenarios.newScenario')}
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={(scenarios ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('accounting:accountingScenarios.noEntries')}
      />

      <Dialog open={dialogOpen} onOpenChange={(open) => { setDialogOpen(open); if (!open) resetForm(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:accountingScenarios.dialogTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1">
              <Label>{t('accounting:accountingScenarios.fields.name')}</Label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={t('accounting:accountingScenarios.placeholders.name')}
              />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:accountingScenarios.fields.description')}</Label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t('accounting:accountingScenarios.placeholders.description')}
              />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:accountingScenarios.fields.type')}</Label>
              <Select value={scenarioType} onValueChange={setScenarioType}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {['Budget', 'Forecast', 'Custom'].map((st) => (
                    <SelectItem key={st} value={st}>
                      {t(`accounting:accountingScenarios.type.${st}`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:accountingScenarios.fields.year')}</Label>
              <Input
                type="number"
                value={year}
                onChange={(e) => setYear(e.target.value)}
                min={2020}
                max={2040}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setDialogOpen(false); resetForm(); }}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              disabled={!name || !year || createMutation.isPending}
              onClick={() => {
                const yearNum = parseInt(year, 10);
                if (!selectedEntityId || !name || !yearNum) return;
                createMutation.mutate(
                  { entityId: selectedEntityId, name, description: description || undefined, scenarioType, year: yearNum },
                  { onSuccess: () => { setDialogOpen(false); resetForm(); } }
                );
              }}
            >
              {t('common:buttons.create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
