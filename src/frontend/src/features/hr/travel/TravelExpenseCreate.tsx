import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
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

const schema = z.object({
  employeeId: z.string().uuid('Ungültige Mitarbeiter-ID'),
  title: z.string().min(1, 'Pflichtfeld'),
  tripStartDate: z.string().min(1, 'Pflichtfeld'),
  tripEndDate: z.string().min(1, 'Pflichtfeld'),
  destination: z.string().min(1, 'Pflichtfeld'),
  businessPurpose: z.string().min(1, 'Pflichtfeld'),
});

type FormValues = z.infer<typeof schema>;

export function Component() {
  const navigate = useNavigate();
  const createReport = useCreateTravelExpenseReport();

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
        title="Neue Reisekostenabrechnung"
        actions={
          <Button variant="outline" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            Abbrechen
          </Button>
        }
      />

      <div className="max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Abrechnungsdaten</CardTitle>
            <CardDescription>
              Füllen Sie alle Pflichtfelder aus, um eine neue Reisekostenabrechnung anzulegen.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
              {/* TODO: Replace with employee selector dropdown once a shared EmployeeSelect component exists */}
              {/* Mitarbeiter-ID */}
              <div className="space-y-1.5">
                <Label htmlFor="employeeId">Mitarbeiter-ID *</Label>
                <Input
                  id="employeeId"
                  placeholder="UUID des Mitarbeiters"
                  {...register('employeeId')}
                />
                {errors.employeeId && (
                  <p className="text-destructive text-xs">{errors.employeeId.message}</p>
                )}
              </div>

              {/* Titel */}
              <div className="space-y-1.5">
                <Label htmlFor="title">Titel *</Label>
                <Input
                  id="title"
                  placeholder="z. B. Dienstreise Berlin – Kundentermin"
                  {...register('title')}
                />
                {errors.title && (
                  <p className="text-destructive text-xs">{errors.title.message}</p>
                )}
              </div>

              {/* Reisezeitraum */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="tripStartDate">Von *</Label>
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
                  <Label htmlFor="tripEndDate">Bis *</Label>
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
                <Label htmlFor="destination">Ziel *</Label>
                <Input
                  id="destination"
                  placeholder="z. B. Berlin, Deutschland"
                  {...register('destination')}
                />
                {errors.destination && (
                  <p className="text-destructive text-xs">{errors.destination.message}</p>
                )}
              </div>

              {/* Geschäftszweck */}
              <div className="space-y-1.5">
                <Label htmlFor="businessPurpose">Geschäftszweck *</Label>
                <Input
                  id="businessPurpose"
                  placeholder="z. B. Kundentermin, Konferenz, Projektarbeit"
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
                  Abrechnung anlegen
                </Button>
                <Button type="button" variant="outline" onClick={() => navigate(-1)}>
                  Abbrechen
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
