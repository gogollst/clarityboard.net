import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useBusinessPartners } from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { useDebounced } from '@/hooks/useDebounced';
import type { BusinessPartnerListItem } from '@/types/accounting';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { Plus, Search } from 'lucide-react';

export function Component() {
  const { t } = useTranslation('accounting');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [activeFilter, setActiveFilter] = useState('');
  const debouncedSearch = useDebounced(search, 300);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, typeFilter, activeFilter]);

  const { data, isLoading } = useBusinessPartners(selectedEntityId, {
    page,
    pageSize: 25,
    search: debouncedSearch || undefined,
    isCreditor: typeFilter === 'creditor' ? true : undefined,
    isDebtor: typeFilter === 'debtor' ? true : undefined,
    isActive: activeFilter === 'active' ? true : activeFilter === 'inactive' ? false : undefined,
  });

  const partners: BusinessPartnerListItem[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 25;

  function getTypeBadge(item: BusinessPartnerListItem) {
    if (item.isCreditor && item.isDebtor) {
      return <Badge variant="secondary">{t('businessPartners.type.both')}</Badge>;
    }
    if (item.isCreditor) {
      return (
        <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300">
          {t('businessPartners.type.creditor')}
        </Badge>
      );
    }
    return (
      <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
        {t('businessPartners.type.debtor')}
      </Badge>
    );
  }

  function getStatusBadge(isActive: boolean) {
    if (isActive) {
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          {t('businessPartners.status.active')}
        </Badge>
      );
    }
    return (
      <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
        {t('businessPartners.status.inactive')}
      </Badge>
    );
  }

  const columns = [
    {
      key: 'partnerNumber',
      header: t('businessPartners.columns.partnerNumber'),
    },
    {
      key: 'name',
      header: t('businessPartners.columns.name'),
      render: (item: Record<string, unknown>) => (
        <button
          className="font-medium text-left hover:underline text-foreground"
          onClick={() => navigate(`/accounting/business-partners/${String(item.id ?? '')}`)}
        >
          {String(item.name ?? '')}
        </button>
      ),
    },
    {
      key: 'type',
      header: t('businessPartners.columns.type'),
      render: (item: Record<string, unknown>) =>
        getTypeBadge(item as unknown as BusinessPartnerListItem),
    },
    {
      key: 'city',
      header: t('businessPartners.columns.city'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.city ?? '—')}
        </span>
      ),
    },
    {
      key: 'isActive',
      header: t('businessPartners.columns.isActive'),
      render: (item: Record<string, unknown>) =>
        getStatusBadge(item.isActive as boolean),
    },
    {
      key: 'openDocumentCount',
      header: t('businessPartners.columns.openDocuments'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {String(item.openDocumentCount ?? 0)}
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title={t('businessPartners.title')}
        actions={
          <Button onClick={() => navigate('/accounting/business-partners/new')}>
            <Plus className="mr-1 h-4 w-4" />
            {t('businessPartners.newPartner')}
          </Button>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder={t('businessPartners.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <Select
          value={typeFilter || 'all'}
          onValueChange={(v) => setTypeFilter(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('businessPartners.filters.all')}</SelectItem>
            <SelectItem value="creditor">{t('businessPartners.filters.creditors')}</SelectItem>
            <SelectItem value="debtor">{t('businessPartners.filters.debtors')}</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={activeFilter || 'all'}
          onValueChange={(v) => setActiveFilter(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('businessPartners.filters.all')}</SelectItem>
            <SelectItem value="active">{t('businessPartners.filters.active')}</SelectItem>
            <SelectItem value="inactive">{t('businessPartners.filters.inactive')}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={partners as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('businessPartners.noEntries')}
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />
    </div>
  );
}
