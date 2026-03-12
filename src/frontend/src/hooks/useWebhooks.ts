import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { PaginatedResponse } from '@/types/api';
import type {
  WebhookConfig,
  MappingRule,
  CreateWebhookConfigRequest,
  UpdateWebhookConfigRequest,
  CreateMappingRuleRequest,
  UpdateMappingRuleRequest,
  WebhookDeadLetterEvent,
} from '@/types/webhook';

// ---------------------------------------------------------------------------
// Webhook Configs
// ---------------------------------------------------------------------------

export function useWebhookConfigs(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.webhooks.configs(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<WebhookConfig[]>(
        '/webhook-config',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateWebhookConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateWebhookConfigRequest) => {
      const { data } = await api.post<WebhookConfig>(
        '/webhook-config',
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Webhook configuration created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.configs(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create webhook configuration');
    },
  });
}

export function useUpdateWebhookConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      request,
      entityId,
    }: {
      request: UpdateWebhookConfigRequest;
      entityId: string;
    }) => {
      const { data } = await api.put<WebhookConfig>(
        `/webhook-config/${request.id}`,
        request,
      );
      return { config: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Webhook configuration updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.configs(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to update webhook configuration');
    },
  });
}

// ---------------------------------------------------------------------------
// Mapping Rules
// ---------------------------------------------------------------------------

export function useCreateMappingRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      request,
      entityId,
    }: {
      request: CreateMappingRuleRequest;
      entityId: string;
    }) => {
      const { data } = await api.post<MappingRule>(
        `/webhook-config/${request.webhookConfigId}/mapping-rules`,
        request,
      );
      return { rule: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Mapping rule created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.configs(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create mapping rule');
    },
  });
}

export function useUpdateMappingRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      request,
      entityId,
    }: {
      request: UpdateMappingRuleRequest;
      entityId: string;
    }) => {
      const { data } = await api.put<MappingRule>(
        `/webhook-config/mapping-rules/${request.id}`,
        request,
      );
      return { rule: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Mapping rule updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.configs(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to update mapping rule');
    },
  });
}

export function useDeleteMappingRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      ruleId,
      entityId,
    }: {
      ruleId: string;
      entityId: string;
    }) => {
      await api.delete(`/webhook-config/mapping-rules/${ruleId}`);
      return { entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Mapping rule deleted');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.configs(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to delete mapping rule');
    },
  });
}

// ---------------------------------------------------------------------------
// Dead Letter Queue
// ---------------------------------------------------------------------------

interface DeadLetterParams {
  page?: number;
  pageSize?: number;
}

export function useDeadLetterQueue(
  entityId: string | null,
  params?: DeadLetterParams,
) {
  return useQuery({
    queryKey: [...queryKeys.webhooks.deadLetter(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        PaginatedResponse<WebhookDeadLetterEvent>
      >('/webhook-config/dead-letter', {
        params: { entityId, ...params },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useRetryDeadLetter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      eventId,
      entityId,
    }: {
      eventId: string;
      entityId: string;
    }) => {
      await api.post(`/webhook-config/dead-letter/${eventId}/retry`);
      return { entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Dead letter event retried');
      queryClient.invalidateQueries({
        queryKey: queryKeys.webhooks.deadLetter(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to retry dead letter event');
    },
  });
}
