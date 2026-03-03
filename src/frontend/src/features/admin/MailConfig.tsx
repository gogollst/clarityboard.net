import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
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

const schema = z.object({
  host: z.string().min(1, 'Host is required').max(500, 'Host must be at most 500 characters'),
  port: z.number().int().min(1, 'Port must be between 1 and 65535').max(65535, 'Port must be between 1 and 65535'),
  username: z.string().min(1, 'Username is required').max(500, 'Username must be at most 500 characters'),
  password: z.string().min(1, 'Password is required'),
  fromEmail: z.string().email('Must be a valid email address').max(256, 'From email must be at most 256 characters'),
  fromName: z.string().min(1, 'From name is required').max(256, 'From name must be at most 256 characters'),
  enableSsl: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

export function Component() {
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
          Mail Configuration
        </h1>
        <p className="text-muted-foreground text-sm mt-1">
          Configure the SMTP server for sending emails (invitations, password resets, notifications).
        </p>
      </div>

      {/* Current status card */}
      {config && (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <Server className="h-4 w-4 text-muted-foreground" />
                Current Configuration
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
                    Active
                  </>
                ) : (
                  <>
                    <AlertCircle className="mr-1 h-3 w-3" />
                    Inactive
                  </>
                )}
              </Badge>
            </div>
            <CardDescription className="text-xs">
              Last updated: {new Date(config.updatedAt).toLocaleString('de-DE')}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">Host</dt>
                <dd className="font-mono mt-0.5">{config.host}:{config.port}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">Username</dt>
                <dd className="font-mono mt-0.5">{config.username}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">From</dt>
                <dd className="mt-0.5">
                  {config.fromName} &lt;{config.fromEmail}&gt;
                </dd>
              </div>
              <div>
                <dt className="text-muted-foreground text-xs uppercase tracking-wide">SSL/TLS</dt>
                <dd className="mt-0.5 flex items-center gap-1">
                  <Shield className="h-3 w-3 text-muted-foreground" />
                  {config.enableSsl ? 'Enabled' : 'Disabled'}
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
            {config ? 'Update SMTP Settings' : 'Configure SMTP Server'}
          </CardTitle>
          <CardDescription>
            {config
              ? 'Update your SMTP configuration. You must re-enter the password to save.'
              : 'Enter your SMTP server details to enable email sending.'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Host + Port */}
            <div className="grid grid-cols-3 gap-4">
              <div className="col-span-2 space-y-1.5">
                <Label htmlFor="host">SMTP Host *</Label>
                <Input
                  id="host"
                  placeholder="smtp.example.com"
                  {...register('host')}
                />
                {errors.host && (
                  <p className="text-destructive text-xs">{errors.host.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="port">Port *</Label>
                <Input
                  id="port"
                  type="number"
                  placeholder="587"
                  {...register('port', { valueAsNumber: true })}
                />
                {errors.port && (
                  <p className="text-destructive text-xs">{errors.port.message}</p>
                )}
              </div>
            </div>

            {/* Username */}
            <div className="space-y-1.5">
              <Label htmlFor="username">Username *</Label>
              <Input
                id="username"
                autoComplete="username"
                placeholder="your-smtp-username"
                {...register('username')}
              />
              {errors.username && (
                <p className="text-destructive text-xs">{errors.username.message}</p>
              )}
            </div>

            {/* Password */}
            <div className="space-y-1.5">
              <Label htmlFor="password">Password *</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder={config ? '••••••••' : 'Enter SMTP password'}
                {...register('password')}
              />
              {errors.password && (
                <p className="text-destructive text-xs">{errors.password.message}</p>
              )}
              <p className="text-muted-foreground text-xs">
                Password is required to save — it is stored encrypted and never shown again.
              </p>
            </div>

            {/* From Email */}
            <div className="space-y-1.5">
              <Label htmlFor="fromEmail">From Email *</Label>
              <Input
                id="fromEmail"
                type="email"
                placeholder="noreply@example.com"
                {...register('fromEmail')}
              />
              {errors.fromEmail && (
                <p className="text-destructive text-xs">{errors.fromEmail.message}</p>
              )}
            </div>

            {/* From Name */}
            <div className="space-y-1.5">
              <Label htmlFor="fromName">From Name *</Label>
              <Input
                id="fromName"
                placeholder="ClarityBoard"
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
                  Enable SSL/TLS
                </Label>
                <p className="text-muted-foreground text-xs">
                  Recommended for most SMTP providers (port 465 or STARTTLS on 587).
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
                {config ? 'Save Changes' : 'Save Configuration'}
              </Button>
              <Button type="button" variant="outline" onClick={handleOpenTest}>
                <FlaskConical className="mr-2 h-4 w-4" />
                Test
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
            <DialogTitle>Send Test Email</DialogTitle>
            <DialogDescription>
              Verify your SMTP configuration by sending a test email using the current form values.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {/* Config summary */}
            <dl className="rounded-md bg-muted/50 p-3 text-sm space-y-1.5">
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Host</dt>
                <dd className="font-mono text-xs">{testConfig?.host}:{testConfig?.port}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">Username</dt>
                <dd className="font-mono text-xs">{testConfig?.username}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">From</dt>
                <dd className="text-xs">{testConfig?.fromName} &lt;{testConfig?.fromEmail}&gt;</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">SSL/TLS</dt>
                <dd className="text-xs">{testConfig?.enableSsl ? 'Enabled' : 'Disabled'}</dd>
              </div>
            </dl>

            {/* Password missing warning */}
            {!testConfig?.password && (
              <div className="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-300">
                Enter your SMTP password in the form to enable sending a test email.
              </div>
            )}

            {/* Recipient */}
            <div className="space-y-1.5">
              <Label htmlFor="testRecipient">Send to</Label>
              <Input
                id="testRecipient"
                type="email"
                autoComplete="email"
                value={recipientEmail}
                onChange={(e) => setRecipientEmail(e.target.value)}
                placeholder="admin@example.com"
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
                  ? 'Test email sent successfully!'
                  : `Failed: ${testResult.errorMessage}`}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsTestOpen(false)}>
              Close
            </Button>
            <Button
              onClick={handleSendTest}
              disabled={sendTest.isPending || !recipientEmail || !testConfig?.password}
            >
              {sendTest.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              Send Test Email
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
