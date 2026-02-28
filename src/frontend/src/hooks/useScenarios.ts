import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { ApiResponse } from '@/types/api';
import type { Scenario, CreateScenarioRequest } from '@/types/scenario';

export function useScenarios(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.scenarios.list(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<Scenario[]>>(
        '/scenarios',
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useScenario(id: string | null) {
  return useQuery({
    queryKey: queryKeys.scenarios.detail(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<Scenario>>(
        `/scenarios/${id}`,
      );
      return data.data;
    },
    enabled: !!id,
  });
}

export function useCreateScenario() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateScenarioRequest) => {
      const { data } = await api.post<ApiResponse<Scenario>>(
        '/scenarios',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Scenario created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.scenarios.list(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create scenario');
    },
  });
}

export function useRunScenario() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id }: { id: string }) => {
      const { data } = await api.post<ApiResponse<Scenario>>(
        `/scenarios/${id}/run`,
      );
      return data.data;
    },
    onSuccess: (data) => {
      toast.success('Scenario run started');
      queryClient.invalidateQueries({
        queryKey: queryKeys.scenarios.detail(data.id),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.scenarios.list(data.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to run scenario');
    },
  });
}
