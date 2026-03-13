import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import i18n from '@/i18n';
import type {
  AiProviderConfig,
  AiProvider,
  AiProviderModel,
  AiCallLogFilters,
  AiCallLog,
  AiCallLogStats,
  AiPromptDetail,
  AiPromptListItem,
  EnhancePromptRequest,
  EnhancePromptResponse,
  ProviderTestResult,
  UpdateAiPromptRequest,
  UpsertProviderRequest,
  AddProviderModelRequest,
  UpdateProviderModelRequest,
} from '@/types/ai';
import type { PaginatedResponse } from '@/types/api';

// ── Providers ─────────────────────────────────────────────────────────────────

export function useAiProviders() {
  return useQuery({
    queryKey: queryKeys.ai.providers(),
    queryFn: async () => {
      const { data } = await api.get<AiProviderConfig[]>('/AiManagement/providers');
      return data;
    },
  });
}

export function useUpsertAiProvider() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      provider,
      request,
    }: {
      provider: AiProvider;
      request: UpsertProviderRequest;
    }) => {
      const { data } = await api.put<AiProviderConfig>(
        `/AiManagement/providers/${provider}`,
        request,
      );
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('ai:providers.toast.saveSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.providers() });
    },
    onError: () => toast.error(i18n.t('ai:providers.toast.saveError')),
  });
}

export function useTestAiProvider() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (provider: AiProvider) => {
      const { data } = await api.post<ProviderTestResult>(
        `/AiManagement/providers/${provider}/test`,
      );
      return data;
    },
    onSuccess: (result) => {
      if (result.isHealthy)
        toast.success(i18n.t('ai:providers.toast.testHealthy', { provider: result.provider, durationMs: result.durationMs }));
      else
        toast.error(i18n.t('ai:providers.toast.testFailed', { provider: result.provider, error: result.errorMessage }));
      qc.invalidateQueries({ queryKey: queryKeys.ai.providers() });
    },
    onError: () => toast.error(i18n.t('ai:providers.toast.testError')),
  });
}

// ── Provider Models ──────────────────────────────────────────────────────────

export function useProviderModels(provider?: AiProvider, activeOnly = true) {
  return useQuery({
    queryKey: queryKeys.ai.providerModels(provider),
    queryFn: async () => {
      const { data } = await api.get<AiProviderModel[]>('/AiManagement/provider-models', {
        params: { ...(provider ? { provider } : {}), activeOnly },
      });
      return data;
    },
  });
}

export function useAddProviderModel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (request: AddProviderModelRequest) => {
      const { data } = await api.post<string>('/AiManagement/provider-models', request);
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('ai:providerModels.toast.addSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.providerModels() });
    },
    onError: () => toast.error(i18n.t('ai:providerModels.toast.addError')),
  });
}

export function useUpdateProviderModel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateProviderModelRequest }) => {
      await api.put(`/AiManagement/provider-models/${id}`, request);
    },
    onSuccess: () => {
      toast.success(i18n.t('ai:providerModels.toast.updateSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.providerModels() });
    },
    onError: () => toast.error(i18n.t('ai:providerModels.toast.updateError')),
  });
}

export function useDeleteProviderModel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/AiManagement/provider-models/${id}`);
    },
    onSuccess: () => {
      toast.success(i18n.t('ai:providerModels.toast.deleteSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.providerModels() });
    },
    onError: () => toast.error(i18n.t('ai:providerModels.toast.deleteError')),
  });
}

export function useToggleProviderModel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      await api.patch(`/AiManagement/provider-models/${id}/toggle`, { isActive });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.ai.providerModels() });
    },
  });
}

// ── Prompts ───────────────────────────────────────────────────────────────────

export function useAiPrompts(module?: string) {
  return useQuery({
    queryKey: queryKeys.ai.prompts(module),
    queryFn: async () => {
      const { data } = await api.get<AiPromptListItem[]>('/AiManagement/prompts', {
        params: module ? { module } : undefined,
      });
      return data;
    },
  });
}

export function useAiPromptDetail(promptKey: string) {
  return useQuery({
    queryKey: queryKeys.ai.promptDetail(promptKey),
    queryFn: async () => {
      const { data } = await api.get<AiPromptDetail>(`/AiManagement/prompts/${promptKey}`);
      return data;
    },
    enabled: !!promptKey,
  });
}

export function useUpdateAiPrompt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({
      promptKey,
      request,
    }: {
      promptKey: string;
      request: UpdateAiPromptRequest;
    }) => {
      await api.put(`/AiManagement/prompts/${promptKey}`, request);
    },
    onSuccess: (_, { promptKey }) => {
      toast.success(i18n.t('ai:prompts.toast.saveSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.promptDetail(promptKey) });
      qc.invalidateQueries({ queryKey: queryKeys.ai.prompts() });
    },
    onError: () => toast.error(i18n.t('ai:prompts.toast.saveError')),
  });
}

export function useEnhancePrompt() {
  return useMutation({
    mutationFn: async ({
      promptKey,
      request,
    }: {
      promptKey: string;
      request: EnhancePromptRequest;
    }) => {
      const { data } = await api.post<EnhancePromptResponse>(
        `/AiManagement/prompts/${promptKey}/enhance`,
        request,
      );
      return data.enhancedSystemPrompt;
    },
    onError: () => toast.error(i18n.t('ai:prompts.toast.enhanceError')),
  });
}

export function useRestorePromptVersion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ promptKey, version }: { promptKey: string; version: number }) => {
      await api.post(`/AiManagement/prompts/${promptKey}/versions/${version}/restore`);
    },
    onSuccess: (_, { promptKey }) => {
      toast.success(i18n.t('ai:prompts.toast.restoreSuccess'));
      qc.invalidateQueries({ queryKey: queryKeys.ai.promptDetail(promptKey) });
    },
    onError: () => toast.error(i18n.t('ai:prompts.toast.restoreError')),
  });
}

// ── Call Logs ─────────────────────────────────────────────────────────────────

export function useAiCallLogs(filters?: AiCallLogFilters) {
  return useQuery({
    queryKey: queryKeys.ai.callLogs(filters as Record<string, unknown>),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<AiCallLog>>('/AiManagement/call-logs', {
        params: filters,
      });
      return data;
    },
  });
}

export function useAiCallLogStats(from?: string, to?: string) {
  return useQuery({
    queryKey: queryKeys.ai.callLogStats(from, to),
    queryFn: async () => {
      const { data } = await api.get<AiCallLogStats>('/AiManagement/call-logs/stats', {
        params: { from, to },
      });
      return data;
    },
  });
}

