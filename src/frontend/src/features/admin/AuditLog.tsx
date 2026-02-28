import { useState } from 'react';
import { useAuditLogs, useExportAuditLogs } from '@/hooks/useAdmin';
import type { AuditLogParams } from '@/types/admin';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
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
import { Download, Search, Loader2 } from 'lucide-react';

const ACTION_TYPES = [
  'all',
  'create',
  'update',
  'delete',
  'login',
  'logout',
  'export',
  'approve',
];

export function Component() {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [userSearch, setUserSearch] = useState('');
  const [actionType, setActionType] = useState('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const params: AuditLogParams = {
    page,
    pageSize,
    userId: userSearch || undefined,
    action: actionType === 'all' ? undefined : actionType,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  };

  const { data, isLoading } = useAuditLogs(params);
  const exportLogs = useExportAuditLogs();

  const handleExport = () => {
    exportLogs.mutate({
      userId: userSearch || undefined,
      action: actionType === 'all' ? undefined : actionType,
      startDate: startDate || undefined,
      endDate: endDate || undefined,
    });
  };

  const items = Array.isArray(data) ? data : data?.items ?? [];
  const totalCount = (data as { totalCount?: number } | undefined)?.totalCount ?? 0;

  const columns = [
    {
      key: 'timestamp',
      header: 'Timestamp',
      render: (item: Record<string, unknown>) => {
        const ts = item.timestamp as string;
        return ts
          ? new Date(ts).toLocaleString('de-DE', {
              dateStyle: 'short',
              timeStyle: 'medium',
            })
          : '-';
      },
    },
    {
      key: 'userName',
      header: 'User',
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.userName ?? '')}</span>
      ),
    },
    {
      key: 'action',
      header: 'Action',
      render: (item: Record<string, unknown>) => (
        <span className="capitalize">{String(item.action ?? '')}</span>
      ),
    },
    {
      key: 'entityType',
      header: 'Entity Type',
    },
    {
      key: 'entityId',
      header: 'Entity ID',
      render: (item: Record<string, unknown>) => (
        <span className="font-mono text-xs">
          {String(item.entityId ?? '-')}
        </span>
      ),
    },
    {
      key: 'ipAddress',
      header: 'IP Address',
      render: (item: Record<string, unknown>) => (
        <span className="font-mono text-xs">
          {String(item.ipAddress ?? '')}
        </span>
      ),
    },
    {
      key: 'details',
      header: 'Details',
      render: (item: Record<string, unknown>) => (
        <span className="max-w-[200px] truncate text-xs text-muted-foreground">
          {String(item.details ?? '')}
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Audit Log"
        actions={
          <Button
            variant="outline"
            onClick={handleExport}
            disabled={exportLogs.isPending}
          >
            {exportLogs.isPending ? (
              <Loader2 className="mr-1 h-4 w-4 animate-spin" />
            ) : (
              <Download className="mr-1 h-4 w-4" />
            )}
            Export CSV
          </Button>
        }
      />

      {/* Filters */}
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search by user..."
            value={userSearch}
            onChange={(e) => {
              setUserSearch(e.target.value);
              setPage(1);
            }}
            className="pl-9"
          />
        </div>
        <div>
          <Select
            value={actionType}
            onValueChange={(v) => {
              setActionType(v);
              setPage(1);
            }}
          >
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="Action type" />
            </SelectTrigger>
            <SelectContent>
              {ACTION_TYPES.map((type) => (
                <SelectItem key={type} value={type}>
                  <span className="capitalize">
                    {type === 'all' ? 'All Actions' : type}
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div>
          <Label className="text-xs">From</Label>
          <Input
            type="date"
            value={startDate}
            onChange={(e) => {
              setStartDate(e.target.value);
              setPage(1);
            }}
            className="w-[150px]"
          />
        </div>
        <div>
          <Label className="text-xs">To</Label>
          <Input
            type="date"
            value={endDate}
            onChange={(e) => {
              setEndDate(e.target.value);
              setPage(1);
            }}
            className="w-[150px]"
          />
        </div>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={items as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage="No audit log entries found."
        pagination={{
          page,
          pageSize,
          total: totalCount,
          onPageChange: setPage,
        }}
      />
    </div>
  );
}
