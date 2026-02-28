import { Link, useNavigate } from 'react-router-dom';
import { useEntity } from '@/hooks/useEntity';
import { useScenarios } from '@/hooks/useScenarios';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import type { ScenarioType } from '@/types/scenario';
import { Plus, FlaskConical } from 'lucide-react';

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
  const { selectedEntityId } = useEntity();
  const { data: scenarios, isLoading } = useScenarios(selectedEntityId);
  const navigate = useNavigate();

  return (
    <div>
      <PageHeader
        title="Scenarios"
        actions={
          <Link to="/scenarios/new">
            <Button>
              <Plus className="mr-1 h-4 w-4" />
              New Scenario
            </Button>
          </Link>
        }
      />

      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="pt-6">
                <Skeleton className="mb-2 h-5 w-40" />
                <Skeleton className="mb-4 h-4 w-20" />
                <Skeleton className="h-4 w-28" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : !scenarios || scenarios.length === 0 ? (
        <EmptyState
          icon={FlaskConical}
          title="No Scenarios"
          description="Create your first scenario to explore what-if analyses."
          action={{
            label: 'New Scenario',
            onClick: () => navigate('/scenarios/new'),
          }}
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {scenarios.map((scenario) => (
            <Link key={scenario.id} to={`/scenarios/${scenario.id}`}>
              <Card className="cursor-pointer transition-shadow hover:shadow-md">
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between">
                    <CardTitle className="text-base">
                      {scenario.name}
                    </CardTitle>
                    <StatusBadge
                      status={scenario.status}
                      variantMap={STATUS_VARIANT_MAP}
                    />
                  </div>
                </CardHeader>
                <CardContent>
                  <Badge
                    variant="secondary"
                    className={TYPE_COLORS[scenario.type]}
                  >
                    {TYPE_LABELS[scenario.type]}
                  </Badge>
                  <p className="mt-3 text-xs text-muted-foreground">
                    Created{' '}
                    {new Date(scenario.createdAt).toLocaleDateString('de-DE')}
                  </p>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
