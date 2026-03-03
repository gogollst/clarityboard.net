import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntities, useCreateEntity, useUpdateEntity, useSetEntityActive } from '@/hooks/useEntity';
import { useUsers } from '@/hooks/useAdmin';
import type { LegalEntity } from '@/types/entity';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
} from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Building2, Plus, Loader2, MapPin, Pencil, PowerOff, Power } from 'lucide-react';

interface FormState {
  name: string;
  legalForm: string;
  managingDirectorId: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  taxId: string;
  vatId: string;
  registrationNumber: string;
  chartOfAccounts: string;
  currency: string;
  fiscalYearStartMonth: string;
  datevClientNumber: string;
  datevConsultantNumber: string;
  parentEntityId: string;
}

const emptyForm: FormState = {
  name: '',
  legalForm: '',
  managingDirectorId: '',
  street: '',
  city: '',
  postalCode: '',
  country: 'DE',
  taxId: '',
  vatId: '',
  registrationNumber: '',
  chartOfAccounts: 'SKR03',
  currency: 'EUR',
  fiscalYearStartMonth: '1',
  datevClientNumber: '',
  datevConsultantNumber: '',
  parentEntityId: '',
};

const LEGAL_FORMS = ['GmbH', 'AG', 'UG', 'GbR', 'KG', 'OHG', 'eK', 'SE', 'KGaA', 'GmbH & Co. KG'];

const COUNTRY_CODES = ['DE', 'AT', 'CH', 'LU', 'NL', 'BE', 'FR', 'GB', 'US'] as const;

const MONTH_KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12'] as const;

function FieldGroup({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <h4 className="mb-3 text-sm font-semibold text-muted-foreground uppercase tracking-wide">{label}</h4>
      <div className="space-y-3">
        {children}
      </div>
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string | null | undefined }) {
  if (!value) return null;
  return (
    <div>
      <span className="text-muted-foreground text-xs">{label}</span>
      <p className="font-medium text-sm">{value}</p>
    </div>
  );
}

export function Component() {
  const { t } = useTranslation('admin');
  const { data: entities, isLoading } = useEntities();
  const { data: usersData } = useUsers();
  const createEntity = useCreateEntity();
  const updateEntity = useUpdateEntity();
  const setEntityActive = useSetEntityActive();

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [editingEntity, setEditingEntity] = useState<LegalEntity | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);

  const users = usersData?.items ?? [];

  const resetForm = () => setForm(emptyForm);

  const updateField = (field: keyof FormState, value: string) => {
    setForm((f) => ({ ...f, [field]: value }));
  };

  const openEdit = (entity: LegalEntity) => {
    setEditingEntity(entity);
    setForm({
      name: entity.name,
      legalForm: entity.legalForm,
      managingDirectorId: entity.managingDirectorId ?? '',
      street: entity.street,
      city: entity.city,
      postalCode: entity.postalCode,
      country: entity.country,
      taxId: entity.taxId ?? '',
      vatId: entity.vatId ?? '',
      registrationNumber: entity.registrationNumber ?? '',
      chartOfAccounts: entity.chartOfAccounts,
      currency: entity.currency,
      fiscalYearStartMonth: String(entity.fiscalYearStartMonth),
      datevClientNumber: entity.datevClientNumber ?? '',
      datevConsultantNumber: entity.datevConsultantNumber ?? '',
      parentEntityId: entity.parentEntityId ?? '',
    });
  };

  const toNullableGuid = (v: string | undefined): string | null =>
    v?.trim() ? v.trim() : null;

  const toOptionalString = (v: string | undefined): string | undefined =>
    v?.trim() || undefined;

  const buildRequestBody = () => ({
    name: form.name,
    legalForm: form.legalForm,
    street: form.street,
    city: form.city,
    postalCode: form.postalCode,
    country: form.country || 'DE',
    currency: form.currency || 'EUR',
    chartOfAccounts: form.chartOfAccounts || 'SKR03',
    fiscalYearStartMonth: parseInt(form.fiscalYearStartMonth, 10) || 1,
    parentEntityId: toNullableGuid(form.parentEntityId),
    registrationNumber: toOptionalString(form.registrationNumber),
    taxId: toOptionalString(form.taxId),
    vatId: toOptionalString(form.vatId),
    datevClientNumber: toOptionalString(form.datevClientNumber),
    datevConsultantNumber: toOptionalString(form.datevConsultantNumber),
    managingDirectorId: toNullableGuid(form.managingDirectorId),
  });

  const handleCreate = () => {
    createEntity.mutate(buildRequestBody(), {
      onSuccess: () => {
        setIsAddOpen(false);
        resetForm();
      },
    });
  };

  const handleEdit = () => {
    if (!editingEntity) return;
    updateEntity.mutate(
      { id: editingEntity.id, ...buildRequestBody() },
      {
        onSuccess: () => {
          setEditingEntity(null);
          resetForm();
        },
      },
    );
  };

  const canSubmit =
    form.name.trim() !== '' &&
    form.legalForm !== '' &&
    form.street.trim() !== '' &&
    form.city.trim() !== '' &&
    form.postalCode.trim() !== '';

  return (
    <div>
      <PageHeader
        title={t('entities.title')}
        description={t('entities.description')}
        actions={
          <Button onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            {t('entities.addEntity')}
          </Button>
        }
      />

      {isLoading ? (
        <div className="space-y-4">
          {[1, 2].map((i) => (
            <Card key={i}>
              <CardHeader>
                <Skeleton className="h-6 w-48" />
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-4">
                  <Skeleton className="h-10 w-full" />
                  <Skeleton className="h-10 w-full" />
                  <Skeleton className="h-10 w-full" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : !entities || entities.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
            <Building2 className="h-7 w-7 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">{t('entities.noEntities.heading')}</h3>
          <p className="mt-1 max-w-sm text-sm text-muted-foreground">
            {t('entities.noEntities.description')}
          </p>
          <Button className="mt-4" onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            {t('entities.noEntities.createFirst')}
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {entities.map((entity: LegalEntity) => (
            <EntityCard
              key={entity.id}
              entity={entity}
              allEntities={entities}
              onEdit={openEdit}
              onToggleActive={(e) => setEntityActive.mutate({ id: e.id, isActive: !e.isActive })}
              isTogglingActive={setEntityActive.isPending}
            />
          ))}
        </div>
      )}

      {/* Edit Entity Dialog */}
      <Dialog open={!!editingEntity} onOpenChange={(open) => { if (!open) { setEditingEntity(null); resetForm(); } }}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{t('entities.dialogs.edit.title')}</DialogTitle>
          </DialogHeader>
          <EntityFormFields
            form={form}
            updateField={updateField}
            entities={entities ?? []}
            users={users}
            excludeEntityId={editingEntity?.id}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => { setEditingEntity(null); resetForm(); }}>
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button onClick={handleEdit} disabled={updateEntity.isPending || !canSubmit}>
              {updateEntity.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('entities.dialogs.edit.saveButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Entity Dialog */}
      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{t('entities.dialogs.create.title')}</DialogTitle>
          </DialogHeader>
          <EntityFormFields
            form={form}
            updateField={updateField}
            entities={entities ?? []}
            users={users}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddOpen(false)}>
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button onClick={handleCreate} disabled={createEntity.isPending || !canSubmit}>
              {createEntity.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('entities.dialogs.create.createButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface EntityFormFieldsProps {
  form: FormState;
  updateField: (field: keyof FormState, value: string) => void;
  entities: LegalEntity[];
  users: { id: string; firstName: string; lastName: string }[];
  excludeEntityId?: string;
}

function EntityFormFields({ form, updateField, entities, users, excludeEntityId }: EntityFormFieldsProps) {
  const { t } = useTranslation('admin');
  const availableParents = entities.filter((e) => e.id !== excludeEntityId);
  const countries = useMemo(
    () => COUNTRY_CODES.map((code) => ({ value: code, label: t(`entities.countries.${code}`) })),
    [t],
  );
  return (
    <div className="space-y-6">
      <FieldGroup label={t('entities.form.sections.companyInfo')}>
        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2 sm:col-span-1">
            <Label>{t('entities.form.fields.companyName')}</Label>
            <Input value={form.name} onChange={(e) => updateField('name', e.target.value)} placeholder={t('entities.form.fields.companyNamePlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.legalForm')}</Label>
            <Select value={form.legalForm} onValueChange={(v) => updateField('legalForm', v)}>
              <SelectTrigger><SelectValue placeholder={t('entities.form.fields.legalFormPlaceholder')} /></SelectTrigger>
              <SelectContent>
                {LEGAL_FORMS.map((lf) => <SelectItem key={lf} value={lf}>{lf}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div>
          <Label>{t('entities.form.fields.managingDirector')}</Label>
          <Select value={form.managingDirectorId} onValueChange={(v) => updateField('managingDirectorId', v)}>
            <SelectTrigger><SelectValue placeholder={t('entities.form.fields.managingDirectorPlaceholder')} /></SelectTrigger>
            <SelectContent>
              {users.map((u) => (
                <SelectItem key={u.id} value={u.id}>{u.firstName} {u.lastName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label={t('entities.form.sections.address')}>
        <div>
          <Label>{t('entities.form.fields.street')}</Label>
          <Input value={form.street} onChange={(e) => updateField('street', e.target.value)} placeholder={t('entities.form.fields.streetPlaceholder')} />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <div>
            <Label>{t('entities.form.fields.postalCode')}</Label>
            <Input value={form.postalCode} onChange={(e) => updateField('postalCode', e.target.value)} placeholder={t('entities.form.fields.postalCodePlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.city')}</Label>
            <Input value={form.city} onChange={(e) => updateField('city', e.target.value)} placeholder={t('entities.form.fields.cityPlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.country')}</Label>
            <Select value={form.country} onValueChange={(v) => updateField('country', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {countries.map((c) => <SelectItem key={c.value} value={c.value}>{c.label} ({c.value})</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label={t('entities.form.sections.taxRegistration')}>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label>{t('entities.form.fields.taxId')}</Label>
            <Input value={form.taxId} onChange={(e) => updateField('taxId', e.target.value)} placeholder={t('entities.form.fields.taxIdPlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.vatId')}</Label>
            <Input value={form.vatId} onChange={(e) => updateField('vatId', e.target.value)} placeholder={t('entities.form.fields.vatIdPlaceholder')} />
          </div>
        </div>
        <div>
          <Label>{t('entities.form.fields.registrationNumber')}</Label>
          <Input value={form.registrationNumber} onChange={(e) => updateField('registrationNumber', e.target.value)} placeholder={t('entities.form.fields.registrationNumberPlaceholder')} />
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label={t('entities.form.sections.accounting')}>
        <div className="grid grid-cols-3 gap-3">
          <div>
            <Label>{t('entities.form.fields.chartOfAccounts')}</Label>
            <Select value={form.chartOfAccounts} onValueChange={(v) => updateField('chartOfAccounts', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="SKR03">SKR03</SelectItem>
                <SelectItem value="SKR04">SKR04</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label>{t('entities.form.fields.currency')}</Label>
            <Input value={form.currency} onChange={(e) => updateField('currency', e.target.value)} placeholder={t('entities.form.fields.currencyPlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.fiscalYearStart')}</Label>
            <Select value={form.fiscalYearStartMonth} onValueChange={(v) => updateField('fiscalYearStartMonth', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {MONTH_KEYS.map((key) => (
                  <SelectItem key={key} value={key}>{t(`entities.months.${key}`)}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label={t('entities.form.sections.datev')}>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label>{t('entities.form.fields.datevClientNumber')}</Label>
            <Input value={form.datevClientNumber} onChange={(e) => updateField('datevClientNumber', e.target.value)} placeholder={t('entities.form.fields.datevClientNumberPlaceholder')} />
          </div>
          <div>
            <Label>{t('entities.form.fields.datevConsultantNumber')}</Label>
            <Input value={form.datevConsultantNumber} onChange={(e) => updateField('datevConsultantNumber', e.target.value)} placeholder={t('entities.form.fields.datevConsultantNumberPlaceholder')} />
          </div>
        </div>
      </FieldGroup>
      {availableParents.length > 0 && (
        <>
          <Separator />
          <FieldGroup label={t('entities.form.sections.hierarchy')}>
            <div>
              <Label>{t('entities.form.fields.parentEntity')}</Label>
              <Select value={form.parentEntityId} onValueChange={(v) => updateField('parentEntityId', v)}>
                <SelectTrigger><SelectValue placeholder={t('entities.form.fields.parentEntityPlaceholder')} /></SelectTrigger>
                <SelectContent>
                  {availableParents.map((e) => <SelectItem key={e.id} value={e.id}>{e.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
          </FieldGroup>
        </>
      )}
    </div>
  );
}

function EntityCard({
  entity,
  allEntities,
  onEdit,
  onToggleActive,
  isTogglingActive,
}: {
  entity: LegalEntity;
  allEntities: LegalEntity[];
  onEdit: (entity: LegalEntity) => void;
  onToggleActive: (entity: LegalEntity) => void;
  isTogglingActive: boolean;
}) {
  const { t } = useTranslation('admin');
  const parentName = entity.parentEntityId
    ? allEntities.find((e) => e.id === entity.parentEntityId)?.name ?? t('entities.card.unknownEntity')
    : null;

  const monthKey = String(entity.fiscalYearStartMonth) as typeof MONTH_KEYS[number];
  const monthName = MONTH_KEYS.includes(monthKey) ? t(`entities.months.${monthKey}`) : undefined;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <CardTitle className="flex items-center gap-2">
            <Building2 className="h-5 w-5 text-muted-foreground" />
            {entity.name}
          </CardTitle>
          <div className="flex items-center gap-2">
            <StatusBadge
              status={entity.isActive ? 'Active' : 'Inactive'}
              variantMap={{ active: 'success', inactive: 'destructive' }}
            />
            <Button size="sm" variant="outline" onClick={() => onEdit(entity)}>
              <Pencil className="h-3.5 w-3.5 mr-1" />
              {t('entities.card.edit')}
            </Button>
            <Button
              size="sm"
              variant={entity.isActive ? 'destructive' : 'default'}
              onClick={() => onToggleActive(entity)}
              disabled={isTogglingActive}
            >
              {isTogglingActive ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              ) : entity.isActive ? (
                <><PowerOff className="h-3.5 w-3.5 mr-1" />{t('entities.card.deactivate')}</>
              ) : (
                <><Power className="h-3.5 w-3.5 mr-1" />{t('entities.card.reactivate')}</>
              )}
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Company Info */}
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3 lg:grid-cols-4">
          <DetailRow label={t('entities.card.legalForm')} value={entity.legalForm} />
          <DetailRow label={t('entities.card.managingDirector')} value={entity.managingDirectorName} />
          <DetailRow label={t('entities.card.currency')} value={entity.currency} />
          <DetailRow label={t('entities.card.chartOfAccounts')} value={entity.chartOfAccounts} />
        </div>

        <Separator />

        {/* Address */}
        <div className="flex items-start gap-2 text-sm">
          <MapPin className="mt-0.5 h-4 w-4 text-muted-foreground flex-shrink-0" />
          <span>{entity.street}, {entity.postalCode} {entity.city}, {entity.country}</span>
        </div>

        {/* Tax & Registration */}
        {(entity.taxId || entity.vatId || entity.registrationNumber) && (
          <>
            <Separator />
            <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3">
              <DetailRow label={t('entities.card.taxId')} value={entity.taxId} />
              <DetailRow label={t('entities.card.vatId')} value={entity.vatId} />
              <DetailRow label={t('entities.card.registration')} value={entity.registrationNumber} />
            </div>
          </>
        )}

        {/* Accounting */}
        <Separator />
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-4">
          <DetailRow label={t('entities.card.fiscalYearStart')} value={monthName} />
          {entity.datevClientNumber && (
            <DetailRow label={t('entities.card.datevClientNr')} value={entity.datevClientNumber} />
          )}
          {entity.datevConsultantNumber && (
            <DetailRow label={t('entities.card.datevConsultantNr')} value={entity.datevConsultantNumber} />
          )}
          {parentName && (
            <DetailRow label={t('entities.card.parentEntity')} value={parentName} />
          )}
        </div>
      </CardContent>
    </Card>
  );
}
