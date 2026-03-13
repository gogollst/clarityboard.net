import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useCreateBusinessPartner, useAccounts } from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { getLocalizedAccountName } from '@/lib/accountUtils';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Textarea } from '@/components/ui/textarea';
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
import {
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
} from '@/components/ui/tabs';
import { ArrowLeft, Loader2 } from 'lucide-react';

type FormValues = {
  name: string;
  taxId: string;
  vatNumber: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  email: string;
  phone: string;
  bankName: string;
  iban: string;
  bic: string;
  isCreditor: boolean;
  isDebtor: boolean;
  paymentTermDays: number;
  defaultExpenseAccountId: string;
  defaultRevenueAccountId: string;
  notes: string;
};

export function Component() {
  const { t, i18n } = useTranslation('accounting');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const createPartner = useCreateBusinessPartner();
  const { data: accounts = [] } = useAccounts(selectedEntityId);

  const schema = useMemo(
    () =>
      z.object({
        name: z.string().min(1, t('businessPartners.validation.nameRequired')).max(200),
        taxId: z.string().max(50),
        vatNumber: z.string().max(50),
        street: z.string().max(200),
        city: z.string().max(100),
        postalCode: z.string().max(20),
        country: z.string().max(2),
        email: z.string().max(200),
        phone: z.string().max(50),
        bankName: z.string().max(200),
        iban: z.string().max(34),
        bic: z.string().max(11),
        isCreditor: z.boolean(),
        isDebtor: z.boolean(),
        paymentTermDays: z.number().min(0, t('businessPartners.validation.paymentTermDaysMin')),
        defaultExpenseAccountId: z.string(),
        defaultRevenueAccountId: z.string(),
        notes: z.string(),
      }).superRefine((data, ctx) => {
        if (!data.isCreditor && !data.isDebtor) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: t('businessPartners.validation.typeRequired'),
            path: ['isCreditor'],
          });
        }
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
      name: '',
      taxId: '',
      vatNumber: '',
      street: '',
      city: '',
      postalCode: '',
      country: '',
      email: '',
      phone: '',
      bankName: '',
      iban: '',
      bic: '',
      isCreditor: true,
      isDebtor: false,
      paymentTermDays: 30,
      defaultExpenseAccountId: '',
      defaultRevenueAccountId: '',
      notes: '',
    },
  });

  const isCreditor = watch('isCreditor');
  const isDebtor = watch('isDebtor');
  const defaultExpenseAccountId = watch('defaultExpenseAccountId');
  const defaultRevenueAccountId = watch('defaultRevenueAccountId');

  const onSubmit = (values: FormValues) => {
    if (!selectedEntityId) return;
    createPartner.mutate(
      {
        entityId: selectedEntityId,
        name: values.name,
        taxId: values.taxId || undefined,
        vatNumber: values.vatNumber || undefined,
        street: values.street || undefined,
        city: values.city || undefined,
        postalCode: values.postalCode || undefined,
        country: values.country || undefined,
        email: values.email || undefined,
        phone: values.phone || undefined,
        bankName: values.bankName || undefined,
        iban: values.iban || undefined,
        bic: values.bic || undefined,
        isCreditor: values.isCreditor,
        isDebtor: values.isDebtor,
        paymentTermDays: values.paymentTermDays,
        defaultExpenseAccountId: values.defaultExpenseAccountId || undefined,
        defaultRevenueAccountId: values.defaultRevenueAccountId || undefined,
        notes: values.notes || undefined,
      },
      {
        onSuccess: () => {
          navigate('/accounting/business-partners');
        },
      },
    );
  };

  const expenseAccounts = accounts.filter(
    (a) => a.accountType === 'Expense' && a.isActive,
  );
  const revenueAccounts = accounts.filter(
    (a) => a.accountType === 'Revenue' && a.isActive,
  );

  return (
    <div>
      <PageHeader
        title={t('businessPartners.createTitle')}
        actions={
          <Button variant="outline" onClick={() => navigate('/accounting/business-partners')}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            {t('common:buttons.back')}
          </Button>
        }
      />

      <div className="max-w-2xl">
        <form onSubmit={handleSubmit(onSubmit)}>
          <Tabs defaultValue="masterData">
            <TabsList className="mb-4">
              <TabsTrigger value="masterData">{t('businessPartners.tabs.masterData')}</TabsTrigger>
              <TabsTrigger value="bank">{t('businessPartners.tabs.bank')}</TabsTrigger>
              <TabsTrigger value="accounting">{t('businessPartners.tabs.accounting')}</TabsTrigger>
            </TabsList>

            <TabsContent value="masterData">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">{t('businessPartners.createCardTitle')}</CardTitle>
                  <CardDescription>{t('businessPartners.createCardDescription')}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-5">
                  <div className="space-y-1.5">
                    <Label htmlFor="name">{t('businessPartners.fields.name')} *</Label>
                    <Input
                      id="name"
                      placeholder={t('businessPartners.placeholders.name')}
                      maxLength={200}
                      {...register('name')}
                    />
                    {errors.name && (
                      <p className="text-destructive text-xs">{errors.name.message}</p>
                    )}
                  </div>

                  <div className="flex items-center gap-6">
                    <div className="flex items-center gap-2">
                      <Checkbox
                        id="isCreditor"
                        checked={isCreditor}
                        onCheckedChange={(checked) =>
                          setValue('isCreditor', !!checked, { shouldValidate: true })
                        }
                      />
                      <Label htmlFor="isCreditor" className="cursor-pointer">
                        {t('businessPartners.fields.isCreditor')}
                      </Label>
                    </div>
                    <div className="flex items-center gap-2">
                      <Checkbox
                        id="isDebtor"
                        checked={isDebtor}
                        onCheckedChange={(checked) =>
                          setValue('isDebtor', !!checked, { shouldValidate: true })
                        }
                      />
                      <Label htmlFor="isDebtor" className="cursor-pointer">
                        {t('businessPartners.fields.isDebtor')}
                      </Label>
                    </div>
                    {errors.isCreditor && (
                      <p className="text-destructive text-xs">{errors.isCreditor.message}</p>
                    )}
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1.5">
                      <Label htmlFor="taxId">{t('businessPartners.fields.taxId')}</Label>
                      <Input
                        id="taxId"
                        placeholder={t('businessPartners.placeholders.taxId')}
                        maxLength={50}
                        {...register('taxId')}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="vatNumber">{t('businessPartners.fields.vatNumber')}</Label>
                      <Input
                        id="vatNumber"
                        placeholder={t('businessPartners.placeholders.vatNumber')}
                        maxLength={50}
                        {...register('vatNumber')}
                      />
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="street">{t('businessPartners.fields.street')}</Label>
                    <Input
                      id="street"
                      placeholder={t('businessPartners.placeholders.street')}
                      maxLength={200}
                      {...register('street')}
                    />
                  </div>

                  <div className="grid grid-cols-3 gap-4">
                    <div className="space-y-1.5">
                      <Label htmlFor="postalCode">{t('businessPartners.fields.postalCode')}</Label>
                      <Input
                        id="postalCode"
                        placeholder={t('businessPartners.placeholders.postalCode')}
                        maxLength={20}
                        {...register('postalCode')}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="city">{t('businessPartners.fields.city')}</Label>
                      <Input
                        id="city"
                        placeholder={t('businessPartners.placeholders.city')}
                        maxLength={100}
                        {...register('city')}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="country">{t('businessPartners.fields.country')}</Label>
                      <Input
                        id="country"
                        placeholder={t('businessPartners.placeholders.country')}
                        maxLength={2}
                        {...register('country')}
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1.5">
                      <Label htmlFor="email">{t('businessPartners.fields.email')}</Label>
                      <Input
                        id="email"
                        type="email"
                        placeholder={t('businessPartners.placeholders.email')}
                        maxLength={200}
                        {...register('email')}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="phone">{t('businessPartners.fields.phone')}</Label>
                      <Input
                        id="phone"
                        placeholder={t('businessPartners.placeholders.phone')}
                        maxLength={50}
                        {...register('phone')}
                      />
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="notes">{t('businessPartners.fields.notes')}</Label>
                    <Textarea
                      id="notes"
                      placeholder={t('businessPartners.placeholders.notes')}
                      rows={3}
                      {...register('notes')}
                    />
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="bank">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">{t('businessPartners.tabs.bank')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-5">
                  <div className="space-y-1.5">
                    <Label htmlFor="bankName">{t('businessPartners.fields.bankName')}</Label>
                    <Input
                      id="bankName"
                      placeholder={t('businessPartners.placeholders.bankName')}
                      maxLength={200}
                      {...register('bankName')}
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1.5">
                      <Label htmlFor="iban">{t('businessPartners.fields.iban')}</Label>
                      <Input
                        id="iban"
                        placeholder={t('businessPartners.placeholders.iban')}
                        maxLength={34}
                        {...register('iban')}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="bic">{t('businessPartners.fields.bic')}</Label>
                      <Input
                        id="bic"
                        placeholder={t('businessPartners.placeholders.bic')}
                        maxLength={11}
                        {...register('bic')}
                      />
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="accounting">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">{t('businessPartners.tabs.accounting')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-5">
                  <div className="space-y-1.5">
                    <Label htmlFor="paymentTermDays">{t('businessPartners.fields.paymentTermDays')}</Label>
                    <Input
                      id="paymentTermDays"
                      type="number"
                      min={0}
                      {...register('paymentTermDays', { valueAsNumber: true })}
                    />
                    {errors.paymentTermDays && (
                      <p className="text-destructive text-xs">{errors.paymentTermDays.message}</p>
                    )}
                  </div>

                  <div className="space-y-1.5">
                    <Label>{t('businessPartners.fields.defaultExpenseAccount')}</Label>
                    <Select
                      value={defaultExpenseAccountId || 'none'}
                      onValueChange={(v) =>
                        setValue('defaultExpenseAccountId', v === 'none' ? '' : v)
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
                      value={defaultRevenueAccountId || 'none'}
                      onValueChange={(v) =>
                        setValue('defaultRevenueAccountId', v === 'none' ? '' : v)
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
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>

          <div className="mt-6 flex gap-3">
            <Button type="submit" disabled={createPartner.isPending}>
              {createPartner.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              {t('businessPartners.createButton')}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate('/accounting/business-partners')}
            >
              {t('common:buttons.cancel')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
