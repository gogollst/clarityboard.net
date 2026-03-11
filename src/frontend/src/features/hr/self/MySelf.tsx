import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyEmployee, useLeaveRequests, useTravelExpenses, useWorkTime } from '@/hooks/useHr';
import type { WorkTimeEntry } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Calendar, Clock, Plane, UserCircle } from 'lucide-react';

// ---------------------------------------------------------------------------
// Status badge helper
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: string }) {
  const { t } = useTranslation('hr');
  const variants: Record<string, string> = {
    Active:     'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300',
    OnLeave:    'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300',
    Terminated: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  };
  return (
    <Badge className={variants[status] ?? ''}>
      {t(`employees.status.${status}`, { defaultValue: status })}
    </Badge>
  );
}

function LeaveStatusBadge({ status }: { status: string }) {
  const { t } = useTranslation('hr');
  const variants: Record<string, string> = {
    Pending:   'bg-amber-100 text-amber-800',
    Approved:  'bg-emerald-100 text-emerald-800',
    Rejected:  'bg-red-100 text-red-800',
    Cancelled: 'bg-gray-100 text-gray-600',
  };
  return (
    <Badge className={variants[status] ?? ''}>
      {t(`leave.status.${status}`, { defaultValue: status })}
    </Badge>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const navigate = useNavigate();

  const { data: employee, isLoading, isError } = useMyEmployee();
  const { data: leaveData } = useLeaveRequests(
    employee ? { employeeId: employee.id } : undefined,
  );
  const { data: travelData } = useTravelExpenses(
    employee ? { employeeId: employee.id } : undefined,
  );
  const { data: worktimeData } = useWorkTime(employee?.id ?? '');

  const recentLeave    = (leaveData?.items ?? []).slice(0, 5);
  const recentTravel   = (travelData?.items ?? []).slice(0, 5);
  const recentWorktime = (worktimeData?.items ?? []).slice(0, 5);

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (isError || !employee) {
    return (
      <div>
        <PageHeader title={t('mySelf.title')} />
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-16">
            <UserCircle className="h-12 w-12 text-muted-foreground" />
            <p className="text-lg font-medium">{t('mySelf.notLinked')}</p>
            <p className="max-w-sm text-center text-sm text-muted-foreground">
              {t('mySelf.notLinkedDescription')}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title={t('mySelf.title')} />

      {/* Quick Actions */}
      <div className="flex flex-wrap gap-2">
        <Button
          size="sm"
          variant="outline"
          onClick={() => navigate('/hr/leave/requests')}
        >
          <Calendar className="mr-1 h-4 w-4" />
          {t('mySelf.requestLeave')}
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => navigate('/hr/worktime')}
        >
          <Clock className="mr-1 h-4 w-4" />
          {t('mySelf.logWorkTime')}
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => navigate('/hr/travel/new')}
        >
          <Plane className="mr-1 h-4 w-4" />
          {t('mySelf.newTravelExpense')}
        </Button>
      </div>

      {/* Profile Cards */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Personal info */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('mySelf.personalInfo')}</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('employees.fields.firstName')}
                </dt>
                <dd className="mt-1 font-medium">{employee.firstName}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('employees.fields.lastName')}
                </dt>
                <dd className="mt-1 font-medium">{employee.lastName}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('employees.fields.dateOfBirth')}
                </dt>
                <dd className="mt-1 font-medium">{formatDate(employee.dateOfBirth)}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('employees.fields.taxId')}
                </dt>
                <dd className="mt-1 font-medium">{employee.taxId || '—'}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>

        {/* Employment info */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('mySelf.employment')}</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.employeeNumber')}
                </dt>
                <dd className="mt-1 font-medium">{employee.employeeNumber}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.status')}
                </dt>
                <dd className="mt-1">
                  <StatusBadge status={employee.status} />
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.hireDate')}
                </dt>
                <dd className="mt-1 font-medium">{formatDate(employee.hireDate)}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.department')}
                </dt>
                <dd className="mt-1 font-medium">{employee.departmentName ?? '—'}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.manager')}
                </dt>
                <dd className="mt-1 font-medium">{employee.managerName ?? '—'}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {t('mySelf.employeeType')}
                </dt>
                <dd className="mt-1 font-medium">
                  {t(`employees.type.${employee.employeeType}`, { defaultValue: employee.employeeType })}
                </dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      </div>

      {/* Recent leave requests */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('mySelf.myLeaveRequests')}</CardTitle>
        </CardHeader>
        <CardContent>
          {recentLeave.length === 0 ? (
            <p className="py-4 text-center text-sm text-muted-foreground">
              {t('common:table.noResults')}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('leave.startDate')}</TableHead>
                  <TableHead>{t('leave.endDate')}</TableHead>
                  <TableHead>{t('leave.workingDays')}</TableHead>
                  <TableHead>{t('leave.status')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentLeave.map((req) => (
                  <TableRow key={req.id}>
                    <TableCell className="text-sm">{formatDate(req.startDate)}</TableCell>
                    <TableCell className="text-sm">{formatDate(req.endDate)}</TableCell>
                    <TableCell className="text-sm tabular-nums">{req.workingDays}</TableCell>
                    <TableCell>
                      <LeaveStatusBadge status={req.status} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Recent travel expenses */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('mySelf.myTravelExpenses')}</CardTitle>
        </CardHeader>
        <CardContent>
          {recentTravel.length === 0 ? (
            <p className="py-4 text-center text-sm text-muted-foreground">
              {t('common:table.noResults')}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('travel.title')}</TableHead>
                  <TableHead>{t('travel.destination')}</TableHead>
                  <TableHead>{t('travel.status')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentTravel.map((exp) => (
                  <TableRow
                    key={exp.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => navigate(`/hr/travel/${exp.id}`)}
                  >
                    <TableCell className="font-medium">{exp.title}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{exp.destination}</TableCell>
                    <TableCell className="text-sm">{exp.status}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Recent worktime */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('mySelf.myWorktime')}</CardTitle>
        </CardHeader>
        <CardContent>
          {recentWorktime.length === 0 ? (
            <p className="py-4 text-center text-sm text-muted-foreground">
              {t('common:table.noResults')}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('worktime.date')}</TableHead>
                  <TableHead className="text-right">{t('worktime.totalMinutes')}</TableHead>
                  <TableHead>{t('worktime.entryType')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentWorktime.map((entry: WorkTimeEntry) => (
                  <TableRow key={entry.id}>
                    <TableCell className="text-sm">{formatDate(entry.date)}</TableCell>
                    <TableCell className="text-right text-sm tabular-nums">
                      {Math.floor(entry.totalMinutes / 60)}h {entry.totalMinutes % 60}m
                    </TableCell>
                    <TableCell className="text-sm">{entry.entryType}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
