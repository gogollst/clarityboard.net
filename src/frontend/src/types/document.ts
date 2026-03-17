export type DocumentStatus = 'uploaded' | 'processing' | 'extracted' | 'review' | 'booked' | 'failed';
export type DocumentDirection = 'incoming' | 'outgoing';

export interface DocumentListItem {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  documentType: string;
  status: DocumentStatus;
  documentDirection: DocumentDirection;
  classificationConfidence?: number;
  vendorName?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  totalAmount?: number;
  netAmount?: number;
  taxAmount?: number;
  currency?: string;
  confidence?: number;
  dueDate?: string;
  reverseCharge: boolean;
  createdAt: string;
  processedAt?: string;
}

export interface DocumentDetail extends DocumentListItem {
  entityId: string;
  bookedJournalEntryId?: string;
  businessPartnerId?: string;
  businessPartnerName?: string;
  businessPartnerNumber?: string;
  suggestedBusinessPartnerId?: string;
  suggestedBusinessPartnerName?: string;
  suggestedBusinessPartnerNumber?: string;
  ocrText?: string;
  ocrMetadata?: OcrMetadata;
  reviewReasons?: (string | ReviewReasonDto)[];
  fields?: DocumentField[];
  bookingSuggestion?: BookingSuggestion;
}

export interface ReviewReasonDto {
  key: string;
  detail?: string;
}

export interface OcrMetadata {
  source?: string;
  confidence?: number;
  usedVision: boolean;
  usedProvider?: string;
  warnings?: string[];
  nativeTextLength?: number;
  visionTextLength?: number;
}

export interface DocumentField {
  id: string;
  fieldName: string;
  fieldValue?: string;
  confidence: number;
  isVerified: boolean;
  correctedValue?: string;
}

export interface BookingSuggestion {
  id: string;
  debitAccountId: string;
  debitAccountNumber?: string;
  debitAccountName?: string;
  creditAccountId: string;
  creditAccountNumber?: string;
  creditAccountName?: string;
  amount: number;
  netAmount?: number;
  vatCode?: string;
  vatAmount?: number;
  description: string;
  confidence: number;
  status: 'suggested' | 'accepted' | 'rejected' | 'modified';
  aiReasoning?: string;
  hrEmployeeId?: string;
  hrEmployeeName?: string;
  isAutoBooked?: boolean;
  rejectionReason?: string;
  invoiceType?: string;
  taxKey?: string;
  vatTreatmentType?: string;
  suggestedEntityId?: string;
  suggestedEntityName?: string;
}

export interface DocumentListParams {
  entityId: string;
  page?: number;
  pageSize?: number;
  status?: DocumentStatus;
  direction?: DocumentDirection;
  search?: string;
}

export interface UploadDocumentRequest {
  file: File;
  entityId: string;
}

export interface DocumentDownloadUrl {
  url: string;
}

export interface ModifyBookingRequest {
  debitAccountId: string;
  creditAccountId: string;
  amount: number;
  vatCode?: string;
  vatAmount?: number;
  description?: string;
  hrEmployeeId?: string;
  targetEntityId?: string;
}

// ── Revenue Schedule (PRA / Erlösabgrenzung) ──────────────────────────

export interface RevenueScheduleEntry {
  id: string;
  documentId: string;
  lineItemIndex: number;
  periodDate: string;
  amount: number;
  revenueAccountNumber: string;
  status: 'planned' | 'booked' | 'cancelled';
  journalEntryId?: string;
  postedAt?: string;
}

export interface DeferredRevenueOverview {
  totalPraBalance: number;
  dueThisMonth: number;
  dueNextMonth: number;
  totalPlannedEntries: number;
  totalBookedEntries: number;
}

export interface PostRevenueEntryResult {
  journalEntryId: string;
  entryNumber: number;
}

export interface PostAllDueResult {
  postedCount: number;
}

export interface CancelRevenueScheduleResult {
  cancelledCount: number;
}

export interface DeleteDocumentPreflight {
  canDelete: boolean;
  blockReason?: string;
  hasBookingSuggestion: boolean;
  hasJournalEntry: boolean;
  journalEntryWillBeReversed: boolean;
  fieldCount: number;
}
