import { useEffect, useState, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import {
  Mail,
  Server,
  Shield,
  CheckCircle,
  AlertCircle,
  Save,
  Loader2,
  FlaskConical,
} from 'lucide-react';
import { useMailConfig, useUpsertMailConfig, useSendTestEmail } from '@/hooks/useAdmin';
import { useAuthStore } from '@/stores/authStore';
import type { UpsertMailConfigRequest } from '@/types/admin';
import { cn } from '@/lib/utils';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

type FormValues = {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
};

export function Component() {
  const { t, i18n } = useTranslation('admin');

  const schema = useMemo(() => z.object({
    host: z.string().min(1, t('mail.form.validation.hostRequired')).max(500, t('mail.form.validation.hostTooLong')),
    port: z.number().int().min(1, t('mail.form.validation.portRange')).max(65535, t('mail.form.validation.portRange')),
    username: z.string().min(1, t('mail.form.validation.usernameRequired')).max(500, t('mail.form.validation.usernameTooLong')),
    password: z.string().min(1, t('mail.form.validation.passwordRequired')),
    fromEmail: z.string().email(t('mail.form.validation.fromEmailInvalid')).max(256, t('mail.form.validation.fromEmailTooLong')),
    fromName: z.string().min(1, t('mail.form.validation.fromNameRequired')).max(256, t('mail.form.validation.fromNameTooLong')),
    enableSsl: z.boolean(),
  }), [t]);

  const { data: config, isLoading } = useMailConfig();
  const upsert = useUpsertMailConfig();
  const sendTest = useSendTestEmail();
  const user = useAuthStore((s) => s.user);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    getValues,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      host: '',
      port: 587,
      username: '',
      password: '',
      fromEmail: '',
      fromName: '',
      enableSsl: true,
    },
  });

  const enableSsl = watch('enableSsl');

  // Test email dialog state
  const [isTestOpen, setIsTestOpen] = useState(false);
  const [testConfig, setTestConfig] = useState<FormValues | null>(null);
  const [recipientEmail, setRecipientEmail] = useState('');
  const [testResult, setTestResult] = useState<{ success: boolean; errorMessage: string | null } | null>(null);

  useEffect(() => {
    if (config) {
      reset({
        host: config.host,
        port: config.port,
        username: config.username,
        password: '',
        fromEmail: config.fromEmail,
        fromName: config.fromName,
        enableSsl: config.enableSsl,
      });
    }
  }, [config, reset]);

  const onSubmit = (values: FormValues) => {
    const request: UpsertMailConfigRequest = {
      host: values.host,
      port: values.port,
      username: values.username,
      password: values.password,
      fromEmail: values.fromEmail,
      fromName: values.fromName,
      enableSsl: values.enableSsl,
    };
    upsert.mutate(request);
  };

  const handleOpenTest = () => {
    setTestConfig(getValues());
    setTestResult(null);
    setRecipientEmail(user?.email ?? '');
    setIsTestOpen(true);
  };

  const handleSendTest = () => {
    if (!testConfig) return;
    sendTest.mutate(
      { ...testConfig, recipientEmail },
      { onSuccess: (result) => setTestResult(result) },
    );
  };

  if (isLoading) {
    return (
      <div className="flex justify-center p-8">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6 max-w-2xl">
      {/* Page header */}
      <div>
        <h1 className="text-xl font-semibold flex items-center gap-2">
          <Mail className="h-5 w-5 text-primary" />
          {t('mail.title')}
        </h1>
        <p className="text-muted-foreground text-sm mt-1">
          {t('mail.description')}
        </p>
      </div>

      {/* Current status card */}
      {config && (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <Server className="h-4 w-4 text-muted-foreground" />
                {t('mail.currentConfig.title')}
              </CardTitle>
              <Badge
                className={
                  config.isActive
                    ? 'bg-emerald-100 text-emerald-800'
                    : 'bg-amber-100 text-amber-800'
                }
              >
                {config.isActive ? (
                  <>
                    <CheckCircle className="mr-1 h-3 w-3" />
                    {t('common:status.active', { ns: 'common' })}
                  </>
                ) : (
                  <>
                    <AlertCircle className="mr-1 h-3 w-3" />
                    {t('common:status.inactive', { ns: 'common' })}
                  </>
                )}
              </Badge>
            </div>
            <CardDescription className="text-xs">
              {t('mail.currentConfig.lastUpdated', { date: new Date(config.updatedAt).toLocaleString(i18n.language) })}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">{t('mail.currentConfig.fields.host')}</dt>
                <dd className="font-mono mt-0.5">{config.host}:{config.port}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">{t('mail.currentConfig.fields.username')}</dt>
                <dd className="font-mono mt-0.5">{config.username}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">{t('mail.currentConfig.fields.from')}</dt>
                <dd className="mt-0.5">
                  {config.fromName} &lt;{config.fromEmail}&gt;
                </dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">{t('mail.currentConfig.fields.sslTls')}</dt>
                <dd className="mt-0.5 flex items-center gap-1">
                  <Shield className="h-3 w-3 text-muted-foreground" />
                  {config.enableSsl ? t('mail.testDialog.enabled') : t('mail.testDialog.disabled')}
                </dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      )}

      {/* Configuration form */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">
            {config ? t('mail.form.updateTitle') : t('mail.form.createTitle')}
          </CardTitle>
          <CardDescription>
            {config ? t('mail.form.updateDescription') : t('mail.form.createDescription')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Host + Port */}
            <div className="grid grid-cols-3 gap-4">
              <div className="col-span-2 space-y-1.5">
                <Label htmlFor="host">{t('mail.form.fields.smtpHost')}</Label>
                <Input
                  id="host"
                  placeholder={t('mail.form.fields.smtpHostPlaceholder')}
                  {...register('host')}
                />
                {errors.host && (
                  <p className="text-destructive text-xs">{errors.host.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="port">{t('mail.form.fields.port')}</Label>
                <Input
                  id="port"
                  type="number"
                  placeholder={t('mail.form.fields.portPlaceholder')}
                  {...register('port', { valueAsNumber: true })}
                />
                {errors.port && (
                  <p className="text-destructive text-xs">{errors.port.message}</p>
                )}
              </div>
            </div>

            {/* Username */}
            <div className="space-y-1.5">
              <Label htmlFor="username">{t('mail.form.fields.username')}</Label>
              <Input
                id="username"
                autoComplete="username"
                placeholder={t('mail.form.fields.usernamePlaceholder')}
                {...register('username')}
              />
              {errors.username && (
                <p className="text-destructive text-xs">{errors.username.message}</p>
              )}
            </div>

            {/* Password */}
            <div className="space-y-1.5">
              <Label htmlFor="password">{t('mail.form.fields.password')}</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder={config ? t('mail.form.fields.passwordExistingPlaceholder') : t('mail.form.fields.passwordPlaceholder')}
                {...register('password')}
              />
              {errors.password && (
                <p className="text-destructive text-xs">{errors.password.message}</p>
              )}
              <p className="text-muted-foreground text-xs">
                {t('mail.form.fields.passwordHint')}
              </p>
            </div>

            {/* From Email */}
            <div className="space-y-1.5">
              <Label htmlFor="fromEmail">{t('mail.form.fields.fromEmail')}</Label>
              <Input
                id="fromEmail"
                type="email"
                placeholder={t('mail.form.fields.fromEmailPlaceholder')}
                {...register('fromEmail')}
              />
              {errors.fromEmail && (
                <p className="text-destructive text-xs">{errors.fromEmail.message}</p>
              )}
            </div>

            {/* From Name */}
            <div className="space-y-1.5">
              <Label htmlFor="fromName">{t('mail.form.fields.fromName')}</Label>
              <Input
                id="fromName"
                placeholder={t('mail.form.fields.fromNamePlaceholder')}
                {...register('fromName')}
              />
              {errors.fromName && (
                <p className="text-destructive text-xs">{errors.fromName.message}</p>
              )}
            </div>

            {/* SSL Toggle */}
            <div className="flex items-center gap-3">
              <Switch
                id="enableSsl"
                checked={enableSsl}
                onCheckedChange={(checked) => setValue('enableSsl', checked, { shouldDirty: true, shouldTouch: true })}
              />
              <div>
                <Label htmlFor="enableSsl" className="cursor-pointer">
                  {t('mail.form.fields.enableSsl')}
                </Label>
                <p className="text-muted-foreground text-xs">
                  {t('mail.form.fields.enableSslHint')}
                </p>
              </div>
            </div>

            {/* Submit + Test */}
            <div className="flex gap-2 pt-1">
              <Button type="submit" disabled={upsert.isPending}>
                {upsert.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Save className="mr-2 h-4 w-4" />
                )}
                {config ? t('mail.form.saveChanges') : t('mail.form.saveConfig')}
              </Button>
              <Button type="button" variant="outline" onClick={handleOpenTest}>
                <FlaskConical className="mr-2 h-4 w-4" />
                {t('mail.form.test')}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Test Email Dialog */}
      <Dialog
        open={isTestOpen}
        onOpenChange={(open) => {
          setIsTestOpen(open);
          if (!open) setTestResult(null);
        }}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{t('mail.testDialog.title')}</DialogTitle>
            <DialogDescription>
              {t('mail.testDialog.description')}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {/* Config summary */}
            <dl className="rounded-md bg-muted/50 p-3 text-sm space-y-1.5">
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t('mail.testDialog.fields.host')}</dt>
                <dd className="font-mono text-xs">{testConfig?.host}:{testConfig?.port}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t('mail.testDialog.fields.username')}</dt>
                <dd className="font-mono text-xs">{testConfig?.username}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t('mail.testDialog.fields.from')}</dt>
                <dd className="text-xs">{testConfig?.fromName} &lt;{testConfig?.fromEmail}&gt;</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t('mail.testDialog.fields.sslTls')}</dt>
                <dd className="text-xs">{testConfig?.enableSsl ? t('mail.testDialog.enabled') : t('mail.testDialog.disabled')}</dd>
              </div>
            </dl>

            {/* Password missing warning */}
            {!testConfig?.password && (
              <div className="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-300">
                {t('mail.testDialog.passwordMissingWarning')}
              </div>
            )}

            {/* Recipient */}
            <div className="space-y-1.5">
              <Label htmlFor="testRecipient">{t('mail.testDialog.sendTo')}</Label>
              <Input
                id="testRecipient"
                type="email"
                autoComplete="email"
                value={recipientEmail}
                onChange={(e) => setRecipientEmail(e.target.value)}
                placeholder={t('mail.testDialog.sendToPlaceholder')}
              />
            </div>

            {/* Result */}
            {testResult && (
              <div className={cn(
                'rounded-md px-3 py-2 text-sm',
                testResult.success
                  ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                  : 'bg-red-50 text-red-800 dark:bg-red-950 dark:text-red-300',
              )}>
                {testResult.success
                  ? t('mail.testDialog.resultSuccess')
                  : t('mail.testDialog.resultFailed', { error: testResult.errorMessage })}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsTestOpen(false)}>
              {t('mail.testDialog.close')}
            </Button>
            <Button
              onClick={handleSendTest}
              disabled={sendTest.isPending || !recipientEmail || !testConfig?.password}
            >
              {sendTest.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              {t('mail.testDialog.sendButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
