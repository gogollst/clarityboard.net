import { useState, useRef, useMemo, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, ChevronsUpDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { Account } from '@/types/accounting';
import { getLocalizedAccountName } from '@/lib/accountUtils';

interface AccountComboboxProps {
  accounts: Account[];
  value: string;
  onValueChange: (value: string) => void;
  placeholder?: string;
}

export function AccountCombobox({ accounts, value, onValueChange, placeholder }: AccountComboboxProps) {
  const { i18n, t } = useTranslation('accounting');
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const lang = i18n.language;

  const selectedAccount = accounts.find((a) => a.id === value);

  const filtered = useMemo(() => {
    if (!search) return accounts;
    const q = search.toLowerCase();
    return accounts.filter(
      (a) =>
        a.accountNumber.toLowerCase().includes(q) ||
        getLocalizedAccountName(a, lang).toLowerCase().includes(q),
    );
  }, [accounts, search, lang]);

  useEffect(() => {
    if (open) {
      // Focus search input when popover opens
      setTimeout(() => inputRef.current?.focus(), 0);
    } else {
      setSearch('');
    }
  }, [open]);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="mt-1 w-full justify-between font-normal"
        >
          <span className="truncate">
            {selectedAccount
              ? `${selectedAccount.accountNumber} — ${getLocalizedAccountName(selectedAccount, lang)}`
              : placeholder ?? ''}
          </span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        <div className="p-2">
          <Input
            ref={inputRef}
            placeholder={t('accounts.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-8"
          />
        </div>
        <ScrollArea className="max-h-60">
          {filtered.length === 0 ? (
            <div className="px-3 py-2 text-sm text-muted-foreground">{t('accounts.noEntries')}</div>
          ) : (
            <div className="p-1">
              {filtered.map((account) => (
                <button
                  key={account.id}
                  type="button"
                  className={cn(
                    'flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground',
                    value === account.id && 'bg-accent',
                  )}
                  onClick={() => {
                    onValueChange(account.id);
                    setOpen(false);
                  }}
                >
                  <Check
                    className={cn('h-4 w-4 shrink-0', value === account.id ? 'opacity-100' : 'opacity-0')}
                  />
                  <span className="truncate">
                    {account.accountNumber} — {getLocalizedAccountName(account, lang)}
                  </span>
                </button>
              ))}
            </div>
          )}
        </ScrollArea>
      </PopoverContent>
    </Popover>
  );
}
