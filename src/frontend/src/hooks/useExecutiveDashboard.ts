import { useQueries } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import { useKpiDashboard, useKpiDefinitions, useEntityAlertEvents } from './useKpis';
import {
  getAllSparklineKpiIds,
  getPeriodDates,
  getPriorPeriodDates,
  type Period,
} from '@/features/dashboard/executive-config';
import type { KpiSnapshot } from '@/types/kpi';

export function useExecutiveDashboard(
  entityId: string | null,
  period: Period,
  compareEnabled: boolean,
) {
  const dashboard = useKpiDashboard(entityId);
  const definitions = useKpiDefinitions();
  const alertEvents = useEntityAlertEvents(entityId, 'active');

  const { from, to } = getPeriodDates(period);
  const sparklineKpiIds = getAllSparklineKpiIds();

  // Batch-fetch sparkline history for all KPIs
  const historyQueries = useQueries({
    queries: entityId
      ? sparklineKpiIds.map((kpiId) => ({
          queryKey: [...queryKeys.kpi.history(entityId, kpiId), from, to],
          queryFn: async () => {
            const { data } = await api.get<KpiSnapshot[]>(
              `/kpi/${kpiId}/history`,
              { params: { from, to } },
            );
            return { kpiId, data };
          },
        }))
      : [],
  });

  // Prior-year comparison (lazy — only when compareEnabled)
  const prior = compareEnabled ? getPriorPeriodDates(period) : null;

  const comparisonQueries = useQueries({
    queries: entityId && prior
      ? sparklineKpiIds.map((kpiId) => ({
          queryKey: [...queryKeys.kpi.history(entityId, kpiId), prior.from, prior.to],
          queryFn: async () => {
            const { data } = await api.get<KpiSnapshot[]>(
              `/kpi/${kpiId}/history`,
              { params: { from: prior.from, to: prior.to } },
            );
            return { kpiId, data };
          },
          enabled: compareEnabled,
        }))
      : [],
  });

  // Build lookup: kpiId → number[]
  const historyMap = new Map<string, number[]>();
  for (const q of historyQueries) {
    if (q.data) {
      historyMap.set(q.data.kpiId, q.data.data.map((s) => s.value));
    }
  }

  const comparisonMap = new Map<string, number[]>();
  for (const q of comparisonQueries) {
    if (q.data) {
      comparisonMap.set(q.data.kpiId, q.data.data.map((s) => s.value));
    }
  }

  // Build KPI definition lookup
  const definitionMap = new Map(
    definitions.data?.map((d) => [d.id, d]) ?? [],
  );

  const isLoading =
    dashboard.isLoading ||
    definitions.isLoading ||
    historyQueries.some((q) => q.isLoading);

  const isError = dashboard.isError;

  return {
    dashboard: dashboard.data,
    definitions: definitionMap,
    alertEvents: alertEvents.data ?? [],
    historyMap,
    comparisonMap,
    isLoading,
    isError,
    refetch: dashboard.refetch,
  };
}
