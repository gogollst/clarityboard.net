import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, MailCheck } from 'lucide-react';
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
// ForgotPasswordPage
// ---------------------------------------------------------------------------

export default function ForgotPasswordPage() {
  const { forgotPassword } = useAuth();
  const { t } = useTranslation(['auth', 'validation']);

  const forgotPasswordSchema = useMemo(
    () =>
      z.object({
        email: z
          .string()
          .min(1, t('validation:email.required'))
          .email(t('validation:email.invalid')),
      }),
    [t],
  );

  type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    setError(null);
    setIsSubmitting(true);

    try {
      await forgotPassword(values.email);
      setSubmitted(true);
    } catch (err: unknown) {
      const message =
        axios.isAxiosError(err) && err.response?.data?.message
          ? String(err.response.data.message)
          : t('auth:forgotPassword.genericError');
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ---------------------------------------------------------------------------
  // Success state
  // ---------------------------------------------------------------------------

  if (submitted) {
    return (
      <Card>
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
            <MailCheck className="h-6 w-6 text-primary" />
          </div>
          <CardTitle className="text-2xl">
            {t('auth:forgotPassword.successTitle')}
          </CardTitle>
          <CardDescription>
            {t('auth:forgotPassword.successMessage')}
          </CardDescription>
        </CardHeader>

        <CardContent className="text-center">
          <Link
            to="/login"
            className="text-sm text-muted-foreground hover:text-primary transition-colors"
          >
            {t('auth:forgotPassword.backToLogin')}
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
          {t('auth:forgotPassword.title')}
        </CardTitle>
        <CardDescription>{t('auth:forgotPassword.subtitle')}</CardDescription>
      </CardHeader>

      <CardContent>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          {/* Email */}
          <div className="space-y-2">
            <Label htmlFor="email">{t('auth:forgotPassword.emailLabel')}</Label>
            <Input
              id="email"
              type="email"
              placeholder={t('auth:forgotPassword.emailPlaceholder')}
              autoComplete="email"
              {...form.register('email')}
            />
            {form.formState.errors.email && (
              <p className="text-sm text-destructive">
                {form.formState.errors.email.message}
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
            {t('auth:forgotPassword.submit')}
          </Button>

          {/* Back link */}
          <div className="text-center">
            <Link
              to="/login"
              className="text-sm text-muted-foreground hover:text-primary transition-colors"
            >
              {t('auth:forgotPassword.backToLogin')}
            </Link>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
