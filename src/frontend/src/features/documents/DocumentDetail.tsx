import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import {
  useDocument,
  useConfirmPartnerMatch,
  useApproveBooking,
  useModifyBooking,
  useRejectBooking,
} from '@/hooks/useDocuments';
import { useAccounts } from '@/hooks/useAccounting';
import { useSearchBusinessPartners, useAssignDocumentPartner } from '@/hooks/useAccounting';
import { useEmployees } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
import { useDebounced } from '@/hooks/useDebounced';
import { formatCurrency } from '@/lib/format';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  Check,
  ChevronDown,
  ChevronUp,
  Handshake,
  Loader2,
  Pencil,
  Plus,
  Search,
  X,
  Zap,
} from 'lucide-react';
import type { ModifyBookingRequest } from '@/types/document';

const STATUS_VARIANT_MAP: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'info'> = {
  uploaded: 'info',
  processing: 'warning',
  extracted: 'success',
  review: 'warning',
  booked: 'success',
  failed: 'destructive',
};

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value ?? '—'}</dd>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function ConfidenceBar({ confidence }: { confidence: number }) {
  const pct = Math.round(confidence * 100);
  const color =
    confidence >= 0.85
      ? 'bg-emerald-500'
      : confidence >= 0.7
        ? 'bg-amber-500'
        : 'bg-red-500';

  return (
    <div className="flex items-center gap-2">
      <div className="h-2 w-24 rounded-full bg-muted overflow-hidden">
        <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-sm font-medium">{pct}%</span>
    </div>
  );
}

export function Component() {
  const { t, i18n } = useTranslation('documents');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const { data: doc, isLoading } = useDocument(id ?? null);
  const approveBooking = useApproveBooking();
  const modifyBooking = useModifyBooking();
  const rejectBooking = useRejectBooking();
  const confirmPartnerMatch = useConfirmPartnerMatch();
  const assignPartner = useAssignDocumentPartner();

  // Account & employee data for modify form
  const { data: accounts = [] } = useAccounts(selectedEntityId);
  const { data: employeesData } = useEmployees();
  const employees = employeesData?.items ?? [];

  // Partner search state
  const [searchQuery, setSearchQuery] = useState('');
  const [showPartnerSearch, setShowPartnerSearch] = useState(false);
  const debouncedSearch = useDebounced(searchQuery, 300);
  const { data: searchResults = [] } = useSearchBusinessPartners(
    selectedEntityId,
    debouncedSearch,
  );

  // UI state
  const [showReasoning, setShowReasoning] = useState(false);
  const [showModifySheet, setShowModifySheet] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>('');

  // Modify form
  const modifyForm = useForm<ModifyBookingRequest>();

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!doc) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('detail.notFound')}
      </div>
    );
  }

  const handleConfirmPartner = () => {
    if (!doc.suggestedBusinessPartnerId || !selectedEntityId) return;
    confirmPartnerMatch.mutate({
      documentId: doc.id,
      businessPartnerId: doc.suggestedBusinessPartnerId,
      entityId: selectedEntityId,
    });
  };

  const handleAssignPartner = (partnerId: string) => {
    if (!selectedEntityId) return;
    assignPartner.mutate(
      {
        documentId: doc.id,
        businessPartnerId: partnerId,
        entityId: selectedEntityId,
      },
      {
        onSuccess: () => {
          setShowPartnerSearch(false);
          setSearchQuery('');
        },
      },
    );
  };

  const handleApproveBooking = () => {
    if (!selectedEntityId) return;
    approveBooking.mutate({
      documentId: doc.id,
      entityId: selectedEntityId,
      hrEmployeeId: selectedEmployeeId || undefined,
    });
  };

  const handleRejectBooking = () => {
    if (!selectedEntityId) return;
    rejectBooking.mutate(
      {
        documentId: doc.id,
        entityId: selectedEntityId,
        reason: rejectReason || undefined,
      },
      {
        onSuccess: () => {
          setShowRejectDialog(false);
          setRejectReason('');
        },
      },
    );
  };

  const handleOpenModify = () => {
    if (!doc.bookingSuggestion) return;
    const bs = doc.bookingSuggestion;
    modifyForm.reset({
      debitAccountId: bs.debitAccountId,
      creditAccountId: bs.creditAccountId,
      amount: bs.amount,
      vatCode: bs.vatCode ?? undefined,
      vatAmount: bs.vatAmount ?? undefined,
      description: bs.description ?? undefined,
      hrEmployeeId: bs.hrEmployeeId ?? undefined,
    });
    setShowModifySheet(true);
  };

  const handleModifySubmit = modifyForm.handleSubmit((data) => {
    if (!selectedEntityId) return;
    modifyBooking.mutate(
      {
        documentId: doc.id,
        entityId: selectedEntityId,
        ...data,
      },
      {
        onSuccess: () => setShowModifySheet(false),
      },
    );
  });

  const bs = doc.bookingSuggestion;
  const canAct =
    (doc.status === 'review' || doc.status === 'extracted') &&
    bs?.status === 'suggested';

  return (
    <div>
      <PageHeader
        title={doc.fileName}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={doc.status} variantMap={STATUS_VARIANT_MAP} />
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/documents')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              {t('detail.backToArchive')}
            </Button>
          </div>
        }
      />

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Document Info */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('detail.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-2 gap-6">
              <DetailRow label={t('detail.fields.fileName')} value={doc.fileName} />
              <DetailRow label={t('detail.fields.contentType')} value={doc.contentType} />
              <DetailRow label={t('detail.fields.fileSize')} value={formatBytes(doc.fileSize)} />
              <DetailRow label={t('detail.fields.createdAt')} value={formatDate(doc.createdAt)} />
              <DetailRow label={t('detail.fields.vendorName')} value={doc.vendorName} />
              <DetailRow label={t('detail.fields.invoiceNumber')} value={doc.invoiceNumber} />
              <DetailRow label={t('detail.fields.invoiceDate')} value={formatDate(doc.invoiceDate)} />
              <DetailRow
                label={t('detail.fields.totalAmount')}
                value={
                  doc.totalAmount != null
                    ? formatCurrency(doc.totalAmount, doc.currency ?? 'EUR')
                    : '—'
                }
              />
            </dl>

            {doc.reviewReasons && doc.reviewReasons.length > 0 && (
              <div className="mt-6 border-t pt-4">
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-2">
                  {t('detail.fields.reviewReasons')}
                </dt>
                <div className="flex flex-wrap gap-2">
                  {doc.reviewReasons.map((reason) => (
                    <Badge key={reason} variant="outline">{reason}</Badge>
                  ))}
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Partner Assignment */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('detail.partnerAssignment.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            {doc.businessPartnerId ? (
              <div className="flex items-center gap-3">
                <Handshake className="h-5 w-5 text-emerald-600" />
                <div>
                  <Link
                    to={`/accounting/business-partners/${doc.businessPartnerId}`}
                    className="font-medium text-foreground hover:underline"
                  >
                    {doc.businessPartnerNumber && (
                      <span className="text-muted-foreground mr-2">{doc.businessPartnerNumber}</span>
                    )}
                    {doc.businessPartnerName ?? doc.businessPartnerId}
                  </Link>
                </div>
              </div>
            ) : doc.suggestedBusinessPartnerId ? (
              <div className="space-y-4">
                <p className="text-sm text-amber-600 dark:text-amber-400">
                  {t('detail.partnerAssignment.fuzzyMatchHint')}
                </p>
                <div className="flex items-center gap-3 rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
                  <Handshake className="h-5 w-5 text-amber-600" />
                  <div className="flex-1">
                    <span className="text-sm font-medium">
                      {doc.suggestedBusinessPartnerNumber && (
                        <span className="text-muted-foreground mr-2">{doc.suggestedBusinessPartnerNumber}</span>
                      )}
                      {doc.suggestedBusinessPartnerName ?? doc.suggestedBusinessPartnerId}
                    </span>
                  </div>
                  <Button
                    size="sm"
                    onClick={handleConfirmPartner}
                    disabled={confirmPartnerMatch.isPending}
                  >
                    {confirmPartnerMatch.isPending ? (
                      <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                    ) : (
                      <Check className="mr-1 h-4 w-4" />
                    )}
                    {t('actions.confirmPartner')}
                  </Button>
                </div>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowPartnerSearch(!showPartnerSearch)}
                >
                  {t('actions.assignPartner')}
                </Button>
              </div>
            ) : (
              <div className="space-y-4">
                <p className="text-sm text-muted-foreground">
                  {t('detail.partnerAssignment.noPartnerAssigned')}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowPartnerSearch(!showPartnerSearch)}
                >
                  <Handshake className="mr-1 h-4 w-4" />
                  {t('actions.assignPartner')}
                </Button>
              </div>
            )}

            {showPartnerSearch && (
              <div className="mt-4 space-y-3 border-t pt-4">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    className="pl-9"
                    placeholder={t('detail.partnerAssignment.search')}
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    autoFocus
                  />
                </div>
                {searchResults.length > 0 && (
                  <ul className="max-h-48 overflow-y-auto rounded-md border">
                    {searchResults.map((partner) => (
                      <li key={partner.id}>
                        <button
                          className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm hover:bg-muted transition-colors"
                          onClick={() => handleAssignPartner(partner.id)}
                          disabled={assignPartner.isPending}
                        >
                          <span className="text-muted-foreground">{partner.partnerNumber}</span>
                          <span className="font-medium">{partner.name}</span>
                          {partner.taxId && (
                            <span className="text-xs text-muted-foreground ml-auto">{partner.taxId}</span>
                          )}
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
                <Link to="/accounting/business-partners/new">
                  <Button variant="ghost" size="sm">
                    <Plus className="mr-1 h-4 w-4" />
                    {t('actions.createNewPartner')}
                  </Button>
                </Link>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Booking Suggestion */}
        {bs && (
          <Card className="lg:col-span-2">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">{t('detail.bookingSuggestion.title')}</CardTitle>
                <div className="flex items-center gap-2">
                  {bs.isAutoBooked && (
                    <Badge variant="default" className="bg-blue-600">
                      <Zap className="mr-1 h-3 w-3" />
                      {t('detail.bookingSuggestion.autoBooked')}
                    </Badge>
                  )}
                  {bs.status === 'rejected' && (
                    <Badge variant="destructive">
                      {t('detail.bookingSuggestion.rejected')}
                    </Badge>
                  )}
                  {bs.status === 'accepted' && (
                    <Badge variant="default" className="bg-emerald-600">
                      {t('actions.approveBooking')}
                    </Badge>
                  )}
                  {bs.status === 'modified' && (
                    <Badge variant="default" className="bg-blue-600">
                      {t('actions.modifyBooking')}
                    </Badge>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-6 sm:grid-cols-4">
                <DetailRow
                  label={t('detail.bookingSuggestion.debitAccount')}
                  value={
                    bs.debitAccountNumber
                      ? `${bs.debitAccountNumber} — ${bs.debitAccountName}`
                      : bs.debitAccountId
                  }
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.creditAccount')}
                  value={
                    bs.creditAccountNumber
                      ? `${bs.creditAccountNumber} — ${bs.creditAccountName}`
                      : bs.creditAccountId
                  }
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.amount')}
                  value={formatCurrency(bs.amount)}
                />
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    {t('detail.bookingSuggestion.confidence')}
                  </dt>
                  <dd className="mt-1">
                    <ConfidenceBar confidence={bs.confidence} />
                  </dd>
                </div>
                {bs.vatCode && (
                  <DetailRow
                    label={t('detail.bookingSuggestion.vatCode')}
                    value={bs.vatCode}
                  />
                )}
                {bs.hrEmployeeName && (
                  <DetailRow
                    label={t('detail.bookingSuggestion.employee')}
                    value={bs.hrEmployeeName}
                  />
                )}
                <div className="sm:col-span-4">
                  <DetailRow
                    label={t('detail.bookingSuggestion.description')}
                    value={bs.description}
                  />
                </div>
              </dl>

              {/* Auto-booked hint */}
              {bs.isAutoBooked && (
                <p className="mt-3 text-xs text-muted-foreground">
                  {t('detail.bookingSuggestion.autoBookedHint')}
                </p>
              )}

              {/* Rejection reason */}
              {bs.status === 'rejected' && bs.rejectionReason && (
                <div className="mt-4 rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-800 dark:bg-red-950">
                  <p className="text-sm text-red-700 dark:text-red-300">
                    <span className="font-medium">{t('detail.bookingSuggestion.rejectReason')}:</span>{' '}
                    {bs.rejectionReason}
                  </p>
                </div>
              )}

              {/* AI Reasoning collapsible */}
              {bs.aiReasoning && (
                <div className="mt-4 border-t pt-4">
                  <button
                    className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
                    onClick={() => setShowReasoning(!showReasoning)}
                  >
                    {showReasoning ? (
                      <ChevronUp className="h-4 w-4" />
                    ) : (
                      <ChevronDown className="h-4 w-4" />
                    )}
                    {showReasoning
                      ? t('detail.bookingSuggestion.hideReasoning')
                      : t('detail.bookingSuggestion.showReasoning')}
                  </button>
                  {showReasoning && (
                    <pre className="mt-2 max-h-64 overflow-auto rounded-md bg-muted p-3 text-xs whitespace-pre-wrap">
                      {bs.aiReasoning}
                    </pre>
                  )}
                </div>
              )}

              {/* Actions */}
              {canAct && (
                <div className="mt-6 border-t pt-4 space-y-4">
                  {/* Optional employee select for approve */}
                  {employees.length > 0 && (
                    <div className="flex items-center gap-3">
                      <Label className="text-sm whitespace-nowrap">
                        {t('detail.bookingSuggestion.selectEmployee')}
                      </Label>
                      <Select value={selectedEmployeeId} onValueChange={setSelectedEmployeeId}>
                        <SelectTrigger className="w-64">
                          <SelectValue placeholder={t('detail.bookingSuggestion.selectEmployee')} />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="none">—</SelectItem>
                          {employees.map((emp) => (
                            <SelectItem key={emp.id} value={emp.id}>
                              {emp.employeeNumber} — {emp.fullName}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}

                  <div className="flex justify-end gap-2">
                    <Button
                      variant="outline"
                      className="text-red-600 border-red-200 hover:bg-red-50"
                      onClick={() => setShowRejectDialog(true)}
                    >
                      <X className="mr-1 h-4 w-4" />
                      {t('actions.rejectBooking')}
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={handleOpenModify}
                    >
                      <Pencil className="mr-1 h-4 w-4" />
                      {t('actions.modifyBooking')}
                    </Button>
                    <Button
                      onClick={handleApproveBooking}
                      disabled={approveBooking.isPending}
                    >
                      {approveBooking.isPending && (
                        <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                      )}
                      <Check className="mr-1 h-4 w-4" />
                      {t('actions.confirmApproval')}
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>

      {/* Reject Dialog */}
      <Dialog open={showRejectDialog} onOpenChange={setShowRejectDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('detail.bookingSuggestion.rejectTitle')}</DialogTitle>
            <DialogDescription>
              {t('detail.bookingSuggestion.rejectConfirm')}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Label>{t('detail.bookingSuggestion.rejectReason')}</Label>
            <Textarea
              className="mt-2"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              rows={3}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowRejectDialog(false)}>
              {t('actions.cancel', { ns: 'common', defaultValue: 'Cancel' })}
            </Button>
            <Button
              variant="destructive"
              onClick={handleRejectBooking}
              disabled={rejectBooking.isPending}
            >
              {rejectBooking.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('actions.rejectBooking')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Modify Sheet */}
      <Sheet open={showModifySheet} onOpenChange={setShowModifySheet}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>{t('detail.bookingSuggestion.modifyTitle')}</SheetTitle>
            <SheetDescription />
          </SheetHeader>
          <form onSubmit={handleModifySubmit} className="mt-6 space-y-4">
            <div>
              <Label>{t('detail.bookingSuggestion.debitAccount')}</Label>
              <Select
                value={modifyForm.watch('debitAccountId')}
                onValueChange={(v) => modifyForm.setValue('debitAccountId', v)}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {accounts.map((acc) => (
                    <SelectItem key={acc.id} value={acc.id}>
                      {acc.accountNumber} — {acc.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.creditAccount')}</Label>
              <Select
                value={modifyForm.watch('creditAccountId')}
                onValueChange={(v) => modifyForm.setValue('creditAccountId', v)}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {accounts.map((acc) => (
                    <SelectItem key={acc.id} value={acc.id}>
                      {acc.accountNumber} — {acc.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.amount')}</Label>
              <Input
                type="number"
                step="0.01"
                className="mt-1"
                {...modifyForm.register('amount', { valueAsNumber: true })}
              />
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.vatCode')}</Label>
              <Input className="mt-1" {...modifyForm.register('vatCode')} />
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.description')}</Label>
              <Textarea className="mt-1" rows={3} {...modifyForm.register('description')} />
            </div>

            {employees.length > 0 && (
              <div>
                <Label>{t('detail.bookingSuggestion.employee')}</Label>
                <Select
                  value={modifyForm.watch('hrEmployeeId') ?? ''}
                  onValueChange={(v) => modifyForm.setValue('hrEmployeeId', v === 'none' ? undefined : v)}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue placeholder={t('detail.bookingSuggestion.selectEmployee')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">—</SelectItem>
                    {employees.map((emp) => (
                      <SelectItem key={emp.id} value={emp.id}>
                        {emp.employeeNumber} — {emp.fullName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="flex justify-end gap-2 pt-4">
              <Button type="button" variant="outline" onClick={() => setShowModifySheet(false)}>
                {t('actions.cancel', { ns: 'common', defaultValue: 'Cancel' })}
              </Button>
              <Button type="submit" disabled={modifyBooking.isPending}>
                {modifyBooking.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
                {t('actions.modifyBooking')}
              </Button>
            </div>
          </form>
        </SheetContent>
      </Sheet>
    </div>
  );
}
