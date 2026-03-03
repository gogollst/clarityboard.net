import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useBudgets, usePlanVsActual, useCreateBudget } from '@/hooks/useBudget';
import PageHeader from '@/components/shared/PageHeader';
import EmptyState from '@/components/shared/EmptyState';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import BarChart from '@/components/charts/BarChart';
import { formatCurrency, formatPercent } from '@/lib/format';
import { cn } from '@/lib/utils';
import { Plus, Loader2, Wallet } from 'lucide-react';

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  draft: 'default',
  approved: 'info',
  active: 'success',
  closed: 'warning',
};

const currentYear = new Date().getFullYear();
const YEARS = Array.from({ length: 5 }, (_, i) => currentYear - 2 + i);

export function Component() {
  const { t } = useTranslation('budget');
  const { selectedEntityId } = useEntity();
  const [year, setYear] = useState(currentYear);
  const [selectedBudgetId, setSelectedBudgetId] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [newBudget, setNewBudget] = useState({ name: '', year: currentYear });

  const { data: budgets, isLoading: budgetsLoading } = useBudgets(
    selectedEntityId,
    year,
  );
  const { data: planVsActual, isLoading: pvaLoading } =
    usePlanVsActual(selectedBudgetId);
  const createBudget = useCreateBudget();

  const handleCreate = () => {
    if (!selectedEntityId) return;
    createBudget.mutate(
      {
        entityId: selectedEntityId,
        name: newBudget.name,
        year: newBudget.year,
        lines: [],
      },
      {
        onSuccess: () => {
          setIsDialogOpen(false);
          setNewBudget({ name: '', year: currentYear });
        },
      },
    );
  };

  const varianceChartData =
    planVsActual?.lines.map((line) => ({
      account: line.accountName,
      [t('chart.planned')]: line.planned,
      [t('chart.actual')]: line.actual,
    })) ?? [];

  return (
    <div>
      <PageHeader
        title={t('title')}
        actions={
          <Button onClick={() => setIsDialogOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            {t('newBudget')}
          </Button>
        }
      />

      {/* Year Filter */}
      <div className="mb-4">
        <Select
          value={String(year)}
          onValueChange={(v) => {
            setYear(Number(v));
            setSelectedBudgetId(null);
          }}
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {YEARS.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Budget List */}
      {budgetsLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full" />
          ))}
        </div>
      ) : !budgets || budgets.length === 0 ? (
        <EmptyState
          icon={Wallet}
          title={t('noBudgets')}
          description={t('noBudgetsDescription', { year })}
          action={{
            label: t('newBudget'),
            onClick: () => setIsDialogOpen(true),
          }}
        />
      ) : (
        <div className="space-y-3">
          {budgets.map((budget) => (
            <Card
              key={budget.id}
              className={cn(
                'cursor-pointer transition-shadow hover:shadow-md',
                selectedBudgetId === budget.id && 'ring-2 ring-primary',
              )}
              onClick={() =>
                setSelectedBudgetId(
                  selectedBudgetId === budget.id ? null : budget.id,
                )
              }
            >
              <CardContent className="flex items-center justify-between py-4">
                <div>
                  <p className="font-medium">{budget.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {budget.year}
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-lg font-semibold">
                    {formatCurrency(budget.totalAmount)}
                  </span>
                  <StatusBadge
                    status={budget.status}
                    variantMap={STATUS_VARIANT_MAP}
                  />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Plan vs Actual */}
      {selectedBudgetId && (
        <div className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>{t('planVsActual')}</CardTitle>
            </CardHeader>
            <CardContent>
              {pvaLoading ? (
                <Skeleton className="h-64 w-full" />
              ) : planVsActual ? (
                <>
                  {/* Variance Bar Chart */}
                  <BarChart
                    data={varianceChartData}
                    categories={[t('chart.planned'), t('chart.actual')]}
                    index="account"
                    colors={['#3b82f6', '#10b981']}
                    valueFormatter={(v) => formatCurrency(v)}
                  />

                  {/* Table */}
                  <div className="mt-6 overflow-x-auto">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>{t('columns.account')}</TableHead>
                          <TableHead className="text-right">{t('columns.planned')}</TableHead>
                          <TableHead className="text-right">{t('columns.actual')}</TableHead>
                          <TableHead className="text-right">
                            {t('columns.variance')}
                          </TableHead>
                          <TableHead className="text-right">
                            {t('columns.variancePct')}
                          </TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {planVsActual.lines.map((line) => {
                          const overBudget = line.variance < 0;
                          return (
                            <TableRow key={line.accountNumber}>
                              <TableCell className="font-medium">
                                {line.accountName}
                              </TableCell>
                              <TableCell className="text-right">
                                {formatCurrency(line.planned)}
                              </TableCell>
                              <TableCell className="text-right">
                                {formatCurrency(line.actual)}
                              </TableCell>
                              <TableCell
                                className={cn(
                                  'text-right',
                                  overBudget
                                    ? 'text-red-600'
                                    : 'text-green-600',
                                )}
                              >
                                {formatCurrency(line.variance)}
                              </TableCell>
                              <TableCell
                                className={cn(
                                  'text-right',
                                  overBudget
                                    ? 'text-red-600'
                                    : 'text-green-600',
                                )}
                              >
                                {formatPercent(line.variancePct)}
                              </TableCell>
                            </TableRow>
                          );
                        })}

                        {/* Totals */}
                        <TableRow className="border-t-2 font-bold">
                          <TableCell>{t('columns.total')}</TableCell>
                          <TableCell className="text-right">
                            {formatCurrency(planVsActual.totalPlanned)}
                          </TableCell>
                          <TableCell className="text-right">
                            {formatCurrency(planVsActual.totalActual)}
                          </TableCell>
                          <TableCell
                            className={cn(
                              'text-right',
                              planVsActual.totalVariance < 0
                                ? 'text-red-600'
                                : 'text-green-600',
                            )}
                          >
                            {formatCurrency(planVsActual.totalVariance)}
                          </TableCell>
                          <TableCell
                            className={cn(
                              'text-right',
                              planVsActual.totalVariance < 0
                                ? 'text-red-600'
                                : 'text-green-600',
                            )}
                          >
                            {planVsActual.totalPlanned !== 0
                              ? formatPercent(
                                  (planVsActual.totalVariance /
                                    planVsActual.totalPlanned) *
                                    100,
                                )
                              : '-'}
                          </TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </div>
                </>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {t('noDataAvailable')}
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* New Budget Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('dialog.createTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>{t('dialog.budgetName')}</Label>
              <Input
                value={newBudget.name}
                onChange={(e) =>
                  setNewBudget((b) => ({ ...b, name: e.target.value }))
                }
                placeholder={t('dialog.budgetNamePlaceholder')}
              />
            </div>
            <div>
              <Label>{t('dialog.year')}</Label>
              <Select
                value={String(newBudget.year)}
                onValueChange={(v) =>
                  setNewBudget((b) => ({ ...b, year: Number(v) }))
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {YEARS.map((y) => (
                    <SelectItem key={y} value={String(y)}>
                      {y}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
            >
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button onClick={handleCreate} disabled={createBudget.isPending}>
              {createBudget.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('common:buttons.create', { ns: 'common' })}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
