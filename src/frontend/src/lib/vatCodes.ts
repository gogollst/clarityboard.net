/**
 * Allowed VAT codes for booking suggestions.
 * Must match the backend's ValidVatCodes in PromptBackedAiServiceAdapter.cs
 * and the AI prompt's output schema.
 */
export const VAT_CODES = [
  { value: 'VSt19', label: 'VSt 19% — Vorsteuer 19%' },
  { value: 'VSt7', label: 'VSt 7% — Vorsteuer 7%' },
  { value: 'USt19', label: 'USt 19% — Umsatzsteuer 19%' },
  { value: 'USt7', label: 'USt 7% — Umsatzsteuer 7%' },
  { value: 'steuerfrei', label: 'Steuerfrei (§4 UStG)' },
  { value: '§13b', label: '§13b UStG — Reverse Charge' },
] as const;

export type VatCode = (typeof VAT_CODES)[number]['value'];
