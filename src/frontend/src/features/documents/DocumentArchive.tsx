import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useEntity } from '@/hooks/useEntity';
import { useDocuments } from '@/hooks/useDocuments';
import type { DocumentStatus } from '@/types/document';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import StatusBadge from '@/components/shared/StatusBadge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { formatCurrency } from '@/lib/format';
import { Upload, Search, Download, Eye } from 'lucide-react';

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  uploaded: 'info',
  processing: 'warning',
  extracted: 'success',
  booked: 'success',
  failed: 'destructive',
};

export function Component() {
  const { selectedEntityId } = useEntity();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<DocumentStatus | 'all'>('all');
  const [search, setSearch] = useState('');
  const pageSize = 20;

  const { data, isLoading } = useDocuments({
    entityId: selectedEntityId ?? '',
    page,
    pageSize,
    status: status === 'all' ? undefined : status,
    search: search || undefined,
  });

  const columns = [
    {
      key: 'fileName',
      header: 'File Name',
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.fileName ?? '')}</span>
      ),
    },
    {
      key: 'vendorName',
      header: 'Vendor',
      render: (item: Record<string, unknown>) =>
        String(item.vendorName ?? '-'),
    },
    {
      key: 'invoiceNumber',
      header: 'Invoice #',
      render: (item: Record<string, unknown>) =>
        String(item.invoiceNumber ?? '-'),
    },
    {
      key: 'invoiceDate',
      header: 'Date',
      render: (item: Record<string, unknown>) =>
        String(item.invoiceDate ?? '-'),
    },
    {
      key: 'totalAmount',
      header: 'Amount',
      render: (item: Record<string, unknown>) => {
        const amount = item.totalAmount as number | undefined;
        return amount != null ? formatCurrency(amount) : '-';
      },
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
      key: 'actions',
      header: 'Actions',
      render: (_item: Record<string, unknown>) => (
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" title="View">
            <Eye className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" title="Download">
            <Download className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Documents"
        actions={
          <Link to="/documents/upload">
            <Button>
              <Upload className="mr-1 h-4 w-4" />
              Upload
            </Button>
          </Link>
        }
      />

      {/* Filters */}
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
        <Select
          value={status}
          onValueChange={(v) => {
            setStatus(v as DocumentStatus | 'all');
            setPage(1);
          }}
        >
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="uploaded">Uploaded</SelectItem>
            <SelectItem value="processing">Processing</SelectItem>
            <SelectItem value="extracted">Extracted</SelectItem>
            <SelectItem value="booked">Booked</SelectItem>
            <SelectItem value="failed">Failed</SelectItem>
          </SelectContent>
        </Select>
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search documents..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="pl-9"
          />
        </div>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={(data?.items ?? []) as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage="No documents found."
        pagination={{
          page,
          pageSize,
          total: data?.totalCount ?? 0,
          onPageChange: setPage,
        }}
      />
    </div>
  );
}
