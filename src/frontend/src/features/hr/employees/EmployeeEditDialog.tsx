import { useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useUpdateEmployee, useDepartments, useEmployees } from '@/hooks/useHr';
import type { EmployeeDetail } from '@/types/hr';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Loader2 } from 'lucide-react';

interface EmployeeEditDialogProps {
  open: boolean;
  onClose: () => void;
  employee: EmployeeDetail;
}

const createSchema = (t: (key: string) => string) =>
  z.object({
    firstName: z.string().min(1, t('employees.validation.firstNameRequired')).max(100),
    lastName: z.string().min(1, t('employees.validation.lastNameRequired')).max(100),
    dateOfBirth: z.string().min(1, t('employees.validation.dateOfBirthRequired')),
    gender: z.string(),
    nationality: z.string().max(100),
    personalEmail: z.union([z.string().email().max(254), z.literal('')]),
    personalPhone: z.string().max(50),
    workEmail: z.union([z.string().email().max(254), z.literal('')]),
    employmentType: z.string(),
    position: z.string().max(200),
    hireDate: z.string(),
    departmentId: z.string(),
    managerId: z.string(),
    taxId: z.string().min(1, t('employees.validation.taxIdRequired')).max(50),
    socialSecurityNumber: z.string().max(200),
    iban: z.string().max(34),
    bic: z.string().max(11),
  });

type FormValues = z.infer<ReturnType<typeof createSchema>>;

export default function EmployeeEditDialog({ open, onClose, employee }: EmployeeEditDialogProps) {
  const { t } = useTranslation('hr');
  const updateEmployee = useUpdateEmployee();
  const { data: departments } = useDepartments(employee.entityId);
  const { data: employeesData } = useEmployees({ entityId: employee.entityId, pageSize: 100 });
  const employees = employeesData?.items ?? [];

  const schema = useMemo(() => createSchema(t), [t]);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: employee.firstName,
      lastName: employee.lastName,
      dateOfBirth: employee.dateOfBirth,
      gender: employee.gender ?? 'NotSpecified',
      nationality: employee.nationality ?? '',
      personalEmail: employee.personalEmail ?? '',
      personalPhone: employee.personalPhone ?? '',
      workEmail: employee.workEmail ?? '',
      employmentType: employee.employmentType ?? '',
      position: employee.position ?? '',
      hireDate: employee.hireDate,
      departmentId: employee.departmentId ?? '',
      managerId: employee.managerId ?? '',
      taxId: employee.taxId,
      socialSecurityNumber: employee.socialSecurityNumber ?? '',
      iban: employee.iban ?? '',
      bic: employee.bic ?? '',
    },
  });

  const gender = watch('gender');
  const employmentType = watch('employmentType');
  const departmentId = watch('departmentId');
  const managerId = watch('managerId');

  const onSubmit = (values: FormValues) => {
    updateEmployee.mutate(
      {
        id: employee.id,
        firstName: values.firstName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth,
        taxId: values.taxId,
        managerId: values.managerId || undefined,
        departmentId: values.departmentId || undefined,
        iban: values.iban || undefined,
        bic: values.bic || undefined,
        socialSecurityNumber: values.socialSecurityNumber || undefined,
        gender: values.gender || 'NotSpecified',
        nationality: values.nationality || undefined,
        position: values.position || undefined,
        employmentType: values.employmentType || undefined,
        workEmail: values.workEmail || undefined,
        personalEmail: values.personalEmail || undefined,
        personalPhone: values.personalPhone || undefined,
      },
      { onSuccess: onClose },
    );
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) onClose(); }}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('employees.editTitle')}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          {/* Personal Data */}
          <fieldset className="space-y-3">
            <legend className="text-sm font-semibold text-muted-foreground">{t('employees.sections.personal')}</legend>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.firstName')} *</Label>
                <Input {...register('firstName')} />
                {errors.firstName && <p className="text-destructive text-xs">{errors.firstName.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.lastName')} *</Label>
                <Input {...register('lastName')} />
                {errors.lastName && <p className="text-destructive text-xs">{errors.lastName.message}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.gender')}</Label>
                <Select value={gender} onValueChange={(v) => setValue('gender', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="NotSpecified">{t('employees.gender.NotSpecified')}</SelectItem>
                    <SelectItem value="Male">{t('employees.gender.Male')}</SelectItem>
                    <SelectItem value="Female">{t('employees.gender.Female')}</SelectItem>
                    <SelectItem value="Diverse">{t('employees.gender.Diverse')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.dateOfBirth')} *</Label>
                <Input type="date" {...register('dateOfBirth')} />
                {errors.dateOfBirth && <p className="text-destructive text-xs">{errors.dateOfBirth.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>{t('employees.fields.nationality')}</Label>
              <Input {...register('nationality')} maxLength={100} />
            </div>
          </fieldset>

          {/* Contact */}
          <fieldset className="space-y-3">
            <legend className="text-sm font-semibold text-muted-foreground">{t('employees.sections.contact')}</legend>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.personalEmail')}</Label>
                <Input type="email" {...register('personalEmail')} maxLength={254} />
                {errors.personalEmail && <p className="text-destructive text-xs">{errors.personalEmail.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.personalPhone')}</Label>
                <Input {...register('personalPhone')} maxLength={50} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>{t('employees.fields.workEmail')}</Label>
              <Input type="email" {...register('workEmail')} maxLength={254} />
              {errors.workEmail && <p className="text-destructive text-xs">{errors.workEmail.message}</p>}
            </div>
          </fieldset>

          {/* Employment */}
          <fieldset className="space-y-3">
            <legend className="text-sm font-semibold text-muted-foreground">{t('employees.sections.employment')}</legend>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.employeeNumber')}</Label>
                <Input value={employee.employeeNumber} disabled />
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.employeeType')}</Label>
                <Input value={employee.employeeType === 'Contractor' ? t('employees.type.Contractor') : t('employees.type.Employee')} disabled />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.employmentType')}</Label>
                <Select value={employmentType || 'none'} onValueChange={(v) => setValue('employmentType', v === 'none' ? '' : v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">—</SelectItem>
                    <SelectItem value="FullTime">{t('employees.employmentType.FullTime')}</SelectItem>
                    <SelectItem value="PartTime">{t('employees.employmentType.PartTime')}</SelectItem>
                    <SelectItem value="WorkingStudent">{t('employees.employmentType.WorkingStudent')}</SelectItem>
                    <SelectItem value="MiniJob">{t('employees.employmentType.MiniJob')}</SelectItem>
                    <SelectItem value="Internship">{t('employees.employmentType.Internship')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.position')}</Label>
                <Input {...register('position')} maxLength={200} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.department')}</Label>
                <Select value={departmentId || 'none'} onValueChange={(v) => setValue('departmentId', v === 'none' ? '' : v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t('employees.placeholders.noDepartment')}</SelectItem>
                    {(departments ?? []).map((d) => (
                      <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.columns.manager')}</Label>
                <Select value={managerId || 'none'} onValueChange={(v) => setValue('managerId', v === 'none' ? '' : v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">—</SelectItem>
                    {employees.filter((e) => e.id !== employee.id).map((e) => (
                      <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </fieldset>

          {/* Finance */}
          <fieldset className="space-y-3">
            <legend className="text-sm font-semibold text-muted-foreground">{t('employees.sections.finance')}</legend>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.fields.taxId')} *</Label>
                <Input {...register('taxId')} maxLength={50} />
                {errors.taxId && <p className="text-destructive text-xs">{errors.taxId.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.fields.socialSecurityNumber')}</Label>
                <Input {...register('socialSecurityNumber')} maxLength={200} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>{t('employees.bankDetails.iban')}</Label>
                <Input {...register('iban')} maxLength={34} />
              </div>
              <div className="space-y-1.5">
                <Label>{t('employees.bankDetails.bic')}</Label>
                <Input {...register('bic')} maxLength={11} />
              </div>
            </div>
          </fieldset>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              {t('common:buttons.cancel')}
            </Button>
            <Button type="submit" disabled={updateEmployee.isPending}>
              {updateEmployee.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('common:buttons.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
