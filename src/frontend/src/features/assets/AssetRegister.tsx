import { useState } from 'react';
import { useEntity } from '@/hooks/useEntity';
import {
  useAssets,
  useAsset,
  useRegisterAsset,
  useDisposeAsset,
  useAnlagenspiegel,
} from '@/hooks/useAssets';
import type { DepreciationMethod } from '@/types/asset';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
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
import { Separator } from '@/components/ui/separator';
import LineChart from '@/components/charts/LineChart';
import { formatCurrency } from '@/lib/format';
import { Plus, Loader2, Trash2 } from 'lucide-react';

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  active: 'success',
  disposed: 'default',
  fully_depreciated: 'info',
};

const currentYear = new Date().getFullYear();
const YEARS = Array.from({ length: 5 }, (_, i) => currentYear - i);

export function Component() {
  const { selectedEntityId } = useEntity();
  const { data: assetsData, isLoading: assetsLoading } = useAssets(selectedEntityId);
  const registerAsset = useRegisterAsset();
  const disposeAsset = useDisposeAsset();

  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);
  const [isDisposeOpen, setIsDisposeOpen] = useState(false);
  const [disposeForm, setDisposeForm] = useState({
    disposalDate: '',
    disposalAmount: '',
  });
  const [registerForm, setRegisterForm] = useState({
    name: '',
    category: '',
    acquisitionDate: '',
    acquisitionCost: '',
    usefulLifeMonths: '',
    depreciationMethod: 'straight_line' as DepreciationMethod,
  });

  // Anlagenspiegel state
  const [spiegelYear, setSpiegelYear] = useState(currentYear);
  const { data: anlagenspiegel, isLoading: spiegelLoading } =
    useAnlagenspiegel(selectedEntityId, spiegelYear);

  // Selected asset detail
  const { data: selectedAsset } = useAsset(selectedAssetId);

  const assets = Array.isArray(assetsData)
    ? assetsData
    : assetsData?.items ?? [];

  const handleRegister = () => {
    if (!selectedEntityId) return;
    registerAsset.mutate(
      {
        entityId: selectedEntityId,
        name: registerForm.name,
        category: registerForm.category,
        acquisitionDate: registerForm.acquisitionDate,
        acquisitionCost: Number(registerForm.acquisitionCost),
        usefulLifeMonths: Number(registerForm.usefulLifeMonths),
        depreciationMethod: registerForm.depreciationMethod,
      },
      {
        onSuccess: () => {
          setIsRegisterOpen(false);
          setRegisterForm({
            name: '',
            category: '',
            acquisitionDate: '',
            acquisitionCost: '',
            usefulLifeMonths: '',
            depreciationMethod: 'straight_line',
          });
        },
      },
    );
  };

  const handleDispose = () => {
    if (!selectedAssetId || !selectedEntityId) return;
    disposeAsset.mutate(
      {
        request: {
          id: selectedAssetId,
          disposalDate: disposeForm.disposalDate,
          disposalAmount: Number(disposeForm.disposalAmount),
        },
        entityId: selectedEntityId,
      },
      {
        onSuccess: () => {
          setIsDisposeOpen(false);
          setIsDetailOpen(false);
          setSelectedAssetId(null);
          setDisposeForm({ disposalDate: '', disposalAmount: '' });
        },
      },
    );
  };

  const depreciationChartData =
    selectedAsset?.schedule.map((entry) => ({
      date: entry.date,
      'Book Value': entry.bookValue,
    })) ?? [];

  const columns = [
    {
      key: 'assetNumber',
      header: 'Asset #',
      render: (item: Record<string, unknown>) =>
        String(item.assetNumber ?? ''),
    },
    {
      key: 'name',
      header: 'Name',
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.name ?? '')}</span>
      ),
    },
    {
      key: 'category',
      header: 'Category',
    },
    {
      key: 'acquisitionDate',
      header: 'Acq. Date',
    },
    {
      key: 'acquisitionCost',
      header: 'Cost',
      render: (item: Record<string, unknown>) =>
        formatCurrency(item.acquisitionCost as number),
    },
    {
      key: 'currentBookValue',
      header: 'Book Value',
      render: (item: Record<string, unknown>) =>
        formatCurrency(item.currentBookValue as number),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={String(item.status ?? '')}
          variantMap={STATUS_VARIANT_MAP}
        />
      ),
    },
    {
      key: 'depreciationMethod',
      header: 'Method',
      render: (item: Record<string, unknown>) => {
        const method = String(item.depreciationMethod ?? '');
        return method === 'straight_line'
          ? 'Straight Line'
          : 'Declining Balance';
      },
    },
  ];

  return (
    <div>
      <PageHeader
        title="Fixed Assets"
        actions={
          <Button onClick={() => setIsRegisterOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            Register Asset
          </Button>
        }
      />

      {/* Asset Table */}
      <DataTable
        columns={columns}
        data={assets as unknown as Record<string, unknown>[]}
        isLoading={assetsLoading}
        emptyMessage="No assets registered."
      />

      {/* Make rows clickable - wrap table with click handler logic */}
      {!assetsLoading && assets.length > 0 && (
        <p className="mt-2 text-xs text-muted-foreground">
          Click on an asset row above to view details. (Use the table&apos;s
          rendered rows.)
        </p>
      )}

      {/* Quick action: clicking an asset */}
      {!assetsLoading && assets.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {assets.map((asset) => (
            <Button
              key={asset.id}
              variant="outline"
              size="sm"
              onClick={() => {
                setSelectedAssetId(asset.id);
                setIsDetailOpen(true);
              }}
            >
              {asset.assetNumber} - {asset.name}
            </Button>
          ))}
        </div>
      )}

      {/* Anlagenspiegel Section */}
      <Separator className="my-8" />
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Anlagenspiegel (Asset Movement Report)</CardTitle>
            <Select
              value={String(spiegelYear)}
              onValueChange={(v) => setSpiegelYear(Number(v))}
            >
              <SelectTrigger className="w-[100px]">
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
        </CardHeader>
        <CardContent>
          {spiegelLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : anlagenspiegel ? (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Category</TableHead>
                    <TableHead className="text-right">Opening Cost</TableHead>
                    <TableHead className="text-right">Additions</TableHead>
                    <TableHead className="text-right">Disposals</TableHead>
                    <TableHead className="text-right">Closing Cost</TableHead>
                    <TableHead className="text-right">Opening Depr.</TableHead>
                    <TableHead className="text-right">Charge</TableHead>
                    <TableHead className="text-right">Disp. Depr.</TableHead>
                    <TableHead className="text-right">Closing Depr.</TableHead>
                    <TableHead className="text-right">NBV</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {anlagenspiegel.categories.map((cat) => (
                    <TableRow key={cat.name}>
                      <TableCell className="font-medium">{cat.name}</TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.openingCost)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.additions)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.disposals)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.closingCost)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.openingDepreciation)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.depreciationCharge)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.disposalDepreciation)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(cat.row.closingDepreciation)}
                      </TableCell>
                      <TableCell className="text-right font-semibold">
                        {formatCurrency(cat.row.netBookValue)}
                      </TableCell>
                    </TableRow>
                  ))}
                  {/* Totals Row */}
                  <TableRow className="border-t-2 font-bold">
                    <TableCell>Total</TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.openingCost)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.additions)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.disposals)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.closingCost)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.openingDepreciation)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.depreciationCharge)}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(
                        anlagenspiegel.total.disposalDepreciation,
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(
                        anlagenspiegel.total.closingDepreciation,
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {formatCurrency(anlagenspiegel.total.netBookValue)}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              No Anlagenspiegel data available for {spiegelYear}.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Asset Detail Dialog */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {selectedAsset
                ? `${selectedAsset.assetNumber} - ${selectedAsset.name}`
                : 'Asset Detail'}
            </DialogTitle>
          </DialogHeader>
          {selectedAsset ? (
            <div className="space-y-6">
              {/* Info Card */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">Category:</span>{' '}
                  {selectedAsset.category}
                </div>
                <div>
                  <span className="text-muted-foreground">Status:</span>{' '}
                  <StatusBadge
                    status={selectedAsset.status}
                    variantMap={STATUS_VARIANT_MAP}
                  />
                </div>
                <div>
                  <span className="text-muted-foreground">
                    Acquisition Date:
                  </span>{' '}
                  {selectedAsset.acquisitionDate}
                </div>
                <div>
                  <span className="text-muted-foreground">
                    Acquisition Cost:
                  </span>{' '}
                  {formatCurrency(selectedAsset.acquisitionCost)}
                </div>
                <div>
                  <span className="text-muted-foreground">Book Value:</span>{' '}
                  {formatCurrency(selectedAsset.currentBookValue)}
                </div>
                <div>
                  <span className="text-muted-foreground">Method:</span>{' '}
                  {selectedAsset.depreciationMethod === 'straight_line'
                    ? 'Straight Line'
                    : 'Declining Balance'}
                </div>
                <div>
                  <span className="text-muted-foreground">Useful Life:</span>{' '}
                  {selectedAsset.usefulLifeMonths} months
                </div>
              </div>

              {/* Depreciation Chart */}
              {depreciationChartData.length > 0 && (
                <div>
                  <h4 className="mb-2 text-sm font-medium">
                    Book Value Over Time
                  </h4>
                  <LineChart
                    data={depreciationChartData}
                    categories={['Book Value']}
                    index="date"
                    valueFormatter={(v) => formatCurrency(v)}
                    showLegend={false}
                  />
                </div>
              )}

              {/* Depreciation Schedule */}
              {selectedAsset.schedule.length > 0 && (
                <div>
                  <h4 className="mb-2 text-sm font-medium">
                    Depreciation Schedule
                  </h4>
                  <div className="max-h-60 overflow-y-auto">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Date</TableHead>
                          <TableHead className="text-right">Amount</TableHead>
                          <TableHead className="text-right">
                            Accumulated
                          </TableHead>
                          <TableHead className="text-right">
                            Book Value
                          </TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {selectedAsset.schedule.map((entry, idx) => (
                          <TableRow key={idx}>
                            <TableCell>{entry.date}</TableCell>
                            <TableCell className="text-right">
                              {formatCurrency(entry.amount)}
                            </TableCell>
                            <TableCell className="text-right">
                              {formatCurrency(
                                entry.accumulatedDepreciation,
                              )}
                            </TableCell>
                            <TableCell className="text-right">
                              {formatCurrency(entry.bookValue)}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <Skeleton className="h-48 w-full" />
          )}
          <DialogFooter>
            {selectedAsset?.status === 'active' && (
              <Button
                variant="destructive"
                onClick={() => setIsDisposeOpen(true)}
              >
                <Trash2 className="mr-1 h-4 w-4" />
                Dispose Asset
              </Button>
            )}
            <Button
              variant="outline"
              onClick={() => setIsDetailOpen(false)}
            >
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dispose Confirmation Dialog */}
      <Dialog open={isDisposeOpen} onOpenChange={setIsDisposeOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Dispose Asset</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              This action will mark the asset as disposed and record the
              disposal transaction.
            </p>
            <div>
              <Label>Disposal Date</Label>
              <Input
                type="date"
                value={disposeForm.disposalDate}
                onChange={(e) =>
                  setDisposeForm((f) => ({
                    ...f,
                    disposalDate: e.target.value,
                  }))
                }
              />
            </div>
            <div>
              <Label>Disposal Amount</Label>
              <Input
                type="number"
                value={disposeForm.disposalAmount}
                onChange={(e) =>
                  setDisposeForm((f) => ({
                    ...f,
                    disposalAmount: e.target.value,
                  }))
                }
                placeholder="0.00"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDisposeOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDispose}
              disabled={disposeAsset.isPending}
            >
              {disposeAsset.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Confirm Disposal
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Register Asset Dialog */}
      <Dialog open={isRegisterOpen} onOpenChange={setIsRegisterOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Register Asset</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Name</Label>
              <Input
                value={registerForm.name}
                onChange={(e) =>
                  setRegisterForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder="e.g. Office Laptop"
              />
            </div>
            <div>
              <Label>Category</Label>
              <Input
                value={registerForm.category}
                onChange={(e) =>
                  setRegisterForm((f) => ({
                    ...f,
                    category: e.target.value,
                  }))
                }
                placeholder="e.g. IT Equipment"
              />
            </div>
            <div>
              <Label>Acquisition Date</Label>
              <Input
                type="date"
                value={registerForm.acquisitionDate}
                onChange={(e) =>
                  setRegisterForm((f) => ({
                    ...f,
                    acquisitionDate: e.target.value,
                  }))
                }
              />
            </div>
            <div>
              <Label>Acquisition Cost</Label>
              <Input
                type="number"
                value={registerForm.acquisitionCost}
                onChange={(e) =>
                  setRegisterForm((f) => ({
                    ...f,
                    acquisitionCost: e.target.value,
                  }))
                }
                placeholder="0.00"
              />
            </div>
            <div>
              <Label>Useful Life (months)</Label>
              <Input
                type="number"
                value={registerForm.usefulLifeMonths}
                onChange={(e) =>
                  setRegisterForm((f) => ({
                    ...f,
                    usefulLifeMonths: e.target.value,
                  }))
                }
                placeholder="e.g. 36"
              />
            </div>
            <div>
              <Label>Depreciation Method</Label>
              <Select
                value={registerForm.depreciationMethod}
                onValueChange={(v) =>
                  setRegisterForm((f) => ({
                    ...f,
                    depreciationMethod: v as DepreciationMethod,
                  }))
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="straight_line">Straight Line</SelectItem>
                  <SelectItem value="declining_balance">
                    Declining Balance
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsRegisterOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleRegister}
              disabled={registerAsset.isPending}
            >
              {registerAsset.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Register
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
