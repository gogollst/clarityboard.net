import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useAccountDetail,
  useUpdateAccount,
  useDeactivateAccount,
} from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { ArrowLeft, Loader2 } from 'lucide-react';

const schema = z.object({
  name: z.string().min(1).max(200),
  vatDefault: z.string().max(10).optional(),
  costCenterDefault: z.string().max(50).optional(),
  bwaLine: z.string().max(10).optional(),
  isAutoPosting: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

export function Component() {
  const { t } = useTranslation('accounting');
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { selectedEntityId } = useEntity();

  const { data: account, isLoading } = useAccountDetail(id ?? null);
  const updateAccount = useUpdateAccount();
  const deactivateAccount = useDeactivateAccount();
  const [showDeactivate, setShowDeactivate] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: account
      ? {
          name: account.name,
          vatDefault: account.vatDefault ?? '',
          costCenterDefault: account.costCenterDefault ?? '',
          bwaLine: account.bwaLine ?? '',
          isAutoPosting: account.isAutoPosting,
        }
      : undefined,
  });

  const onSubmit = (values: FormValues) => {
    if (!id || !selectedEntityId) return;
    updateAccount.mutate(
      {
        id,
        entityId: selectedEntityId,
        ...values,
        vatDefault: values.vatDefault || undefined,
        costCenterDefault: values.costCenterDefault || undefined,
        bwaLine: values.bwaLine || undefined,
      },
      {
        onSuccess: () => setIsEditing(false),
      },
    );
  };

  const handleDeactivate = () => {
    if (!id || !selectedEntityId) return;
    deactivateAccount.mutate(
      { id, entityId: selectedEntityId },
      {
        onSuccess: () => {
          setShowDeactivate(false);
          navigate('/accounting/accounts');
        },
      },
    );
  };

  if (isLoading) {
    return (
      <div className="flex justify-center p-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!account) {
    return (
      <div className="p-6">
        <p className="text-muted-foreground">{t('accounts.notFound')}</p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate('/accounting/accounts')}
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        {t('accounts.backToList')}
      </Button>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-3">
              <span className="font-mono tabular-nums">{account.accountNumber}</span>
              <span>{account.name}</span>
            </CardTitle>
            <div className="mt-2 flex items-center gap-2">
              <Badge
                className={
                  account.isActive
                    ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300'
                    : 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300'
                }
              >
                {account.isActive
                  ? t('accounts.status.active')
                  : t('accounts.status.inactive')}
              </Badge>
              <Badge variant="secondary">{t(`accounts.accountTypes.${account.accountType}`)}</Badge>
              <Badge variant="secondary">
                {t('accounts.fields.accountClass')} {account.accountClass}
              </Badge>
              {account.isSystemAccount && (
                <Badge variant="secondary">{t('accounts.fields.isSystemAccount')}</Badge>
              )}
            </div>
          </div>
          <div className="flex gap-2">
            {!isEditing && account.isActive && (
              <>
                <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>
                  {t('accounts.actions.edit')}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => setShowDeactivate(true)}
                >
                  {t('accounts.actions.deactivate')}
                </Button>
              </>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {isEditing ? (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div>
                <Label>{t('accounts.fields.name')}</Label>
                <Input {...register('name')} />
                {errors.name && (
                  <p className="mt-1 text-sm text-destructive">
                    {t('accounts.validation.required')}
                  </p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label>{t('accounts.fields.vatDefault')}</Label>
                  <Input {...register('vatDefault')} />
                </div>
                <div>
                  <Label>{t('accounts.fields.bwaLine')}</Label>
                  <Input {...register('bwaLine')} />
                </div>
              </div>

              <div>
                <Label>{t('accounts.fields.costCenterDefault')}</Label>
                <Input {...register('costCenterDefault')} />
              </div>

              <div className="flex items-center gap-2">
                <Checkbox
                  id="isAutoPosting"
                  checked={watch('isAutoPosting')}
                  onCheckedChange={(checked) =>
                    setValue('isAutoPosting', checked === true)
                  }
                />
                <Label htmlFor="isAutoPosting">{t('accounts.fields.isAutoPosting')}</Label>
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsEditing(false);
                    reset();
                  }}
                >
                  {t('accounts.actions.cancel')}
                </Button>
                <Button type="submit" disabled={updateAccount.isPending}>
                  {updateAccount.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {t('accounts.actions.save')}
                </Button>
              </div>
            </form>
          ) : (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">{t('accounts.fields.vatDefault')}</span>
                  <p className="font-medium">{account.vatDefault || '—'}</p>
                </div>
                <div>
                  <span className="text-muted-foreground">{t('accounts.fields.bwaLine')}</span>
                  <p className="font-medium">{account.bwaLine || '—'}</p>
                </div>
                <div>
                  <span className="text-muted-foreground">{t('accounts.fields.costCenterDefault')}</span>
                  <p className="font-medium">{account.costCenterDefault || '—'}</p>
                </div>
                <div>
                  <span className="text-muted-foreground">{t('accounts.fields.isAutoPosting')}</span>
                  <p className="font-medium">{account.isAutoPosting ? t('accounts.yes') : t('accounts.no')}</p>
                </div>
              </div>

              <div className="border-t pt-4">
                <h4 className="mb-2 text-sm font-medium">{t('accounts.statistics')}</h4>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-muted-foreground">{t('accounts.journalEntryCount')}</span>
                    <p className="font-medium tabular-nums">{account.journalEntryCount}</p>
                  </div>
                  <div>
                    <span className="text-muted-foreground">{t('accounts.lastBookingDate')}</span>
                    <p className="font-medium">
                      {account.lastBookingDate
                        ? new Date(account.lastBookingDate).toLocaleDateString()
                        : '—'}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={showDeactivate} onOpenChange={setShowDeactivate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('accounts.confirmDeactivateTitle')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {t('accounts.confirmDeactivate')}
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDeactivate(false)}>
              {t('accounts.actions.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeactivate}
              disabled={deactivateAccount.isPending}
            >
              {deactivateAccount.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              {t('accounts.actions.deactivate')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
