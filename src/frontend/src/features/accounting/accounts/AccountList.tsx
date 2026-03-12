import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAccounts, useSeedAccounts } from '@/hooks/useAccounting';
import { useEntity } from '@/hooks/useEntity';
import { useDebounced } from '@/hooks/useDebounced';
import type { Account } from '@/types/accounting';
import PageHeader from '@/components/shared/PageHeader';
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
import { Plus, Search, Download, Loader2 } from 'lucide-react';

export function Component() {
  const { t } = useTranslation('accounting');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const [search, setSearch] = useState('');
  const [activeFilter, setActiveFilter] = useState('active');
  const [typeFilter, setTypeFilter] = useState('all');
  const debouncedSearch = useDebounced(search, 300);

  const { data: accounts, isLoading } = useAccounts(selectedEntityId, {
    search: debouncedSearch || undefined,
    accountType: typeFilter !== 'all' ? typeFilter : undefined,
    activeOnly: activeFilter !== 'all' ? activeFilter === 'active' : undefined,
  });

  const seedAccounts = useSeedAccounts();

  const groupedByClass = useMemo(() => {
    if (!accounts) return {};
    const groups: Record<number, Account[]> = {};
    for (const acc of accounts) {
      if (!groups[acc.accountClass]) groups[acc.accountClass] = [];
      groups[acc.accountClass].push(acc);
    }
    return groups;
  }, [accounts]);

  const sortedClasses = useMemo(
    () => Object.keys(groupedByClass).map(Number).sort((a, b) => a - b),
    [groupedByClass],
  );

  if (!selectedEntityId) {
    return (
      <div className="p-6">
        <p className="text-muted-foreground">{t('accounts.noEntitySelected')}</p>
      </div>
    );
  }

  const isEmpty = !isLoading && (!accounts || accounts.length === 0) && !debouncedSearch && typeFilter === 'all';

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title={t('accounts.title')}
        actions={
          <Button onClick={() => navigate('/accounting/accounts/new')} size="sm">
            <Plus className="mr-2 h-4 w-4" />
            {t('accounts.create')}
          </Button>
        }
      />

      {isEmpty ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed p-12 text-center">
          <h3 className="text-lg font-semibold">{t('accounts.emptyState.noAccounts')}</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            {t('accounts.emptyState.seedSkr03Description')}
          </p>
          <Button
            className="mt-4"
            onClick={() => seedAccounts.mutate({ entityId: selectedEntityId })}
            disabled={seedAccounts.isPending}
          >
            {seedAccounts.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Download className="mr-2 h-4 w-4" />
            )}
            {t('accounts.actions.seedSkr03')}
          </Button>
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative flex-1 min-w-[200px] max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('accounts.searchPlaceholder')}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>

            <Select value={typeFilter} onValueChange={setTypeFilter}>
              <SelectTrigger className="w-[160px]">
                <SelectValue placeholder={t('accounts.filters.allTypes')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('accounts.filters.allTypes')}</SelectItem>
                <SelectItem value="asset">{t('accounts.accountTypes.asset')}</SelectItem>
                <SelectItem value="liability">{t('accounts.accountTypes.liability')}</SelectItem>
                <SelectItem value="equity">{t('accounts.accountTypes.equity')}</SelectItem>
                <SelectItem value="revenue">{t('accounts.accountTypes.revenue')}</SelectItem>
                <SelectItem value="expense">{t('accounts.accountTypes.expense')}</SelectItem>
              </SelectContent>
            </Select>

            <Select value={activeFilter} onValueChange={setActiveFilter}>
              <SelectTrigger className="w-[140px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="active">{t('accounts.filters.active')}</SelectItem>
                <SelectItem value="all">{t('accounts.filters.all')}</SelectItem>
                <SelectItem value="inactive">{t('accounts.filters.inactive')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="flex justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : accounts && accounts.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">{t('accounts.noEntries')}</p>
          ) : (
            <div className="space-y-4">
              {sortedClasses.map((cls) => (
                <div key={cls} className="rounded-lg border">
                  <div className="flex items-center gap-3 border-b bg-muted/50 px-4 py-3">
                    <Badge variant="secondary" className="tabular-nums">
                      {cls}
                    </Badge>
                    <span className="text-sm font-medium">
                      {t(`accounts.accountClasses.${cls}`)}
                    </span>
                    <span className="ml-auto text-xs text-muted-foreground">
                      {groupedByClass[cls].length} {t('accounts.accountsCount')}
                    </span>
                  </div>
                  <div className="divide-y">
                    {groupedByClass[cls].map((account) => (
                      <div
                        key={account.id}
                        className="flex cursor-pointer items-center gap-4 px-4 py-2.5 transition-colors hover:bg-muted/30"
                        onClick={() => navigate(`/accounting/accounts/${account.id}`)}
                      >
                        <span className="w-16 font-mono text-sm tabular-nums">
                          {account.accountNumber}
                        </span>
                        <span className="flex-1 text-sm">{account.name}</span>
                        <AccountTypeBadge type={account.accountType} t={t} />
                        {account.vatDefault && (
                          <span className="text-xs text-muted-foreground">
                            {account.vatDefault}%
                          </span>
                        )}
                        {!account.isActive && (
                          <Badge variant="secondary" className="text-xs">
                            {t('accounts.filters.inactive')}
                          </Badge>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}

function AccountTypeBadge({ type, t }: { type: string; t: (key: string) => string }) {
  const colors: Record<string, string> = {
    asset: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
    liability: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
    equity: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300',
    revenue: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
    expense: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300',
  };

  return (
    <Badge className={`text-xs ${colors[type] ?? ''}`}>
      {t(`accounts.accountTypes.${type}`)}
    </Badge>
  );
}
