import { useState } from 'react';
import { useTranslation } from 'react-i18next';
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
  const { t, i18n } = useTranslation('admin');
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
      header: t('audit.columns.timestamp'),
      render: (item: Record<string, unknown>) => {
        const ts = item.timestamp as string;
        return ts
          ? new Date(ts).toLocaleString(i18n.language, {
              dateStyle: 'short',
              timeStyle: 'medium',
            })
          : '-';
      },
    },
    {
      key: 'userName',
      header: t('audit.columns.user'),
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.userName ?? '')}</span>
      ),
    },
    {
      key: 'action',
      header: t('audit.columns.action'),
      render: (item: Record<string, unknown>) => (
        <span className="capitalize">{String(item.action ?? '')}</span>
      ),
    },
    {
      key: 'entityType',
      header: t('audit.columns.entityType'),
    },
    {
      key: 'entityId',
      header: t('audit.columns.entityId'),
      render: (item: Record<string, unknown>) => (
        <span className="font-mono text-xs">
          {String(item.entityId ?? '-')}
        </span>
      ),
    },
    {
      key: 'ipAddress',
      header: t('audit.columns.ipAddress'),
      render: (item: Record<string, unknown>) => (
        <span className="font-mono text-xs">
          {String(item.ipAddress ?? '')}
        </span>
      ),
    },
    {
      key: 'details',
      header: t('audit.columns.details'),
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
        title={t('audit.title')}
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
            {t('audit.exportCsv')}
          </Button>
        }
      />

      {/* Filters */}
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={t('audit.filters.searchByUser')}
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
              <SelectValue placeholder={t('audit.filters.actionType')} />
            </SelectTrigger>
            <SelectContent>
              {ACTION_TYPES.map((type) => (
                <SelectItem key={type} value={type}>
                  <span className="capitalize">
                    {type === 'all' ? t('audit.filters.allActions') : type}
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div>
          <Label className="text-xs">{t('audit.filters.from')}</Label>
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
          <Label className="text-xs">{t('audit.filters.to')}</Label>
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
        emptyMessage={t('audit.noEntries')}
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
