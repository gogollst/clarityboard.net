export type DocumentStatus = 'uploaded' | 'processing' | 'extracted' | 'review' | 'booked' | 'failed';

export interface Document {
  id: string;
  entityId: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  status: DocumentStatus;
  vendorName?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  totalAmount?: number;
  currency?: string;
  businessPartnerId?: string;
  businessPartnerName?: string;
  businessPartnerNumber?: string;
  suggestedBusinessPartnerId?: string;
  suggestedBusinessPartnerName?: string;
  suggestedBusinessPartnerNumber?: string;
  reviewReasons?: string[];
  bookingSuggestion?: BookingSuggestion;
  createdAt: string;
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
}

export interface DocumentListParams {
  entityId: string;
  page?: number;
  pageSize?: number;
  status?: DocumentStatus;
  search?: string;
}

export interface UploadDocumentRequest {
  file: File;
  entityId: string;
}

export interface DocumentDownloadUrl {
  url: string;
  expiresAt: string;
}

export interface ModifyBookingRequest {
  debitAccountId: string;
  creditAccountId: string;
  amount: number;
  vatCode?: string;
  vatAmount?: number;
  description?: string;
  hrEmployeeId?: string;
}
