import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { useEntityStore } from '@/stores/entityStore';
import type { LegalEntity, CreateEntityRequest, UpdateEntityRequest } from '@/types/entity';

export function useEntity() {
  const { entities, selectedEntityId, setSelectedEntity } = useEntityStore();

  const selectedEntity = entities.find((e) => e.id === selectedEntityId) ?? null;

  return {
    entities,
    selectedEntity,
    selectedEntityId,
    switchEntity: setSelectedEntity,
  };
}

export function useEntities() {
  return useQuery({
    queryKey: queryKeys.entities.list(),
    queryFn: async () => {
      const { data } = await api.get<LegalEntity[]>('/entity');
      return data;
    },
  });
}

export function useCreateEntity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateEntityRequest) => {
      const { data } = await api.post<LegalEntity>('/entity', request);
      return data;
    },
    onSuccess: (data) => {
      toast.success(`Entity "${data.name}" created successfully.`);
      queryClient.invalidateQueries({ queryKey: queryKeys.entities.list() });
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { data?: { detail?: string; errors?: Record<string, string[]> } } };
      const detail = axiosError.response?.data?.detail;
      const errors = axiosError.response?.data?.errors;
      const message = detail
        ?? (errors ? Object.values(errors).flat().join('; ') : null)
        ?? 'Failed to create entity';
      toast.error(message);
    },
  });
}

export function useUpdateEntity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...request }: UpdateEntityRequest & { id: string }) => {
      const { data } = await api.put<LegalEntity>(`/entity/${id}`, request);
      return data;
    },
    onSuccess: (data) => {
      toast.success(`Entity "${data.name}" updated successfully.`);
      queryClient.invalidateQueries({ queryKey: queryKeys.entities.list() });
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { data?: { detail?: string; errors?: Record<string, string[]> } } };
      const detail = axiosError.response?.data?.detail;
      const errors = axiosError.response?.data?.errors;
      const message = detail
        ?? (errors ? Object.values(errors).flat().join('; ') : null)
        ?? 'Failed to update entity';
      toast.error(message);
    },
  });
}

export function useSetEntityActive() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      await api.patch(`/entity/${id}/active`, { isActive });
    },
    onSuccess: (_, variables) => {
      toast.success(variables.isActive ? 'Entity reactivated.' : 'Entity deactivated.');
      queryClient.invalidateQueries({ queryKey: queryKeys.entities.list() });
    },
    onError: () => {
      toast.error('Failed to update entity status');
    },
  });
}
