import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { ApiResponse, PaginatedResponse } from '@/types/api';
import type {
  Document,
  DocumentListParams,
  UploadDocumentRequest,
  DocumentDownloadUrl,
} from '@/types/document';

// ---------------------------------------------------------------------------
// Document List & Detail
// ---------------------------------------------------------------------------

export function useDocuments(params: DocumentListParams) {
  return useQuery({
    queryKey: [...queryKeys.documents.list(params.entityId), params],
    queryFn: async () => {
      const { data } = await api.get<
        ApiResponse<PaginatedResponse<Document>>
      >('/documents', { params });
      return data.data;
    },
    enabled: !!params.entityId,
  });
}

export function useDocument(id: string | null) {
  return useQuery({
    queryKey: queryKeys.documents.detail(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<Document>>(
        `/documents/${id}`,
      );
      return data.data;
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

      const { data } = await api.post<ApiResponse<Document>>(
        '/documents/upload',
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );
      return data.data;
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
      const { data } = await api.get<ApiResponse<DocumentDownloadUrl>>(
        `/documents/${id}/download`,
      );
      return data.data;
    },
    enabled: !!id,
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
    }: {
      documentId: string;
      entityId: string;
    }) => {
      const { data } = await api.post<ApiResponse<Document>>(
        `/documents/${documentId}/approve-booking`,
      );
      return { document: data.data, entityId };
    },
    onSuccess: ({ document, entityId }) => {
      toast.success('Booking suggestion approved');
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.detail(document.id),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.documents.list(entityId),
      });
      // Also refresh accounting data since a journal entry was created
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
