import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useEmployee,
  useSalaryHistory,
  useContractHistory,
  useTerminateEmployee,
  useUpdateEmployee,
  useUpdateSalary,
} from '@/hooks/useHr';
import { useAuth } from '@/hooks/useAuth';
import { useEntity } from '@/hooks/useEntity';
import type { SalaryEntry, ContractEntry, EmployeeDetail } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import { ArrowLeft, Loader2, Plus } from 'lucide-react';

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
// Salary history tab
// ---------------------------------------------------------------------------

interface SalaryTabProps {
  entries: SalaryEntry[];
  isLoading: boolean;
  employeeId: string;
}

function SalaryTab({ entries, isLoading, employeeId }: SalaryTabProps) {
  const { t, i18n } = useTranslation('hr');
  const { hasPermission } = useAuth();
  const updateSalary = useUpdateSalary();
  const [open, setOpen] = useState(false);
  const [salaryType, setSalaryType] = useState('Monthly');
  const [grossAmountEur, setGrossAmountEur] = useState('');
  const [validFrom, setValidFrom] = useState('');
  const [changeReason, setChangeReason] = useState('');

  const canManageSalary = hasPermission('hr.salary.manage');

  function resetForm() {
    setSalaryType('Monthly');
    setGrossAmountEur('');
    setValidFrom('');
    setChangeReason('');
  }

  function handleClose() {
    resetForm();
    setOpen(false);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const cents = Math.round(parseFloat(grossAmountEur) * 100);
    if (!cents || cents <= 0) return;
    updateSalary.mutate(
      {
        employeeId,
        salaryType,
        grossAmountCents: cents,
        currencyCode: 'EUR',
        bonusAmountCents: 0,
        bonusCurrencyCode: 'EUR',
        paymentCycleMonths: 12,
        validFrom,
        changeReason,
      },
      { onSuccess: handleClose },
    );
  }

  function formatCents(cents: number): string {
    return (cents / 100).toLocaleString(i18n.language, {
      style: 'currency',
      currency: 'EUR',
    });
  }

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function salaryTypeLabel(type: string): string {
    switch (type) {
      case 'Monthly':   return t('employees.salary.typeMonthly');
      case 'Hourly':    return t('employees.salary.typeHourly');
      case 'DailyRate': return t('employees.salary.typeDailyRate');
      default:          return type;
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-10 w-full rounded" />
        ))}
      </div>
    );
  }

  return (
    <>
      {canManageSalary && (
        <div className="mb-4 flex justify-end">
          <Button size="sm" onClick={() => setOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            {t('employees.salary.addButton')}
          </Button>
        </div>
      )}

      {entries.length === 0 ? (
        <p className="py-8 text-center text-sm text-muted-foreground">
          {t('employees.salary.noEntries')}
        </p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('employees.salary.type')}</TableHead>
              <TableHead className="text-right">{t('employees.salary.grossAmount')}</TableHead>
              <TableHead>{t('employees.salary.validFrom')}</TableHead>
              <TableHead>{t('employees.salary.changeReason')}</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {entries.map((entry) => (
              <TableRow key={entry.id}>
                <TableCell className="text-sm text-muted-foreground">
                  {salaryTypeLabel(entry.salaryType)}
                </TableCell>
                <TableCell className="text-right font-medium tabular-nums">
                  {formatCents(entry.grossAmountCents)}
                </TableCell>
                <TableCell className="text-sm">
                  {formatDate(entry.validFrom)}
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {entry.changeReason || '—'}
                </TableCell>
                <TableCell>
                  <CurrentBadge isCurrent={entry.isCurrent} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={open} onOpenChange={(v) => { if (!v) handleClose(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('employees.salary.dialogTitle')}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label>{t('employees.salary.type')}</Label>
              <Select value={salaryType} onValueChange={setSalaryType}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Monthly">{t('employees.salary.typeMonthly')}</SelectItem>
                  <SelectItem value="Hourly">{t('employees.salary.typeHourly')}</SelectItem>
                  <SelectItem value="DailyRate">{t('employees.salary.typeDailyRate')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="grossAmount">{t('employees.salary.grossAmountEur')} *</Label>
              <Input
                id="grossAmount"
                type="number"
                min="0.01"
                step="0.01"
                value={grossAmountEur}
                onChange={(e) => setGrossAmountEur(e.target.value)}
                placeholder={t('employees.salary.grossAmountPlaceholder')}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="salaryValidFrom">{t('employees.salary.validFrom')} *</Label>
              <Input
                id="salaryValidFrom"
                type="date"
                value={validFrom}
                onChange={(e) => setValidFrom(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="changeReason">{t('employees.salary.changeReason')} *</Label>
              <Input
                id="changeReason"
                value={changeReason}
                onChange={(e) => setChangeReason(e.target.value)}
                placeholder={t('employees.salary.changeReasonPlaceholder')}
                required
              />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={handleClose}>
                {t('common:buttons.cancel')}
              </Button>
              <Button type="submit" disabled={updateSalary.isPending}>
                {updateSalary.isPending && (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                )}
                {t('common:buttons.save')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}

// ---------------------------------------------------------------------------
// Contract history tab
// ---------------------------------------------------------------------------

function ContractTab({ entries }: { entries: ContractEntry[] }) {
  const { t, i18n } = useTranslation('hr');

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

  if (entries.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        {t('employees.contracts.noEntries')}
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{t('employees.contracts.contractType')}</TableHead>
          <TableHead className="text-right">{t('employees.contracts.weeklyHours')}</TableHead>
          <TableHead>{t('employees.contracts.startDate')}</TableHead>
          <TableHead>{t('employees.contracts.validFrom')}</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="font-medium">
              {contractTypeLabel(entry.contractType)}
            </TableCell>
            <TableCell className="text-right text-sm tabular-nums">
              {entry.weeklyHours} h
            </TableCell>
            <TableCell className="text-sm">
              {formatDate(entry.startDate)}
            </TableCell>
            <TableCell className="text-sm">
              {formatDate(entry.validFrom)}
            </TableCell>
            <TableCell>
              <CurrentBadge isCurrent={entry.isCurrent} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

// ---------------------------------------------------------------------------
// Bank details tab
// ---------------------------------------------------------------------------

function BankDetailsTab({ employee }: { employee: EmployeeDetail }) {
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
  const { entities } = useEntity();
  const [terminateOpen, setTerminateOpen] = useState(false);
  const [editEntityId, setEditEntityId] = useState<string>('');
  const updateEmployee = useUpdateEmployee();

  const { data: employee, isLoading } = useEmployee(id ?? '');
  const { data: salaryHistory = [], isLoading: salaryLoading } = useSalaryHistory(id ?? '');
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
          <TabsTrigger value="gehalt">{t('employees.tabs.salary')}</TabsTrigger>
          <TabsTrigger value="vertraege">{t('employees.tabs.contracts')}</TabsTrigger>
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
                <DetailRow
                  label={t('employees.fields.department')}
                  value={employee.departmentName ?? '—'}
                />
                <DetailRow
                  label={t('employees.columns.manager')}
                  value={employee.managerName ?? '—'}
                />
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
        {/* Salary tab                                                         */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="gehalt">
          <Card>
            <CardContent className="pt-6">
              <SalaryTab
                entries={salaryHistory}
                isLoading={salaryLoading}
                employeeId={id ?? ''}
              />
            </CardContent>
          </Card>
        </TabsContent>

        {/* ----------------------------------------------------------------- */}
        {/* Contracts tab                                                      */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="vertraege">
          <Card>
            <CardContent className="pt-6">
              <ContractTab entries={contractHistory} />
            </CardContent>
          </Card>
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
    </div>
  );
}
