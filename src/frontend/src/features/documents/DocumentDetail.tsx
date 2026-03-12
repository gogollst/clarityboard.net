import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useDocument, useConfirmPartnerMatch, useApproveBooking } from '@/hooks/useDocuments';
import { useSearchBusinessPartners, useAssignDocumentPartner } from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { useDebounced } from '@/hooks/useDebounced';
import { formatCurrency } from '@/lib/format';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, Check, Handshake, Loader2, Plus, Search } from 'lucide-react';

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

export function Component() {
  const { t, i18n } = useTranslation('documents');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const { data: doc, isLoading } = useDocument(id ?? null);
  const approveBooking = useApproveBooking();
  const confirmPartnerMatch = useConfirmPartnerMatch();
  const assignPartner = useAssignDocumentPartner();

  // Partner search state
  const [searchQuery, setSearchQuery] = useState('');
  const [showPartnerSearch, setShowPartnerSearch] = useState(false);
  const debouncedSearch = useDebounced(searchQuery, 300);
  const { data: searchResults = [] } = useSearchBusinessPartners(
    selectedEntityId,
    debouncedSearch,
  );

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
    approveBooking.mutate({ documentId: doc.id, entityId: selectedEntityId });
  };

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
              // Partner already assigned
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
              // Fuzzy match suggestion
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

                {/* Also allow manual search */}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowPartnerSearch(!showPartnerSearch)}
                >
                  {t('actions.assignPartner')}
                </Button>
              </div>
            ) : (
              // No partner — show assignment UI
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

            {/* Partner search dropdown */}
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
        {doc.bookingSuggestion && (
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle className="text-base">{t('detail.bookingSuggestion.title')}</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-6 sm:grid-cols-4">
                <DetailRow
                  label={t('detail.bookingSuggestion.debitAccount')}
                  value={doc.bookingSuggestion.debitAccount}
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.creditAccount')}
                  value={doc.bookingSuggestion.creditAccount}
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.amount')}
                  value={formatCurrency(doc.bookingSuggestion.amount)}
                />
                <DetailRow
                  label={t('detail.bookingSuggestion.confidence')}
                  value={`${(doc.bookingSuggestion.confidence * 100).toFixed(0)}%`}
                />
                <div className="sm:col-span-4">
                  <DetailRow
                    label={t('detail.bookingSuggestion.description')}
                    value={doc.bookingSuggestion.description}
                  />
                </div>
              </dl>

              {doc.status === 'review' && (
                <div className="mt-6 flex justify-end border-t pt-4">
                  <Button
                    onClick={handleApproveBooking}
                    disabled={approveBooking.isPending}
                  >
                    {approveBooking.isPending && (
                      <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                    )}
                    {t('actions.approveBooking')}
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
