import { useEffect, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Shield, Save, Loader2 } from 'lucide-react';
import { useAuthConfig, useUpsertAuthConfig } from '@/hooks/useAdmin';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

type FormValues = {
  tokenLifetimeHours: number;
  rememberMeTokenLifetimeDays: number;
};

export function Component() {
  const { t } = useTranslation('admin');

  const schema = useMemo(() => z.object({
    tokenLifetimeHours: z
      .number()
      .int()
      .min(1, t('auth.validation.minHour'))
      .max(168, t('auth.validation.maxHours')),
    rememberMeTokenLifetimeDays: z
      .number()
      .int()
      .min(1, t('auth.validation.minDay'))
      .max(365, t('auth.validation.maxDays')),
  }), [t]);

  const { data: config, isLoading } = useAuthConfig();
  const upsert = useUpsertAuthConfig();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { tokenLifetimeHours: 24, rememberMeTokenLifetimeDays: 30 },
  });

  useEffect(() => {
    if (config) {
      reset({
        tokenLifetimeHours: config.tokenLifetimeHours,
        rememberMeTokenLifetimeDays: config.rememberMeTokenLifetimeDays,
      });
    }
  }, [config, reset]);

  const onSubmit = (values: FormValues) => {
    upsert.mutate(values, { onSuccess: () => reset(values) });
  };

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <div className="flex items-center gap-3">
        <Shield className="h-6 w-6 text-primary" />
        <div>
          <h1 className="text-2xl font-semibold">{t('auth.title')}</h1>
          <p className="text-sm text-muted-foreground">
            {t('auth.description')}
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit(onSubmit)}>
        <Card>
          <CardHeader>
            <CardTitle>{t('auth.tokenLifetimes.title')}</CardTitle>
            <CardDescription>
              {t('auth.tokenLifetimes.description')}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="tokenLifetimeHours">
                {t('auth.tokenLifetimes.sessionToken.label')}
              </Label>
              <Input
                id="tokenLifetimeHours"
                type="number"
                min={1}
                max={168}
                {...register('tokenLifetimeHours', { valueAsNumber: true })}
                className="w-40"
              />
              <p className="text-xs text-muted-foreground">
                {t('auth.tokenLifetimes.sessionToken.hint')}
              </p>
              {errors.tokenLifetimeHours && (
                <p className="text-sm text-destructive">
                  {errors.tokenLifetimeHours.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="rememberMeTokenLifetimeDays">
                {t('auth.tokenLifetimes.rememberMeToken.label')}
              </Label>
              <Input
                id="rememberMeTokenLifetimeDays"
                type="number"
                min={1}
                max={365}
                {...register('rememberMeTokenLifetimeDays', {
                  valueAsNumber: true,
                })}
                className="w-40"
              />
              <p className="text-xs text-muted-foreground">
                {t('auth.tokenLifetimes.rememberMeToken.hint')}
              </p>
              {errors.rememberMeTokenLifetimeDays && (
                <p className="text-sm text-destructive">
                  {errors.rememberMeTokenLifetimeDays.message}
                </p>
              )}
            </div>

            <div className="flex justify-end pt-2">
              <Button
                type="submit"
                disabled={upsert.isPending || !isDirty}
              >
                {upsert.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Save className="h-4 w-4" />
                )}
                {t('auth.saveSettings')}
              </Button>
            </div>
          </CardContent>
        </Card>
      </form>
    </div>
  );
}
