import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useBusinessPartner,
  useUpdateBusinessPartner,
  useDeactivateBusinessPartner,
  useAccounts,
} from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { getLocalizedAccountName } from '@/lib/accountUtils';
import type { BusinessPartner } from '@/types/accounting';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
} from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { ArrowLeft, Loader2, Pencil } from 'lucide-react';

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value ?? '—'}</dd>
    </div>
  );
}

function TypeBadge({ partner }: { partner: BusinessPartner }) {
  const { t } = useTranslation('accounting');
  if (partner.isCreditor && partner.isDebtor) {
    return <Badge variant="secondary">{t('businessPartners.type.both')}</Badge>;
  }
  if (partner.isCreditor) {
    return (
      <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300">
        {t('businessPartners.type.creditor')}
      </Badge>
    );
  }
  return (
    <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
      {t('businessPartners.type.debtor')}
    </Badge>
  );
}

function StatusBadge({ isActive }: { isActive: boolean }) {
  const { t } = useTranslation('accounting');
  if (isActive) {
    return (
      <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
        {t('businessPartners.status.active')}
      </Badge>
    );
  }
  return (
    <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
      {t('businessPartners.status.inactive')}
    </Badge>
  );
}

// ---------------------------------------------------------------------------
// Edit Dialog
// ---------------------------------------------------------------------------

interface EditDialogProps {
  open: boolean;
  onClose: () => void;
  partner: BusinessPartner;
  entityId: string;
}

function EditDialog({ open, onClose, partner, entityId }: EditDialogProps) {
  const { t, i18n } = useTranslation('accounting');
  const updatePartner = useUpdateBusinessPartner();
  const { data: accounts = [] } = useAccounts(entityId);

  const [form, setForm] = useState({
    name: partner.name,
    taxId: partner.taxId ?? '',
    vatNumber: partner.vatNumber ?? '',
    street: partner.street ?? '',
    city: partner.city ?? '',
    postalCode: partner.postalCode ?? '',
    country: partner.country ?? '',
    email: partner.email ?? '',
    phone: partner.phone ?? '',
    bankName: partner.bankName ?? '',
    iban: partner.iban ?? '',
    bic: partner.bic ?? '',
    isCreditor: partner.isCreditor,
    isDebtor: partner.isDebtor,
    paymentTermDays: partner.paymentTermDays,
    defaultExpenseAccountId: partner.defaultExpenseAccountId ?? '',
    defaultRevenueAccountId: partner.defaultRevenueAccountId ?? '',
    contactEmployeeId: partner.contactEmployeeId ?? '',
    notes: partner.notes ?? '',
    isActive: partner.isActive,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim() || (!form.isCreditor && !form.isDebtor)) return;
    updatePartner.mutate(
      {
        entityId,
        id: partner.id,
        name: form.name,
        taxId: form.taxId || undefined,
        vatNumber: form.vatNumber || undefined,
        street: form.street || undefined,
        city: form.city || undefined,
        postalCode: form.postalCode || undefined,
        country: form.country || undefined,
        email: form.email || undefined,
        phone: form.phone || undefined,
        bankName: form.bankName || undefined,
        iban: form.iban || undefined,
        bic: form.bic || undefined,
        isCreditor: form.isCreditor,
        isDebtor: form.isDebtor,
        paymentTermDays: form.paymentTermDays,
        defaultExpenseAccountId: form.defaultExpenseAccountId || undefined,
        defaultRevenueAccountId: form.defaultRevenueAccountId || undefined,
        contactEmployeeId: form.contactEmployeeId || undefined,
        isActive: form.isActive,
        notes: form.notes || undefined,
      },
      { onSuccess: onClose },
    );
  };

  const expenseAccounts = accounts.filter((a) => a.accountType === 'Expense' && a.isActive);
  const revenueAccounts = accounts.filter((a) => a.accountType === 'Revenue' && a.isActive);

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) onClose(); }}>
      <DialogContent className="max-w-lg max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('businessPartners.actions.edit')}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label>{t('businessPartners.fields.name')} *</Label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              maxLength={200}
              required
            />
          </div>

          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <Checkbox
                id="editIsCreditor"
                checked={form.isCreditor}
                onCheckedChange={(c) => setForm({ ...form, isCreditor: !!c })}
              />
              <Label htmlFor="editIsCreditor" className="cursor-pointer">
                {t('businessPartners.fields.isCreditor')}
              </Label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="editIsDebtor"
                checked={form.isDebtor}
                onCheckedChange={(c) => setForm({ ...form, isDebtor: !!c })}
              />
              <Label htmlFor="editIsDebtor" className="cursor-pointer">
                {t('businessPartners.fields.isDebtor')}
              </Label>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.taxId')}</Label>
              <Input
                value={form.taxId}
                onChange={(e) => setForm({ ...form, taxId: e.target.value })}
                maxLength={50}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.vatNumber')}</Label>
              <Input
                value={form.vatNumber}
                onChange={(e) => setForm({ ...form, vatNumber: e.target.value })}
                maxLength={50}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>{t('businessPartners.fields.street')}</Label>
            <Input
              value={form.street}
              onChange={(e) => setForm({ ...form, street: e.target.value })}
              maxLength={200}
            />
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.postalCode')}</Label>
              <Input
                value={form.postalCode}
                onChange={(e) => setForm({ ...form, postalCode: e.target.value })}
                maxLength={20}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.city')}</Label>
              <Input
                value={form.city}
                onChange={(e) => setForm({ ...form, city: e.target.value })}
                maxLength={100}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.country')}</Label>
              <Input
                value={form.country}
                onChange={(e) => setForm({ ...form, country: e.target.value })}
                maxLength={2}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.email')}</Label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                maxLength={200}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.phone')}</Label>
              <Input
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                maxLength={50}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>{t('businessPartners.fields.bankName')}</Label>
            <Input
              value={form.bankName}
              onChange={(e) => setForm({ ...form, bankName: e.target.value })}
              maxLength={200}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.iban')}</Label>
              <Input
                value={form.iban}
                onChange={(e) => setForm({ ...form, iban: e.target.value })}
                maxLength={34}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.bic')}</Label>
              <Input
                value={form.bic}
                onChange={(e) => setForm({ ...form, bic: e.target.value })}
                maxLength={11}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>{t('businessPartners.fields.paymentTermDays')}</Label>
            <Input
              type="number"
              min={0}
              value={form.paymentTermDays}
              onChange={(e) => setForm({ ...form, paymentTermDays: parseInt(e.target.value) || 0 })}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.defaultExpenseAccount')}</Label>
              <Select
                value={form.defaultExpenseAccountId || 'none'}
                onValueChange={(v) =>
                  setForm({ ...form, defaultExpenseAccountId: v === 'none' ? '' : v })
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">—</SelectItem>
                  {expenseAccounts.map((a) => (
                    <SelectItem key={a.id} value={a.id}>
                      {a.accountNumber} — {getLocalizedAccountName(a, i18n.language)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('businessPartners.fields.defaultRevenueAccount')}</Label>
              <Select
                value={form.defaultRevenueAccountId || 'none'}
                onValueChange={(v) =>
                  setForm({ ...form, defaultRevenueAccountId: v === 'none' ? '' : v })
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">—</SelectItem>
                  {revenueAccounts.map((a) => (
                    <SelectItem key={a.id} value={a.id}>
                      {a.accountNumber} — {getLocalizedAccountName(a, i18n.language)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>{t('businessPartners.fields.notes')}</Label>
            <Textarea
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
              rows={3}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              {t('common:buttons.cancel')}
            </Button>
            <Button type="submit" disabled={updatePartner.isPending}>
              {updatePartner.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('common:buttons.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t, i18n } = useTranslation('accounting');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const [editOpen, setEditOpen] = useState(false);
  const [deactivateOpen, setDeactivateOpen] = useState(false);
  const deactivatePartner = useDeactivateBusinessPartner();

  const { data: partner, isLoading } = useBusinessPartner(id ?? null, selectedEntityId);

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!partner) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('businessPartners.notFound')}
      </div>
    );
  }

  const handleDeactivate = () => {
    if (!selectedEntityId) return;
    deactivatePartner.mutate(
      { id: partner.id, entityId: selectedEntityId },
      {
        onSuccess: () => {
          setDeactivateOpen(false);
        },
      },
    );
  };

  return (
    <div>
      <PageHeader
        title={`${partner.partnerNumber} — ${partner.name}`}
        actions={
          <div className="flex items-center gap-2">
            <TypeBadge partner={partner} />
            <StatusBadge isActive={partner.isActive} />
            <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
              <Pencil className="mr-1 h-4 w-4" />
              {t('businessPartners.actions.edit')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/accounting/business-partners')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              {t('common:buttons.back')}
            </Button>
          </div>
        }
      />

      <Tabs defaultValue="masterData">
        <TabsList className="mb-6">
          <TabsTrigger value="masterData">{t('businessPartners.tabs.masterData')}</TabsTrigger>
          <TabsTrigger value="bank">{t('businessPartners.tabs.bank')}</TabsTrigger>
          <TabsTrigger value="accounting">{t('businessPartners.tabs.accounting')}</TabsTrigger>
        </TabsList>

        <TabsContent value="masterData">
          <Card>
            <CardContent className="pt-6">
              <dl className="grid grid-cols-2 gap-6">
                <DetailRow label={t('businessPartners.fields.partnerNumber')} value={partner.partnerNumber} />
                <DetailRow label={t('businessPartners.fields.name')} value={partner.name} />
                <DetailRow label={t('businessPartners.fields.taxId')} value={partner.taxId} />
                <DetailRow label={t('businessPartners.fields.vatNumber')} value={partner.vatNumber} />
                <DetailRow label={t('businessPartners.fields.street')} value={partner.street} />
                <DetailRow
                  label={t('businessPartners.fields.city')}
                  value={
                    [partner.postalCode, partner.city].filter(Boolean).join(' ') || '—'
                  }
                />
                <DetailRow label={t('businessPartners.fields.country')} value={partner.country} />
                <DetailRow label={t('businessPartners.fields.email')} value={partner.email} />
                <DetailRow label={t('businessPartners.fields.phone')} value={partner.phone} />
                <DetailRow
                  label={t('businessPartners.fields.contactEmployee')}
                  value={partner.contactEmployeeName}
                />
                <DetailRow
                  label={t('businessPartners.fields.isActive')}
                  value={<StatusBadge isActive={partner.isActive} />}
                />
                <DetailRow
                  label={t('common:createdAt')}
                  value={formatDate(partner.createdAt)}
                />
              </dl>

              {partner.notes && (
                <div className="mt-6 border-t pt-4">
                  <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    {t('businessPartners.fields.notes')}
                  </dt>
                  <dd className="mt-1 whitespace-pre-wrap text-sm text-foreground">{partner.notes}</dd>
                </div>
              )}

              {partner.isActive && (
                <div className="mt-6 flex justify-end border-t pt-4">
                  <Button variant="destructive" onClick={() => setDeactivateOpen(true)}>
                    {t('businessPartners.actions.deactivate')}
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="bank">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('businessPartners.tabs.bank')}</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-6">
                <DetailRow label={t('businessPartners.fields.bankName')} value={partner.bankName} />
                <DetailRow label={t('businessPartners.fields.iban')} value={partner.iban} />
                <DetailRow label={t('businessPartners.fields.bic')} value={partner.bic} />
              </dl>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="accounting">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('businessPartners.tabs.accounting')}</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-6">
                <DetailRow
                  label={t('businessPartners.fields.paymentTermDays')}
                  value={`${partner.paymentTermDays}`}
                />
                <DetailRow
                  label={t('businessPartners.fields.defaultExpenseAccount')}
                  value={partner.defaultExpenseAccountId ?? '—'}
                />
                <DetailRow
                  label={t('businessPartners.fields.defaultRevenueAccount')}
                  value={partner.defaultRevenueAccountId ?? '—'}
                />
              </dl>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {editOpen && selectedEntityId && (
        <EditDialog
          open={editOpen}
          onClose={() => setEditOpen(false)}
          partner={partner}
          entityId={selectedEntityId}
        />
      )}

      <Dialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('businessPartners.actions.deactivate')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {t('businessPartners.confirmDeactivate')}
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeactivateOpen(false)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeactivate}
              disabled={deactivatePartner.isPending}
            >
              {deactivatePartner.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('businessPartners.actions.deactivate')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
