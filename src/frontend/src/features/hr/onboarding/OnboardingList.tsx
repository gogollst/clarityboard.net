import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEmployees, useCreateOnboardingChecklist } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { useOnboardingChecklists } from '@/hooks/useHr';
import { Plus, ListChecks } from 'lucide-react';

// ---------------------------------------------------------------------------
// Status badge
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: string }) {
  const { t } = useTranslation('hr');
  const classes: Record<string, string> = {
    InProgress: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300',
    Completed:  'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300',
  };
  return (
    <Badge className={classes[status] ?? ''}>
      {t(`onboarding.status.${status}`, { defaultValue: status })}
    </Badge>
  );
}

// ---------------------------------------------------------------------------
// Create Checklist Dialog
// ---------------------------------------------------------------------------

function CreateChecklistDialog() {
  const { t } = useTranslation('hr');
  const { selectedEntityId } = useEntity();
  const { data: employeesData } = useEmployees(
    selectedEntityId ? { entityId: selectedEntityId, pageSize: 100 } : undefined,
  );
  const create = useCreateOnboardingChecklist();

  const [open, setOpen] = useState(false);
  const [employeeId, setEmployeeId] = useState('');
  const [title, setTitle] = useState('');

  function handleSubmit() {
    if (!employeeId || !title.trim()) return;
    create.mutate(
      { employeeId, title: title.trim() },
      {
        onSuccess: () => {
          setOpen(false);
          setEmployeeId('');
          setTitle('');
        },
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm">
          <Plus className="mr-1 h-4 w-4" />
          {t('onboarding.createChecklist')}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('onboarding.newChecklist')}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <label className="text-sm font-medium">{t('onboarding.employeeLabel')}</label>
            <Select value={employeeId} onValueChange={setEmployeeId}>
              <SelectTrigger>
                <SelectValue placeholder="—" />
              </SelectTrigger>
              <SelectContent>
                {(employeesData?.items ?? []).map((e) => (
                  <SelectItem key={e.id} value={e.id}>
                    {e.fullName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium">{t('onboarding.checklistTitle')}</label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={t('onboarding.checklistTitle')}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            {t('common:buttons.cancel')}
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!employeeId || !title.trim() || create.isPending}
          >
            {t('common:buttons.create')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t } = useTranslation('hr');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const { data: employeesData } = useEmployees(
    selectedEntityId ? { entityId: selectedEntityId, pageSize: 100 } : undefined,
  );
  const [selectedEmployee, setSelectedEmployee] = useState('');

  const { data: checklists, isLoading } = useOnboardingChecklists(selectedEmployee);

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('onboarding.title')}
        actions={<CreateChecklistDialog />}
      />

      {/* Employee filter */}
      <div className="max-w-xs">
        <Select value={selectedEmployee} onValueChange={setSelectedEmployee}>
          <SelectTrigger>
            <SelectValue placeholder={t('onboarding.employeeLabel')} />
          </SelectTrigger>
          <SelectContent>
            {(employeesData?.items ?? []).map((e) => (
              <SelectItem key={e.id} value={e.id}>
                {e.fullName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardHeader>
                <div className="h-4 w-3/4 rounded bg-muted" />
              </CardHeader>
              <CardContent>
                <div className="h-2 rounded bg-muted" />
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {!isLoading && !selectedEmployee && (
        <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
          <ListChecks className="h-10 w-10" />
          <p className="text-sm">{t('onboarding.employeeLabel')}</p>
        </div>
      )}

      {!isLoading && selectedEmployee && (checklists ?? []).length === 0 && (
        <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
          <ListChecks className="h-10 w-10" />
          <p className="text-sm">{t('onboarding.noChecklists')}</p>
        </div>
      )}

      {!isLoading && (checklists ?? []).length > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {(checklists ?? []).map((cl) => {
            const pct = cl.totalTasks > 0
              ? Math.round((cl.completedTasks / cl.totalTasks) * 100)
              : 0;
            return (
              <Card
                key={cl.id}
                className="cursor-pointer transition-shadow hover:shadow-md"
                onClick={() => navigate(`/hr/onboarding/${cl.id}`)}
              >
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between gap-2">
                    <CardTitle className="text-sm font-medium leading-snug">
                      {cl.title}
                    </CardTitle>
                    <StatusBadge status={cl.status} />
                  </div>
                </CardHeader>
                <CardContent className="space-y-2">
                  <Progress value={pct} className="h-1.5" />
                  <p className="text-xs text-muted-foreground">
                    {t('onboarding.progress', {
                      completed: cl.completedTasks,
                      total:     cl.totalTasks,
                    })}
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
