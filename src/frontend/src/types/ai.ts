export type AiProvider = 'Anthropic' | 'OpenAI' | 'Grok' | 'Gemini' | 'ZAI' | 'Manus' | 'DeepL';

export const AI_PROVIDERS: AiProvider[] = ['Anthropic', 'OpenAI', 'Grok', 'Gemini', 'ZAI', 'Manus', 'DeepL'];

// ── Provider Models (fetched from DB) ─────────────────────────────────────────

export interface AiProviderModel {
  id: string;
  provider: AiProvider;
  modelId: string;
  displayName: string;
  sortOrder: number;
  description: string | null;
  isActive: boolean;
}

export interface AddProviderModelRequest {
  provider: AiProvider;
  modelId: string;
  displayName: string;
  sortOrder: number;
  description?: string;
}

export interface UpdateProviderModelRequest {
  displayName: string;
  sortOrder: number;
  description?: string;
}

// ── Provider ──────────────────────────────────────────────────────────────────

export interface AiProviderConfig {
  id: string;
  provider: AiProvider;
  providerName: string;
  /** Masked key hint, e.g. "****...abcd" */
  keyHint: string;
  isActive: boolean;
  isHealthy: boolean;
  lastTestedAt: string | null;
  baseUrl: string | null;
  modelDefault: string | null;
  createdAt: string;
}

export interface ProviderTestResult {
  provider: AiProvider;
  isHealthy: boolean;
  durationMs: number;
  errorMessage: string | null;
}

export interface UpsertProviderRequest {
  apiKey: string;
  baseUrl?: string;
  modelDefault?: string;
}

// ── Prompts ───────────────────────────────────────────────────────────────────

export interface AiPromptListItem {
  id: string;
  promptKey: string;
  name: string;
  module: string;
  primaryProvider: AiProvider;
  fallbackProvider: AiProvider;
  isActive: boolean;
  isSystemPrompt: boolean;
  version: number;
  updatedAt: string;
}

export interface AiPromptDetail {
  id: string;
  promptKey: string;
  name: string;
  description: string;
  module: string;
  functionDescription: string;
  systemPrompt: string;
  userPromptTemplate: string | null;
  exampleInput: string | null;
  exampleOutput: string | null;
  primaryProvider: AiProvider;
  primaryModel: string;
  fallbackProvider: AiProvider;
  fallbackModel: string;
  temperature: number;
  maxTokens: number;
  isActive: boolean;
  isSystemPrompt: boolean;
  version: number;
  createdAt: string;
  updatedAt: string;
  lastEditedByUserId: string | null;
  versions: AiPromptVersion[];
}

export interface AiPromptVersion {
  id: string;
  version: number;
  systemPrompt: string;
  userPromptTemplate: string | null;
  primaryProvider: AiProvider;
  primaryModel: string;
  fallbackProvider: AiProvider;
  fallbackModel: string;
  temperature: number;
  maxTokens: number;
  changeSummary: string;
  createdAt: string;
  createdByUserId: string;
}

export interface UpdateAiPromptRequest {
  systemPrompt: string;
  userPromptTemplate?: string;
  primaryProvider: AiProvider;
  primaryModel: string;
  fallbackProvider: AiProvider;
  fallbackModel: string;
  temperature: number;
  maxTokens: number;
  changeSummary: string;
}

export interface EnhancePromptRequest {
  currentSystemPrompt: string;
  userPromptTemplate?: string;
  description: string;
  functionDescription: string;
}

export interface EnhancePromptResponse {
  enhancedSystemPrompt: string;
}

// ── Call Logs ─────────────────────────────────────────────────────────────────

export interface AiCallLog {
  id: string;
  promptId: string;
  promptKey: string;
  usedProvider: AiProvider;
  usedFallback: boolean;
  inputTokens: number;
  outputTokens: number;
  durationMs: number;
  isSuccess: boolean;
  errorMessage: string | null;
  userId: string | null;
  entityId: string | null;
  createdAt: string;
}

export interface AiCallLogStats {
  totalCalls: number;
  successfulCalls: number;
  successRate: number;
  avgDurationMs: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  fallbackCount: number;
}

export interface AiCallLogFilters {
  promptKey?: string;
  provider?: AiProvider;
  isSuccess?: boolean;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

