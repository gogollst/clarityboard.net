import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { ApiResponse } from '@/types/api';
import type { DatevExport, TriggerDatevExportRequest } from '@/types/admin';

// ---------------------------------------------------------------------------
// DATEV Export Queries
// ---------------------------------------------------------------------------

export function useDatevExports(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.datev.exports(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<DatevExport[]>>(
        '/datev/exports',
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useDatevDownloadUrl(exportId: string | null) {
  return useQuery({
    queryKey: queryKeys.datev.downloadUrl(exportId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<{ url: string }>>(
        `/datev/exports/${exportId}/download`,
      );
      return data.data;
    },
    enabled: !!exportId,
  });
}

// ---------------------------------------------------------------------------
// DATEV Export Mutation
// ---------------------------------------------------------------------------

export function useTriggerExport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: TriggerDatevExportRequest) => {
      const { data } = await api.post<ApiResponse<DatevExport>>(
        '/datev/export',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success('DATEV export triggered');
      queryClient.invalidateQueries({
        queryKey: queryKeys.datev.exports(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to trigger DATEV export');
    },
  });
}
