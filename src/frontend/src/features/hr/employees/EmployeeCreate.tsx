import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateEmployee, useDepartments } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
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

const schema = z.object({
  firstName: z.string().min(1, 'Vorname ist erforderlich'),
  lastName: z.string().min(1, 'Nachname ist erforderlich'),
  employeeNumber: z
    .string()
    .min(1, 'Personalnummer ist erforderlich')
    .max(20, 'Personalnummer darf maximal 20 Zeichen haben'),
  employeeType: z.string().min(1, 'Anstellungsart ist erforderlich'),
  hireDate: z.string().min(1, 'Einstellungsdatum ist erforderlich'),
  dateOfBirth: z.string().min(1, 'Geburtsdatum ist erforderlich'),
  taxId: z
    .string()
    .min(1, 'Steuer-ID ist erforderlich')
    .max(50, 'Steuer-ID darf maximal 50 Zeichen haben'),
  departmentId: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

export function Component() {
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const createEmployee = useCreateEmployee();
  const { data: departments } = useDepartments(selectedEntityId ?? undefined);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: '',
      lastName: '',
      employeeNumber: '',
      employeeType: '',
      hireDate: '',
      dateOfBirth: '',
      taxId: '',
      departmentId: '',
    },
  });

  const employeeType = watch('employeeType');
  const departmentId = watch('departmentId');

  const onSubmit = (values: FormValues) => {
    if (!selectedEntityId) return;

    createEmployee.mutate(
      {
        entityId: selectedEntityId,
        employeeNumber: values.employeeNumber,
        employeeType: values.employeeType,
        firstName: values.firstName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth,
        taxId: values.taxId,
        hireDate: values.hireDate,
        departmentId: values.departmentId || undefined,
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
        title="Neuer Mitarbeiter"
        actions={
          <Button variant="outline" onClick={() => navigate('/hr/employees')}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            Zurück
          </Button>
        }
      />

      <div className="max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Mitarbeiterdaten</CardTitle>
            <CardDescription>
              Füllen Sie alle Pflichtfelder aus, um einen neuen Mitarbeiter anzulegen.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
              {/* Name */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="firstName">Vorname *</Label>
                  <Input
                    id="firstName"
                    placeholder="Max"
                    {...register('firstName')}
                  />
                  {errors.firstName && (
                    <p className="text-destructive text-xs">
                      {errors.firstName.message}
                    </p>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="lastName">Nachname *</Label>
                  <Input
                    id="lastName"
                    placeholder="Mustermann"
                    {...register('lastName')}
                  />
                  {errors.lastName && (
                    <p className="text-destructive text-xs">
                      {errors.lastName.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Personalnummer */}
              <div className="space-y-1.5">
                <Label htmlFor="employeeNumber">Personalnummer *</Label>
                <Input
                  id="employeeNumber"
                  placeholder="EMP-001"
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
                <Label htmlFor="employeeType">Anstellungsart *</Label>
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
                    <SelectValue placeholder="Anstellungsart wählen" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Employee">Festangestellt</SelectItem>
                    <SelectItem value="Contractor">Contractor</SelectItem>
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
                  <Label htmlFor="hireDate">Einstellungsdatum *</Label>
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
                  <Label htmlFor="dateOfBirth">Geburtsdatum *</Label>
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
                <Label htmlFor="taxId">Steuer-ID *</Label>
                <Input
                  id="taxId"
                  placeholder="12345678901"
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
                <Label htmlFor="departmentId">Abteilung</Label>
                <Select
                  value={departmentId || 'none'}
                  onValueChange={(v) =>
                    setValue('departmentId', v === 'none' ? '' : v, {
                      shouldDirty: true,
                    })
                  }
                >
                  <SelectTrigger id="departmentId">
                    <SelectValue placeholder="Abteilung wählen (optional)" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Keine Abteilung</SelectItem>
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

              {/* Actions */}
              <div className="flex gap-3 pt-2">
                <Button
                  type="submit"
                  disabled={createEmployee.isPending || !selectedEntityId}
                >
                  {createEmployee.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  Mitarbeiter anlegen
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/hr/employees')}
                >
                  Abbrechen
                </Button>
              </div>

              {!selectedEntityId && (
                <p className="text-destructive text-sm">
                  Bitte wählen Sie zuerst eine Organisation aus.
                </p>
              )}
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
