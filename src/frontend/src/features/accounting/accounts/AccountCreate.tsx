import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateAccount } from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
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
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { ArrowLeft, Loader2 } from 'lucide-react';

const schema = z.object({
  accountNumber: z
    .string()
    .min(1, 'required')
    .max(10)
    .regex(/^\d+$/, 'digits_only'),
  name: z.string().min(1, 'required').max(200),
  accountType: z.enum(['asset', 'liability', 'equity', 'revenue', 'expense']),
  accountClass: z.number().int().min(0).max(9),
  vatDefault: z.string().max(10).optional(),
  datevAuto: z.string().max(10).optional(),
  costCenterDefault: z.string().max(50).optional(),
  bwaLine: z.string().max(10).optional(),
  isAutoPosting: z.boolean().optional(),
});

type FormValues = z.infer<typeof schema>;

export function Component() {
  const { t, i18n } = useTranslation('accounting');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const createAccount = useCreateAccount();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      accountNumber: '',
      name: '',
      accountType: 'expense',
      accountClass: 4,
      isAutoPosting: false,
    },
  });

  const onSubmit = (values: FormValues) => {
    if (!selectedEntityId) return;
    createAccount.mutate(
      {
        entityId: selectedEntityId,
        ...values,
        vatDefault: values.vatDefault || undefined,
        datevAuto: values.datevAuto || undefined,
        costCenterDefault: values.costCenterDefault || undefined,
        bwaLine: values.bwaLine || undefined,
        sourceLanguage: i18n.language,
      },
      { onSuccess: () => navigate('/accounting/accounts') },
    );
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <Button variant="ghost" size="sm" onClick={() => navigate('/accounting/accounts')}>
        <ArrowLeft className="mr-2 h-4 w-4" />
        {t('accounts.backToList')}
      </Button>

      <Card>
        <CardHeader>
          <CardTitle>{t('accounts.createTitle')}</CardTitle>
          <CardDescription>{t('accounts.createDescription')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>{t('accounts.fields.accountNumber')}</Label>
                <Input
                  {...register('accountNumber')}
                  placeholder={t('accounts.placeholders.accountNumber')}
                />
                {errors.accountNumber && (
                  <p className="mt-1 text-sm text-destructive">
                    {t(`accounts.validation.${errors.accountNumber.message}`)}
                  </p>
                )}
              </div>
              <div>
                <Label>{t('accounts.fields.accountClass')}</Label>
                <Select
                  value={String(watch('accountClass'))}
                  onValueChange={(v) => setValue('accountClass', Number(v))}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {[0, 1, 2, 3, 4, 5, 6, 7, 8, 9].map((c) => (
                      <SelectItem key={c} value={String(c)}>
                        {c} - {t(`accounts.accountClasses.${c}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div>
              <Label>{t('accounts.fields.name')}</Label>
              <Input
                {...register('name')}
                placeholder={t('accounts.placeholders.name')}
              />
              {errors.name ? (
                <p className="mt-1 text-sm text-destructive">
                  {t(`accounts.validation.${errors.name.message}`)}
                </p>
              ) : (
                <p className="mt-1 text-xs text-muted-foreground">
                  {t('accounts.translationHint')}
                </p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>{t('accounts.fields.accountType')}</Label>
                <Select
                  value={watch('accountType')}
                  onValueChange={(v) =>
                    setValue('accountType', v as FormValues['accountType'])
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {['asset', 'liability', 'equity', 'revenue', 'expense'].map(
                      (type) => (
                        <SelectItem key={type} value={type}>
                          {t(`accounts.accountTypes.${type}`)}
                        </SelectItem>
                      ),
                    )}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>{t('accounts.fields.vatDefault')}</Label>
                <Input
                  {...register('vatDefault')}
                  placeholder={t('accounts.placeholders.vatDefault')}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>{t('accounts.fields.bwaLine')}</Label>
                <Input
                  {...register('bwaLine')}
                  placeholder={t('accounts.placeholders.bwaLine')}
                />
              </div>
              <div>
                <Label>{t('accounts.fields.costCenterDefault')}</Label>
                <Input
                  {...register('costCenterDefault')}
                  placeholder={t('accounts.placeholders.costCenterDefault')}
                />
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Checkbox
                id="isAutoPosting"
                checked={watch('isAutoPosting')}
                onCheckedChange={(checked) =>
                  setValue('isAutoPosting', checked === true)
                }
              />
              <Label htmlFor="isAutoPosting">
                {t('accounts.fields.isAutoPosting')}
              </Label>
            </div>

            <div className="flex justify-end pt-4">
              <Button type="submit" disabled={createAccount.isPending}>
                {createAccount.isPending && (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                )}
                {t('accounts.actions.create')}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
