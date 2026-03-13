import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { PaginatedResponse } from '@/types/api';
import type {
  Document,
  DocumentListParams,
  UploadDocumentRequest,
  DocumentDownloadUrl,
  ModifyBookingRequest,
} from '@/types/document';

// ---------------------------------------------------------------------------
// Document List & Detail
// ---------------------------------------------------------------------------

export function useDocuments(params: DocumentListParams) {
  return useQuery({
    queryKey: [...queryKeys.documents.list(params.entityId), params],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<Document>>(
        '/documents',
        { params },
      );
      return data;
    },
    enabled: !!params.entityId,
  });
}

export function useDocument(id: string | null) {
  return useQuery({
    queryKey: queryKeys.documents.detail(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<Document>(`/documents/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

// ---------------------------------------------------------------------------
// Upload
// ---------------------------------------------------------------------------

export function useUploadDocument() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UploadDocumentRequest) => {
      const formData = new FormData();
      formData.append('file', request.file);
      formData.append('entityId', request.entityId);

      const { data } = await api.post<{ documentId: string }>(
        '/documents/upload',
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );
      return { id: data.documentId, status: 'uploaded' as const };
    },
    onSuccess: (_data, variables) => {
      toast.success('Document uploaded successfully');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to upload document');
    },
  });
}

// ---------------------------------------------------------------------------
// Download URL
// ---------------------------------------------------------------------------

export function useDocumentDownloadUrl(id: string | null) {
  return useQuery({
    queryKey: queryKeys.documents.downloadUrl(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<DocumentDownloadUrl>(
        `/documents/${id}/download`,
      );
      return data;
    },
    enabled: !!id,
  });
}

// ---------------------------------------------------------------------------
// Confirm Partner Match (fuzzy match confirmation)
// ---------------------------------------------------------------------------

export function useConfirmPartnerMatch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      businessPartnerId,
      entityId,
    }: {
      documentId: string;
      businessPartnerId: string;
      entityId: string;
    }) => {
      await api.post(`/accounting/documents/${documentId}/confirm-partner`, {
        businessPartnerId,
      });
      return { documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Business partner confirmed');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(documentId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to confirm business partner');
    },
  });
}

// ---------------------------------------------------------------------------
// Approve AI Booking Suggestion
// ---------------------------------------------------------------------------

export function useApproveBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      entityId,
      hrEmployeeId,
    }: {
      documentId: string;
      entityId: string;
      hrEmployeeId?: string;
    }) => {
      const { data } = await api.post<Document>(
        `/documents/${documentId}/approve-booking`,
        hrEmployeeId ? { hrEmployeeId } : undefined,
      );
      return { document: data, entityId };
    },
    onSuccess: ({ document, entityId }) => {
      toast.success('Booking suggestion approved');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(document.id),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to approve booking suggestion');
    },
  });
}

// ---------------------------------------------------------------------------
// Modify Booking Suggestion
// ---------------------------------------------------------------------------

export function useModifyBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      entityId,
      ...request
    }: ModifyBookingRequest & { documentId: string; entityId: string }) => {
      const { data } = await api.post<string>(
        `/documents/${documentId}/modify-booking`,
        request,
      );
      return { journalEntryId: data, documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Booking created with modifications');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(documentId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to modify booking suggestion');
    },
  });
}

// ---------------------------------------------------------------------------
// Reject Booking Suggestion
// ---------------------------------------------------------------------------

export function useRejectBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      entityId,
      reason,
    }: {
      documentId: string;
      entityId: string;
      reason?: string;
    }) => {
      await api.post(`/documents/${documentId}/reject-booking`, reason ? { reason } : undefined);
      return { documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Booking suggestion rejected');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(documentId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to reject booking suggestion');
    },
  });
}
