import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useRef } from 'react';
import {
  useEmployee,
  useContractHistory,
  useTerminateEmployee,
  useUpdateEmployee,
  useCreateContract,
  useUpdateContract,
  useEmployeeDocuments,
  useUploadDocument,
  useAttachDocToContract,
  useDetachDocFromContract,
} from '@/hooks/useHr';
import { useAuth } from '@/hooks/useAuth';
import { useAuthStore } from '@/stores/authStore';
import type { ContractEntry, EmployeeDetail as EmployeeDetailType, CreateContractRequest, UpdateContractRequest } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
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
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { ArrowLeft, Loader2, Plus, Pencil, FileText, Link2, Unlink, ChevronDown, Download, Upload } from 'lucide-react';
import EmployeeEditDialog from './EmployeeEditDialog';

// ---------------------------------------------------------------------------
// Profile detail row helper
// ---------------------------------------------------------------------------

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value}</dd>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Termination dialog
// ---------------------------------------------------------------------------

interface TerminationDialogProps {
  open: boolean;
  onClose: () => void;
  employeeId: string;
}

function TerminationDialog({ open, onClose, employeeId }: TerminationDialogProps) {
  const { t } = useTranslation('hr');
  const [terminationDate, setTerminationDate] = useState('');
  const [reason, setReason] = useState('');
  const terminateEmployee = useTerminateEmployee();

  const handleClose = () => {
    setTerminationDate('');
    setReason('');
    onClose();
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!terminationDate || !reason) return;

    terminateEmployee.mutate(
      { id: employeeId, terminationDate, reason },
      { onSuccess: handleClose },
    );
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) handleClose(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('employees.profile.terminationDialogTitle')}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="terminationDate">{t('employees.profile.terminationDate')} *</Label>
            <Input
              id="terminationDate"
              type="date"
              value={terminationDate}
              onChange={(e) => setTerminationDate(e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="reason">{t('employees.profile.terminationReason')} *</Label>
            <Input
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder={t('employees.profile.terminationReasonPlaceholder')}
              required
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={terminateEmployee.isPending}
            >
              {terminateEmployee.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('employees.profile.confirmTermination')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Status badge
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: string }) {
  const { t } = useTranslation('hr');
  switch (status) {
    case 'Active':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          {t('employees.status.Active')}
        </Badge>
      );
    case 'OnLeave':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          {t('employees.status.OnLeave')}
        </Badge>
      );
    case 'Terminated':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          {t('employees.status.Terminated')}
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

// ---------------------------------------------------------------------------
// Current badge
// ---------------------------------------------------------------------------

function CurrentBadge({ isCurrent }: { isCurrent: boolean }) {
  const { t } = useTranslation('hr');
  if (!isCurrent) return null;
  return (
    <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
      {t('employees.salary.current')}
    </Badge>
  );
}

// ---------------------------------------------------------------------------
// Contract form dialog
// ---------------------------------------------------------------------------

interface ContractFormData {
  contractType: string;
  employmentType: string;
  weeklyHours: string;
  workdaysPerWeek: string;
  startDate: string;
  endDate: string;
  probationEndDate: string;
  employeeNoticeWeeks: string;
  employerNoticeWeeks: string;
  salaryType: string;
  grossAmountEur: string;
  currencyCode: string;
  bonusAmountEur: string;
  paymentCycleMonths: string;
  annualVacationDays: string;
  fixedTermReason: string;
  fixedTermExtensionCount: string;
  has13thSalary: boolean;
  hasVacationBonus: boolean;
  variablePayEur: string;
  variablePayDescription: string;
  notes: string;
  validFrom: string;
  changeReason: string;
}

const emptyForm: ContractFormData = {
  contractType: 'Permanent',
  employmentType: '',
  weeklyHours: '40',
  workdaysPerWeek: '5',
  startDate: '',
  endDate: '',
  probationEndDate: '',
  employeeNoticeWeeks: '4',
  employerNoticeWeeks: '4',
  salaryType: 'Monthly',
  grossAmountEur: '',
  currencyCode: 'EUR',
  bonusAmountEur: '0',
  paymentCycleMonths: '12',
  annualVacationDays: '20',
  fixedTermReason: '',
  fixedTermExtensionCount: '0',
  has13thSalary: false,
  hasVacationBonus: false,
  variablePayEur: '0',
  variablePayDescription: '',
  notes: '',
  validFrom: '',
  changeReason: '',
};

function contractToForm(c: ContractEntry): ContractFormData {
  return {
    contractType: c.contractType,
    employmentType: c.employmentType ?? '',
    weeklyHours: String(c.weeklyHours),
    workdaysPerWeek: String(c.workdaysPerWeek),
    startDate: c.startDate,
    endDate: c.endDate ?? '',
    probationEndDate: c.probationEndDate ?? '',
    employeeNoticeWeeks: String(c.employeeNoticeWeeks),
    employerNoticeWeeks: String(c.employerNoticeWeeks),
    salaryType: c.salaryType,
    grossAmountEur: (c.grossAmountCents / 100).toFixed(2),
    currencyCode: c.currencyCode,
    bonusAmountEur: (c.bonusAmountCents / 100).toFixed(2),
    paymentCycleMonths: String(c.paymentCycleMonths),
    annualVacationDays: String(c.annualVacationDays),
    fixedTermReason: c.fixedTermReason ?? '',
    fixedTermExtensionCount: String(c.fixedTermExtensionCount),
    has13thSalary: c.has13thSalary,
    hasVacationBonus: c.hasVacationBonus,
    variablePayEur: (c.variablePayCents / 100).toFixed(2),
    variablePayDescription: c.variablePayDescription ?? '',
    notes: c.notes ?? '',
    validFrom: '',
    changeReason: '',
  };
}

interface ContractFormDialogProps {
  open: boolean;
  onClose: () => void;
  employeeId: string;
  editContract?: ContractEntry;
}

function ContractFormDialog({ open, onClose, employeeId, editContract }: ContractFormDialogProps) {
  const { t } = useTranslation('hr');
  const isEdit = !!editContract;
  const [form, setForm] = useState<ContractFormData>(
    editContract ? contractToForm(editContract) : emptyForm,
  );
  const createContract = useCreateContract();
  const updateContract = useUpdateContract();
  const isPending = createContract.isPending || updateContract.isPending;

  const set = (field: keyof ContractFormData, value: string | boolean) =>
    setForm((prev) => ({ ...prev, [field]: value }));

  const handleClose = () => {
    setForm(emptyForm);
    onClose();
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const grossCents = Math.round(parseFloat(form.grossAmountEur) * 100);
    const bonusCents = Math.round(parseFloat(form.bonusAmountEur || '0') * 100);
    const variableCents = Math.round(parseFloat(form.variablePayEur || '0') * 100);

    if (isEdit && editContract) {
      const req: UpdateContractRequest & { employeeId: string; contractId: string } = {
        employeeId: employeeId,
        contractId: editContract.id,
        contractType: form.contractType,
        weeklyHours: parseFloat(form.weeklyHours),
        workdaysPerWeek: parseInt(form.workdaysPerWeek),
        startDate: form.startDate,
        endDate: form.endDate || undefined,
        probationEndDate: form.probationEndDate || undefined,
        employeeNoticeWeeks: parseInt(form.employeeNoticeWeeks),
        salaryType: form.salaryType,
        grossAmountCents: grossCents,
        currencyCode: form.currencyCode,
        bonusAmountCents: bonusCents,
        bonusCurrencyCode: form.currencyCode,
        paymentCycleMonths: parseInt(form.paymentCycleMonths),
        employmentType: form.employmentType || undefined,
        employerNoticeWeeks: parseInt(form.employerNoticeWeeks),
        annualVacationDays: parseInt(form.annualVacationDays),
        fixedTermReason: form.fixedTermReason || undefined,
        fixedTermExtensionCount: parseInt(form.fixedTermExtensionCount),
        has13thSalary: form.has13thSalary,
        hasVacationBonus: form.hasVacationBonus,
        variablePayCents: variableCents,
        variablePayDescription: form.variablePayDescription || undefined,
        notes: form.notes || undefined,
      };
      updateContract.mutate(req, { onSuccess: handleClose });
    } else {
      const req: CreateContractRequest & { employeeId: string } = {
        employeeId: employeeId,
        contractType: form.contractType,
        weeklyHours: parseFloat(form.weeklyHours),
        workdaysPerWeek: parseInt(form.workdaysPerWeek),
        startDate: form.startDate,
        endDate: form.endDate || undefined,
        probationEndDate: form.probationEndDate || undefined,
        employeeNoticeWeeks: parseInt(form.employeeNoticeWeeks),
        validFrom: form.validFrom,
        changeReason: form.changeReason,
        salaryType: form.salaryType,
        grossAmountCents: grossCents,
        currencyCode: form.currencyCode,
        bonusAmountCents: bonusCents,
        bonusCurrencyCode: form.currencyCode,
        paymentCycleMonths: parseInt(form.paymentCycleMonths),
        employmentType: form.employmentType || undefined,
        employerNoticeWeeks: parseInt(form.employerNoticeWeeks),
        annualVacationDays: parseInt(form.annualVacationDays),
        fixedTermReason: form.fixedTermReason || undefined,
        fixedTermExtensionCount: parseInt(form.fixedTermExtensionCount),
        has13thSalary: form.has13thSalary,
        hasVacationBonus: form.hasVacationBonus,
        variablePayCents: variableCents,
        variablePayDescription: form.variablePayDescription || undefined,
        notes: form.notes || undefined,
      };
      createContract.mutate(req, { onSuccess: handleClose });
    }
  };

  const isFixedTerm = form.contractType === 'FixedTerm';

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) handleClose(); }}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? t('employees.contract.editTitle') : t('employees.contract.createTitle')}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Section 1: Contract basics */}
          <div>
            <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionBasics')}</h4>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>{t('employees.contracts.contractType')} *</Label>
                <Select value={form.contractType} onValueChange={(v) => set('contractType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Permanent">{t('employees.contracts.typePermanent')}</SelectItem>
                    <SelectItem value="FixedTerm">{t('employees.contracts.typeFixedTerm')}</SelectItem>
                    <SelectItem value="Freelance">{t('employees.contracts.typeFreelance')}</SelectItem>
                    <SelectItem value="WorkingStudent">{t('employees.contracts.typeWorkingStudent')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('employees.fields.employmentType')}</Label>
                <Select value={form.employmentType || '_none'} onValueChange={(v) => set('employmentType', v === '_none' ? '' : v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">—</SelectItem>
                    <SelectItem value="FullTime">{t('employees.employmentType.FullTime')}</SelectItem>
                    <SelectItem value="PartTime">{t('employees.employmentType.PartTime')}</SelectItem>
                    <SelectItem value="MiniJob">{t('employees.employmentType.MiniJob')}</SelectItem>
                    <SelectItem value="Internship">{t('employees.employmentType.Internship')}</SelectItem>
                    <SelectItem value="WorkingStudent">{t('employees.employmentType.WorkingStudent')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contracts.startDate')} *</Label>
                <Input type="date" value={form.startDate} onChange={(e) => set('startDate', e.target.value)} required />
              </div>
              {isFixedTerm && (
                <div className="space-y-2">
                  <Label>{t('employees.contract.endDate')} *</Label>
                  <Input type="date" value={form.endDate} onChange={(e) => set('endDate', e.target.value)} required={isFixedTerm} />
                </div>
              )}
              <div className="space-y-2">
                <Label>{t('employees.contract.probationEndDate')}</Label>
                <Input type="date" value={form.probationEndDate} onChange={(e) => set('probationEndDate', e.target.value)} />
              </div>
            </div>
          </div>

          {/* Section 2: Working time */}
          <div>
            <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionWorkingTime')}</h4>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>{t('employees.contracts.weeklyHours')} *</Label>
                <Input type="number" min="0.5" max="60" step="0.5" value={form.weeklyHours} onChange={(e) => set('weeklyHours', e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.workdaysPerWeek')} *</Label>
                <Input type="number" min="1" max="7" value={form.workdaysPerWeek} onChange={(e) => set('workdaysPerWeek', e.target.value)} required />
              </div>
            </div>
          </div>

          {/* Section 3: Compensation */}
          <div>
            <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionCompensation')}</h4>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>{t('employees.salary.type')} *</Label>
                <Select value={form.salaryType} onValueChange={(v) => set('salaryType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Monthly">{t('employees.salary.typeMonthly')}</SelectItem>
                    <SelectItem value="Hourly">{t('employees.salary.typeHourly')}</SelectItem>
                    <SelectItem value="DailyRate">{t('employees.salary.typeDailyRate')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('employees.salary.grossAmountEur')} *</Label>
                <Input type="number" min="0.01" step="0.01" value={form.grossAmountEur} onChange={(e) => set('grossAmountEur', e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.bonusAmount')}</Label>
                <Input type="number" min="0" step="0.01" value={form.bonusAmountEur} onChange={(e) => set('bonusAmountEur', e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.paymentCycle')}</Label>
                <Input type="number" min="1" max="24" value={form.paymentCycleMonths} onChange={(e) => set('paymentCycleMonths', e.target.value)} />
              </div>
            </div>
          </div>

          {/* Section 4: Vacation & notice periods */}
          <div>
            <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionConditions')}</h4>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label>{t('employees.contract.annualVacationDays')}</Label>
                <Input type="number" min="0" max="365" value={form.annualVacationDays} onChange={(e) => set('annualVacationDays', e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.employeeNoticeWeeks')}</Label>
                <Input type="number" min="0" value={form.employeeNoticeWeeks} onChange={(e) => set('employeeNoticeWeeks', e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.employerNoticeWeeks')}</Label>
                <Input type="number" min="0" value={form.employerNoticeWeeks} onChange={(e) => set('employerNoticeWeeks', e.target.value)} />
              </div>
            </div>
          </div>

          {/* Section 5: Fixed-term (conditional) */}
          {isFixedTerm && (
            <div>
              <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionFixedTerm')}</h4>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>{t('employees.contract.fixedTermReason')} *</Label>
                  <Input value={form.fixedTermReason} onChange={(e) => set('fixedTermReason', e.target.value)} required={isFixedTerm} />
                </div>
                <div className="space-y-2">
                  <Label>{t('employees.contract.extensionCount')}</Label>
                  <Input type="number" min="0" max="4" value={form.fixedTermExtensionCount} onChange={(e) => set('fixedTermExtensionCount', e.target.value)} />
                </div>
              </div>
            </div>
          )}

          {/* Section 6: Special payments */}
          <div>
            <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionSpecialPayments')}</h4>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex items-center gap-2">
                <Checkbox id="has13th" checked={form.has13thSalary} onCheckedChange={(v) => set('has13thSalary', !!v)} />
                <Label htmlFor="has13th">{t('employees.contract.has13thSalary')}</Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="hasVacBonus" checked={form.hasVacationBonus} onCheckedChange={(v) => set('hasVacationBonus', !!v)} />
                <Label htmlFor="hasVacBonus">{t('employees.contract.hasVacationBonus')}</Label>
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.variablePay')}</Label>
                <Input type="number" min="0" step="0.01" value={form.variablePayEur} onChange={(e) => set('variablePayEur', e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>{t('employees.contract.variablePayDescription')}</Label>
                <Input value={form.variablePayDescription} onChange={(e) => set('variablePayDescription', e.target.value)} />
              </div>
            </div>
          </div>

          {/* Section 7: Notes */}
          <div className="space-y-2">
            <Label>{t('employees.contract.notes')}</Label>
            <Textarea value={form.notes} onChange={(e) => set('notes', e.target.value)} rows={3} />
          </div>

          {/* Section 8: Meta (only for create) */}
          {!isEdit && (
            <div>
              <h4 className="mb-3 text-sm font-medium text-muted-foreground">{t('employees.contract.sectionMeta')}</h4>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>{t('employees.salary.validFrom')} *</Label>
                  <Input type="date" value={form.validFrom} onChange={(e) => set('validFrom', e.target.value)} required />
                </div>
                <div className="space-y-2">
                  <Label>{t('employees.salary.changeReason')} *</Label>
                  <Input value={form.changeReason} onChange={(e) => set('changeReason', e.target.value)} required />
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              {t('common:buttons.cancel')}
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('common:buttons.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Attach document dialog
// ---------------------------------------------------------------------------

interface AttachDocDialogProps {
  open: boolean;
  onClose: () => void;
  employeeId: string;
  contractId: string;
  existingDocIds: string[];
}

function AttachDocDialog({ open, onClose, employeeId, contractId, existingDocIds }: AttachDocDialogProps) {
  const { t } = useTranslation('hr');
  const { data: documents = [] } = useEmployeeDocuments(employeeId);
  const attachDoc = useAttachDocToContract();
  const uploadDoc = useUploadDocument(employeeId);
  const unlinked = documents.filter((d) => !existingDocIds.includes(d.id));

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadTitle, setUploadTitle] = useState('');
  const [uploadType, setUploadType] = useState('Contract');

  const resetUpload = () => {
    setUploadFile(null);
    setUploadTitle('');
    setUploadType('Contract');
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleUploadAndAttach = () => {
    if (!uploadFile) return;
    const formData = new FormData();
    formData.append('file', uploadFile);
    formData.append('documentType', uploadType);
    formData.append('title', uploadTitle || uploadFile.name);
    uploadDoc.mutate(formData, {
      onSuccess: (data: { id: string }) => {
        attachDoc.mutate({ employeeId, contractId, docId: data.id }, {
          onSuccess: () => { resetUpload(); onClose(); },
        });
      },
    });
  };

  const busy = uploadDoc.isPending || attachDoc.isPending;

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) { resetUpload(); onClose(); } }}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('employees.contract.attachDocument')}</DialogTitle>
        </DialogHeader>

        {/* Upload new document */}
        <div className="space-y-3 border-b pb-4">
          <h4 className="text-sm font-medium">{t('employees.contract.uploadNew')}</h4>
          <input
            ref={fileInputRef}
            type="file"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0] ?? null;
              setUploadFile(file);
              if (file && !uploadTitle) setUploadTitle(file.name);
            }}
          />
          <div className="flex items-center gap-2">
            <Button type="button" size="sm" variant="outline" onClick={() => fileInputRef.current?.click()}>
              <Upload className="mr-1 h-3 w-3" />
              {t('documents.upload.selectFile')}
            </Button>
            {uploadFile && (
              <span className="truncate max-w-[200px] text-sm text-muted-foreground">{uploadFile.name}</span>
            )}
          </div>
          {uploadFile && (
            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label>{t('documents.upload.title')}</Label>
                <Input
                  value={uploadTitle}
                  onChange={(e) => setUploadTitle(e.target.value)}
                  placeholder={t('documents.upload.titlePlaceholder')}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>{t('documents.upload.documentType')}</Label>
                <Select value={uploadType} onValueChange={setUploadType}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Contract">{t('documents.documentType.Contract')}</SelectItem>
                    <SelectItem value="Certificate">{t('documents.documentType.Certificate')}</SelectItem>
                    <SelectItem value="IdCopy">{t('documents.documentType.IdCopy')}</SelectItem>
                    <SelectItem value="Payslip">{t('documents.documentType.Payslip')}</SelectItem>
                    <SelectItem value="Other">{t('documents.documentType.Other')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          )}
          {uploadFile && (
            <Button size="sm" disabled={busy} onClick={handleUploadAndAttach}>
              {busy && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
              {t('employees.contract.uploadAndAttach')}
            </Button>
          )}
        </div>

        {/* Link existing unlinked documents */}
        {unlinked.length > 0 && (
          <div className="space-y-2">
            <h4 className="text-sm font-medium">{t('employees.contract.linkExisting')}</h4>
            {unlinked.map((doc) => (
              <div key={doc.id} className="flex items-center justify-between rounded border p-2">
                <div className="flex items-center gap-2">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{doc.title || doc.fileName}</span>
                  <Badge variant="secondary" className="text-xs">{doc.documentType}</Badge>
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={attachDoc.isPending}
                  onClick={() => attachDoc.mutate({ employeeId, contractId, docId: doc.id }, { onSuccess: onClose })}
                >
                  <Link2 className="mr-1 h-3 w-3" />
                  {t('employees.contract.link')}
                </Button>
              </div>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Contract tab (new full version)
// ---------------------------------------------------------------------------

interface ContractTabProps {
  entries: ContractEntry[];
  employeeId: string;
}

function ContractTab({ entries, employeeId }: ContractTabProps) {
  const { t, i18n } = useTranslation('hr');
  const { hasPermission } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [attachOpen, setAttachOpen] = useState(false);
  const detachDoc = useDetachDocFromContract();
  const canManage = hasPermission('hr.manage');

  const current = entries.find((c) => c.isCurrent);
  const history = entries.filter((c) => !c.isCurrent);

  function formatCents(cents: number, currency = 'EUR'): string {
    return (cents / 100).toLocaleString(i18n.language, { style: 'currency', currency });
  }

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function contractTypeLabel(type: string): string {
    switch (type) {
      case 'Permanent':      return t('employees.contracts.typePermanent');
      case 'FixedTerm':      return t('employees.contracts.typeFixedTerm');
      case 'Freelance':      return t('employees.contracts.typeFreelance');
      case 'WorkingStudent': return t('employees.contracts.typeWorkingStudent');
      default:               return type;
    }
  }

  function salaryTypeLabel(type: string): string {
    switch (type) {
      case 'Monthly':   return t('employees.salary.typeMonthly');
      case 'Hourly':    return t('employees.salary.typeHourly');
      case 'DailyRate': return t('employees.salary.typeDailyRate');
      default:          return type;
    }
  }

  function employmentTypeLabel(type: string | undefined): string {
    if (!type) return '—';
    return t(`employees.employmentType.${type}`, type);
  }

  return (
    <>
      {/* Actions */}
      {canManage && (
        <div className="mb-4 flex justify-end gap-2">
          {current && (
            <Button size="sm" variant="outline" onClick={() => setEditOpen(true)}>
              <Pencil className="mr-1 h-4 w-4" />
              {t('employees.contract.editButton')}
            </Button>
          )}
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            {t('employees.contract.newButton')}
          </Button>
        </div>
      )}

      {/* Active contract */}
      {current ? (
        <Card className="mb-6">
          <CardHeader className="pb-3">
            <div className="flex items-center gap-2">
              <Badge>{contractTypeLabel(current.contractType)}</Badge>
              {current.employmentType && (
                <Badge variant="outline">{employmentTypeLabel(current.employmentType)}</Badge>
              )}
              <CurrentBadge isCurrent />
              <span className="ml-auto text-sm text-muted-foreground">
                {formatDate(current.startDate)} – {current.endDate ? formatDate(current.endDate) : t('employees.contract.openEnded')}
              </span>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-x-8 gap-y-4 md:grid-cols-3">
              {/* Working time */}
              <DetailRow label={t('employees.contracts.weeklyHours')} value={`${current.weeklyHours} h`} />
              <DetailRow label={t('employees.contract.workdaysPerWeek')} value={current.workdaysPerWeek} />

              {/* Compensation */}
              <DetailRow label={t('employees.salary.grossAmount')} value={`${formatCents(current.grossAmountCents, current.currencyCode)} (${salaryTypeLabel(current.salaryType)})`} />
              {current.bonusAmountCents > 0 && (
                <DetailRow label={t('employees.contract.bonusAmount')} value={formatCents(current.bonusAmountCents, current.bonusCurrencyCode)} />
              )}
              {current.has13thSalary && <DetailRow label={t('employees.contract.has13thSalary')} value={t('employees.contract.yes')} />}
              {current.hasVacationBonus && <DetailRow label={t('employees.contract.hasVacationBonus')} value={t('employees.contract.yes')} />}
              {current.variablePayCents > 0 && (
                <DetailRow label={t('employees.contract.variablePay')} value={`${formatCents(current.variablePayCents)} ${current.variablePayDescription ? `(${current.variablePayDescription})` : ''}`} />
              )}
              <DetailRow label={t('employees.contract.paymentCycle')} value={`${current.paymentCycleMonths}x`} />

              {/* Conditions */}
              <DetailRow label={t('employees.contract.annualVacationDays')} value={`${current.annualVacationDays} ${t('employees.contract.days')}`} />
              <DetailRow label={t('employees.contract.employeeNoticeWeeks')} value={`${current.employeeNoticeWeeks} ${t('employees.contract.weeks')}`} />
              <DetailRow label={t('employees.contract.employerNoticeWeeks')} value={`${current.employerNoticeWeeks} ${t('employees.contract.weeks')}`} />
              {current.probationEndDate && (
                <DetailRow label={t('employees.contract.probationEndDate')} value={formatDate(current.probationEndDate)} />
              )}

              {/* Fixed-term */}
              {current.contractType === 'FixedTerm' && current.fixedTermReason && (
                <>
                  <DetailRow label={t('employees.contract.fixedTermReason')} value={current.fixedTermReason} />
                  <DetailRow label={t('employees.contract.extensionCount')} value={current.fixedTermExtensionCount} />
                </>
              )}
            </div>

            {/* Notes (collapsible) */}
            {current.notes && (
              <Collapsible className="mt-4">
                <CollapsibleTrigger className="flex items-center gap-1 text-sm font-medium text-muted-foreground hover:text-foreground">
                  <ChevronDown className="h-4 w-4" />
                  {t('employees.contract.notes')}
                </CollapsibleTrigger>
                <CollapsibleContent className="mt-2 rounded border p-3 text-sm whitespace-pre-wrap">
                  {current.notes}
                </CollapsibleContent>
              </Collapsible>
            )}

            {/* Documents */}
            <div className="mt-4 border-t pt-4">
              <div className="mb-2 flex items-center justify-between">
                <h4 className="text-sm font-medium">{t('employees.contract.linkedDocuments')}</h4>
                {canManage && (
                  <Button size="sm" variant="outline" onClick={() => setAttachOpen(true)}>
                    <Link2 className="mr-1 h-3 w-3" />
                    {t('employees.contract.attachDocument')}
                  </Button>
                )}
              </div>
              {current.documents.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('employees.contract.noDocuments')}</p>
              ) : (
                <div className="space-y-1">
                  {current.documents.map((doc) => (
                    <div key={doc.id} className="flex items-center justify-between rounded border p-2">
                      <div className="flex items-center gap-2">
                        <FileText className="h-4 w-4 text-muted-foreground" />
                        <span className="text-sm">{doc.title || doc.fileName}</span>
                        <Badge variant="secondary" className="text-xs">{doc.documentType}</Badge>
                      </div>
                      <div className="flex items-center gap-1">
                        <Button size="sm" variant="ghost" asChild>
                          <a href={`/api/hr/employees/${employeeId}/documents/${doc.id}/download`} target="_blank" rel="noreferrer">
                            <Download className="h-3 w-3" />
                          </a>
                        </Button>
                        {canManage && (
                          <Button
                            size="sm"
                            variant="ghost"
                            disabled={detachDoc.isPending}
                            onClick={() => detachDoc.mutate({ employeeId, contractId: current.id, docId: doc.id })}
                          >
                            <Unlink className="h-3 w-3" />
                          </Button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      ) : (
        <p className="py-8 text-center text-sm text-muted-foreground">
          {t('employees.contracts.noEntries')}
        </p>
      )}

      {/* Contract history */}
      {history.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">{t('employees.contract.history')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('employees.contracts.contractType')}</TableHead>
                  <TableHead className="text-right">{t('employees.salary.grossAmount')}</TableHead>
                  <TableHead>{t('employees.contracts.startDate')}</TableHead>
                  <TableHead>{t('employees.salary.changeReason')}</TableHead>
                  <TableHead>{t('employees.contract.status')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell className="font-medium">{contractTypeLabel(entry.contractType)}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatCents(entry.grossAmountCents, entry.currencyCode)}</TableCell>
                    <TableCell className="text-sm">{formatDate(entry.startDate)}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{entry.changeReason || '—'}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{t('employees.contract.closed')}</Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Dialogs */}
      {createOpen && (
        <ContractFormDialog
          open={createOpen}
          onClose={() => setCreateOpen(false)}
          employeeId={employeeId}
        />
      )}
      {editOpen && current && (
        <ContractFormDialog
          open={editOpen}
          onClose={() => setEditOpen(false)}
          employeeId={employeeId}
          editContract={current}
        />
      )}
      {attachOpen && current && (
        <AttachDocDialog
          open={attachOpen}
          onClose={() => setAttachOpen(false)}
          employeeId={employeeId}
          contractId={current.id}
          existingDocIds={current.documents.map((d) => d.id)}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// Bank details tab
// ---------------------------------------------------------------------------

function BankDetailsTab({ employee }: { employee: EmployeeDetailType }) {
  const { t } = useTranslation('hr');
  const updateEmployee = useUpdateEmployee();
  const [iban, setIban] = useState(employee.iban ?? '');
  const [bic, setBic] = useState(employee.bic ?? '');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateEmployee.mutate({
      id:          employee.id,
      firstName:   employee.firstName,
      lastName:    employee.lastName,
      dateOfBirth: employee.dateOfBirth,
      taxId:       employee.taxId,
      managerId:   employee.managerId,
      departmentId: employee.departmentId,
      iban:        iban.trim() || undefined,
      bic:         bic.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid grid-cols-2 gap-6">
        <div className="space-y-2">
          <Label htmlFor="iban">{t('employees.bankDetails.iban')}</Label>
          <Input
            id="iban"
            value={iban}
            onChange={(e) => setIban(e.target.value)}
            placeholder={t('employees.bankDetails.ibanPlaceholder')}
            maxLength={34}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="bic">{t('employees.bankDetails.bic')}</Label>
          <Input
            id="bic"
            value={bic}
            onChange={(e) => setBic(e.target.value)}
            placeholder={t('employees.bankDetails.bicPlaceholder')}
            maxLength={11}
          />
        </div>
      </div>
      <div className="flex justify-end border-t pt-4">
        <Button type="submit" disabled={updateEmployee.isPending}>
          {updateEmployee.isPending && (
            <Loader2 className="mr-1 h-4 w-4 animate-spin" />
          )}
          {t('common:buttons.save')}
        </Button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const authEntities = useAuthStore((s) => s.user?.entities ?? []);
  const entities = authEntities.map((e) => ({ id: e.entityId, name: e.entityName }));
  const [terminateOpen, setTerminateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [editEntityId, setEditEntityId] = useState<string>('');
  const updateEmployee = useUpdateEmployee();

  const { data: employee, isLoading } = useEmployee(id ?? '');
  const { data: contractHistory = [] } = useContractHistory(id ?? '');

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function employeeTypeLabel(type: string): string {
    return type === 'Contractor'
      ? t('employees.type.Contractor')
      : t('employees.type.Employee');
  }

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!employee) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('employees.notFound')}
      </div>
    );
  }

  const fullName = `${employee.firstName} ${employee.lastName}`;

  return (
    <div>
      <PageHeader
        title={fullName}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={employee.status} />
            {hasPermission('hr.manage') && employee.status !== 'Terminated' && (
              <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
                <Pencil className="mr-1 h-4 w-4" />
                {t('common:buttons.edit')}
              </Button>
            )}
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/hr/employees')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              {t('common:buttons.back')}
            </Button>
          </div>
        }
      />

      <Tabs defaultValue="profil">
        <TabsList className="mb-6">
          <TabsTrigger value="profil">{t('employees.tabs.profile')}</TabsTrigger>
          <TabsTrigger value="vertrag">{t('employees.tabs.contract')}</TabsTrigger>
          <TabsTrigger value="bank">{t('employees.tabs.bank')}</TabsTrigger>
        </TabsList>

        {/* ----------------------------------------------------------------- */}
        {/* Profile tab                                                        */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="profil">
          <Card>
            <CardContent className="pt-6">
              <dl className="grid grid-cols-2 gap-6">
                <DetailRow
                  label={t('employees.fields.employeeNumber')}
                  value={employee.employeeNumber}
                />
                <DetailRow
                  label={t('employees.fields.employeeType')}
                  value={employeeTypeLabel(employee.employeeType)}
                />
                <DetailRow
                  label={t('employees.fields.status')}
                  value={<StatusBadge status={employee.status} />}
                />
                <DetailRow
                  label={t('employees.fields.hireDate')}
                  value={formatDate(employee.hireDate)}
                />
                <DetailRow
                  label={t('employees.fields.dateOfBirth')}
                  value={formatDate(employee.dateOfBirth)}
                />
                <DetailRow
                  label={t('employees.fields.taxId')}
                  value={employee.taxId || '—'}
                />
                {employee.gender && employee.gender !== 'NotSpecified' && (
                  <DetailRow
                    label={t('employees.fields.gender')}
                    value={t(`employees.gender.${employee.gender}`)}
                  />
                )}
                {employee.nationality && (
                  <DetailRow
                    label={t('employees.fields.nationality')}
                    value={employee.nationality}
                  />
                )}
                {employee.position && (
                  <DetailRow
                    label={t('employees.fields.position')}
                    value={employee.position}
                  />
                )}
                {employee.employmentType && (
                  <DetailRow
                    label={t('employees.fields.employmentType')}
                    value={t(`employees.employmentType.${employee.employmentType}`)}
                  />
                )}
                <DetailRow
                  label={t('employees.fields.department')}
                  value={employee.departmentName ?? '—'}
                />
                <DetailRow
                  label={t('employees.columns.manager')}
                  value={employee.managerName ?? '—'}
                />
                {employee.workEmail && (
                  <DetailRow
                    label={t('employees.fields.workEmail')}
                    value={employee.workEmail}
                  />
                )}
                {employee.personalEmail && (
                  <DetailRow
                    label={t('employees.fields.personalEmail')}
                    value={employee.personalEmail}
                  />
                )}
                {employee.personalPhone && (
                  <DetailRow
                    label={t('employees.fields.personalPhone')}
                    value={employee.personalPhone}
                  />
                )}
                {employee.socialSecurityNumber && (
                  <DetailRow
                    label={t('employees.fields.socialSecurityNumber')}
                    value={employee.socialSecurityNumber}
                  />
                )}
                <DetailRow
                  label={t('employees.fields.entity')}
                  value={entities.find((e) => e.id === employee.entityId)?.name ?? employee.entityId}
                />
              </dl>

              {hasPermission('hr.manage') && employee.status !== 'Terminated' && entities.length > 1 && (
                <div className="mt-6 border-t pt-4">
                  <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    {t('employees.fields.entity')}
                  </p>
                  <div className="flex items-center gap-3">
                    <Select
                      value={editEntityId || employee.entityId}
                      onValueChange={setEditEntityId}
                    >
                      <SelectTrigger className="w-64">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {entities.map((e) => (
                          <SelectItem key={e.id} value={e.id}>
                            {e.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={updateEmployee.isPending || (editEntityId === '' || editEntityId === employee.entityId)}
                      onClick={() => {
                        if (!editEntityId || editEntityId === employee.entityId) return;
                        updateEmployee.mutate({
                          id: employee.id,
                          firstName: employee.firstName,
                          lastName: employee.lastName,
                          dateOfBirth: employee.dateOfBirth,
                          taxId: employee.taxId,
                          managerId: employee.managerId,
                          departmentId: employee.departmentId,
                          iban: employee.iban,
                          bic: employee.bic,
                          entityId: editEntityId,
                        }, { onSuccess: () => setEditEntityId('') });
                      }}
                    >
                      {updateEmployee.isPending && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                      {t('common:buttons.save')}
                    </Button>
                  </div>
                </div>
              )}

              {employee.status !== 'Terminated' && (
                <div className="mt-6 flex justify-end border-t pt-4">
                  <Button
                    variant="destructive"
                    onClick={() => setTerminateOpen(true)}
                  >
                    {t('employees.terminateButton')}
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* ----------------------------------------------------------------- */}
        {/* Contract tab                                                       */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="vertrag">
          <ContractTab entries={contractHistory} employeeId={id ?? ''} />
        </TabsContent>

        {/* ----------------------------------------------------------------- */}
        {/* Bank details tab                                                   */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="bank">
          <Card>
            <CardContent className="pt-6">
              <BankDetailsTab employee={employee} />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <TerminationDialog
        open={terminateOpen}
        onClose={() => setTerminateOpen(false)}
        employeeId={employee.id}
      />

      {editOpen && (
        <EmployeeEditDialog
          open={editOpen}
          onClose={() => setEditOpen(false)}
          employee={employee}
        />
      )}
    </div>
  );
}
