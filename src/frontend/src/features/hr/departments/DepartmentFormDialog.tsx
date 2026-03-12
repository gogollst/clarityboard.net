import { useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useCreateDepartment, useUpdateDepartment } from '@/hooks/useHr';
import type { Department, EmployeeListItem } from '@/types/hr';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
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

interface DepartmentFormDialogProps {
  mode: 'create' | 'edit';
  open: boolean;
  onClose: () => void;
  entityId: string;
  department?: Department;
  departments: Department[];
  employees: EmployeeListItem[];
}

const createSchema = (t: (key: string) => string) =>
  z.object({
    name: z.string().min(1, t('departments.validation.nameRequired')).max(200),
    code: z.string().min(1, t('departments.validation.codeRequired')).max(50),
    description: z.string().max(500),
    parentDepartmentId: z.string(),
    managerId: z.string(),
  });

type FormValues = z.infer<ReturnType<typeof createSchema>>;

export default function DepartmentFormDialog({
  mode,
  open,
  onClose,
  entityId,
  department,
  departments,
  employees,
}: DepartmentFormDialogProps) {
  const { t } = useTranslation('hr');
  const createDepartment = useCreateDepartment();
  const updateDepartment = useUpdateDepartment();
  const isPending = mode === 'create' ? createDepartment.isPending : updateDepartment.isPending;

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
      name: department?.name ?? '',
      code: department?.code ?? '',
      description: '',
      parentDepartmentId: department?.parentDepartmentId ?? '',
      managerId: department?.managerId ?? '',
    },
  });

  const parentDepartmentId = watch('parentDepartmentId');
  const managerId = watch('managerId');

  const availableParents = departments.filter((d) => d.id !== department?.id);

  const onSubmit = (values: FormValues) => {
    if (mode === 'create') {
      createDepartment.mutate(
        {
          entityId,
          name: values.name,
          code: values.code,
          description: values.description || undefined,
          parentDepartmentId: values.parentDepartmentId || undefined,
          managerId: values.managerId || undefined,
        },
        { onSuccess: onClose },
      );
    } else if (department) {
      updateDepartment.mutate(
        {
          id: department.id,
          entityId,
          name: values.name,
          code: values.code,
          description: values.description || undefined,
          parentDepartmentId: values.parentDepartmentId || undefined,
          managerId: values.managerId || undefined,
        },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) onClose(); }}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {mode === 'create' ? t('departments.createTitle') : t('departments.editTitle')}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('departments.fields.name')} *</Label>
              <Input {...register('name')} maxLength={200} />
              {errors.name && <p className="text-destructive text-xs">{errors.name.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>{t('departments.fields.code')} *</Label>
              <Input {...register('code')} maxLength={50} />
              {errors.code && <p className="text-destructive text-xs">{errors.code.message}</p>}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>{t('departments.fields.description')}</Label>
            <Textarea {...register('description')} maxLength={500} rows={2} />
          </div>

          <div className="space-y-1.5">
            <Label>{t('departments.fields.parentDepartment')}</Label>
            <Select
              value={parentDepartmentId || 'none'}
              onValueChange={(v) => setValue('parentDepartmentId', v === 'none' ? '' : v)}
            >
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="none">—</SelectItem>
                {availableParents.map((d) => (
                  <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('departments.fields.manager')}</Label>
            <Select
              value={managerId || 'none'}
              onValueChange={(v) => setValue('managerId', v === 'none' ? '' : v)}
            >
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="none">—</SelectItem>
                {employees.map((e) => (
                  <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
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
