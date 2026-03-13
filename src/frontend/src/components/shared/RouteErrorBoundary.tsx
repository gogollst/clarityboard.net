import { useRouteError, isRouteErrorResponse, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';

export default function RouteErrorBoundary() {
  const error = useRouteError();
  const navigate = useNavigate();
  const { t } = useTranslation('common');

  let title = t('error.unexpectedTitle', 'Something went wrong');
  let description = t('error.unexpectedDescription', 'An unexpected error occurred. Please try again.');

  if (isRouteErrorResponse(error)) {
    if (error.status === 404) {
      title = t('error.notFoundTitle', 'Page not found');
      description = t('error.notFoundDescription', 'The page you are looking for does not exist.');
    } else {
      title = `${error.status} – ${error.statusText}`;
      description = error.data?.message ?? description;
    }
  }

  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-destructive/10">
        <AlertTriangle className="h-7 w-7 text-destructive" />
      </div>
      <h3 className="mt-4 text-lg font-semibold">{title}</h3>
      <p className="mt-1 max-w-sm text-sm text-muted-foreground">{description}</p>
      <div className="mt-4 flex gap-2">
        <Button variant="outline" onClick={() => navigate(-1)}>
          {t('error.goBack', 'Go back')}
        </Button>
        <Button onClick={() => navigate('/')}>
          {t('error.goHome', 'Go to dashboard')}
        </Button>
      </div>
    </div>
  );
}
