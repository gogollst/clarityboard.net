import i18n from '@/i18n';

const LOCALE_MAP: Record<string, string> = {
  de: 'de-DE',
  en: 'en-US',
  ru: 'ru-RU',
};

function getLocale(): string {
  return LOCALE_MAP[i18n.language] ?? 'de-DE';
}

// Cache formatters by locale+currency to avoid re-creation on every call
const currencyCache = new Map<string, Intl.NumberFormat>();

function getCurrencyFormatter(currency: string): Intl.NumberFormat {
  const locale = getLocale();
  const key = `${locale}:${currency}`;
  let fmt = currencyCache.get(key);
  if (!fmt) {
    fmt = new Intl.NumberFormat(locale, {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    currencyCache.set(key, fmt);
  }
  return fmt;
}

export function formatCurrency(value: number, currency = 'EUR'): string {
  return getCurrencyFormatter(currency).format(value);
}

export function formatPercent(value: number): string {
  return new Intl.NumberFormat(getLocale(), {
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(value / 100);
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat(getLocale(), {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatDays(value: number): string {
  return `${formatNumber(value)} days`;
}

export function formatCompactNumber(value: number): string {
  if (Math.abs(value) >= 1_000_000) {
    return `${(value / 1_000_000).toFixed(1)}M`;
  }
  if (Math.abs(value) >= 1_000) {
    return `${(value / 1_000).toFixed(1)}K`;
  }
  return formatNumber(value);
}

/**
 * Returns the decimal separator for the current app locale.
 * Used by CurrencyInput and other input components.
 */
export function getDecimalSeparator(): string {
  const locale = getLocale();
  const parts = new Intl.NumberFormat(locale).formatToParts(1.1);
  return parts.find(p => p.type === 'decimal')?.value ?? ',';
}

/**
 * Formats a number with exactly 2 decimal places using the current locale's
 * decimal separator. For use in editable inputs (not currency display).
 */
export function formatDecimal2(value: number): string {
  return value.toFixed(2).replace('.', getDecimalSeparator());
}

/**
 * Parses a localized decimal string to a number.
 * Accepts both comma and dot as decimal separator.
 */
export function parseDecimal(input: string): number {
  if (!input || input === '-' || input === ',' || input === '.' || input === '-,' || input === '-.') return 0;
  const normalized = input.replace(',', '.');
  const num = parseFloat(normalized);
  return isNaN(num) ? 0 : num;
}

/**
 * Rounds a number to exactly 2 decimal places (for currency precision).
 */
export function roundTo2(value: number): number {
  return Math.round(value * 100) / 100;
}
