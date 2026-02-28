import { useState } from 'react';
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

const COUNTRIES = [
  { value: 'DE', label: 'Deutschland' },
  { value: 'AT', label: 'Österreich' },
  { value: 'CH', label: 'Schweiz' },
  { value: 'LU', label: 'Luxemburg' },
  { value: 'NL', label: 'Niederlande' },
  { value: 'BE', label: 'Belgien' },
  { value: 'FR', label: 'Frankreich' },
  { value: 'GB', label: 'Großbritannien' },
  { value: 'US', label: 'USA' },
];

const MONTHS = [
  { value: '1', label: 'January' },
  { value: '2', label: 'February' },
  { value: '3', label: 'March' },
  { value: '4', label: 'April' },
  { value: '5', label: 'May' },
  { value: '6', label: 'June' },
  { value: '7', label: 'July' },
  { value: '8', label: 'August' },
  { value: '9', label: 'September' },
  { value: '10', label: 'October' },
  { value: '11', label: 'November' },
  { value: '12', label: 'December' },
];

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
        title="Entity Configuration"
        description="View and manage legal entities"
        actions={
          <Button onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            Add Entity
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
          <h3 className="mt-4 text-lg font-semibold">No Entities</h3>
          <p className="mt-1 max-w-sm text-sm text-muted-foreground">
            No legal entities have been configured yet. Create one to get started.
          </p>
          <Button className="mt-4" onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            Create First Entity
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
            <DialogTitle>Edit Legal Entity</DialogTitle>
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
              Cancel
            </Button>
            <Button onClick={handleEdit} disabled={updateEntity.isPending || !canSubmit}>
              {updateEntity.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Entity Dialog */}
      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Create Legal Entity</DialogTitle>
          </DialogHeader>
          <EntityFormFields
            form={form}
            updateField={updateField}
            entities={entities ?? []}
            users={users}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={createEntity.isPending || !canSubmit}>
              {createEntity.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              Create Entity
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
  const availableParents = entities.filter((e) => e.id !== excludeEntityId);
  return (
    <div className="space-y-6">
      <FieldGroup label="Company Information">
        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2 sm:col-span-1">
            <Label>Company Name *</Label>
            <Input value={form.name} onChange={(e) => updateField('name', e.target.value)} placeholder="Muster GmbH" />
          </div>
          <div>
            <Label>Legal Form *</Label>
            <Select value={form.legalForm} onValueChange={(v) => updateField('legalForm', v)}>
              <SelectTrigger><SelectValue placeholder="Select legal form" /></SelectTrigger>
              <SelectContent>
                {LEGAL_FORMS.map((lf) => <SelectItem key={lf} value={lf}>{lf}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div>
          <Label>Managing Director</Label>
          <Select value={form.managingDirectorId} onValueChange={(v) => updateField('managingDirectorId', v)}>
            <SelectTrigger><SelectValue placeholder="None" /></SelectTrigger>
            <SelectContent>
              {users.map((u) => (
                <SelectItem key={u.id} value={u.id}>{u.firstName} {u.lastName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label="Address">
        <div>
          <Label>Street *</Label>
          <Input value={form.street} onChange={(e) => updateField('street', e.target.value)} placeholder="Musterstraße 1" />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <div>
            <Label>Postal Code *</Label>
            <Input value={form.postalCode} onChange={(e) => updateField('postalCode', e.target.value)} placeholder="10115" />
          </div>
          <div>
            <Label>City *</Label>
            <Input value={form.city} onChange={(e) => updateField('city', e.target.value)} placeholder="Berlin" />
          </div>
          <div>
            <Label>Country</Label>
            <Select value={form.country} onValueChange={(v) => updateField('country', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {COUNTRIES.map((c) => <SelectItem key={c.value} value={c.value}>{c.label} ({c.value})</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label="Tax & Registration">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label>Tax ID (Steuernummer)</Label>
            <Input value={form.taxId} onChange={(e) => updateField('taxId', e.target.value)} placeholder="27/123/45678" />
          </div>
          <div>
            <Label>VAT ID (USt-IdNr.)</Label>
            <Input value={form.vatId} onChange={(e) => updateField('vatId', e.target.value)} placeholder="DE123456789" />
          </div>
        </div>
        <div>
          <Label>Registration Number (Handelsregister)</Label>
          <Input value={form.registrationNumber} onChange={(e) => updateField('registrationNumber', e.target.value)} placeholder="HRB 12345 B" />
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label="Accounting">
        <div className="grid grid-cols-3 gap-3">
          <div>
            <Label>Chart of Accounts *</Label>
            <Select value={form.chartOfAccounts} onValueChange={(v) => updateField('chartOfAccounts', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="SKR03">SKR03</SelectItem>
                <SelectItem value="SKR04">SKR04</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label>Currency</Label>
            <Input value={form.currency} onChange={(e) => updateField('currency', e.target.value)} placeholder="EUR" />
          </div>
          <div>
            <Label>Fiscal Year Start</Label>
            <Select value={form.fiscalYearStartMonth} onValueChange={(v) => updateField('fiscalYearStartMonth', v)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {MONTHS.map((m) => <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
        </div>
      </FieldGroup>
      <Separator />
      <FieldGroup label="DATEV Integration">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label>Client Number (Mandantennr.)</Label>
            <Input value={form.datevClientNumber} onChange={(e) => updateField('datevClientNumber', e.target.value)} placeholder="12345" />
          </div>
          <div>
            <Label>Consultant Number (Beraternr.)</Label>
            <Input value={form.datevConsultantNumber} onChange={(e) => updateField('datevConsultantNumber', e.target.value)} placeholder="67890" />
          </div>
        </div>
      </FieldGroup>
      {availableParents.length > 0 && (
        <>
          <Separator />
          <FieldGroup label="Hierarchy">
            <div>
              <Label>Parent Entity</Label>
              <Select value={form.parentEntityId} onValueChange={(v) => updateField('parentEntityId', v)}>
                <SelectTrigger><SelectValue placeholder="None (top-level entity)" /></SelectTrigger>
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
  const parentName = entity.parentEntityId
    ? allEntities.find((e) => e.id === entity.parentEntityId)?.name ?? 'Unknown'
    : null;

  const monthName = MONTHS.find((m) => m.value === String(entity.fiscalYearStartMonth))?.label;

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
              Edit
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
                <><PowerOff className="h-3.5 w-3.5 mr-1" />Deactivate</>
              ) : (
                <><Power className="h-3.5 w-3.5 mr-1" />Reactivate</>
              )}
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Company Info */}
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3 lg:grid-cols-4">
          <DetailRow label="Legal Form" value={entity.legalForm} />
          <DetailRow label="Managing Director" value={entity.managingDirectorName} />
          <DetailRow label="Currency" value={entity.currency} />
          <DetailRow label="Chart of Accounts" value={entity.chartOfAccounts} />
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
              <DetailRow label="Tax ID" value={entity.taxId} />
              <DetailRow label="VAT ID" value={entity.vatId} />
              <DetailRow label="Registration" value={entity.registrationNumber} />
            </div>
          </>
        )}

        {/* Accounting */}
        <Separator />
        <div className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-4">
          <DetailRow label="Fiscal Year Start" value={monthName} />
          {entity.datevClientNumber && (
            <DetailRow label="DATEV Client Nr." value={entity.datevClientNumber} />
          )}
          {entity.datevConsultantNumber && (
            <DetailRow label="DATEV Consultant Nr." value={entity.datevConsultantNumber} />
          )}
          {parentName && (
            <DetailRow label="Parent Entity" value={parentName} />
          )}
        </div>
      </CardContent>
    </Card>
  );
}
