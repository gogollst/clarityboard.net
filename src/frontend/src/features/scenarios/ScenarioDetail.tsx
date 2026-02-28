import { useParams } from 'react-router-dom';
import { useScenario, useRunScenario } from '@/hooks/useScenarios';
import { useKpiDefinitions } from '@/hooks/useKpis';
import type { ScenarioType } from '@/types/scenario';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { formatCurrency, formatPercent, formatNumber } from '@/lib/format';
import { cn } from '@/lib/utils';
import { Loader2, Play } from 'lucide-react';

const TYPE_COLORS: Record<ScenarioType, string> = {
  best_case: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  worst_case: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  custom: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  stress_test: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
};

const TYPE_LABELS: Record<ScenarioType, string> = {
  best_case: 'Best Case',
  worst_case: 'Worst Case',
  custom: 'Custom',
  stress_test: 'Stress Test',
};

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  draft: 'default',
  running: 'warning',
  completed: 'success',
};

export function Component() {
  const { id } = useParams<{ id: string }>();
  const { data: scenario, isLoading } = useScenario(id ?? null);
  const { data: kpiDefs } = useKpiDefinitions();
  const runScenario = useRunScenario();

  const kpiNameMap = new Map(kpiDefs?.map((k) => [k.id, k]) ?? []);

  const handleRun = () => {
    if (!id) return;
    runScenario.mutate({ id });
  };

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        Scenario not found.
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={scenario.name}
        actions={
          <div className="flex items-center gap-2">
            <Badge
              variant="secondary"
              className={TYPE_COLORS[scenario.type]}
            >
              {TYPE_LABELS[scenario.type]}
            </Badge>
            <StatusBadge
              status={scenario.status}
              variantMap={STATUS_VARIANT_MAP}
            />
            {scenario.status === 'draft' && (
              <Button onClick={handleRun} disabled={runScenario.isPending}>
                {runScenario.isPending ? (
                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                ) : (
                  <Play className="mr-1 h-4 w-4" />
                )}
                Run Scenario
              </Button>
            )}
          </div>
        }
      />

      {/* Parameters */}
      <Card>
        <CardHeader>
          <CardTitle>Parameters</CardTitle>
        </CardHeader>
        <CardContent>
          {scenario.parameters.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No parameters configured.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>KPI</TableHead>
                  <TableHead>Adjustment Type</TableHead>
                  <TableHead className="text-right">Value</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {scenario.parameters.map((param, idx) => (
                  <TableRow key={idx}>
                    <TableCell className="font-medium">
                      {kpiNameMap.get(param.kpiId)?.name ?? param.kpiId}
                    </TableCell>
                    <TableCell className="capitalize">
                      {param.adjustmentType}
                    </TableCell>
                    <TableCell className="text-right">
                      {param.adjustmentType === 'percentage'
                        ? `${param.adjustmentValue}%`
                        : formatNumber(param.adjustmentValue)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Results (only when completed) */}
      {scenario.status === 'completed' && scenario.results && (
        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Results Comparison</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>KPI Name</TableHead>
                  <TableHead className="text-right">Baseline</TableHead>
                  <TableHead className="text-right">Projected</TableHead>
                  <TableHead className="text-right">Delta</TableHead>
                  <TableHead className="text-right">Delta %</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {scenario.results.map((result) => {
                  const kpi = kpiNameMap.get(result.kpiId);
                  const deltaPct =
                    result.baselineValue !== 0
                      ? (result.delta / result.baselineValue) * 100
                      : 0;
                  const isImprovement = kpi
                    ? kpi.direction === 'higher_better'
                      ? result.delta > 0
                      : kpi.direction === 'lower_better'
                        ? result.delta < 0
                        : Math.abs(result.delta) < Math.abs(result.baselineValue * 0.05)
                    : result.delta > 0;

                  return (
                    <TableRow key={result.kpiId}>
                      <TableCell className="font-medium">
                        {kpi?.name ?? result.kpiId}
                      </TableCell>
                      <TableCell className="text-right">
                        {kpi?.unit === 'currency'
                          ? formatCurrency(result.baselineValue)
                          : kpi?.unit === 'percentage'
                            ? formatPercent(result.baselineValue)
                            : formatNumber(result.baselineValue)}
                      </TableCell>
                      <TableCell className="text-right">
                        {kpi?.unit === 'currency'
                          ? formatCurrency(result.projectedValue)
                          : kpi?.unit === 'percentage'
                            ? formatPercent(result.projectedValue)
                            : formatNumber(result.projectedValue)}
                      </TableCell>
                      <TableCell
                        className={cn(
                          'text-right',
                          isImprovement
                            ? 'text-green-600'
                            : 'text-red-600',
                        )}
                      >
                        {kpi?.unit === 'currency'
                          ? formatCurrency(result.delta)
                          : formatNumber(result.delta)}
                      </TableCell>
                      <TableCell
                        className={cn(
                          'text-right',
                          isImprovement
                            ? 'text-green-600'
                            : 'text-red-600',
                        )}
                      >
                        {deltaPct >= 0 ? '+' : ''}
                        {formatPercent(deltaPct)}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {scenario.status === 'running' && (
        <Card className="mt-6">
          <CardContent className="flex items-center justify-center py-12">
            <Loader2 className="mr-2 h-5 w-5 animate-spin text-muted-foreground" />
            <span className="text-muted-foreground">
              Scenario is running...
            </span>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
