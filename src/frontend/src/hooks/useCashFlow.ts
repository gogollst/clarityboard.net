import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { PaginatedResponse } from '@/types/api';
import type {
  CashFlowOverview,
  CashFlowForecast,
  WorkingCapital,
  CashFlowEntry,
  CreateCashFlowEntryRequest,
} from '@/types/cashflow';

// ---------------------------------------------------------------------------
// Overview & Forecast
// ---------------------------------------------------------------------------

export function useCashFlowOverview(
  entityId: string | null,
  startDate?: string,
  endDate?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.cashflow.overview(entityId ?? ''),
      startDate,
      endDate,
    ],
    queryFn: async () => {
      const { data } = await api.get<CashFlowOverview>(
        '/cashflow/overview',
        { params: { entityId, startDate, endDate } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCashFlowForecast(
  entityId: string | null,
  weeks?: number,
) {
  return useQuery({
    queryKey: [...queryKeys.cashflow.forecast(entityId ?? ''), weeks],
    queryFn: async () => {
      const { data } = await api.get<CashFlowForecast>(
        '/cashflow/forecast',
        { params: { entityId, weeks } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCashFlowWorkingCapital(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.cashflow.workingCapital(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<WorkingCapital>(
        '/cashflow/working-capital',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

// ---------------------------------------------------------------------------
// Cash Flow Entries
// ---------------------------------------------------------------------------

interface CashFlowEntryParams {
  page?: number;
  pageSize?: number;
}

export function useCashFlowEntries(
  entityId: string | null,
  params?: CashFlowEntryParams,
) {
  return useQuery({
    queryKey: [...queryKeys.cashflow.entries(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        PaginatedResponse<CashFlowEntry>
      >('/cashflow/entries', {
        params: { entityId, ...params },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateCashFlowEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateCashFlowEntryRequest) => {
      const { data } = await api.post<CashFlowEntry>(
        '/cashflow/entries',
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Cash flow entry created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.cashflow.entries(variables.entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.cashflow.overview(variables.entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.cashflow.forecast(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create cash flow entry');
    },
  });
}
