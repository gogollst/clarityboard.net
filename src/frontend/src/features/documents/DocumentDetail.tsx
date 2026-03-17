import { useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import {
  useDocument,
  useConfirmPartnerMatch,
  useApproveBooking,
  useModifyBooking,
  useRejectBooking,
  useReprocessDocument,
  useDeleteDocumentPreflight,
  useDeleteDocument,
  useRevenueSchedule,
} from '@/hooks/useDocuments';
import { useAccounts } from '@/hooks/useAccounting';
import { useSearchBusinessPartners, useAssignDocumentPartner } from '@/hooks/useAccounting';
import { useEmployees } from '@/hooks/useHr';
import { useEntity, useEntities } from '@/hooks/useEntity';
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
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
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
import { AccountCombobox } from '@/components/shared/AccountCombobox';
import { CurrencyInput } from '@/components/shared/CurrencyInput';
import { getReviewReasonDisplay } from '@/lib/reviewReasonUtils';
import {
  ArrowLeft,
  Check,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  FileText,
  Handshake,
  Loader2,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Upload,
  X,
  XCircle,
  Zap,
  AlertTriangle,
} from 'lucide-react';
import type { ModifyBookingRequest, DocumentField } from '@/types/document';
import { VAT_CODES, VAT_RATES } from '@/lib/vatCodes';

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

function getFieldValue(fields: DocumentField[] | undefined, name: string): string | undefined {
  if (!fields) return undefined;
  const field = fields.find((f) => f.fieldName === name);
  return field?.correctedValue ?? field?.fieldValue ?? undefined;
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

function FieldConfidenceBadge({ confidence }: { confidence: number }) {
  const pct = Math.round(confidence * 100);
  const variant = confidence >= 0.85 ? 'default' : confidence >= 0.7 ? 'secondary' : 'destructive';
  return <Badge variant={variant}>{pct}%</Badge>;
}

export function Component() {
  const { t, i18n } = useTranslation('documents');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const { data: doc, isLoading } = useDocument(selectedEntityId, id ?? null);
  const approveBooking = useApproveBooking();
  const modifyBooking = useModifyBooking();
  const rejectBooking = useRejectBooking();
  const confirmPartnerMatch = useConfirmPartnerMatch();
  const assignPartner = useAssignDocumentPartner();
  const reprocessDocument = useReprocessDocument();
  const deleteDocument = useDeleteDocument();
  const { data: preflight, refetch: fetchPreflight, isFetching: isPreflightLoading } = useDeleteDocumentPreflight(selectedEntityId, id ?? null);
  const { data: revenueScheduleData = [] } = useRevenueSchedule(selectedEntityId, id ?? null);

  // Entities for target entity selection
  const { data: entities = [] } = useEntities();

  // UI state
  const [showReasoning, setShowReasoning] = useState(false);
  const [showModifySheet, setShowModifySheet] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>('');
  const [showOcrPanel, setShowOcrPanel] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [targetEntityId, setTargetEntityId] = useState<string>('');
  const [showRevenueSchedule, setShowRevenueSchedule] = useState(true);

  // Account & employee data for modify form
  // Load accounts for the target entity (AI suggestion may point to a different entity)
  const modifyTargetEntityId = targetEntityId || doc?.bookingSuggestion?.suggestedEntityId || selectedEntityId;
  const { data: accounts = [] } = useAccounts(modifyTargetEntityId);
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

  // Modify form
  const modifyForm = useForm<ModifyBookingRequest>();

  // Amount breakdown state for modify sheet (UI-only, not submitted directly)
  const [modifyGross, setModifyGross] = useState(0);
  const [modifyNet, setModifyNet] = useState(0);
  const [modifyTax, setModifyTax] = useState(0);

  const amountMismatch = Math.abs(modifyGross - (modifyNet + modifyTax)) > 0.02;

  const round2 = (n: number) => Math.round(n * 100) / 100;

  const handleNetChange = useCallback((value: number) => {
    const v = round2(value);
    setModifyNet(v);
    const newGross = round2(v + modifyTax);
    setModifyGross(newGross);
    modifyForm.setValue('amount', newGross);
  }, [modifyTax, modifyForm]);

  const handleTaxChange = useCallback((value: number) => {
    const v = round2(value);
    setModifyTax(v);
    const newGross = round2(modifyNet + v);
    setModifyGross(newGross);
    modifyForm.setValue('amount', newGross);
    modifyForm.setValue('vatAmount', v);
  }, [modifyNet, modifyForm]);

  const handleVatCodeChangeInModify = useCallback((code: string | undefined) => {
    modifyForm.setValue('vatCode', code);
    if (code && VAT_RATES[code] !== undefined) {
      const rate = VAT_RATES[code];
      const newTax = round2(modifyNet * rate);
      setModifyTax(newTax);
      const newGross = round2(modifyNet + newTax);
      setModifyGross(newGross);
      modifyForm.setValue('amount', newGross);
      modifyForm.setValue('vatAmount', newTax);
    }
  }, [modifyNet, modifyForm]);

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

  // Effective target entity: user override > AI suggestion > uploaded entity
  const effectiveTargetEntityId = targetEntityId
    || doc.bookingSuggestion?.suggestedEntityId
    || selectedEntityId
    || '';

  const handleApproveBooking = () => {
    if (!selectedEntityId) return;
    approveBooking.mutate({
      documentId: doc.id,
      entityId: selectedEntityId,
      hrEmployeeId: selectedEmployeeId || undefined,
      targetEntityId: effectiveTargetEntityId && effectiveTargetEntityId !== selectedEntityId ? effectiveTargetEntityId : undefined,
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
    const gross = bs.amount;
    const tax = bs.vatAmount ?? 0;
    const net = bs.netAmount ?? gross - tax;
    modifyForm.reset({
      debitAccountId: bs.debitAccountId,
      creditAccountId: bs.creditAccountId,
      amount: gross,
      vatCode: bs.vatCode ?? undefined,
      vatAmount: tax || undefined,
      description: bs.description ?? undefined,
      hrEmployeeId: bs.hrEmployeeId ?? undefined,
    });
    setModifyGross(gross);
    setModifyNet(net);
    setModifyTax(tax);
    setShowModifySheet(true);
  };

  const handleModifySubmit = modifyForm.handleSubmit((data) => {
    if (!selectedEntityId) return;
    modifyBooking.mutate(
      {
        documentId: doc.id,
        entityId: selectedEntityId,
        ...data,
        targetEntityId: effectiveTargetEntityId && effectiveTargetEntityId !== selectedEntityId ? effectiveTargetEntityId : undefined,
      },
      {
        onSuccess: () => setShowModifySheet(false),
      },
    );
  });

  const handleReprocess = () => {
    if (!selectedEntityId) return;
    reprocessDocument.mutate({
      documentId: doc.id,
      entityId: selectedEntityId,
    });
  };

  const handleOpenDeleteDialog = async () => {
    await fetchPreflight();
    setShowDeleteDialog(true);
  };

  const handleDeleteDocument = () => {
    if (!selectedEntityId) return;
    deleteDocument.mutate(
      { documentId: doc.id, entityId: selectedEntityId },
      { onSuccess: () => navigate('/documents') },
    );
  };

  const bs = doc.bookingSuggestion;
  const canAct =
    (doc.status === 'review' || doc.status === 'extracted') &&
    bs?.status === 'suggested';
  const isProcessing = doc.status === 'uploaded' || doc.status === 'processing';
  const isFailed = doc.status === 'failed';
  const isBooked = doc.status === 'booked';

  return (
    <div>
      <PageHeader
        title={doc.fileName}
        actions={
          <div className="flex items-center gap-2">
            <Badge variant={doc.documentDirection === 'outgoing' ? 'default' : 'secondary'}>
              {t(`directions.${doc.documentDirection ?? 'incoming'}`)}
            </Badge>
            <StatusBadge status={doc.status} variantMap={STATUS_VARIANT_MAP} />
            {doc.confidence != null && (
              <ConfidenceBar confidence={doc.confidence} />
            )}
            <Button
              variant="outline"
              size="sm"
              className="text-red-600 border-red-200 hover:bg-red-50 dark:border-red-800 dark:hover:bg-red-950"
              onClick={handleOpenDeleteDialog}
            >
              <Trash2 className="mr-1 h-4 w-4" />
              {t('actions.delete')}
            </Button>
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

      {/* Processing Banner */}
      {isProcessing && (
        <Card className="mb-6 border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-950">
          <CardContent className="flex items-center gap-3 py-4">
            <Loader2 className="h-5 w-5 animate-spin text-blue-600" />
            <div>
              <p className="font-medium text-blue-700 dark:text-blue-300">
                {t('detail.processing')}
              </p>
              <p className="text-sm text-blue-600 dark:text-blue-400">
                {t('detail.processingHint')}
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Failed Banner */}
      {isFailed && (
        <Card className="mb-6 border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-950">
          <CardContent className="flex items-center justify-between py-4">
            <div className="flex items-center gap-3">
              <XCircle className="h-5 w-5 text-red-600" />
              <div>
                <p className="font-medium text-red-700 dark:text-red-300">
                  {t('detail.failedTitle')}
                </p>
                <p className="text-sm text-red-600 dark:text-red-400">
                  {t('detail.failedHint')}
                </p>
              </div>
            </div>
            <Button
              variant="outline"
              onClick={handleReprocess}
              disabled={reprocessDocument.isPending}
            >
              {reprocessDocument.isPending ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <RefreshCw className="mr-1 h-4 w-4" />
              )}
              {t('actions.reprocess')}
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Booked Success Banner */}
      {isBooked && (
        <Card className="mb-6 border-emerald-200 bg-emerald-50 dark:border-emerald-800 dark:bg-emerald-950">
          <CardContent className="flex items-center justify-between py-4">
            <div className="flex items-center gap-3">
              <CheckCircle2 className="h-5 w-5 text-emerald-600" />
              <div>
                <p className="font-medium text-emerald-700 dark:text-emerald-300">
                  {t('detail.bookedTitle')}
                </p>
                <p className="text-sm text-emerald-600 dark:text-emerald-400">
                  {t('detail.bookedHint')}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {doc.bookedJournalEntryId && (
                <Link to={`/accounting/journal-entries/${doc.bookedJournalEntryId}`}>
                  <Button variant="outline" size="sm">
                    <FileText className="mr-1 h-4 w-4" />
                    {t('detail.viewJournalEntry')}
                  </Button>
                </Link>
              )}
              <Link to="/documents/upload">
                <Button variant="outline" size="sm">
                  <Upload className="mr-1 h-4 w-4" />
                  {t('detail.uploadAnother')}
                </Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Review Reasons Panel */}
      {doc.reviewReasons && doc.reviewReasons.length > 0 && doc.status === 'review' && (
        <Card className="mb-6 border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-950">
          <CardHeader className="pb-2">
            <CardTitle className="text-base flex items-center gap-2 text-amber-700 dark:text-amber-300">
              <AlertTriangle className="h-4 w-4" />
              {t('detail.fields.reviewReasons')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {doc.reviewReasons.map((reason, idx) => {
                const display = getReviewReasonDisplay(reason, t);
                const reasonKey = typeof reason === 'string' ? reason : reason.key;
                const severityStyles = {
                  error: 'border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-950/30',
                  warning: 'border-amber-200 bg-white dark:border-amber-700 dark:bg-amber-900/30',
                  info: 'border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-950/30',
                };
                const severityTextStyles = {
                  error: 'text-red-800 dark:text-red-200',
                  warning: 'text-amber-800 dark:text-amber-200',
                  info: 'text-blue-800 dark:text-blue-200',
                };
                const severityHintStyles = {
                  error: 'text-red-600 dark:text-red-400',
                  warning: 'text-amber-600 dark:text-amber-400',
                  info: 'text-blue-600 dark:text-blue-400',
                };
                return (
                  <div key={`${reasonKey}-${idx}`} className={`rounded-md border p-3 ${severityStyles[display.severity]}`}>
                    <p className={`text-sm font-medium ${severityTextStyles[display.severity]}`}>
                      {display.isAiFreetext && (
                        <Badge variant="outline" className="mr-2 text-[10px] px-1.5 py-0 align-middle border-amber-300 text-amber-600 dark:border-amber-600 dark:text-amber-400">
                          {t('reviewReasons.aiNote')}
                        </Badge>
                      )}
                      {display.isAiFreetext ? <span className="italic">{display.label}</span> : display.label}
                    </p>
                    {display.hint && (
                      <p className={`mt-1 text-xs ${severityHintStyles[display.severity]}`}>
                        {display.hint}
                      </p>
                    )}
                    {display.detail && (
                      <p className="mt-1 text-xs text-muted-foreground italic">
                        {display.detail}
                      </p>
                    )}
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}

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
              <DetailRow
                label={doc.documentDirection === 'outgoing' ? t('detail.fields.customerName') : t('detail.fields.vendorName')}
                value={doc.vendorName}
              />
              <DetailRow label={t('detail.fields.invoiceNumber')} value={doc.invoiceNumber} />
              <DetailRow label={t('detail.fields.invoiceDate')} value={formatDate(doc.invoiceDate)} />
              <DetailRow
                label={t('detail.fields.netAmount')}
                value={
                  doc.netAmount != null
                    ? formatCurrency(doc.netAmount, doc.currency ?? 'EUR')
                    : '—'
                }
              />
              <DetailRow
                label={t('detail.fields.taxAmount')}
                value={
                  doc.taxAmount != null
                    ? formatCurrency(doc.taxAmount, doc.currency ?? 'EUR')
                    : '—'
                }
              />
              <DetailRow
                label={t('detail.fields.totalAmount')}
                value={
                  doc.totalAmount != null
                    ? formatCurrency(doc.totalAmount, doc.currency ?? 'EUR')
                    : '—'
                }
              />
              {getFieldValue(doc.fields, 'due_date') && (
                <DetailRow label={t('detail.fields.dueDate')} value={formatDate(getFieldValue(doc.fields, 'due_date'))} />
              )}
              {getFieldValue(doc.fields, 'service_period_start') ? (
                <DetailRow
                  label={t('detail.fields.servicePeriod')}
                  value={`${formatDate(getFieldValue(doc.fields, 'service_period_start'))} – ${formatDate(getFieldValue(doc.fields, 'service_period_end'))}`}
                />
              ) : doc.reviewReasons?.some((r) => (typeof r === 'string' ? r : r.key) === 'missing_service_period') ? (
                <div className="col-span-2">
                  <dt className="text-xs font-medium uppercase tracking-wide text-amber-600 dark:text-amber-400">
                    {t('detail.fields.servicePeriod')} — {t('reviewReasons.missingServicePeriod')}
                  </dt>
                  <dd className="mt-1 flex items-center gap-2">
                    <Input type="date" className="w-40" placeholder="Start" />
                    <span className="text-muted-foreground">–</span>
                    <Input type="date" className="w-40" placeholder="Ende" />
                    <span className="text-xs text-muted-foreground">
                      {t('reviewReasons.missingServicePeriodHint')}
                    </span>
                  </dd>
                </div>
              ) : null}
              {doc.reverseCharge && (
                <DetailRow label={t('detail.fields.reverseCharge')} value={<Badge variant="outline">Reverse Charge</Badge>} />
              )}
            </dl>
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

        {/* Extracted Fields */}
        {doc.fields && doc.fields.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('detail.fieldsPanel.title')}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="pb-2 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t('detail.fieldsPanel.fieldName')}
                      </th>
                      <th className="pb-2 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t('detail.fieldsPanel.fieldValue')}
                      </th>
                      <th className="pb-2 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t('detail.fieldsPanel.confidence')}
                      </th>
                      <th className="pb-2 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t('detail.fieldsPanel.verified')}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {doc.fields.map((field: DocumentField) => (
                      <tr key={field.id} className="border-b last:border-0">
                        <td className="py-2 font-medium">
                          {t(`detail.fieldsPanel.fieldLabels.${field.fieldName}`, { defaultValue: field.fieldName })}
                        </td>
                        <td className="py-2">
                          {field.fieldName.endsWith('product_category') ? (
                            <Badge variant="outline">
                              {field.correctedValue ?? field.fieldValue ?? '—'}
                            </Badge>
                          ) : (
                            <>
                              {field.correctedValue ?? field.fieldValue ?? '—'}
                              {field.correctedValue && field.fieldValue && (
                                <span className="ml-2 text-xs text-muted-foreground line-through">
                                  {field.fieldValue}
                                </span>
                              )}
                            </>
                          )}
                        </td>
                        <td className="py-2">
                          <FieldConfidenceBadge confidence={field.confidence} />
                        </td>
                        <td className="py-2">
                          {field.isVerified ? (
                            <Check className="h-4 w-4 text-emerald-500" />
                          ) : (
                            <span className="text-muted-foreground">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        )}

        {/* OCR Panel */}
        {(doc.ocrText || doc.ocrMetadata) && (
          <Card>
            <Collapsible open={showOcrPanel} onOpenChange={setShowOcrPanel}>
              <CardHeader>
                <CollapsibleTrigger asChild>
                  <button className="flex w-full items-center justify-between">
                    <CardTitle className="text-base">{t('detail.ocrPanel.title')}</CardTitle>
                    {showOcrPanel ? (
                      <ChevronUp className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    )}
                  </button>
                </CollapsibleTrigger>
              </CardHeader>
              <CollapsibleContent>
                <CardContent>
                  {doc.ocrMetadata && (
                    <dl className="grid grid-cols-2 gap-4 mb-4 sm:grid-cols-4">
                      {doc.ocrMetadata.source && (
                        <DetailRow label={t('detail.ocrPanel.source')} value={doc.ocrMetadata.source} />
                      )}
                      {doc.ocrMetadata.usedProvider && (
                        <DetailRow label={t('detail.ocrPanel.provider')} value={doc.ocrMetadata.usedProvider} />
                      )}
                      <DetailRow
                        label={t('detail.ocrPanel.vision')}
                        value={doc.ocrMetadata.usedVision ? 'Yes' : 'No'}
                      />
                      {doc.ocrMetadata.confidence != null && (
                        <div>
                          <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                            {t('detail.fieldsPanel.confidence')}
                          </dt>
                          <dd className="mt-1">
                            <ConfidenceBar confidence={doc.ocrMetadata.confidence} />
                          </dd>
                        </div>
                      )}
                    </dl>
                  )}
                  {doc.ocrMetadata?.warnings && doc.ocrMetadata.warnings.length > 0 && (
                    <div className="mb-4 rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
                      {doc.ocrMetadata.warnings.map((w, i) => (
                        <p key={i} className="text-sm text-amber-700 dark:text-amber-300">{w}</p>
                      ))}
                    </div>
                  )}
                  {doc.ocrText && (
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-2">
                        {t('detail.ocrPanel.ocrText')}
                      </dt>
                      <pre className="max-h-64 overflow-auto rounded-md bg-muted p-3 text-xs whitespace-pre-wrap">
                        {doc.ocrText}
                      </pre>
                    </div>
                  )}
                </CardContent>
              </CollapsibleContent>
            </Collapsible>
          </Card>
        )}

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
                  label={t('detail.bookingSuggestion.netAmount')}
                  value={formatCurrency(bs.netAmount ?? bs.amount - (bs.vatAmount ?? 0))}
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.taxAmount')}
                  value={formatCurrency(bs.vatAmount ?? 0)}
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.grossAmount')}
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
                  {/* Target entity select */}
                  {entities.length > 1 && (
                    <div className="flex items-center gap-3">
                      <Label className="text-sm whitespace-nowrap">
                        {t('detail.bookingSuggestion.targetEntity')}
                      </Label>
                      <Select
                        value={targetEntityId || bs.suggestedEntityId || selectedEntityId || ''}
                        onValueChange={setTargetEntityId}
                      >
                        <SelectTrigger className="w-64">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {entities.map((entity) => (
                            <SelectItem key={entity.id} value={entity.id}>
                              {entity.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {bs.suggestedEntityId && bs.suggestedEntityId !== selectedEntityId && (
                        <span className="text-xs text-amber-600">
                          {t('detail.bookingSuggestion.entitySuggested')}
                        </span>
                      )}
                    </div>
                  )}

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

        {/* Revenue Schedule */}
        {revenueScheduleData.length > 0 && (
          <Card className="lg:col-span-2">
            <Collapsible open={showRevenueSchedule} onOpenChange={setShowRevenueSchedule}>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-base">{t('detail.revenueSchedule.title')}</CardTitle>
                <CollapsibleTrigger asChild>
                  <Button variant="ghost" size="sm">
                    {showRevenueSchedule ? (
                      <ChevronUp className="h-4 w-4" />
                    ) : (
                      <ChevronDown className="h-4 w-4" />
                    )}
                  </Button>
                </CollapsibleTrigger>
              </CardHeader>
              <CardContent>
                {/* Summary row */}
                {(() => {
                  const totalAmount = revenueScheduleData.reduce((sum, e) => sum + e.amount, 0);
                  const bookedCount = revenueScheduleData.filter((e) => e.status === 'booked').length;
                  const totalCount = revenueScheduleData.length;
                  const progressPct = totalCount > 0 ? Math.round((bookedCount / totalCount) * 100) : 0;
                  return (
                    <div className="mb-4 flex flex-wrap items-center gap-4">
                      <span className="text-sm text-muted-foreground">
                        {t('detail.revenueSchedule.totalAmount')}:{' '}
                        <span className="font-semibold text-foreground">
                          {formatCurrency(totalAmount, doc?.currency ?? 'EUR')}
                        </span>
                      </span>
                      <span className="text-sm text-muted-foreground">
                        {t('detail.revenueSchedule.booked')}: {bookedCount} / {totalCount}
                      </span>
                      <div className="flex items-center gap-2">
                        <div className="h-2 w-32 rounded-full bg-muted overflow-hidden">
                          <div
                            className="h-full rounded-full bg-emerald-500"
                            style={{ width: `${progressPct}%` }}
                          />
                        </div>
                        <span className="text-xs text-muted-foreground">{progressPct}%</span>
                      </div>
                    </div>
                  );
                })()}

                <CollapsibleContent>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b text-left text-muted-foreground">
                          <th className="pb-2 pr-4 font-medium">{t('detail.revenueSchedule.period')}</th>
                          <th className="pb-2 pr-4 font-medium text-right">{t('detail.revenueSchedule.amount')}</th>
                          <th className="pb-2 pr-4 font-medium">{t('detail.revenueSchedule.account')}</th>
                          <th className="pb-2 font-medium">{t('detail.revenueSchedule.status')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {revenueScheduleData.map((entry) => {
                          const periodDate = new Date(entry.periodDate);
                          const periodLabel = new Intl.DateTimeFormat(i18n.language, {
                            year: 'numeric',
                            month: '2-digit',
                          }).format(periodDate);

                          const statusVariant =
                            entry.status === 'booked'
                              ? 'default'
                              : entry.status === 'cancelled'
                                ? 'secondary'
                                : 'outline';
                          const statusClass =
                            entry.status === 'booked'
                              ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200'
                              : entry.status === 'cancelled'
                                ? 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
                                : 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200';

                          return (
                            <tr key={entry.id} className="border-b last:border-0">
                              <td className="py-2 pr-4">{periodLabel}</td>
                              <td className="py-2 pr-4 text-right font-medium">
                                {formatCurrency(entry.amount, doc?.currency ?? 'EUR')}
                              </td>
                              <td className="py-2 pr-4 font-mono text-xs">{entry.revenueAccountNumber}</td>
                              <td className="py-2">
                                <Badge variant={statusVariant} className={statusClass}>
                                  {t(`detail.revenueSchedule.statuses.${entry.status}`)}
                                </Badge>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </CollapsibleContent>
              </CardContent>
            </Collapsible>
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
              <AccountCombobox
                accounts={accounts}
                value={modifyForm.watch('debitAccountId')}
                onValueChange={(v) => modifyForm.setValue('debitAccountId', v)}
              />
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.creditAccount')}</Label>
              <AccountCombobox
                accounts={accounts}
                value={modifyForm.watch('creditAccountId')}
                onValueChange={(v) => modifyForm.setValue('creditAccountId', v)}
              />
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.netAmount')}</Label>
              <CurrencyInput
                className="mt-1"
                value={modifyNet}
                onValueChange={handleNetChange}
              />
            </div>

            <div>
              <Label>{t('detail.bookingSuggestion.taxAmount')}</Label>
              <CurrencyInput
                className="mt-1"
                value={modifyTax}
                onValueChange={handleTaxChange}
              />
            </div>

            <div>
              <Label className="text-muted-foreground">{t('detail.bookingSuggestion.grossAmount')}</Label>
              <div className="mt-1 h-9 rounded-md border bg-muted/50 px-3 py-2 text-sm tabular-nums">
                {formatCurrency(modifyGross)}
              </div>
            </div>

            {amountMismatch && (
              <div className="flex items-center gap-2 rounded-md border border-amber-200 bg-amber-50 p-2 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200">
                <AlertTriangle className="h-4 w-4 shrink-0" />
                {t('detail.bookingSuggestion.amountMismatchWarning')}
              </div>
            )}

            <div>
              <Label>{t('detail.bookingSuggestion.vatCode')}</Label>
              <Select
                value={modifyForm.watch('vatCode') ?? ''}
                onValueChange={(v) => handleVatCodeChangeInModify(v === 'none' ? undefined : v)}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue placeholder="—" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">—</SelectItem>
                  {VAT_CODES.map((vc) => (
                    <SelectItem key={vc.value} value={vc.value}>
                      {vc.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
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

      {/* Delete Confirmation Dialog */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('delete.title')}</DialogTitle>
            <DialogDescription>
              {t('delete.confirm', { fileName: doc.fileName })}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4 space-y-3">
            {isPreflightLoading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                {t('delete.loading')}
              </div>
            ) : preflight ? (
              <>
                {!preflight.canDelete && preflight.blockReason && (
                  <div className="rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-800 dark:bg-red-950">
                    <p className="text-sm font-medium text-red-700 dark:text-red-300">
                      {t('delete.blocked')}
                    </p>
                    <p className="text-sm text-red-600 dark:text-red-400">
                      {preflight.blockReason.startsWith('closed_period:')
                        ? t('delete.closedPeriod', { period: preflight.blockReason.split(':')[1] })
                        : preflight.blockReason}
                    </p>
                  </div>
                )}
                {preflight.canDelete && (
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center gap-2">
                      <span className="text-muted-foreground">•</span>
                      {t('delete.willDeleteDocument')}
                    </li>
                    {preflight.fieldCount > 0 && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.willDeleteFields', { count: preflight.fieldCount })}
                      </li>
                    )}
                    {preflight.hasBookingSuggestion && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.willDeleteBookingSuggestion')}
                      </li>
                    )}
                    {preflight.hasJournalEntry && preflight.journalEntryWillBeReversed && (
                      <li className="flex items-center gap-2 text-amber-600 dark:text-amber-400">
                        <AlertTriangle className="h-4 w-4 shrink-0" />
                        {t('delete.willReverseJournalEntry')}
                      </li>
                    )}
                    {preflight.hasJournalEntry && !preflight.journalEntryWillBeReversed && (
                      <li className="flex items-center gap-2">
                        <span className="text-muted-foreground">•</span>
                        {t('delete.journalEntryAlreadyReversed')}
                      </li>
                    )}
                  </ul>
                )}
                {preflight.canDelete && (
                  <p className="text-xs text-muted-foreground italic">{t('delete.irreversible')}</p>
                )}
              </>
            ) : null}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDeleteDialog(false)}>
              {t('actions.cancel', { ns: 'common', defaultValue: 'Cancel' })}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteDocument}
              disabled={deleteDocument.isPending || !preflight?.canDelete}
            >
              {deleteDocument.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              <Trash2 className="mr-1 h-4 w-4" />
              {t('actions.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
