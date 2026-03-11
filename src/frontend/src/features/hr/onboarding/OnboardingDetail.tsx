import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useOnboardingChecklist,
  useCompleteOnboardingTask,
  useReopenOnboardingTask,
  useAddOnboardingTask,
} from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Checkbox } from '@/components/ui/checkbox';
import { ArrowLeft, Plus } from 'lucide-react';

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
// Add Task Dialog
// ---------------------------------------------------------------------------

function AddTaskDialog({ checklistId, employeeId }: { checklistId: string; employeeId: string }) {
  const { t } = useTranslation('hr');
  const addTask = useAddOnboardingTask(checklistId, employeeId);
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [dueDate, setDueDate] = useState('');

  function handleSubmit() {
    if (!title.trim()) return;
    addTask.mutate(
      {
        title:       title.trim(),
        description: description.trim() || undefined,
        dueDate:     dueDate || undefined,
        sortOrder:   0,
      },
      {
        onSuccess: () => {
          setOpen(false);
          setTitle('');
          setDescription('');
          setDueDate('');
        },
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm" variant="outline">
          <Plus className="mr-1 h-4 w-4" />
          {t('onboarding.addTask')}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('onboarding.addTask')}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <label className="text-sm font-medium">{t('onboarding.taskTitle')}</label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={t('onboarding.taskTitle')}
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium">{t('onboarding.taskDescription')}</label>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={t('onboarding.taskDescription')}
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium">{t('onboarding.dueDate')}</label>
            <Input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            {t('common:buttons.cancel')}
          </Button>
          <Button onClick={handleSubmit} disabled={!title.trim() || addTask.isPending}>
            {t('common:buttons.add')}
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
  const { t, i18n } = useTranslation('hr');
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const { data: checklist, isLoading } = useOnboardingChecklist(id ?? '');
  const complete = useCompleteOnboardingTask(id ?? '', checklist?.employeeId ?? '');
  const reopen  = useReopenOnboardingTask(id ?? '', checklist?.employeeId ?? '');

  function formatDate(iso: string | undefined) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (!checklist) return null;

  const completedCount = checklist.tasks.filter((t) => t.isCompleted).length;
  const pct = checklist.tasks.length > 0
    ? Math.round((completedCount / checklist.tasks.length) * 100)
    : 0;

  return (
    <div className="space-y-6">
      <PageHeader
        title={checklist.title}
        actions={
          <div className="flex items-center gap-2">
            <AddTaskDialog checklistId={checklist.id} employeeId={checklist.employeeId} />
            <Button size="sm" variant="ghost" onClick={() => navigate('/hr/onboarding')}>
              <ArrowLeft className="mr-1 h-4 w-4" />
              {t('onboarding.back')}
            </Button>
          </div>
        }
      />

      {/* Progress overview */}
      <Card>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">
              {t('onboarding.progress', {
                completed: completedCount,
                total:     checklist.tasks.length,
              })}
            </CardTitle>
            <StatusBadge status={checklist.status} />
          </div>
        </CardHeader>
        <CardContent>
          <Progress value={pct} className="h-2" />
        </CardContent>
      </Card>

      {/* Task list */}
      <Card>
        <CardContent className="divide-y p-0">
          {checklist.tasks.length === 0 && (
            <p className="py-8 text-center text-sm text-muted-foreground">
              {t('common:table.noResults')}
            </p>
          )}
          {checklist.tasks.map((task) => (
            <div key={task.id} className="flex items-start gap-3 px-4 py-3">
              <Checkbox
                checked={task.isCompleted}
                onCheckedChange={(checked) => {
                  if (checked) {
                    complete.mutate(task.id);
                  } else {
                    reopen.mutate(task.id);
                  }
                }}
                className="mt-0.5"
              />
              <div className="min-w-0 flex-1">
                <p
                  className={`text-sm font-medium leading-snug ${
                    task.isCompleted ? 'text-muted-foreground line-through' : ''
                  }`}
                >
                  {task.title}
                </p>
                {task.description && (
                  <p className="mt-0.5 text-xs text-muted-foreground">{task.description}</p>
                )}
                {task.dueDate && (
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {t('onboarding.dueDate')}: {formatDate(task.dueDate)}
                  </p>
                )}
              </div>
              {task.isCompleted && task.completedAt && (
                <span className="shrink-0 text-xs text-muted-foreground">
                  {formatDate(task.completedAt)}
                </span>
              )}
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
