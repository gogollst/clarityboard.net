import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { ApiResponse, PaginatedResponse } from '@/types/api';
import type {
  JournalEntry,
  CreateJournalEntryRequest,
  TrialBalance,
  ProfitAndLoss,
  BalanceSheet,
  FiscalPeriod,
  CreateFiscalPeriodRequest,
  UpdateFiscalPeriodStatusRequest,
  VatReconciliation,
} from '@/types/accounting';

// ---------------------------------------------------------------------------
// Journal Entries
// ---------------------------------------------------------------------------

interface JournalEntryParams {
  page?: number;
  pageSize?: number;
  startDate?: string;
  endDate?: string;
}

export function useJournalEntries(
  entityId: string | null,
  params?: JournalEntryParams,
) {
  return useQuery({
    queryKey: [...queryKeys.accounting.journalEntries(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        ApiResponse<PaginatedResponse<JournalEntry>>
      >('/accounting/journal-entries', {
        params: { entityId, ...params },
      });
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useJournalEntry(id: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.journalEntry(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<JournalEntry>>(
        `/accounting/journal-entries/${id}`,
      );
      return data.data;
    },
    enabled: !!id,
  });
}

export function useCreateJournalEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateJournalEntryRequest) => {
      const { data } = await api.post<ApiResponse<JournalEntry>>(
        '/accounting/journal-entries',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Journal entry created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(variables.entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create journal entry');
    },
  });
}

export function useReverseJournalEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      entityId,
    }: {
      id: string;
      entityId: string;
    }) => {
      const { data } = await api.post<ApiResponse<JournalEntry>>(
        `/accounting/journal-entries/${id}/reverse`,
      );
      return { entry: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Journal entry reversed');
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to reverse journal entry');
    },
  });
}

// ---------------------------------------------------------------------------
// Financial Reports
// ---------------------------------------------------------------------------

export function useTrialBalance(entityId: string | null, date?: string) {
  return useQuery({
    queryKey: [...queryKeys.accounting.trialBalance(entityId ?? ''), date],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<TrialBalance>>(
        '/accounting/trial-balance',
        { params: { entityId, date } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useProfitAndLoss(
  entityId: string | null,
  startDate?: string,
  endDate?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.accounting.profitAndLoss(entityId ?? ''),
      startDate,
      endDate,
    ],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<ProfitAndLoss>>(
        '/accounting/profit-loss',
        { params: { entityId, startDate, endDate } },
      );
      return data.data;
    },
    enabled: !!entityId && !!startDate && !!endDate,
  });
}

export function useBalanceSheet(entityId: string | null, date?: string) {
  return useQuery({
    queryKey: [...queryKeys.accounting.balanceSheet(entityId ?? ''), date],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<BalanceSheet>>(
        '/accounting/balance-sheet',
        { params: { entityId, date } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

// ---------------------------------------------------------------------------
// Fiscal Periods
// ---------------------------------------------------------------------------

export function useFiscalPeriods(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.fiscalPeriods(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<FiscalPeriod[]>>(
        '/accounting/fiscal-periods',
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useCreateFiscalPeriod() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateFiscalPeriodRequest) => {
      const { data } = await api.post<ApiResponse<FiscalPeriod>>(
        '/accounting/fiscal-periods',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Fiscal period created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.fiscalPeriods(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create fiscal period');
    },
  });
}

export function useUpdateFiscalPeriodStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      status,
      entityId,
    }: UpdateFiscalPeriodStatusRequest & { entityId: string }) => {
      const { data } = await api.put<ApiResponse<FiscalPeriod>>(
        `/accounting/fiscal-periods/${id}/status`,
        { status },
      );
      return { period: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Fiscal period status updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.fiscalPeriods(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to update fiscal period status');
    },
  });
}

// ---------------------------------------------------------------------------
// VAT Reconciliation
// ---------------------------------------------------------------------------

export function useVatReconciliation(
  entityId: string | null,
  startDate?: string,
  endDate?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.accounting.vatReconciliation(entityId ?? ''),
      startDate,
      endDate,
    ],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<VatReconciliation>>(
        '/accounting/vat-reconciliation',
        { params: { entityId, startDate, endDate } },
      );
      return data.data;
    },
    enabled: !!entityId && !!startDate && !!endDate,
  });
}
