import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useCostCenters, useCreateCostCenter } from '@/hooks/useAccounting';
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

  const { data: costCenters, isLoading } = useCostCenters(selectedEntityId);
  const createMutation = useCreateCostCenter();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [code, setCode] = useState('');
  const [shortName, setShortName] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState('Department');

  const resetForm = () => {
    setCode('');
    setShortName('');
    setDescription('');
    setType('Department');
  };

  const columns = [
    { key: 'code', header: t('accounting:costCenters.columns.code') },
    { key: 'shortName', header: t('accounting:costCenters.columns.shortName') },
    {
      key: 'type',
      header: t('accounting:costCenters.columns.type'),
      render: (item: Record<string, unknown>) =>
        t(`accounting:costCenters.type.${item.type}`, {
          defaultValue: String(item.type ?? ''),
        }),
    },
    {
      key: 'isActive',
      header: t('accounting:costCenters.columns.isActive'),
      render: (item: Record<string, unknown>) => (
        <Badge variant={item.isActive ? 'default' : 'secondary'}>
          {item.isActive ? t('common:status.active') : t('common:status.inactive')}
        </Badge>
      ),
    },
  ];

  if (!selectedEntityId) {
    return (
      <div>
        <PageHeader title={t('accounting:costCenters.title')} />
        <p className="text-muted-foreground">{t('accounting:costCenters.noEntitySelected')}</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={t('accounting:costCenters.title')}
        actions={
          <Button onClick={() => setDialogOpen(true)}>
            {t('accounting:costCenters.newCostCenter')}
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={(costCenters ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('accounting:costCenters.noEntries')}
      />

      <Dialog open={dialogOpen} onOpenChange={(open) => { setDialogOpen(open); if (!open) resetForm(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounting:costCenters.dialogTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1">
              <Label>{t('accounting:costCenters.fields.code')}</Label>
              <Input
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder={t('accounting:costCenters.placeholders.code')}
              />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:costCenters.fields.shortName')}</Label>
              <Input
                value={shortName}
                onChange={(e) => setShortName(e.target.value)}
                placeholder={t('accounting:costCenters.placeholders.shortName')}
              />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:costCenters.fields.description')}</Label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t('accounting:costCenters.placeholders.description')}
              />
            </div>
            <div className="space-y-1">
              <Label>{t('accounting:costCenters.fields.type')}</Label>
              <Select value={type} onValueChange={setType}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {['Department', 'Project', 'Employee', 'Other'].map((t_) => (
                    <SelectItem key={t_} value={t_}>
                      {t(`accounting:costCenters.type.${t_}`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setDialogOpen(false); resetForm(); }}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              disabled={!code || !shortName || createMutation.isPending}
              onClick={() => {
                if (!selectedEntityId || !code || !shortName) return;
                createMutation.mutate(
                  { entityId: selectedEntityId, code, shortName, description: description || undefined, type },
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
