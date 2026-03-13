import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import i18n from '@/i18n';
import type { PaginatedResponse } from '@/types/api';
import type {
  DocumentListItem,
  DocumentDetail,
  DocumentListParams,
  UploadDocumentRequest,
  DocumentDownloadUrl,
  ModifyBookingRequest,
  DeleteDocumentPreflight,
} from '@/types/document';

// ---------------------------------------------------------------------------
// Document List & Detail
// ---------------------------------------------------------------------------

export function useDocuments(params: DocumentListParams) {
  return useQuery({
    queryKey: [...queryKeys.documents.list(params.entityId), params],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<DocumentListItem>>(
        '/documents',
        { params },
      );
      return data;
    },
    enabled: !!params.entityId,
  });
}

export function useDocument(entityId: string | null, id: string | null) {
  return useQuery({
    queryKey: queryKeys.documents.detail(entityId ?? '', id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<DocumentDetail>(`/documents/${id}`, {
        params: { entityId },
      });
      return data;
    },
    enabled: !!entityId && !!id,
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

export function useDocumentDownloadUrl(entityId: string | null, id: string | null) {
  return useQuery({
    queryKey: queryKeys.documents.downloadUrl(entityId ?? '', id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<DocumentDownloadUrl>(
        `/documents/${id}/download`,
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId && !!id,
  });
}

// ---------------------------------------------------------------------------
// Reprocess Document
// ---------------------------------------------------------------------------

export function useReprocessDocument() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      entityId,
    }: {
      documentId: string;
      entityId: string;
    }) => {
      await api.post(`/documents/${documentId}/reprocess`, null, {
        params: { entityId },
      });
      return { documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Document reprocessing started');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(entityId, documentId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to reprocess document');
    },
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
        queryKey: queryKeys.documents.detail(entityId, documentId),
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
      targetEntityId,
    }: {
      documentId: string;
      entityId: string;
      hrEmployeeId?: string;
      targetEntityId?: string;
    }) => {
      const body: Record<string, string | undefined> = {};
      if (hrEmployeeId) body.hrEmployeeId = hrEmployeeId;
      if (targetEntityId) body.targetEntityId = targetEntityId;
      const { data } = await api.post<string>(
        `/documents/${documentId}/approve-booking`,
        Object.keys(body).length > 0 ? body : undefined,
        { params: { entityId } },
      );
      return { journalEntryId: data, documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Booking suggestion approved');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(entityId, documentId),
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
        { params: { entityId } },
      );
      return { journalEntryId: data, documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Booking created with modifications');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(entityId, documentId),
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
// Delete Document (Preflight + Execute)
// ---------------------------------------------------------------------------

export function useDeleteDocumentPreflight(entityId: string | null, documentId: string | null) {
  return useQuery({
    queryKey: [...queryKeys.documents.detail(entityId ?? '', documentId ?? ''), 'delete-preflight'],
    queryFn: async () => {
      const { data } = await api.get<DeleteDocumentPreflight>(
        `/documents/${documentId}/delete-preflight`,
        { params: { entityId } },
      );
      return data;
    },
    enabled: false, // Only fetch on demand
  });
}

export function useDeleteDocument() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      entityId,
    }: {
      documentId: string;
      entityId: string;
    }) => {
      await api.delete(`/documents/${documentId}`, {
        params: { entityId },
      });
      return { documentId, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('documents:toast.documentDeleted'));
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
      toast.error(i18n.t('documents:toast.documentDeleteError'));
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
      await api.post(
        `/documents/${documentId}/reject-booking`,
        reason ? { reason } : undefined,
        { params: { entityId } },
      );
      return { documentId, entityId };
    },
    onSuccess: ({ documentId, entityId }) => {
      toast.success('Booking suggestion rejected');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(entityId, documentId),
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
