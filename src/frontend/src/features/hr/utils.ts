import i18n from '@/i18n';

export function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(i18n.language);
}

export function formatEur(cents: number): string {
  return (cents / 100).toLocaleString(i18n.language, { style: 'currency', currency: 'EUR' });
}
