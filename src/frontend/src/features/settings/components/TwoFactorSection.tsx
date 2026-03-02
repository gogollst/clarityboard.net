import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, ShieldCheck, ShieldOff } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useProfile, useDisable2FA } from '@/hooks/useSettings';
import { api } from '@/lib/api';

// ── Schemas ────────────────────────────────────────────────────────────

const disableSchema = z.object({
  password: z.string().min(1, 'Password is required'),
});

type DisableFormValues = z.infer<typeof disableSchema>;

const verifySchema = z.object({
  totpCode: z
    .string()
    .length(6, 'Code must be 6 digits')
    .regex(/^\d{6}$/, 'Code must contain only digits'),
});

type VerifyFormValues = z.infer<typeof verifySchema>;

// ── Component ──────────────────────────────────────────────────────────

export function TwoFactorSection() {
  const { data: profile } = useProfile();
  const disable2FA = useDisable2FA();

  // Setup flow state
  const [showSetupDialog, setShowSetupDialog] = useState(false);
  const [setupData, setSetupData] = useState<{
    qrCodeUri: string;
    secret: string;
    recoveryCodes: string[];
  } | null>(null);
  const [isSettingUp, setIsSettingUp] = useState(false);

  // Disable flow state
  const [showDisableDialog, setShowDisableDialog] = useState(false);

  const disableForm = useForm<DisableFormValues>({
    resolver: zodResolver(disableSchema),
    defaultValues: { password: '' },
  });

  const verifyForm = useForm<VerifyFormValues>({
    resolver: zodResolver(verifySchema),
    defaultValues: { totpCode: '' },
  });

  const twoFactorEnabled = profile?.twoFactorEnabled ?? false;

  // ── Enable flow ──────────────────────────────────────────────────────

  const handleSetup = async () => {
    setIsSettingUp(true);
    try {
      const { data } = await api.post<{
        qrCodeUri: string;
        secret: string;
        recoveryCodes: string[];
      }>('/auth/2fa/setup');
      setSetupData(data);
      setShowSetupDialog(true);
    } catch {
      toast.error('Failed to initialize 2FA setup');
    } finally {
      setIsSettingUp(false);
    }
  };

  const handleVerify = async (values: VerifyFormValues) => {
    try {
      const { data } = await api.post<{ verified: boolean }>('/auth/2fa/verify', {
        totpCode: values.totpCode,
      });
      if (data.verified) {
        toast.success('Two-factor authentication enabled');
        setShowSetupDialog(false);
        setSetupData(null);
        verifyForm.reset();
      } else {
        toast.error('Invalid verification code');
      }
    } catch {
      toast.error('Failed to verify code');
    }
  };

  // ── Disable flow ─────────────────────────────────────────────────────

  const handleDisable = async (values: DisableFormValues) => {
    await disable2FA.mutateAsync({ password: values.password });
    setShowDisableDialog(false);
    disableForm.reset();
  };

  // ── Render ───────────────────────────────────────────────────────────

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Two-Factor Authentication</CardTitle>
          <CardDescription>
            Add an extra layer of security to your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {twoFactorEnabled ? (
                <ShieldCheck className="h-5 w-5 text-green-600" />
              ) : (
                <ShieldOff className="h-5 w-5 text-muted-foreground" />
              )}
              <div>
                <p className="font-medium">
                  Authenticator App
                </p>
                <p className="text-sm text-muted-foreground">
                  Use an authenticator app to generate one-time codes
                </p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Badge variant={twoFactorEnabled ? 'default' : 'secondary'}>
                {twoFactorEnabled ? 'Enabled' : 'Disabled'}
              </Badge>

              {twoFactorEnabled ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowDisableDialog(true)}
                >
                  Disable
                </Button>
              ) : (
                <Button
                  size="sm"
                  disabled={isSettingUp}
                  onClick={handleSetup}
                >
                  {isSettingUp && <Loader2 className="h-4 w-4 animate-spin" />}
                  Enable
                </Button>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* ── Setup Dialog ──────────────────────────────────────────────── */}

      <Dialog open={showSetupDialog} onOpenChange={(open) => {
        if (!open) {
          setShowSetupDialog(false);
          setSetupData(null);
          verifyForm.reset();
        }
      }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Set Up Two-Factor Authentication</DialogTitle>
            <DialogDescription>
              Scan the QR code with your authenticator app, then enter the verification code.
            </DialogDescription>
          </DialogHeader>

          {setupData && (
            <div className="space-y-4">
              <div className="flex justify-center">
                <img
                  src={setupData.qrCodeUri}
                  alt="2FA QR Code"
                  className="h-48 w-48"
                />
              </div>

              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">
                  Manual entry key
                </Label>
                <p className="rounded-md bg-muted p-2 text-center font-mono text-sm select-all">
                  {setupData.secret}
                </p>
              </div>

              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">
                  Recovery codes (save these somewhere safe)
                </Label>
                <div className="rounded-md bg-muted p-2 font-mono text-xs">
                  {setupData.recoveryCodes.map((code) => (
                    <div key={code}>{code}</div>
                  ))}
                </div>
              </div>

              <form onSubmit={verifyForm.handleSubmit(handleVerify)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="totpCode">Verification Code</Label>
                  <Input
                    id="totpCode"
                    type="text"
                    inputMode="numeric"
                    maxLength={6}
                    placeholder="000000"
                    autoComplete="one-time-code"
                    className="text-center text-lg tracking-widest"
                    {...verifyForm.register('totpCode')}
                  />
                  {verifyForm.formState.errors.totpCode && (
                    <p className="text-sm text-destructive">
                      {verifyForm.formState.errors.totpCode.message}
                    </p>
                  )}
                </div>

                <DialogFooter>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setShowSetupDialog(false);
                      setSetupData(null);
                      verifyForm.reset();
                    }}
                  >
                    Cancel
                  </Button>
                  <Button type="submit">Verify &amp; Enable</Button>
                </DialogFooter>
              </form>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* ── Disable Dialog ────────────────────────────────────────────── */}

      <Dialog open={showDisableDialog} onOpenChange={(open) => {
        if (!open) {
          setShowDisableDialog(false);
          disableForm.reset();
        }
      }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Disable Two-Factor Authentication</DialogTitle>
            <DialogDescription>
              Enter your password to confirm disabling two-factor authentication.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={disableForm.handleSubmit(handleDisable)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="disablePassword">Password</Label>
              <Input
                id="disablePassword"
                type="password"
                autoComplete="current-password"
                {...disableForm.register('password')}
              />
              {disableForm.formState.errors.password && (
                <p className="text-sm text-destructive">
                  {disableForm.formState.errors.password.message}
                </p>
              )}
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setShowDisableDialog(false);
                  disableForm.reset();
                }}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={disable2FA.isPending}
              >
                {disable2FA.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                Disable 2FA
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
