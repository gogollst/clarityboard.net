import { useState } from 'react';
import { useEntity } from '@/hooks/useEntity';
import { useTriggerExport, useDatevExports } from '@/hooks/useDatev';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Download, Loader2 } from 'lucide-react';

const MONTHS = [
  { value: '01', label: 'January' },
  { value: '02', label: 'February' },
  { value: '03', label: 'March' },
  { value: '04', label: 'April' },
  { value: '05', label: 'May' },
  { value: '06', label: 'June' },
  { value: '07', label: 'July' },
  { value: '08', label: 'August' },
  { value: '09', label: 'September' },
  { value: '10', label: 'October' },
  { value: '11', label: 'November' },
  { value: '12', label: 'December' },
];

const YEARS = Array.from({ length: 5 }, (_, i) => {
  const year = new Date().getFullYear() - i;
  return { value: String(year), label: String(year) };
});

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  pending: 'warning',
  completed: 'success',
  failed: 'destructive',
};

export function Component() {
  const { selectedEntityId } = useEntity();
  const { data: exports, isLoading } = useDatevExports(selectedEntityId);
  const triggerExport = useTriggerExport();

  const [startMonth, setStartMonth] = useState('01');
  const [startYear, setStartYear] = useState(String(new Date().getFullYear()));
  const [endMonth, setEndMonth] = useState(
    String(new Date().getMonth() + 1).padStart(2, '0'),
  );
  const [endYear, setEndYear] = useState(String(new Date().getFullYear()));

  const handleExport = () => {
    if (!selectedEntityId) return;
    triggerExport.mutate({
      entityId: selectedEntityId,
      startDate: `${startYear}-${startMonth}-01`,
      endDate: `${endYear}-${endMonth}-01`,
    });
  };

  return (
    <div>
      <PageHeader
        title="DATEV Export"
        description="Export accounting data in DATEV format"
      />

      {/* Export Form */}
      <Card>
        <CardHeader>
          <CardTitle>New Export</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <div className="flex gap-2">
              <div>
                <Label>Start Month</Label>
                <Select value={startMonth} onValueChange={setStartMonth}>
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MONTHS.map((m) => (
                      <SelectItem key={m.value} value={m.value}>
                        {m.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Start Year</Label>
                <Select value={startYear} onValueChange={setStartYear}>
                  <SelectTrigger className="w-[100px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {YEARS.map((y) => (
                      <SelectItem key={y.value} value={y.value}>
                        {y.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="flex gap-2">
              <div>
                <Label>End Month</Label>
                <Select value={endMonth} onValueChange={setEndMonth}>
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MONTHS.map((m) => (
                      <SelectItem key={m.value} value={m.value}>
                        {m.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>End Year</Label>
                <Select value={endYear} onValueChange={setEndYear}>
                  <SelectTrigger className="w-[100px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {YEARS.map((y) => (
                      <SelectItem key={y.value} value={y.value}>
                        {y.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <Button
              onClick={handleExport}
              disabled={triggerExport.isPending}
            >
              {triggerExport.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Export
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Export History */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>Export History</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Period</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>File Name</TableHead>
                  <TableHead>Created At</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {exports && exports.length > 0 ? (
                  exports.map((exp) => (
                    <TableRow key={exp.id}>
                      <TableCell className="font-medium">
                        {exp.startDate} - {exp.endDate}
                      </TableCell>
                      <TableCell>
                        <StatusBadge
                          status={exp.status}
                          variantMap={STATUS_VARIANT_MAP}
                        />
                      </TableCell>
                      <TableCell>{exp.fileName ?? '-'}</TableCell>
                      <TableCell>
                        {new Date(exp.createdAt).toLocaleDateString('de-DE')}
                      </TableCell>
                      <TableCell>
                        {exp.status === 'completed' && (
                          <Button variant="ghost" size="sm" title="Download">
                            <Download className="h-4 w-4" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell
                      colSpan={5}
                      className="text-center text-muted-foreground"
                    >
                      No exports yet.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
