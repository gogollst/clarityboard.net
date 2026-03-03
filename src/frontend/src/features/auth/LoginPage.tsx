import { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Loader2, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/hooks/useAuth';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
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

// ---------------------------------------------------------------------------
// LoginPage
// ---------------------------------------------------------------------------

export default function LoginPage() {
  const navigate = useNavigate();
  const { login, verify2FA } = useAuth();
  const { t } = useTranslation(['auth', 'validation']);

  const loginSchema = useMemo(
    () =>
      z.object({
        email: z
          .string()
          .min(1, t('validation:email.required'))
          .email(t('validation:email.invalid')),
        password: z.string().min(1, t('validation:password.required')),
      }),
    [t],
  );

  const totpSchema = useMemo(
    () =>
      z.object({
        totpCode: z
          .string()
          .length(6, t('validation:code.length', { count: 6 }))
          .regex(/^\d{6}$/, t('validation:code.onlyDigits')),
      }),
    [t],
  );

  type LoginFormValues = z.infer<typeof loginSchema>;
  type TotpFormValues = z.infer<typeof totpSchema>;

  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // 2FA state
  const [show2FADialog, setShow2FADialog] = useState(false);
  const [challengeToken, setChallengeToken] = useState<string | null>(null);
  const [is2FASubmitting, setIs2FASubmitting] = useState(false);
  const [twoFAError, setTwoFAError] = useState<string | null>(null);

  // Login form
  const loginForm = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  // 2FA form
  const totpForm = useForm<TotpFormValues>({
    resolver: zodResolver(totpSchema),
    defaultValues: { totpCode: '' },
  });

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  const onLoginSubmit = async (values: LoginFormValues) => {
    setError(null);
    setIsSubmitting(true);

    try {
      const result = await login(values.email, values.password, rememberMe);

      if (result.requires2FA) {
        setChallengeToken(result.challengeToken ?? null);
        setShow2FADialog(true);
      } else {
        navigate('/', { replace: true });
      }
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : t('auth:login.invalidCredentials');
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const on2FASubmit = async (values: TotpFormValues) => {
    if (!challengeToken) return;

    setTwoFAError(null);
    setIs2FASubmitting(true);

    try {
      await verify2FA(challengeToken, values.totpCode);
      setShow2FADialog(false);
      navigate('/', { replace: true });
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : t('auth:login.invalidCode');
      setTwoFAError(message);
    } finally {
      setIs2FASubmitting(false);
    }
  };

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <>
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">{t('auth:login.title')}</CardTitle>
          <CardDescription>{t('auth:login.subtitle')}</CardDescription>
        </CardHeader>

        <CardContent>
          <form
            onSubmit={loginForm.handleSubmit(onLoginSubmit)}
            className="space-y-4"
          >
            {/* Email */}
            <div className="space-y-2">
              <Label htmlFor="email">{t('auth:login.email')}</Label>
              <Input
                id="email"
                type="email"
                placeholder={t('auth:login.emailPlaceholder')}
                autoComplete="email"
                {...loginForm.register('email')}
              />
              {loginForm.formState.errors.email && (
                <p className="text-sm text-destructive">
                  {loginForm.formState.errors.email.message}
                </p>
              )}
            </div>

            {/* Password */}
            <div className="space-y-2">
              <Label htmlFor="password">{t('auth:login.password')}</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder={t('auth:login.passwordPlaceholder')}
                  autoComplete="current-password"
                  className="pr-10"
                  {...loginForm.register('password')}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                  onClick={() => setShowPassword((prev) => !prev)}
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <Eye className="h-4 w-4 text-muted-foreground" />
                  )}
                  <span className="sr-only">
                    {showPassword
                      ? t('auth:login.hidePassword')
                      : t('auth:login.showPassword')}
                  </span>
                </Button>
              </div>
              {loginForm.formState.errors.password && (
                <p className="text-sm text-destructive">
                  {loginForm.formState.errors.password.message}
                </p>
              )}
            </div>

            {/* Remember me + Forgot password */}
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                  className="h-4 w-4 rounded border-border accent-primary"
                />
                <span className="text-sm text-muted-foreground">
                  {t('auth:login.rememberMe')}
                </span>
              </label>
              <Link
                to="/forgot-password"
                className="text-sm text-muted-foreground hover:text-primary transition-colors"
              >
                {t('auth:login.forgotPassword')}
              </Link>
            </div>

            {/* Error message */}
            {error && (
              <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </p>
            )}

            {/* Submit */}
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {t('auth:login.signIn')}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* ------------------------------------------------------------------- */}
      {/* 2FA Challenge Dialog                                                 */}
      {/* ------------------------------------------------------------------- */}

      <Dialog open={show2FADialog} onOpenChange={setShow2FADialog}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShieldCheck className="h-5 w-5 text-primary" />
              {t('auth:login.twoFactor.title')}
            </DialogTitle>
            <DialogDescription>
              {t('auth:login.twoFactor.description')}
            </DialogDescription>
          </DialogHeader>

          <form
            onSubmit={totpForm.handleSubmit(on2FASubmit)}
            className="space-y-4"
          >
            <div className="space-y-2">
              <Label htmlFor="totpCode">
                {t('auth:login.twoFactor.verificationCode')}
              </Label>
              <Input
                id="totpCode"
                type="text"
                inputMode="numeric"
                maxLength={6}
                placeholder={t('auth:login.twoFactor.codePlaceholder')}
                autoComplete="one-time-code"
                className="text-center text-lg tracking-widest"
                {...totpForm.register('totpCode')}
              />
              {totpForm.formState.errors.totpCode && (
                <p className="text-sm text-destructive">
                  {totpForm.formState.errors.totpCode.message}
                </p>
              )}
            </div>

            {twoFAError && (
              <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {twoFAError}
              </p>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setShow2FADialog(false);
                  totpForm.reset();
                  setTwoFAError(null);
                }}
              >
                {t('auth:login.twoFactor.cancel')}
              </Button>
              <Button type="submit" disabled={is2FASubmitting}>
                {is2FASubmitting && (
                  <Loader2 className="h-4 w-4 animate-spin" />
                )}
                {t('auth:login.twoFactor.verify')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
