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
  bookingSuggestion?: BookingSuggestion;
  createdAt: string;
}

export interface BookingSuggestion {
  id: string;
  debitAccount: string;
  creditAccount: string;
  amount: number;
  description: string;
  confidence: number;
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
