import { useState, useCallback, useRef, type ComponentProps } from 'react';
import { Input } from '@/components/ui/input';

interface CurrencyInputProps extends Omit<ComponentProps<typeof Input>, 'value' | 'onChange' | 'type'> {
  value: number;
  onValueChange: (value: number) => void;
}

/**
 * Decimal currency input that:
 * - Displays values with exactly 2 decimal places (on blur)
 * - Allows free-form editing while focused
 * - Limits to max 2 decimal places on input
 * - Uses comma as decimal separator for display (German format)
 * - Normalizes internally to number with 2 decimal precision
 */
export function CurrencyInput({ value, onValueChange, onFocus, onBlur, ...props }: CurrencyInputProps) {
  const [editValue, setEditValue] = useState<string | null>(null);
  const isFocused = useRef(false);

  // When not focused, display formatted value from props; when focused, show editValue
  const displayValue = editValue !== null ? editValue : formatForDisplay(value);

  const handleFocus = useCallback((e: React.FocusEvent<HTMLInputElement>) => {
    isFocused.current = true;
    setEditValue(value === 0 ? '' : value.toFixed(2).replace('.', ','));
    onFocus?.(e);
  }, [value, onFocus]);

  const handleBlur = useCallback((e: React.FocusEvent<HTMLInputElement>) => {
    isFocused.current = false;
    const parsed = parseInput(editValue ?? '');
    const rounded = roundTo2(parsed);
    onValueChange(rounded);
    setEditValue(null); // revert to displaying formatted prop value
    onBlur?.(e);
  }, [editValue, onValueChange, onBlur]);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;

    // Allow empty, minus, or partial input while typing
    if (raw === '' || raw === '-' || raw === ',' || raw === '-,') {
      setEditValue(raw);
      return;
    }

    // Normalize: accept both comma and dot as decimal separator
    const normalized = raw.replace(',', '.');

    // Validate: optional minus, digits, optional dot + max 2 decimals
    if (!/^-?\d*\.?\d{0,2}$/.test(normalized)) {
      return; // reject input with >2 decimal places or invalid chars
    }

    setEditValue(raw);

    // Parse and propagate if it's a valid number
    const num = parseFloat(normalized);
    if (!isNaN(num)) {
      onValueChange(roundTo2(num));
    }
  }, [onValueChange]);

  return (
    <Input
      {...props}
      type="text"
      inputMode="decimal"
      value={displayValue}
      onFocus={handleFocus}
      onBlur={handleBlur}
      onChange={handleChange}
    />
  );
}

function formatForDisplay(value: number): string {
  return value.toFixed(2).replace('.', ',');
}

function parseInput(input: string): number {
  if (!input || input === '-' || input === ',' || input === '-,') return 0;
  const normalized = input.replace(',', '.');
  const num = parseFloat(normalized);
  return isNaN(num) ? 0 : num;
}

function roundTo2(value: number): number {
  return Math.round(value * 100) / 100;
}
