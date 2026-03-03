import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import {
  useCashFlowOverview,
  useCashFlowWorkingCapital,
  useCreateCashFlowEntry,
} from '@/hooks/useCashFlow';
import PageHeader from '@/components/shared/PageHeader';
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
import { formatCurrency, formatDays } from '@/lib/format';
import { Plus, ChevronRight, Loader2 } from 'lucide-react';

export function Component() {
  const { t } = useTranslation('cashflow');
  const { selectedEntityId } = useEntity();
  const { data: overview, isLoading: overviewLoading } =
    useCashFlowOverview(selectedEntityId);
  const { data: workingCapital, isLoading: wcLoading } =
    useCashFlowWorkingCapital(selectedEntityId);
  const createEntry = useCreateCashFlowEntry();

  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [entryForm, setEntryForm] = useState({
    description: '',
    amount: '',
    entryDate: '',
    category: 'operating',
    certainty: 'confirmed',
  });

  const handleCreateEntry = () => {
    if (!selectedEntityId) return;
    createEntry.mutate(
      {
        entityId: selectedEntityId,
        description: entryForm.description,
        amount: Number(entryForm.amount),
        entryDate: entryForm.entryDate,
        category: entryForm.category,
        certainty: entryForm.certainty,
      },
      {
        onSuccess: () => {
          setIsDialogOpen(false);
          setEntryForm({
            description: '',
            amount: '',
            entryDate: '',
            category: 'operating',
            certainty: 'confirmed',
          });
        },
      },
    );
  };

  const summaryCards = [
    { label: t('overview.operatingCF'), value: overview?.operatingCashFlow },
    { label: t('overview.investingCF'), value: overview?.investingCashFlow },
    { label: t('overview.financingCF'), value: overview?.financingCashFlow },
    { label: t('overview.netCF'), value: overview?.netCashFlow },
  ];

  const waterfallData = overview
    ? [
        { name: t('overview.waterfallBarOperating'), value: overview.operatingCashFlow },
        { name: t('overview.waterfallBarInvesting'), value: overview.investingCashFlow },
        { name: t('overview.waterfallBarFinancing'), value: overview.financingCashFlow },
        { name: t('overview.waterfallBarNet'), value: overview.netCashFlow },
      ]
    : [];

  const wcMetrics = workingCapital
    ? [
        { label: t('workingCapital.dso'), value: formatDays(workingCapital.dso) },
        { label: t('workingCapital.dio'), value: formatDays(workingCapital.dio) },
        { label: t('workingCapital.dpo'), value: formatDays(workingCapital.dpo) },
        { label: t('workingCapital.ccc'), value: formatDays(workingCapital.ccc) },
      ]
    : [];

  return (
    <div>
      <PageHeader
        title={t('title')}
        actions={
          <div className="flex items-center gap-2">
            <Link to="/cashflow/forecast">
              <Button variant="outline">
                {t('actions.forecast')}
                <ChevronRight className="ml-1 h-4 w-4" />
              </Button>
            </Link>
            <Button onClick={() => setIsDialogOpen(true)}>
              <Plus className="mr-1 h-4 w-4" />
              {t('actions.manualEntry')}
            </Button>
          </div>
        }
      />

      {/* Summary Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {summaryCards.map((card) => (
          <Card key={card.label}>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {card.label}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {overviewLoading ? (
                <Skeleton className="h-7 w-28" />
              ) : (
                <p className="text-2xl font-bold">
                  {formatCurrency(card.value ?? 0)}
                </p>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Cash Flow Waterfall */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>{t('overview.waterfallCard')}</CardTitle>
        </CardHeader>
        <CardContent>
          {overviewLoading ? (
            <Skeleton className="h-72 w-full" />
          ) : (
            <BarChart
              data={waterfallData}
              categories={['value']}
              index="name"
              valueFormatter={(v) => formatCurrency(v)}
              showLegend={false}
            />
          )}
        </CardContent>
      </Card>

      {/* Working Capital Metrics */}
      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {wcLoading
          ? Array.from({ length: 4 }).map((_, i) => (
              <Card key={i}>
                <CardContent className="pt-6">
                  <Skeleton className="mb-2 h-4 w-12" />
                  <Skeleton className="h-7 w-20" />
                </CardContent>
              </Card>
            ))
          : wcMetrics.map((m) => (
              <Card key={m.label}>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    {m.label}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-2xl font-bold">{m.value}</p>
                </CardContent>
              </Card>
            ))}
      </div>

      {/* Aging Buckets */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>{t('overview.agingCard')}</CardTitle>
        </CardHeader>
        <CardContent>
          {wcLoading ? (
            <Skeleton className="h-40 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('overview.agingRange')}</TableHead>
                  <TableHead className="text-right">{t('overview.agingAmount')}</TableHead>
                  <TableHead className="text-right">{t('overview.agingCount')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {workingCapital?.agingBuckets.map((bucket) => (
                  <TableRow key={bucket.range}>
                    <TableCell className="font-medium">
                      {bucket.range}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(bucket.amount)}
                    </TableCell>
                    <TableCell className="text-right">{bucket.count}</TableCell>
                  </TableRow>
                ))}
                {(!workingCapital ||
                  workingCapital.agingBuckets.length === 0) && (
                  <TableRow>
                    <TableCell
                      colSpan={3}
                      className="text-center text-muted-foreground"
                    >
                      {t('overview.noAgingData')}
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Manual Entry Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('entry.dialogTitle')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>{t('entry.description')}</Label>
              <Input
                value={entryForm.description}
                onChange={(e) =>
                  setEntryForm((f) => ({
                    ...f,
                    description: e.target.value,
                  }))
                }
                placeholder={t('entry.descriptionPlaceholder')}
              />
            </div>
            <div>
              <Label>{t('entry.amount')}</Label>
              <Input
                type="number"
                value={entryForm.amount}
                onChange={(e) =>
                  setEntryForm((f) => ({ ...f, amount: e.target.value }))
                }
                placeholder="0.00"
              />
            </div>
            <div>
              <Label>{t('entry.date')}</Label>
              <Input
                type="date"
                value={entryForm.entryDate}
                onChange={(e) =>
                  setEntryForm((f) => ({ ...f, entryDate: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>{t('entry.category')}</Label>
              <Select
                value={entryForm.category}
                onValueChange={(v) =>
                  setEntryForm((f) => ({ ...f, category: v }))
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="operating">{t('entry.categoryOperating')}</SelectItem>
                  <SelectItem value="investing">{t('entry.categoryInvesting')}</SelectItem>
                  <SelectItem value="financing">{t('entry.categoryFinancing')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>{t('entry.certainty')}</Label>
              <Select
                value={entryForm.certainty}
                onValueChange={(v) =>
                  setEntryForm((f) => ({ ...f, certainty: v }))
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="confirmed">{t('entry.certaintyConfirmed')}</SelectItem>
                  <SelectItem value="expected">{t('entry.certaintyExpected')}</SelectItem>
                  <SelectItem value="planned">{t('entry.certaintyPlanned')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
            >
              {t('common:buttons.cancel')}
            </Button>
            <Button
              onClick={handleCreateEntry}
              disabled={createEntry.isPending}
            >
              {createEntry.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('common:buttons.create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
