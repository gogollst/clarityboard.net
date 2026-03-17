import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type {
  DashboardDto,
  KpiSnapshot,
  KpiDefinition,
  AlertDto,
} from '@/types/kpi';
import type { WorkingCapital } from '@/types/cashflow';

// ---------------------------------------------------------------------------
// Dashboard & KPI Data
// ---------------------------------------------------------------------------

export function useKpiDashboard(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.kpi.dashboard(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<DashboardDto>(
        '/kpi/dashboard',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
    refetchInterval: 60_000, // refresh every minute as fallback to SignalR
  });
}

export function useKpiHistory(
  entityId: string | null,
  kpiId: string | null,
  startDate?: string,
  endDate?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.kpi.history(entityId ?? '', kpiId ?? ''),
      startDate,
      endDate,
    ],
    queryFn: async () => {
      const { data } = await api.get<KpiSnapshot[]>(
        `/kpi/${kpiId}/history`,
        { params: { from: startDate, to: endDate } },
      );
      return data;
    },
    enabled: !!entityId && !!kpiId,
  });
}

export function useKpiDefinitions() {
  return useQuery({
    queryKey: queryKeys.kpi.definitions(),
    queryFn: async () => {
      const { data } = await api.get<KpiDefinition[]>(
        '/kpi/definitions',
      );
      return data;
    },
    staleTime: 10 * 60 * 1000, // definitions rarely change
  });
}

// ---------------------------------------------------------------------------
// Alerts
// ---------------------------------------------------------------------------

export interface CreateAlertRequest {
  entityId: string;
  kpiId: string;
  severity: AlertDto['severity'];
  title: string;
  thresholdValue: number;
  comparisonOperator: 'gt' | 'gte' | 'lt' | 'lte' | 'eq';
}

export interface UpdateAlertRequest {
  id: string;
  entityId: string;
  severity?: AlertDto['severity'];
  title?: string;
  thresholdValue?: number;
  comparisonOperator?: 'gt' | 'gte' | 'lt' | 'lte' | 'eq';
}

export interface AlertEvent {
  id: string;
  alertId: string;
  eventType: string;
  value: number | null;
  message: string;
  timestamp: string;
}

export function useKpiAlerts(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.kpi.alerts(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<AlertDto[]>(
        '/kpi/alerts',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateKpiAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateAlertRequest) => {
      const { data } = await api.post<AlertDto>(
        '/kpi/alerts',
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Alert created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alerts(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to create alert');
    },
  });
}

export function useUpdateKpiAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpdateAlertRequest) => {
      const { id, entityId: _, ...body } = request; // eslint-disable-line @typescript-eslint/no-unused-vars
      const { data } = await api.put<AlertDto>(
        `/kpi/alerts/${id}`,
        body,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success('Alert updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alerts(variables.entityId),
      });
    },
    onError: () => {
      toast.error('Failed to update alert');
    },
  });
}

export function useDeleteKpiAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      entityId,
    }: {
      id: string;
      entityId: string;
    }) => {
      await api.delete(`/kpi/alerts/${id}`);
      return { entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success('Alert deactivated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alerts(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to deactivate alert');
    },
  });
}

export function useAlertEvents(alertId: string | null) {
  return useQuery({
    queryKey: queryKeys.kpi.alertEvents(alertId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<AlertEvent[]>(
        `/kpi/alerts/${alertId}/events`,
      );
      return data;
    },
    enabled: !!alertId,
  });
}

export function useEntityAlertEvents(
  entityId: string | null,
  status?: 'active' | 'acknowledged' | 'resolved',
) {
  return useQuery({
    queryKey: queryKeys.kpi.entityAlertEvents(entityId ?? '', status),
    queryFn: async () => {
      const { data } = await api.get<AlertDto[]>(
        '/kpi/alert-events',
        { params: { entityId, status } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useAcknowledgeAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      alertId,
      entityId,
    }: {
      alertId: string;
      entityId: string;
    }) => {
      await api.post(`/kpi/alerts/${alertId}/acknowledge`);
      return { alertId, entityId };
    },
    onSuccess: ({ alertId, entityId }) => {
      toast.success('Alert acknowledged');
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alerts(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alertEvents(alertId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.dashboard(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to acknowledge alert');
    },
  });
}

export function useResolveAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      alertId,
      entityId,
    }: {
      alertId: string;
      entityId: string;
    }) => {
      await api.post(`/kpi/alerts/${alertId}/resolve`);
      return { alertId, entityId };
    },
    onSuccess: ({ alertId, entityId }) => {
      toast.success('Alert resolved');
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alerts(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.alertEvents(alertId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.dashboard(entityId),
      });
    },
    onError: () => {
      toast.error('Failed to resolve alert');
    },
  });
}

// ---------------------------------------------------------------------------
// KPI Drill-Down & Working Capital
// ---------------------------------------------------------------------------

export interface KpiDrillDown {
  kpiId: string;
  kpiName: string;
  currentValue: number;
  components: KpiDrillDownComponent[];
}

export interface KpiDrillDownComponent {
  name: string;
  value: number;
  weight: number;
  trend: 'up' | 'down' | 'flat';
}

export function useKpiDrillDown(
  entityId: string | null,
  kpiId: string | null,
  date?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.kpi.drillDown(entityId ?? '', kpiId ?? ''),
      date,
    ],
    queryFn: async () => {
      const { data } = await api.get<KpiDrillDown>(
        '/kpi/drill-down',
        { params: { entityId, kpiId, date } },
      );
      return data;
    },
    enabled: !!entityId && !!kpiId,
  });
}

export function useWorkingCapital(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.kpi.workingCapital(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<WorkingCapital>(
        '/kpi/working-capital',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}
