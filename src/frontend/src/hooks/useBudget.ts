import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type {
  Budget,
  PlanVsActual,
  BudgetRevision,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  CreateBudgetRevisionRequest,
} from '@/types/budget';

// ---------------------------------------------------------------------------
// Budget Queries
// ---------------------------------------------------------------------------

export function useBudgets(entityId: string | null, year?: number) {
  return useQuery({
    queryKey: [...queryKeys.budget.list(entityId ?? ''), year],
    queryFn: async () => {
      const { data } = await api.get<Budget[]>('/budget', {
        params: { entityId, year },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useBudget(id: string | null) {
  return useQuery({
    queryKey: queryKeys.budget.detail(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<Budget>(
        `/budget/${id}`,
      );
      return data;
    },
    enabled: !!id,
  });
}

export function usePlanVsActual(budgetId: string | null) {
  return useQuery({
    queryKey: queryKeys.budget.planVsActual(budgetId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<PlanVsActual>(
        `/budget/${budgetId}/plan-vs-actual`,
      );
      return data;
    },
    enabled: !!budgetId,
  });
}

// ---------------------------------------------------------------------------
// Budget Mutations
// ---------------------------------------------------------------------------

export function useCreateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateBudgetRequest) => {
      const { data } = await api.post<Budget>(
        '/budget',
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Budget created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.budget.list(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create budget');
    },
  });
}

export function useUpdateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpdateBudgetRequest) => {
      const { data } = await api.put<Budget>(
        `/budget/${request.id}`,
        request,
      );
      return data;
    },
    onSuccess: (data) => {
      toast.success('Budget updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.budget.detail(data.id),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.budget.list(data.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to update budget');
    },
  });
}

export function useCreateBudgetRevision() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateBudgetRevisionRequest) => {
      const { data } = await api.post<BudgetRevision>(
        `/budget/${request.budgetId}/revisions`,
        { note: request.note },
      );
      return { revision: data, budgetId: request.budgetId };
    },
    onSuccess: ({ budgetId }) => {
      toast.success('Budget revision created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.budget.detail(budgetId),
      });
    },
    onError: () => {
      toast.error('Failed to create budget revision');
    },
  });
}
