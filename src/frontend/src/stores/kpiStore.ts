import { create } from 'zustand';
import type { KpiUpdateMessage, AlertDto } from '@/types/kpi';

interface KpiRealtimeState {
  /** Latest KPI snapshots received via SignalR, keyed by `${entityId}:${kpiId}` */
  kpiUpdates: Map<string, KpiUpdateMessage>;
  /** Real-time alerts received via SignalR */
  realtimeAlerts: AlertDto[];

  /** Merge an incoming KPI update from SignalR into the local store. */
  mergeKpiUpdate: (update: KpiUpdateMessage) => void;
  /** Add a new alert received from SignalR. */
  addAlert: (alert: AlertDto) => void;
  /** Mark a local alert as acknowledged. */
  acknowledgeAlert: (alertId: string) => void;
  /** Mark a local alert as resolved and remove it from the active list. */
  resolveAlert: (alertId: string) => void;
  /** Clear all real-time data (e.g. on entity switch or disconnect). */
  clearAll: () => void;
}

export const useKpiStore = create<KpiRealtimeState>((set) => ({
  kpiUpdates: new Map(),
  realtimeAlerts: [],

  mergeKpiUpdate: (update) =>
    set((state) => {
      const key = `${update.entityId}:${update.kpiId}`;
      const next = new Map(state.kpiUpdates);
      next.set(key, update);
      return { kpiUpdates: next };
    }),

  addAlert: (alert) =>
    set((state) => {
      // Avoid duplicates
      const exists = state.realtimeAlerts.some((a) => a.id === alert.id);
      if (exists) return state;
      return { realtimeAlerts: [alert, ...state.realtimeAlerts] };
    }),

  acknowledgeAlert: (alertId) =>
    set((state) => ({
      realtimeAlerts: state.realtimeAlerts.map((a) =>
        a.id === alertId ? { ...a, status: 'acknowledged' as const } : a,
      ),
    })),

  resolveAlert: (alertId) =>
    set((state) => ({
      realtimeAlerts: state.realtimeAlerts.filter((a) => a.id !== alertId),
    })),

  clearAll: () =>
    set({
      kpiUpdates: new Map(),
      realtimeAlerts: [],
    }),
}));
