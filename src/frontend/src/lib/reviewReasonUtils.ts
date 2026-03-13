import type { TFunction } from 'i18next';
import type { ReviewReasonDto } from '@/types/document';

/**
 * Mapping from backend review-reason keys to i18n translation keys.
 * Every snake_case key that the backend can generate must have an entry here.
 */
const REVIEW_REASON_MAP: Record<string, { labelKey: string; hintKey: string }> = {
  // ── Extraction / Booking confidence ────────────────────────────────
  low_extraction_confidence: { labelKey: 'reviewReasons.lowExtraction', hintKey: 'reviewReasons.lowExtractionHint' },
  low_booking_confidence: { labelKey: 'reviewReasons.lowBooking', hintKey: 'reviewReasons.lowBookingHint' },

  // ── Partner matching ───────────────────────────────────────────────
  partner_fuzzy_match: { labelKey: 'reviewReasons.fuzzyPartner', hintKey: 'reviewReasons.fuzzyPartnerHint' },
  entity_mismatch_suspected: { labelKey: 'reviewReasons.entityMismatch', hintKey: 'reviewReasons.entityMismatchHint' },

  // ── Booking suggestion ─────────────────────────────────────────────
  booking_suggestion_failed: { labelKey: 'reviewReasons.bookingFailed', hintKey: 'reviewReasons.bookingFailedHint' },
  booking_suggestion_unresolved_accounts: { labelKey: 'reviewReasons.unresolvedAccounts', hintKey: 'reviewReasons.unresolvedAccountsHint' },
  ai_needs_manual_review: { labelKey: 'reviewReasons.aiManualReview', hintKey: 'reviewReasons.aiManualReviewHint' },

  // ── Azure Document Intelligence ────────────────────────────────────
  azure_doc_intelligence_failed: { labelKey: 'reviewReasons.azureFailed', hintKey: 'reviewReasons.azureFailedHint' },
  azure_doc_intelligence_low_confidence: { labelKey: 'reviewReasons.azureLowConfidence', hintKey: 'reviewReasons.azureLowConfidenceHint' },
  azure_doc_intelligence_empty_text: { labelKey: 'reviewReasons.azureEmptyText', hintKey: 'reviewReasons.azureEmptyTextHint' },

  // ── OCR / Vision ───────────────────────────────────────────────────
  native_text_empty: { labelKey: 'reviewReasons.nativeTextEmpty', hintKey: 'reviewReasons.nativeTextEmptyHint' },
  native_text_low_quality: { labelKey: 'reviewReasons.nativeTextLowQuality', hintKey: 'reviewReasons.nativeTextLowQualityHint' },
  unsupported_content_type_for_vision: { labelKey: 'reviewReasons.unsupportedContentType', hintKey: 'reviewReasons.unsupportedContentTypeHint' },
  pdf_rasterization_failed: { labelKey: 'reviewReasons.pdfRasterizationFailed', hintKey: 'reviewReasons.pdfRasterizationFailedHint' },
  pdf_rasterization_empty: { labelKey: 'reviewReasons.pdfRasterizationEmpty', hintKey: 'reviewReasons.pdfRasterizationEmptyHint' },
  vision_ocr_empty: { labelKey: 'reviewReasons.visionOcrEmpty', hintKey: 'reviewReasons.visionOcrEmptyHint' },
  vision_ocr_low_confidence: { labelKey: 'reviewReasons.visionOcrLowConfidence', hintKey: 'reviewReasons.visionOcrLowConfidenceHint' },
  vision_ocr_partial_pages: { labelKey: 'reviewReasons.visionOcrPartialPages', hintKey: 'reviewReasons.visionOcrPartialPagesHint' },
  vision_provider_fallback_used: { labelKey: 'reviewReasons.visionProviderFallback', hintKey: 'reviewReasons.visionProviderFallbackHint' },
  vision_ocr_timeout: { labelKey: 'reviewReasons.visionOcrTimeout', hintKey: 'reviewReasons.visionOcrTimeoutHint' },
  vision_ocr_failed: { labelKey: 'reviewReasons.visionOcrFailed', hintKey: 'reviewReasons.visionOcrFailedHint' },

  // ── Tax / Compliance ───────────────────────────────────────────────
  reverse_charge_detected: { labelKey: 'reviewReasons.reverseCharge', hintKey: 'reviewReasons.reverseChargeHint' },
  activation_required: { labelKey: 'reviewReasons.activationRequired', hintKey: 'reviewReasons.activationRequiredHint' },
  entertainment_expense_70_30: { labelKey: 'reviewReasons.entertainmentExpense', hintKey: 'reviewReasons.entertainmentExpenseHint' },
  intra_community_acquisition: { labelKey: 'reviewReasons.intraCommunity', hintKey: 'reviewReasons.intraCommunityHint' },

  // ── AI-generated structured keys ───────────────────────────────────
  cost_center_split_required: { labelKey: 'reviewReasons.costCenterSplit', hintKey: 'reviewReasons.costCenterSplitHint' },
  high_consulting_activation: { labelKey: 'reviewReasons.highConsultingActivation', hintKey: 'reviewReasons.highConsultingActivationHint' },
  project_assignment_check: { labelKey: 'reviewReasons.projectAssignment', hintKey: 'reviewReasons.projectAssignmentHint' },
  manual_review_rule: { labelKey: 'reviewReasons.manualReviewRule', hintKey: 'reviewReasons.manualReviewRuleHint' },
  ocr_quality_issues: { labelKey: 'reviewReasons.ocrQualityIssues', hintKey: 'reviewReasons.ocrQualityIssuesHint' },
  amount_plausibility_check: { labelKey: 'reviewReasons.amountPlausibility', hintKey: 'reviewReasons.amountPlausibilityHint' },
  tax_treatment_unclear: { labelKey: 'reviewReasons.taxTreatmentUnclear', hintKey: 'reviewReasons.taxTreatmentUnclearHint' },
  multi_period_allocation: { labelKey: 'reviewReasons.multiPeriodAllocation', hintKey: 'reviewReasons.multiPeriodAllocationHint' },
  intercompany_transaction: { labelKey: 'reviewReasons.intercompanyTransaction', hintKey: 'reviewReasons.intercompanyTransactionHint' },
  duplicate_invoice_suspected: { labelKey: 'reviewReasons.duplicateInvoice', hintKey: 'reviewReasons.duplicateInvoiceHint' },
};

export interface ReviewReasonDisplay {
  label: string;
  hint?: string;
  detail?: string;
  isAiFreetext: boolean;
}

/**
 * Normalizes a review reason from either old string format or new object format.
 */
function normalizeReason(reason: string | ReviewReasonDto): { key: string; detail?: string } {
  if (typeof reason === 'string') {
    return { key: reason };
  }
  return { key: reason.key, detail: reason.detail || undefined };
}

/**
 * Resolves a review reason (string or object) to a localized display object.
 *
 * - Known keys → translated label + hint, optional AI detail
 * - "custom" key → detail shown as AI freetext
 * - Unknown strings (legacy) → displayed as-is, flagged as AI freetext
 */
export function getReviewReasonDisplay(
  reason: string | ReviewReasonDto,
  t: TFunction,
): ReviewReasonDisplay {
  const { key, detail } = normalizeReason(reason);

  const mapped = REVIEW_REASON_MAP[key];
  if (mapped) {
    return {
      label: t(mapped.labelKey),
      hint: t(mapped.hintKey),
      detail: detail || undefined,
      isAiFreetext: false,
    };
  }

  // "custom" key from AI → use detail as the display text
  if (key === 'custom' && detail) {
    return { label: detail, hint: undefined, detail: undefined, isAiFreetext: true };
  }

  // Unknown key (legacy freetext or unmapped key)
  return { label: key, hint: detail || undefined, isAiFreetext: true };
}
