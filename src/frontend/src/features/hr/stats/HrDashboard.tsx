import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useHeadcountStats, useTurnoverStats, useSalaryBands, useDepartments } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const { selectedEntityId } = useEntity();
  const entityId = selectedEntityId ?? '';
  const [departmentId, setDepartmentId] = useState('');

  const { data: departments } = useDepartments(entityId || undefined);
  const { data: headcount, isLoading: hcLoading } = useHeadcountStats(entityId, departmentId || undefined);
  const { data: turnover, isLoading: toLoading } = useTurnoverStats(entityId, departmentId || undefined);
  const { data: salaryBands, isLoading: sbLoading } = useSalaryBands(entityId, departmentId || undefined);

  function formatCents(cents: number | null | undefined): string {
    if (cents == null) return '—';
    return (cents / 100).toLocaleString(i18n.language, { style: 'currency', currency: 'EUR' });
  }

  if (!selectedEntityId) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('stats.noEntitySelected')}
      </div>
    );
  }

  return (
    <div>
      <PageHeader title={t('stats.title')} />

      {/* Department filter */}
      <div className="mb-4">
        <Select
          value={departmentId || 'all'}
          onValueChange={(v) => setDepartmentId(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-60">
            <SelectValue placeholder={t('stats.filterByDepartment')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('stats.allDepartments')}</SelectItem>
            {(departments ?? []).filter(d => d.isActive).map((dept) => (
              <SelectItem key={dept.id} value={dept.id}>{dept.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* KPI cards */}
      <div className="mb-6 grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalActive ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">{t('stats.kpi.activeEmployees')}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalEmployees ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">{t('stats.kpi.permanentEmployees')}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalContractors ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">{t('stats.kpi.contractors')}</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-2 gap-6 mb-6">
        {/* Headcount trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('stats.headcountTrend')}</CardTitle>
          </CardHeader>
          <CardContent>
            {hcLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : (
              <ResponsiveContainer width="100%" height={200}>
                <LineChart data={headcount?.monthlyTrend ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Line type="monotone" dataKey="count" stroke="#6366f1" dot={false} strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Turnover */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('stats.turnoverTrend')}</CardTitle>
          </CardHeader>
          <CardContent>
            {toLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : (
              <ResponsiveContainer width="100%" height={200}>
                <BarChart data={turnover?.monthlyTurnover ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="newHires" name={t('stats.chartHires')} fill="#34d399" />
                  <Bar dataKey="terminations" name={t('stats.chartTerminations')} fill="#f87171" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Salary summary */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('stats.salaryOverview')}</CardTitle>
          </CardHeader>
          <CardContent>
            {sbLoading ? (
              <Skeleton className="h-32 w-full" />
            ) : (
              <dl className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">{t('stats.salaryMin')}</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.minSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">{t('stats.salaryMax')}</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.maxSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">{t('stats.salaryAvg')}</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.avgSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">{t('stats.salaryMedian')}</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.medianSalaryCents)}</dd>
                </div>
              </dl>
            )}
          </CardContent>
        </Card>

        {/* Salary bands */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('stats.salaryDistribution')}</CardTitle>
          </CardHeader>
          <CardContent>
            {sbLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : (
              <ResponsiveContainer width="100%" height={200}>
                <BarChart data={salaryBands?.bands ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="count" name={t('stats.chartEmployees')} fill="#6366f1" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
