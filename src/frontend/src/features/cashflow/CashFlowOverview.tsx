import { useState } from 'react';
import { Link } from 'react-router-dom';
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
    { label: 'Operating CF', value: overview?.operatingCashFlow },
    { label: 'Investing CF', value: overview?.investingCashFlow },
    { label: 'Financing CF', value: overview?.financingCashFlow },
    { label: 'Net CF', value: overview?.netCashFlow },
  ];

  const waterfallData = overview
    ? [
        { name: 'Operating', value: overview.operatingCashFlow },
        { name: 'Investing', value: overview.investingCashFlow },
        { name: 'Financing', value: overview.financingCashFlow },
        { name: 'Net', value: overview.netCashFlow },
      ]
    : [];

  const wcMetrics = workingCapital
    ? [
        { label: 'DSO', value: formatDays(workingCapital.dso) },
        { label: 'DIO', value: formatDays(workingCapital.dio) },
        { label: 'DPO', value: formatDays(workingCapital.dpo) },
        { label: 'CCC', value: formatDays(workingCapital.ccc) },
      ]
    : [];

  return (
    <div>
      <PageHeader
        title="Cash Flow"
        actions={
          <div className="flex items-center gap-2">
            <Link to="/cashflow/forecast">
              <Button variant="outline">
                Forecast
                <ChevronRight className="ml-1 h-4 w-4" />
              </Button>
            </Link>
            <Button onClick={() => setIsDialogOpen(true)}>
              <Plus className="mr-1 h-4 w-4" />
              Manual Entry
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
          <CardTitle>Cash Flow Waterfall</CardTitle>
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
          <CardTitle>Aging Buckets</CardTitle>
        </CardHeader>
        <CardContent>
          {wcLoading ? (
            <Skeleton className="h-40 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Range</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead className="text-right">Count</TableHead>
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
                      No aging data available.
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
            <DialogTitle>Create Cash Flow Entry</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Description</Label>
              <Input
                value={entryForm.description}
                onChange={(e) =>
                  setEntryForm((f) => ({
                    ...f,
                    description: e.target.value,
                  }))
                }
                placeholder="Payment description"
              />
            </div>
            <div>
              <Label>Amount</Label>
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
              <Label>Date</Label>
              <Input
                type="date"
                value={entryForm.entryDate}
                onChange={(e) =>
                  setEntryForm((f) => ({ ...f, entryDate: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>Category</Label>
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
                  <SelectItem value="operating">Operating</SelectItem>
                  <SelectItem value="investing">Investing</SelectItem>
                  <SelectItem value="financing">Financing</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>Certainty</Label>
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
                  <SelectItem value="confirmed">Confirmed</SelectItem>
                  <SelectItem value="expected">Expected</SelectItem>
                  <SelectItem value="planned">Planned</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateEntry}
              disabled={createEntry.isPending}
            >
              {createEntry.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
