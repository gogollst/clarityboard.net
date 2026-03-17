import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Pencil, Trash2, Loader2, Package } from 'lucide-react';
import { useEntity } from '@/hooks/useEntity';
import { useAccounts } from '@/hooks/useAccounting';
import {
  useProductMappings,
  useUpsertProductMapping,
  useDeleteProductMapping,
} from '@/hooks/useAdmin';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { AccountCombobox } from '@/components/shared/AccountCombobox';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import type { ProductCategoryMapping } from '@/types/admin';

const PRODUCT_CATEGORIES = [
  'SAAS_LICENSE',
  'ON_PREM_LICENSE',
  'HOSTING',
  'MAINTENANCE',
  'ONE_TIME_SERVICE',
  'DISCOUNT',
] as const;

interface FormState {
  id?: string;
  productNamePattern: string;
  productCategory: string;
  revenueAccountId: string;
  isActive: boolean;
}

const EMPTY_FORM: FormState = {
  productNamePattern: '',
  productCategory: '',
  revenueAccountId: '',
  isActive: true,
};

export function Component() {
  const { t } = useTranslation('admin');
  const { selectedEntityId } = useEntity();
  const entityId = selectedEntityId ?? '';
  const { data: mappings, isLoading } = useProductMappings(entityId);
  const { data: accounts = [] } = useAccounts(entityId);
  const upsertMutation = useUpsertProductMapping();
  const deleteMutation = useDeleteProductMapping();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<ProductCategoryMapping | null>(null);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);

  const openCreate = () => {
    setForm(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (m: ProductCategoryMapping) => {
    setForm({
      id: m.id,
      productNamePattern: m.productNamePattern,
      productCategory: m.productCategory,
      revenueAccountId: m.revenueAccountId ?? '',
      isActive: m.isActive,
    });
    setDialogOpen(true);
  };

  const handleSave = () => {
    if (!form.productNamePattern || !form.productCategory) return;
    upsertMutation.mutate(
      {
        id: form.id,
        entityId,
        productNamePattern: form.productNamePattern,
        productCategory: form.productCategory,
        revenueAccountId: form.revenueAccountId || undefined,
        isActive: form.isActive,
      },
      { onSuccess: () => setDialogOpen(false) },
    );
  };

  const handleDelete = () => {
    if (!deleteTarget) return;
    deleteMutation.mutate(
      { mappingId: deleteTarget.id, entityId },
      { onSuccess: () => setDeleteTarget(null) },
    );
  };

  return (
    <div className="space-y-6 p-6">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              {t('productMappings.title')}
            </CardTitle>
            <CardDescription>{t('productMappings.description')}</CardDescription>
          </div>
          <Button onClick={openCreate} size="sm">
            <Plus className="mr-1 h-4 w-4" />
            {t('productMappings.addMapping')}
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : !mappings?.length ? (
            <p className="py-8 text-center text-muted-foreground">
              {t('productMappings.noMappings')}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('productMappings.columns.pattern')}</TableHead>
                  <TableHead>{t('productMappings.columns.category')}</TableHead>
                  <TableHead>{t('productMappings.columns.revenueAccount')}</TableHead>
                  <TableHead>{t('productMappings.columns.active')}</TableHead>
                  <TableHead className="w-24">{t('productMappings.columns.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {mappings.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-medium">{m.productNamePattern}</TableCell>
                    <TableCell>
                      <Badge variant="outline">
                        {t(`productMappings.categories.${m.productCategory}`, { defaultValue: m.productCategory })}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {m.revenueAccountNumber
                        ? `${m.revenueAccountNumber} – ${m.revenueAccountName}`
                        : '—'}
                    </TableCell>
                    <TableCell>
                      <Badge variant={m.isActive ? 'default' : 'secondary'}>
                        {m.isActive ? 'On' : 'Off'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(m)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => setDeleteTarget(m)}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create / Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {form.id ? t('productMappings.dialog.editTitle') : t('productMappings.dialog.createTitle')}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label>{t('productMappings.dialog.pattern')}</Label>
              <Input
                value={form.productNamePattern}
                onChange={(e) => setForm((f) => ({ ...f, productNamePattern: e.target.value }))}
                placeholder={t('productMappings.dialog.patternPlaceholder')}
              />
              <p className="text-xs text-muted-foreground">
                {t('productMappings.dialog.patternHint')}
              </p>
            </div>
            <div className="space-y-2">
              <Label>{t('productMappings.dialog.category')}</Label>
              <Select
                value={form.productCategory}
                onValueChange={(v) => setForm((f) => ({ ...f, productCategory: v }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('productMappings.dialog.categoryPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {PRODUCT_CATEGORIES.map((cat) => (
                    <SelectItem key={cat} value={cat}>
                      {t(`productMappings.categories.${cat}`, { defaultValue: cat })}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('productMappings.dialog.revenueAccount')}</Label>
              <AccountCombobox
                accounts={accounts}
                value={form.revenueAccountId}
                onValueChange={(v) => setForm((f) => ({ ...f, revenueAccountId: v }))}
              />
            </div>
            <div className="flex items-center gap-2">
              <Switch
                checked={form.isActive}
                onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
              />
              <Label>{t('productMappings.dialog.active')}</Label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              {t('productMappings.dialog.cancel')}
            </Button>
            <Button
              onClick={handleSave}
              disabled={!form.productNamePattern || !form.productCategory || upsertMutation.isPending}
            >
              {upsertMutation.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('productMappings.dialog.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={() => setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('productMappings.confirmDelete', { pattern: deleteTarget?.productNamePattern })}</DialogTitle>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              {t('productMappings.dialog.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('productMappings.dialog.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
