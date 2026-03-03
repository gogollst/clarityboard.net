import { useEffect, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { useProfile, useUpdateProfile } from '@/hooks/useSettings';
import { AvatarSection } from './AvatarSection';

const TIMEZONES = [
  'Europe/Berlin',
  'Europe/Vienna',
  'Europe/Zurich',
  'Europe/London',
  'Europe/Paris',
  'Europe/Amsterdam',
  'Europe/Brussels',
  'Europe/Madrid',
  'Europe/Rome',
  'Europe/Stockholm',
  'Europe/Warsaw',
  'Europe/Prague',
  'Europe/Helsinki',
  'Europe/Lisbon',
  'Europe/Dublin',
  'Europe/Athens',
  'Europe/Istanbul',
  'UTC',
] as const;

type ProfileFormValues = {
  firstName: string;
  lastName: string;
  bio: string | null;
  locale: 'de' | 'en';
  timezone: string;
};

export function ProfileSection() {
  const { t } = useTranslation('settings');
  const { data: profile, isLoading, dataUpdatedAt } = useProfile();
  const updateProfile = useUpdateProfile();

  const profileSchema = useMemo(
    () =>
      z.object({
        firstName: z.string().min(1, t('profile.validation.firstNameRequired')).max(100),
        lastName: z.string().min(1, t('profile.validation.lastNameRequired')).max(100),
        bio: z.string().max(500, t('profile.validation.bioMaxLength')).nullable(),
        locale: z.enum(['de', 'en']),
        timezone: z.string().min(1, t('profile.validation.timezoneRequired')),
      }),
    [t],
  );

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      bio: null,
      locale: 'de',
      timezone: 'Europe/Berlin',
    },
  });

  useEffect(() => {
    if (profile) {
      form.reset({
        firstName: profile.firstName,
        lastName: profile.lastName,
        bio: profile.bio,
        locale: profile.locale as 'de' | 'en',
        timezone: profile.timezone,
      });
    }
  }, [profile, form]);

  const onSubmit = async (values: ProfileFormValues) => {
    await updateProfile.mutateAsync(values);
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-4 w-64" />
        </CardHeader>
        <CardContent className="space-y-4">
          <Skeleton className="h-20 w-20 rounded-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-20 w-full" />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('profile.title')}</CardTitle>
        <CardDescription>{t('profile.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="mb-6">
          <AvatarSection
            avatarUrl={profile?.avatarUrl ? `${profile.avatarUrl}?v=${dataUpdatedAt}` : null}
            firstName={profile?.firstName ?? ''}
            lastName={profile?.lastName ?? ''}
          />
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="firstName">{t('profile.firstName')}</Label>
              <Input id="firstName" {...form.register('firstName')} />
              {form.formState.errors.firstName && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.firstName.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="lastName">{t('profile.lastName')}</Label>
              <Input id="lastName" {...form.register('lastName')} />
              {form.formState.errors.lastName && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.lastName.message}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="bio">{t('profile.bio')}</Label>
            <Textarea
              id="bio"
              placeholder={t('profile.bioPlaceholder')}
              rows={3}
              {...form.register('bio')}
            />
            {form.formState.errors.bio && (
              <p className="text-sm text-destructive">
                {form.formState.errors.bio.message}
              </p>
            )}
            <p className="text-xs text-muted-foreground">
              {(form.watch('bio') ?? '').length}/500
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>{t('profile.language')}</Label>
              <Select
                value={form.watch('locale')}
                onValueChange={(value) => form.setValue('locale', value as 'de' | 'en')}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="de">Deutsch</SelectItem>
                  <SelectItem value="en">English</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>{t('profile.timezone')}</Label>
              <Select
                value={form.watch('timezone')}
                onValueChange={(value) => form.setValue('timezone', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {TIMEZONES.map((tz) => (
                    <SelectItem key={tz} value={tz}>
                      {tz.replace('_', ' ')}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex justify-end pt-2">
            <Button type="submit" disabled={updateProfile.isPending}>
              {updateProfile.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {t('common:buttons.save')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
