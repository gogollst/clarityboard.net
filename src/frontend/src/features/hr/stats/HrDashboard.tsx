import { useEntity } from '@/hooks/useEntity';
import { useHeadcountStats, useTurnoverStats, useSalaryBands } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
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
  const { selectedEntityId } = useEntity();
  const entityId = selectedEntityId ?? '';

  const { data: headcount, isLoading: hcLoading } = useHeadcountStats(entityId);
  const { data: turnover, isLoading: toLoading } = useTurnoverStats(entityId);
  const { data: salaryBands, isLoading: sbLoading } = useSalaryBands(entityId);

  if (!selectedEntityId) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        Bitte wählen Sie eine Entity aus.
      </div>
    );
  }

  function formatCents(cents: number | null | undefined): string {
    if (cents == null) return '—';
    return (cents / 100).toLocaleString('de-DE', { style: 'currency', currency: 'EUR' });
  }

  return (
    <div>
      <PageHeader title="HR-Statistiken" />

      {/* KPI cards */}
      <div className="mb-6 grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalActive ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">Aktive Mitarbeiter</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalEmployees ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">Festangestellt</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold">
              {hcLoading ? <Skeleton className="h-8 w-16" /> : headcount?.totalContractors ?? 0}
            </div>
            <p className="text-sm text-muted-foreground">Contractors</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-2 gap-6 mb-6">
        {/* Headcount trend */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Headcount (letzte 12 Monate)</CardTitle>
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
            <CardTitle className="text-base">Fluktuation (letzte 12 Monate)</CardTitle>
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
                  <Bar dataKey="newHires" name="Einstellungen" fill="#34d399" />
                  <Bar dataKey="terminations" name="Kündigungen" fill="#f87171" />
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
            <CardTitle className="text-base">Gehaltsübersicht</CardTitle>
          </CardHeader>
          <CardContent>
            {sbLoading ? (
              <Skeleton className="h-32 w-full" />
            ) : (
              <dl className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Minimum</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.minSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Maximum</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.maxSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Durchschnitt</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.avgSalaryCents)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Median</dt>
                  <dd className="font-medium">{formatCents(salaryBands?.medianSalaryCents)}</dd>
                </div>
              </dl>
            )}
          </CardContent>
        </Card>

        {/* Salary bands */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Gehaltsverteilung</CardTitle>
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
                  <Bar dataKey="count" name="Mitarbeiter" fill="#6366f1" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
