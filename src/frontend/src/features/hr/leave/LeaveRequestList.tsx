import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useLeaveRequests,
  useApproveLeaveRequest,
  useRejectLeaveRequest,
} from '@/hooks/useHr';
import type { LeaveRequest } from '@/types/hr';
import { formatDate } from '../utils';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import { Badge } from '@/components/ui/badge';
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
import { Check, X } from 'lucide-react';

// ---------------------------------------------------------------------------
// Rejection Dialog
// ---------------------------------------------------------------------------

interface RejectDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isPending: boolean;
}

function RejectDialog({ open, onClose, onConfirm, isPending }: RejectDialogProps) {
  const { t } = useTranslation('hr');
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (!open) setReason('');
  }, [open]);

  const handleClose = () => {
    setReason('');
    onClose();
  };

  const handleConfirm = () => {
    if (!reason.trim()) return;
    onConfirm(reason.trim());
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) handleClose(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('leave.rejectDialogTitle')}</DialogTitle>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="rejectionReason">{t('leave.rejectionReason')} *</Label>
          <Input
            id="rejectionReason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t('leave.rejectionReasonPlaceholder')}
          />
        </div>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={handleClose}>
            {t('common:buttons.cancel')}
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={!reason.trim() || isPending}
            onClick={handleConfirm}
          >
            {t('common:buttons.confirm')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t } = useTranslation('hr');
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState<string | null>(null);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [status, year]);

  const { data, isLoading } = useLeaveRequests({
    status: status || undefined,
    year,
    page,
    pageSize: 25,
  });

  const approveRequest = useApproveLeaveRequest();
  const rejectRequest = useRejectLeaveRequest();

  const requests: LeaveRequest[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 25;

  const handleApprove = (id: string) => {
    approveRequest.mutate(id);
  };

  const handleRejectClick = (id: string) => {
    setSelectedRequestId(id);
    setRejectDialogOpen(true);
  };

  const handleRejectConfirm = (reason: string) => {
    if (!selectedRequestId) return;
    rejectRequest.mutate(
      { id: selectedRequestId, reason },
      {
        onSuccess: () => {
          setRejectDialogOpen(false);
          setSelectedRequestId(null);
        },
      },
    );
  };

  const handleRejectClose = () => {
    setRejectDialogOpen(false);
    setSelectedRequestId(null);
  };

  function getLeaveStatusBadge(leaveStatus: string) {
    switch (leaveStatus) {
      case 'Pending':
        return (
          <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
            {t('leave.status.Pending')}
          </Badge>
        );
      case 'Approved':
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
            {t('leave.status.Approved')}
          </Badge>
        );
      case 'Rejected':
        return (
          <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
            {t('leave.status.Rejected')}
          </Badge>
        );
      case 'Cancelled':
        return (
          <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
            {t('leave.status.Cancelled')}
          </Badge>
        );
      default:
        return <Badge variant="secondary">{leaveStatus}</Badge>;
    }
  }

  const columns = [
    {
      key: 'employeeFullName',
      header: t('leave.columns.employee'),
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">{String(item.employeeFullName ?? '')}</span>
      ),
    },
    {
      key: 'leaveTypeName',
      header: t('leave.columns.leaveType'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.leaveTypeName ?? '')}
        </span>
      ),
    },
    {
      key: 'startDate',
      header: t('leave.columns.period'),
      render: (item: Record<string, unknown>) => {
        const start = item.startDate as string | undefined;
        const end = item.endDate as string | undefined;
        if (!start || !end) return '—';
        return (
          <span className="text-sm tabular-nums">
            {formatDate(start)} – {formatDate(end)}
          </span>
        );
      },
    },
    {
      key: 'workingDays',
      header: t('leave.columns.workingDays'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm tabular-nums text-center">
          {String(item.workingDays ?? '—')}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('leave.columns.status'),
      render: (item: Record<string, unknown>) =>
        getLeaveStatusBadge(String(item.status ?? '')),
    },
    {
      key: 'requestedAt',
      header: t('leave.columns.requestedAt'),
      render: (item: Record<string, unknown>) => {
        const raw = item.requestedAt as string | undefined;
        if (!raw) return '—';
        return (
          <span className="text-sm text-muted-foreground tabular-nums">
            {formatDate(raw)}
          </span>
        );
      },
    },
    {
      key: 'actions',
      header: '',
      render: (item: Record<string, unknown>) => {
        if (String(item.status ?? '') !== 'Pending') return null;
        const id = String(item.id ?? '');
        return (
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="outline"
              className="border-emerald-500 text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-950"
              disabled={approveRequest.isPending}
              onClick={() => handleApprove(id)}
            >
              <Check className="mr-1 h-3 w-3" />
              {t('leave.actions.approve')}
            </Button>
            <Button
              size="sm"
              variant="outline"
              className="border-destructive text-destructive hover:bg-destructive/10"
              disabled={rejectRequest.isPending}
              onClick={() => handleRejectClick(id)}
            >
              <X className="mr-1 h-3 w-3" />
              {t('leave.actions.reject')}
            </Button>
          </div>
        );
      },
    },
  ];

  const currentYear = new Date().getFullYear();
  const yearOptions = [currentYear - 1, currentYear, currentYear + 1];

  return (
    <div>
      <PageHeader title={t('leave.title')} />

      {/* Filter bar */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Select
          value={status || 'all'}
          onValueChange={(v) => setStatus(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder={t('leave.allStatuses')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('leave.allStatuses')}</SelectItem>
            <SelectItem value="Pending">{t('leave.status.Pending')}</SelectItem>
            <SelectItem value="Approved">{t('leave.status.Approved')}</SelectItem>
            <SelectItem value="Rejected">{t('leave.status.Rejected')}</SelectItem>
            <SelectItem value="Cancelled">{t('leave.status.Cancelled')}</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={String(year)}
          onValueChange={(v) => setYear(Number(v))}
        >
          <SelectTrigger className="w-32">
            <SelectValue placeholder={t('leave.filterYear')} />
          </SelectTrigger>
          <SelectContent>
            {yearOptions.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={requests as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('leave.noRequests')}
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />

      <RejectDialog
        open={rejectDialogOpen}
        onClose={handleRejectClose}
        onConfirm={handleRejectConfirm}
        isPending={rejectRequest.isPending}
      />
    </div>
  );
}
