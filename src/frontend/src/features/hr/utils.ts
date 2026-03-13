import i18n from '@/i18n';
import { formatCurrency } from '@/lib/format';

export function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(i18n.language);
}

export function formatEur(cents: number): string {
  return formatCurrency(cents / 100);
}
