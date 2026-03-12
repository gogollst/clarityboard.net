import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useCreateEmployee, useDepartments } from '@/hooks/useHr';
import { useEntity, useEntities } from '@/hooks/useEntity';
import PageHeader from '@/components/shared/PageHeader';
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
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from '@/components/ui/card';
import { ArrowLeft, Loader2 } from 'lucide-react';

type FormValues = {
  entityId: string;
  firstName: string;
  lastName: string;
  employeeNumber: string;
  employeeType: string;
  hireDate: string;
  dateOfBirth: string;
  taxId: string;
  departmentId?: string;
  gender?: string;
  nationality?: string;
  position?: string;
  employmentType?: string;
  workEmail?: string;
  personalEmail?: string;
  personalPhone?: string;
};

export function Component() {
  const { t } = useTranslation('hr');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const { data: entities = [] } = useEntities();
  const createEmployee = useCreateEmployee();
  const { data: departments } = useDepartments(selectedEntityId ?? undefined);

  const schema = useMemo(
    () =>
      z.object({
        entityId: z.string().min(1, t('employees.validation.entityRequired')),
        firstName: z.string().min(1, t('employees.validation.firstNameRequired')),
        lastName: z.string().min(1, t('employees.validation.lastNameRequired')),
        employeeNumber: z
          .string()
          .min(1, t('employees.validation.employeeNumberRequired'))
          .max(20, t('employees.validation.employeeNumberMaxLength')),
        employeeType: z.string().min(1, t('employees.validation.employeeTypeRequired')),
        hireDate: z.string().min(1, t('employees.validation.hireDateRequired')),
        dateOfBirth: z.string().min(1, t('employees.validation.dateOfBirthRequired')),
        taxId: z
          .string()
          .min(1, t('employees.validation.taxIdRequired'))
          .max(50, t('employees.validation.taxIdMaxLength')),
        departmentId: z.string().optional(),
        gender: z.string().optional(),
        nationality: z.string().max(100).optional().or(z.literal('')),
        position: z.string().max(200).optional().or(z.literal('')),
        employmentType: z.string().optional(),
        workEmail: z.string().email().max(254).optional().or(z.literal('')),
        personalEmail: z.string().email().max(254).optional().or(z.literal('')),
        personalPhone: z.string().max(50).optional().or(z.literal('')),
      }),
    [t],
  );

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      entityId: selectedEntityId ?? '',
      firstName: '',
      lastName: '',
      employeeNumber: '',
      employeeType: '',
      hireDate: '',
      dateOfBirth: '',
      taxId: '',
      departmentId: '',
      gender: '',
      nationality: '',
      position: '',
      employmentType: '',
      workEmail: '',
      personalEmail: '',
      personalPhone: '',
    },
  });

  const entityId = watch('entityId');
  const employeeType = watch('employeeType');
  const departmentId = watch('departmentId');
  const gender = watch('gender');
  const employmentTypeVal = watch('employmentType');

  const onSubmit = (values: FormValues) => {
    createEmployee.mutate(
      {
        entityId: values.entityId,
        employeeNumber: values.employeeNumber,
        employeeType: values.employeeType,
        firstName: values.firstName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth,
        taxId: values.taxId,
        hireDate: values.hireDate,
        departmentId: values.departmentId || undefined,
        gender: values.gender || undefined,
        nationality: values.nationality || undefined,
        position: values.position || undefined,
        employmentType: values.employmentType || undefined,
        workEmail: values.workEmail || undefined,
        personalEmail: values.personalEmail || undefined,
        personalPhone: values.personalPhone || undefined,
      },
      {
        onSuccess: () => {
          navigate('/hr/employees');
        },
      },
    );
  };

  return (
    <div>
      <PageHeader
        title={t('employees.createTitle')}
        actions={
          <Button variant="outline" onClick={() => navigate('/hr/employees')}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            {t('common:buttons.back')}
          </Button>
        }
      />

      <div className="max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('employees.createCardTitle')}</CardTitle>
            <CardDescription>
              {t('employees.createCardDescription')}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
              {/* Name */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="firstName">{t('employees.fields.firstName')} *</Label>
                  <Input
                    id="firstName"
                    placeholder={t('employees.placeholders.firstName')}
                    {...register('firstName')}
                  />
                  {errors.firstName && (
                    <p className="text-destructive text-xs">
                      {errors.firstName.message}
                    </p>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="lastName">{t('employees.fields.lastName')} *</Label>
                  <Input
                    id="lastName"
                    placeholder={t('employees.placeholders.lastName')}
                    {...register('lastName')}
                  />
                  {errors.lastName && (
                    <p className="text-destructive text-xs">
                      {errors.lastName.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Niederlassung */}
              <div className="space-y-1.5">
                <Label htmlFor="entityId">{t('employees.fields.entity')} *</Label>
                <Select
                  value={entityId}
                  onValueChange={(v) => setValue('entityId', v, { shouldValidate: true, shouldDirty: true })}
                >
                  <SelectTrigger id="entityId">
                    <SelectValue placeholder={t('employees.placeholders.selectEntity')} />
                  </SelectTrigger>
                  <SelectContent>
                    {entities.map((e) => (
                      <SelectItem key={e.id} value={e.id}>
                        {e.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.entityId && (
                  <p className="text-destructive text-xs">{errors.entityId.message}</p>
                )}
              </div>

              {/* Personalnummer */}
              <div className="space-y-1.5">
                <Label htmlFor="employeeNumber">{t('employees.fields.employeeNumber')} *</Label>
                <Input
                  id="employeeNumber"
                  placeholder={t('employees.placeholders.employeeNumber')}
                  maxLength={20}
                  {...register('employeeNumber')}
                />
                {errors.employeeNumber && (
                  <p className="text-destructive text-xs">
                    {errors.employeeNumber.message}
                  </p>
                )}
              </div>

              {/* Anstellungsart */}
              <div className="space-y-1.5">
                <Label htmlFor="employeeType">{t('employees.fields.employeeType')} *</Label>
                <Select
                  value={employeeType}
                  onValueChange={(v) =>
                    setValue('employeeType', v, {
                      shouldValidate: true,
                      shouldDirty: true,
                    })
                  }
                >
                  <SelectTrigger id="employeeType">
                    <SelectValue placeholder={t('employees.placeholders.selectType')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Employee">{t('employees.type.Employee')}</SelectItem>
                    <SelectItem value="Contractor">{t('employees.type.Contractor')}</SelectItem>
                  </SelectContent>
                </Select>
                {errors.employeeType && (
                  <p className="text-destructive text-xs">
                    {errors.employeeType.message}
                  </p>
                )}
              </div>

              {/* Einstellungsdatum + Geburtsdatum */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="hireDate">{t('employees.fields.hireDate')} *</Label>
                  <Input
                    id="hireDate"
                    type="date"
                    {...register('hireDate')}
                  />
                  {errors.hireDate && (
                    <p className="text-destructive text-xs">
                      {errors.hireDate.message}
                    </p>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="dateOfBirth">{t('employees.fields.dateOfBirth')} *</Label>
                  <Input
                    id="dateOfBirth"
                    type="date"
                    {...register('dateOfBirth')}
                  />
                  {errors.dateOfBirth && (
                    <p className="text-destructive text-xs">
                      {errors.dateOfBirth.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Steuer-ID */}
              <div className="space-y-1.5">
                <Label htmlFor="taxId">{t('employees.fields.taxId')} *</Label>
                <Input
                  id="taxId"
                  placeholder={t('employees.placeholders.taxId')}
                  maxLength={50}
                  {...register('taxId')}
                />
                {errors.taxId && (
                  <p className="text-destructive text-xs">
                    {errors.taxId.message}
                  </p>
                )}
              </div>

              {/* Abteilung */}
              <div className="space-y-1.5">
                <Label htmlFor="departmentId">{t('employees.fields.department')}</Label>
                <Select
                  value={departmentId || 'none'}
                  onValueChange={(v) =>
                    setValue('departmentId', v === 'none' ? '' : v, {
                      shouldDirty: true,
                    })
                  }
                >
                  <SelectTrigger id="departmentId">
                    <SelectValue placeholder={t('employees.placeholders.selectDepartment')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t('employees.placeholders.noDepartment')}</SelectItem>
                    {(departments ?? []).map((dept) => (
                      <SelectItem key={dept.id} value={dept.id}>
                        {dept.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.departmentId && (
                  <p className="text-destructive text-xs">
                    {errors.departmentId.message}
                  </p>
                )}
              </div>

              {/* Optional: Gender + Employment Type */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.gender')}</Label>
                  <Select
                    value={gender || 'none'}
                    onValueChange={(v) => setValue('gender', v === 'none' ? '' : v, { shouldDirty: true })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">—</SelectItem>
                      <SelectItem value="Male">{t('employees.gender.Male')}</SelectItem>
                      <SelectItem value="Female">{t('employees.gender.Female')}</SelectItem>
                      <SelectItem value="Diverse">{t('employees.gender.Diverse')}</SelectItem>
                      <SelectItem value="NotSpecified">{t('employees.gender.NotSpecified')}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.employmentType')}</Label>
                  <Select
                    value={employmentTypeVal || 'none'}
                    onValueChange={(v) => setValue('employmentType', v === 'none' ? '' : v, { shouldDirty: true })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
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
              </div>

              {/* Optional: Position + Nationality */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.position')}</Label>
                  <Input
                    placeholder={t('employees.placeholders.position')}
                    maxLength={200}
                    {...register('position')}
                  />
                </div>
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.nationality')}</Label>
                  <Input
                    placeholder={t('employees.placeholders.nationality')}
                    maxLength={100}
                    {...register('nationality')}
                  />
                </div>
              </div>

              {/* Optional: Contact */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.workEmail')}</Label>
                  <Input
                    type="email"
                    maxLength={254}
                    {...register('workEmail')}
                  />
                  {errors.workEmail && (
                    <p className="text-destructive text-xs">{errors.workEmail.message}</p>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label>{t('employees.fields.personalPhone')}</Label>
                  <Input
                    maxLength={50}
                    {...register('personalPhone')}
                  />
                </div>
              </div>

              {/* Actions */}
              <div className="flex gap-3 pt-2">
                <Button
                  type="submit"
                  disabled={createEmployee.isPending}
                >
                  {createEmployee.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {t('employees.createButton')}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/hr/employees')}
                >
                  {t('common:buttons.cancel')}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
