import { useState, useMemo } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Loader2, Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAcceptInvitation } from '@/hooks/useAuth';
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
// Password requirement row
// ---------------------------------------------------------------------------

function Requirement({ met, label }: { met: boolean; label: string }) {
  return (
    <li className={`flex items-center gap-1.5 text-xs ${met ? 'text-emerald-600' : 'text-muted-foreground'}`}>
      <Check className={`h-3 w-3 ${met ? 'opacity-100' : 'opacity-20'}`} />
      {label}
    </li>
  );
}

// ---------------------------------------------------------------------------
// AcceptInvitationPage
// ---------------------------------------------------------------------------

export default function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();
  const acceptInvitation = useAcceptInvitation();
  const { t } = useTranslation(['auth', 'validation']);

  const schema = useMemo(
    () =>
      z
        .object({
          password: z
            .string()
            .min(8, t('validation:password.minLength', { count: 8 }))
            .regex(/[A-Z]/, t('validation:password.uppercase'))
            .regex(/[0-9]/, t('validation:password.number')),
          confirmPassword: z
            .string()
            .min(1, t('validation:password.confirmRequired')),
        })
        .refine((data) => data.password === data.confirmPassword, {
          message: t('validation:password.mismatch'),
          path: ['confirmPassword'],
        }),
    [t],
  );

  type FormValues = z.infer<typeof schema>;

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  const password = form.watch('password');

  const requirements = [
    { met: password.length >= 8, label: t('auth:acceptInvitation.req8Chars') },
    { met: /[A-Z]/.test(password), label: t('auth:acceptInvitation.reqUppercase') },
    { met: /[0-9]/.test(password), label: t('auth:acceptInvitation.reqNumber') },
  ];

  // ---------------------------------------------------------------------------
  // No token
  // ---------------------------------------------------------------------------

  if (!token) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            {t('auth:acceptInvitation.invalidLink')}
          </CardTitle>
          <CardDescription>
            {t('auth:acceptInvitation.invalidLinkDescription')}
          </CardDescription>
        </CardHeader>
        <CardContent className="text-center">
          <Link
            to="/login"
            className="text-sm text-muted-foreground hover:text-primary transition-colors"
          >
            {t('auth:acceptInvitation.backToLogin')}
          </Link>
        </CardContent>
      </Card>
    );
  }

  // ---------------------------------------------------------------------------
  // Form submit
  // ---------------------------------------------------------------------------

  const onSubmit = async (_values: FormValues) => {
    setError(null);
    acceptInvitation.mutate(
      { token, password: _values.password },
      {
        onSuccess: () => {
          navigate('/login', { replace: true });
        },
        onError: (err: unknown) => {
          const message =
            axios.isAxiosError(err) && err.response?.data?.message
              ? String(err.response.data.message)
              : t('auth:acceptInvitation.tokenExpired');
          setError(message);
        },
      },
    );
  };

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <Card>
      <CardHeader className="text-center">
        <CardTitle className="text-2xl">
          {t('auth:acceptInvitation.title')}
        </CardTitle>
        <CardDescription>
          {t('auth:acceptInvitation.subtitle')}
        </CardDescription>
      </CardHeader>

      <CardContent>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          {/* Password */}
          <div className="space-y-2">
            <Label htmlFor="password">
              {t('auth:acceptInvitation.password')}
            </Label>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? 'text' : 'password'}
                placeholder={t('auth:acceptInvitation.passwordPlaceholder')}
                autoComplete="new-password"
                className="pr-10"
                {...form.register('password')}
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
            {form.formState.errors.password && (
              <p className="text-sm text-destructive">
                {form.formState.errors.password.message}
              </p>
            )}

            {/* Requirements checklist */}
            <ul className="space-y-1 pt-1">
              {requirements.map((r) => (
                <Requirement key={r.label} met={r.met} label={r.label} />
              ))}
            </ul>
          </div>

          {/* Confirm password */}
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">
              {t('auth:acceptInvitation.confirmPassword')}
            </Label>
            <div className="relative">
              <Input
                id="confirmPassword"
                type={showConfirm ? 'text' : 'password'}
                placeholder={t('auth:acceptInvitation.confirmPasswordPlaceholder')}
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

          {/* Error */}
          {error && (
            <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {error}
            </p>
          )}

          {/* Submit */}
          <Button
            type="submit"
            className="w-full"
            disabled={acceptInvitation.isPending}
          >
            {acceptInvitation.isPending && (
              <Loader2 className="h-4 w-4 animate-spin" />
            )}
            {t('auth:acceptInvitation.submit')}
          </Button>

          <div className="text-center">
            <Link
              to="/login"
              className="text-sm text-muted-foreground hover:text-primary transition-colors"
            >
              {t('auth:acceptInvitation.backToLogin')}
            </Link>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
