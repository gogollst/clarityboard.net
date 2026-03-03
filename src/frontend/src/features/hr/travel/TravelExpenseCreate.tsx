import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useCreateTravelExpenseReport } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from '@/components/ui/card';
import { ArrowLeft, Loader2 } from 'lucide-react';

type FormValues = {
  employeeId: string;
  title: string;
  tripStartDate: string;
  tripEndDate: string;
  destination: string;
  businessPurpose: string;
};

export function Component() {
  const { t } = useTranslation('hr');
  const navigate = useNavigate();
  const createReport = useCreateTravelExpenseReport();

  const schema = useMemo(
    () =>
      z.object({
        employeeId: z.string().uuid(t('travel.validation.invalidEmployeeId')),
        title: z.string().min(1, t('travel.validation.required')),
        tripStartDate: z.string().min(1, t('travel.validation.required')),
        tripEndDate: z.string().min(1, t('travel.validation.required')),
        destination: z.string().min(1, t('travel.validation.required')),
        businessPurpose: z.string().min(1, t('travel.validation.required')),
      }),
    [t],
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      employeeId: '',
      title: '',
      tripStartDate: '',
      tripEndDate: '',
      destination: '',
      businessPurpose: '',
    },
  });

  const onSubmit = (values: FormValues) => {
    createReport.mutate(values, {
      onSuccess: (data) => {
        navigate(`/hr/travel/${data.id}`);
      },
    });
  };

  return (
    <div>
      <PageHeader
        title={t('travel.createTitle')}
        actions={
          <Button variant="outline" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            {t('common:buttons.cancel')}
          </Button>
        }
      />

      <div className="max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('travel.createCardTitle')}</CardTitle>
            <CardDescription>
              {t('travel.createCardDescription')}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
              {/* TODO: Replace with employee selector dropdown once a shared EmployeeSelect component exists */}
              {/* Mitarbeiter-ID */}
              <div className="space-y-1.5">
                <Label htmlFor="employeeId">{t('travel.fields.employeeId')} *</Label>
                <Input
                  id="employeeId"
                  placeholder={t('travel.placeholders.employeeId')}
                  {...register('employeeId')}
                />
                {errors.employeeId && (
                  <p className="text-destructive text-xs">{errors.employeeId.message}</p>
                )}
              </div>

              {/* Titel */}
              <div className="space-y-1.5">
                <Label htmlFor="title">{t('travel.fields.title')} *</Label>
                <Input
                  id="title"
                  placeholder={t('travel.placeholders.title')}
                  {...register('title')}
                />
                {errors.title && (
                  <p className="text-destructive text-xs">{errors.title.message}</p>
                )}
              </div>

              {/* Reisezeitraum */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="tripStartDate">{t('travel.fields.tripStartDate')} *</Label>
                  <Input
                    id="tripStartDate"
                    type="date"
                    {...register('tripStartDate')}
                  />
                  {errors.tripStartDate && (
                    <p className="text-destructive text-xs">{errors.tripStartDate.message}</p>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="tripEndDate">{t('travel.fields.tripEndDate')} *</Label>
                  <Input
                    id="tripEndDate"
                    type="date"
                    {...register('tripEndDate')}
                  />
                  {errors.tripEndDate && (
                    <p className="text-destructive text-xs">{errors.tripEndDate.message}</p>
                  )}
                </div>
              </div>

              {/* Ziel */}
              <div className="space-y-1.5">
                <Label htmlFor="destination">{t('travel.fields.destination')} *</Label>
                <Input
                  id="destination"
                  placeholder={t('travel.placeholders.destination')}
                  {...register('destination')}
                />
                {errors.destination && (
                  <p className="text-destructive text-xs">{errors.destination.message}</p>
                )}
              </div>

              {/* Geschäftszweck */}
              <div className="space-y-1.5">
                <Label htmlFor="businessPurpose">{t('travel.fields.businessPurpose')} *</Label>
                <Input
                  id="businessPurpose"
                  placeholder={t('travel.placeholders.businessPurpose')}
                  {...register('businessPurpose')}
                />
                {errors.businessPurpose && (
                  <p className="text-destructive text-xs">{errors.businessPurpose.message}</p>
                )}
              </div>

              {/* Actions */}
              <div className="flex gap-3 pt-2">
                <Button type="submit" disabled={createReport.isPending}>
                  {createReport.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {t('travel.createButton')}
                </Button>
                <Button type="button" variant="outline" onClick={() => navigate(-1)}>
                  {t('common:buttons.cancel')}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
