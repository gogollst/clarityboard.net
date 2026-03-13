import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Pencil, Trash2, Loader2, ToggleLeft, ToggleRight } from 'lucide-react';
import {
  useProviderModels, useAddProviderModel, useUpdateProviderModel,
  useDeleteProviderModel, useToggleProviderModel,
} from '@/hooks/useAiManagement';
import { AI_PROVIDERS } from '@/types/ai';
import type { AiProvider, AiProviderModel } from '@/types/ai';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';

const CHAT_PROVIDERS = AI_PROVIDERS.filter(p => p !== 'DeepL');

const PROVIDER_LABELS: Record<string, string> = {
  Anthropic: 'Anthropic',
  OpenAI:    'OpenAI',
  Grok:      'X.AI (Grok)',
  Gemini:    'Gemini (Google)',
  ZAI:       'Z.AI (ZhipuAI)',
  Manus:     'Manus (Agent)',
};

type FormData = {
  provider: AiProvider;
  modelId: string;
  displayName: string;
  sortOrder: number;
  description: string;
};

const emptyForm = (provider?: AiProvider): FormData => ({
  provider: provider ?? 'Anthropic',
  modelId: '',
  displayName: '',
  sortOrder: 0,
  description: '',
});

export function Component() {
  const { t } = useTranslation('ai');
  const { data: models = [], isLoading } = useProviderModels(undefined, false);
  const addModel    = useAddProviderModel();
  const updateModel = useUpdateProviderModel();
  const deleteModel = useDeleteProviderModel();
  const toggleModel = useToggleProviderModel();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId]   = useState<string | null>(null);
  const [form, setForm]             = useState<FormData>(emptyForm());
  const [deleteConfirm, setDeleteConfirm] = useState<AiProviderModel | null>(null);

  const set = <K extends keyof FormData>(k: K, v: FormData[K]) =>
    setForm(prev => ({ ...prev, [k]: v }));

  const openAdd = (provider?: AiProvider) => {
    setEditingId(null);
    const providerModels = models.filter(m => m.provider === (provider ?? 'Anthropic'));
    const maxSort = providerModels.reduce((max, m) => Math.max(max, m.sortOrder), 0);
    setForm({ ...emptyForm(provider), sortOrder: maxSort + 10 });
    setDialogOpen(true);
  };

  const openEdit = (m: AiProviderModel) => {
    setEditingId(m.id);
    setForm({
      provider: m.provider,
      modelId: m.modelId,
      displayName: m.displayName,
      sortOrder: m.sortOrder,
      description: m.description ?? '',
    });
    setDialogOpen(true);
  };

  const handleSave = () => {
    if (editingId) {
      updateModel.mutate({ id: editingId, request: {
        displayName: form.displayName,
        sortOrder: form.sortOrder,
        description: form.description || undefined,
      }}, { onSuccess: () => setDialogOpen(false) });
    } else {
      addModel.mutate({
        provider: form.provider,
        modelId: form.modelId,
        displayName: form.displayName,
        sortOrder: form.sortOrder,
        description: form.description || undefined,
      }, { onSuccess: () => setDialogOpen(false) });
    }
  };

  const handleDelete = () => {
    if (!deleteConfirm) return;
    deleteModel.mutate(deleteConfirm.id, { onSuccess: () => setDeleteConfirm(null) });
  };

  const isSaving = addModel.isPending || updateModel.isPending;

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  const grouped = CHAT_PROVIDERS.map(provider => ({
    provider,
    label: PROVIDER_LABELS[provider] ?? provider,
    models: models.filter(m => m.provider === provider),
  }));

  return (
    <div className="space-y-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{t('providerModels.title')}</h1>
          <p className="text-muted-foreground mt-1">{t('providerModels.description')}</p>
        </div>
        <Button onClick={() => openAdd()}>
          <Plus className="mr-2 h-4 w-4" />{t('providerModels.addModel')}
        </Button>
      </div>

      {grouped.map(({ provider, label, models: providerModels }) => (
        <div key={provider} className="rounded-lg border">
          <div className="flex items-center justify-between border-b bg-muted/50 px-4 py-3">
            <div className="flex items-center gap-2">
              <h2 className="text-sm font-semibold">{label}</h2>
              <Badge variant="secondary" className="text-xs">{providerModels.length}</Badge>
            </div>
            <Button size="sm" variant="ghost" onClick={() => openAdd(provider)}>
              <Plus className="mr-1 h-3 w-3" />{t('providerModels.add')}
            </Button>
          </div>
          {providerModels.length === 0 ? (
            <p className="text-muted-foreground px-4 py-6 text-center text-sm">{t('providerModels.noModels')}</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[200px]">{t('providerModels.columns.modelId')}</TableHead>
                  <TableHead className="w-[180px]">{t('providerModels.columns.displayName')}</TableHead>
                  <TableHead>{t('providerModels.columns.description')}</TableHead>
                  <TableHead className="w-[80px] text-center">{t('providerModels.columns.sort')}</TableHead>
                  <TableHead className="w-[80px] text-center">{t('providerModels.columns.status')}</TableHead>
                  <TableHead className="w-[120px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {providerModels.map(m => (
                  <TableRow key={m.id} className={!m.isActive ? 'opacity-50' : ''}>
                    <TableCell className="font-mono text-xs">{m.modelId}</TableCell>
                    <TableCell className="text-sm font-medium">{m.displayName}</TableCell>
                    <TableCell className="text-muted-foreground text-xs max-w-xs truncate">{m.description}</TableCell>
                    <TableCell className="text-center text-xs text-muted-foreground">{m.sortOrder}</TableCell>
                    <TableCell className="text-center">
                      <button
                        onClick={() => toggleModel.mutate({ id: m.id, isActive: !m.isActive })}
                        className="inline-flex items-center"
                        title={m.isActive ? t('providerModels.deactivate') : t('providerModels.activate')}
                      >
                        {m.isActive
                          ? <ToggleRight className="h-5 w-5 text-emerald-600" />
                          : <ToggleLeft className="h-5 w-5 text-muted-foreground" />}
                      </button>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1 justify-end">
                        <Button size="sm" variant="ghost" onClick={() => openEdit(m)}>
                          <Pencil className="h-3 w-3" />
                        </Button>
                        <Button size="sm" variant="ghost" className="text-destructive" onClick={() => setDeleteConfirm(m)}>
                          <Trash2 className="h-3 w-3" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </div>
      ))}

      {/* Add / Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {editingId ? t('providerModels.editTitle') : t('providerModels.addTitle')}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('providerModels.form.provider')}</Label>
                <Select value={form.provider} onValueChange={v => set('provider', v as AiProvider)} disabled={!!editingId}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {CHAT_PROVIDERS.map(p => (
                      <SelectItem key={p} value={p}>{PROVIDER_LABELS[p] ?? p}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>{t('providerModels.form.modelId')}</Label>
                <Input
                  value={form.modelId}
                  onChange={e => set('modelId', e.target.value)}
                  placeholder="z.B. gpt-5.4"
                  disabled={!!editingId}
                  className="font-mono"
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('providerModels.form.displayName')}</Label>
                <Input
                  value={form.displayName}
                  onChange={e => set('displayName', e.target.value)}
                  placeholder="z.B. GPT-5.4"
                />
              </div>
              <div className="space-y-1.5">
                <Label>{t('providerModels.form.sortOrder')}</Label>
                <Input
                  type="number"
                  value={form.sortOrder}
                  onChange={e => set('sortOrder', parseInt(e.target.value, 10) || 0)}
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>{t('providerModels.form.description')}</Label>
              <Input
                value={form.description}
                onChange={e => set('description', e.target.value)}
                placeholder={t('providerModels.form.descriptionPlaceholder')}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setDialogOpen(false)}>{t('common:buttons.cancel')}</Button>
            <Button
              disabled={!form.modelId.trim() || !form.displayName.trim() || isSaving}
              onClick={handleSave}
            >
              {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {editingId ? t('common:buttons.save') : t('providerModels.addModel')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation */}
      <Dialog open={!!deleteConfirm} onOpenChange={open => !open && setDeleteConfirm(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('providerModels.deleteTitle')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground py-2">
            {t('providerModels.deleteConfirm', {
              model: deleteConfirm?.displayName,
              modelId: deleteConfirm?.modelId,
            })}
          </p>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setDeleteConfirm(null)}>{t('common:buttons.cancel')}</Button>
            <Button variant="destructive" disabled={deleteModel.isPending} onClick={handleDelete}>
              {deleteModel.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {t('common:buttons.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
