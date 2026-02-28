import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { ApiResponse, PaginatedResponse } from '@/types/api';
import type {
  FixedAsset,
  Anlagenspiegel,
  RegisterAssetRequest,
  DisposeAssetRequest,
} from '@/types/asset';

// ---------------------------------------------------------------------------
// Asset Queries
// ---------------------------------------------------------------------------

interface AssetListParams {
  page?: number;
  pageSize?: number;
}

export function useAssets(
  entityId: string | null,
  params?: AssetListParams,
) {
  return useQuery({
    queryKey: [...queryKeys.assets.list(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        ApiResponse<PaginatedResponse<FixedAsset>>
      >('/assets', {
        params: { entityId, ...params },
      });
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useAsset(id: string | null) {
  return useQuery({
    queryKey: queryKeys.assets.detail(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<FixedAsset>>(
        `/assets/${id}`,
      );
      return data.data;
    },
    enabled: !!id,
  });
}

export function useAnlagenspiegel(
  entityId: string | null,
  year: number,
) {
  return useQuery({
    queryKey: queryKeys.assets.anlagenspiegel(entityId ?? '', year),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<Anlagenspiegel>>(
        '/assets/anlagenspiegel',
        { params: { entityId, year } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

// ---------------------------------------------------------------------------
// Asset Mutations
// ---------------------------------------------------------------------------

export function useRegisterAsset() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: RegisterAssetRequest) => {
      const { data } = await api.post<ApiResponse<FixedAsset>>(
        '/assets',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Asset registered');
      queryClient.invalidateQueries({
        queryKey: queryKeys.assets.list(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to register asset');
    },
  });
}

export function useDisposeAsset() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      request,
      entityId,
    }: {
      request: DisposeAssetRequest;
      entityId: string;
    }) => {
      const { data } = await api.post<ApiResponse<FixedAsset>>(
        `/assets/${request.id}/dispose`,
        request,
      );
      return { asset: data.data, entityId };
    },
    onSuccess: ({ asset, entityId }) => {
      toast.success('Asset disposed');
      queryClient.invalidateQueries({
        queryKey: queryKeys.assets.detail(asset.id),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.assets.list(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to dispose asset');
    },
  });
}
