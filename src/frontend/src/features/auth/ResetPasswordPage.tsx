import { useState, useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Loader2, CheckCircle2 } from 'lucide-react';
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

// ---------------------------------------------------------------------------
// ResetPasswordPage
// ---------------------------------------------------------------------------

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const { resetPasswordViaToken } = useAuth();
  const { t } = useTranslation(['auth', 'validation']);

  const resetPasswordSchema = useMemo(
    () =>
      z
        .object({
          newPassword: z
            .string()
            .min(8, t('validation:password.minLength', { count: 8 })),
          confirmPassword: z
            .string()
            .min(1, t('validation:password.confirmRequired')),
        })
        .refine((data) => data.newPassword === data.confirmPassword, {
          message: t('validation:password.mismatch'),
          path: ['confirmPassword'],
        }),
    [t],
  );

  type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  // ---------------------------------------------------------------------------
  // Form handler
  // ---------------------------------------------------------------------------

  const onSubmit = async (values: ResetPasswordFormValues) => {
    setError(null);
    setIsSubmitting(true);

    try {
      await resetPasswordViaToken(token!, values.newPassword);
      setSuccess(true);
    } catch (err: unknown) {
      const message =
        axios.isAxiosError(err) && err.response?.data?.message
          ? String(err.response.data.message)
          : t('auth:resetPassword.tokenExpired');
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ---------------------------------------------------------------------------
  // No token — invalid link
  // ---------------------------------------------------------------------------

  if (!token) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            {t('auth:resetPassword.invalidLink')}
          </CardTitle>
          <CardDescription>
            {t('auth:resetPassword.invalidLinkDescription')}
          </CardDescription>
        </CardHeader>

        <CardContent className="text-center">
          <Link
            to="/login"
            className="text-sm text-muted-foreground hover:text-primary transition-colors"
          >
            {t('auth:resetPassword.backToLogin')}
          </Link>
        </CardContent>
      </Card>
    );
  }

  // ---------------------------------------------------------------------------
  // Success state
  // ---------------------------------------------------------------------------

  if (success) {
    return (
      <Card>
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
            <CheckCircle2 className="h-6 w-6 text-primary" />
          </div>
          <CardTitle className="text-2xl">
            {t('auth:resetPassword.success')}
          </CardTitle>
          <CardDescription>
            {t('auth:resetPassword.successSubtitle')}
          </CardDescription>
        </CardHeader>

        <CardContent className="text-center">
          <Link
            to="/login"
            className="text-sm text-muted-foreground hover:text-primary transition-colors"
          >
            {t('auth:resetPassword.goToSignIn')}
          </Link>
        </CardContent>
      </Card>
    );
  }

  // ---------------------------------------------------------------------------
  // Form state
  // ---------------------------------------------------------------------------

  return (
    <Card>
      <CardHeader className="text-center">
        <CardTitle className="text-2xl">
          {t('auth:resetPassword.title')}
        </CardTitle>
        <CardDescription>{t('auth:resetPassword.subtitle')}</CardDescription>
      </CardHeader>

      <CardContent>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          {/* New Password */}
          <div className="space-y-2">
            <Label htmlFor="newPassword">
              {t('auth:resetPassword.newPassword')}
            </Label>
            <div className="relative">
              <Input
                id="newPassword"
                type={showPassword ? 'text' : 'password'}
                placeholder={t('auth:resetPassword.newPasswordPlaceholder')}
                autoComplete="new-password"
                className="pr-10"
                {...form.register('newPassword')}
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
            {form.formState.errors.newPassword && (
              <p className="text-sm text-destructive">
                {form.formState.errors.newPassword.message}
              </p>
            )}
          </div>

          {/* Confirm Password */}
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">
              {t('auth:resetPassword.confirmPassword')}
            </Label>
            <div className="relative">
              <Input
                id="confirmPassword"
                type={showConfirm ? 'text' : 'password'}
                placeholder={t('auth:resetPassword.confirmPasswordPlaceholder')}
                autoComplete="new-password"
                className="pr-10"
                {...form.register('confirmPassword')}
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                onClick={() => setShowConfirm((prev) => !prev)}
                tabIndex={-1}
              >
                {showConfirm ? (
                  <EyeOff className="h-4 w-4 text-muted-foreground" />
                ) : (
                  <Eye className="h-4 w-4 text-muted-foreground" />
                )}
                <span className="sr-only">
                  {showConfirm
                    ? t('auth:login.hidePassword')
                    : t('auth:login.showPassword')}
                </span>
              </Button>
            </div>
            {form.formState.errors.confirmPassword && (
              <p className="text-sm text-destructive">
                {form.formState.errors.confirmPassword.message}
              </p>
            )}
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
            {t('auth:resetPassword.submit')}
          </Button>

          {/* Back link */}
          <div className="text-center">
            <Link
              to="/login"
              className="text-sm text-muted-foreground hover:text-primary transition-colors"
            >
              {t('auth:resetPassword.backToLogin')}
            </Link>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
