import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type {
  AiProviderConfig,
  AiProvider,
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
      toast.success('API key saved and tested successfully');
      qc.invalidateQueries({ queryKey: queryKeys.ai.providers() });
    },
    onError: () => toast.error('Failed to save API key'),
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
      if (result.isHealthy) toast.success(`${result.provider} is healthy (${result.durationMs}ms)`);
      else toast.error(`${result.provider} test failed: ${result.errorMessage}`);
      qc.invalidateQueries({ queryKey: queryKeys.ai.providers() });
    },
    onError: () => toast.error('Provider test failed'),
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
      toast.success('Prompt saved');
      qc.invalidateQueries({ queryKey: queryKeys.ai.promptDetail(promptKey) });
      qc.invalidateQueries({ queryKey: queryKeys.ai.prompts() });
    },
    onError: () => toast.error('Failed to save prompt'),
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
    onError: () => toast.error('Prompt enhancement failed'),
  });
}

export function useRestorePromptVersion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ promptKey, version }: { promptKey: string; version: number }) => {
      await api.post(`/AiManagement/prompts/${promptKey}/versions/${version}/restore`);
    },
    onSuccess: (_, { promptKey }) => {
      toast.success('Version restored');
      qc.invalidateQueries({ queryKey: queryKeys.ai.promptDetail(promptKey) });
    },
    onError: () => toast.error('Failed to restore version'),
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

