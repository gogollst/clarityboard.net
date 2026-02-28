import { useEffect, useRef, useCallback } from 'react';
import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HubConnectionState,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useUiStore } from '@/stores/uiStore';
import { useKpiStore } from '@/stores/kpiStore';
import { getAccessToken } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { KpiUpdateMessage, AlertDto } from '@/types/kpi';

/**
 * Exponential backoff retry policy for SignalR reconnection.
 * Retries at 0s, 2s, 5s, 10s, 30s, then every 30s.
 */
const RETRY_DELAYS_MS = [0, 2_000, 5_000, 10_000, 30_000];

function retryPolicy(retryContext: { previousRetryCount: number }) {
  const index = Math.min(
    retryContext.previousRetryCount,
    RETRY_DELAYS_MS.length - 1,
  );
  return RETRY_DELAYS_MS[index];
}

interface UseSignalROptions {
  /** Current entity id to join the entity-scoped SignalR group. */
  entityId: string | null;
  /** Whether the user is authenticated (controls connection lifecycle). */
  enabled: boolean;
}

interface UseSignalRReturn {
  isConnected: boolean;
}

/**
 * SignalR connection manager hook.
 *
 * Connects to the `/hubs/kpi` and `/hubs/alert` hubs on mount when enabled.
 * Automatically reconnects with exponential backoff.
 * Joins entity-scoped groups when the entityId changes.
 * Pushes real-time KPI updates and alerts into the Zustand kpiStore
 * and invalidates relevant TanStack Query caches.
 */
export function useSignalR({
  entityId,
  enabled,
}: UseSignalROptions): UseSignalRReturn {
  const queryClient = useQueryClient();
  const setConnectionStatus = useUiStore((s) => s.setConnectionStatus);
  const connectionStatus = useUiStore((s) => s.connectionStatus);
  const { mergeKpiUpdate, addAlert, clearAll } = useKpiStore();

  const kpiHubRef = useRef<HubConnection | null>(null);
  const alertHubRef = useRef<HubConnection | null>(null);
  const currentEntityRef = useRef<string | null>(null);

  // -----------------------------------------------------------------------
  // Build a hub connection with shared config
  // -----------------------------------------------------------------------
  const buildConnection = useCallback((hubUrl: string) => {
    return new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: retryPolicy })
      .configureLogging(LogLevel.Warning)
      .build();
  }, []);

  // -----------------------------------------------------------------------
  // Join / leave entity groups
  // -----------------------------------------------------------------------
  const joinEntityGroup = useCallback(
    async (connection: HubConnection, newEntityId: string) => {
      if (connection.state !== HubConnectionState.Connected) return;
      try {
        await connection.invoke('JoinEntityGroup', newEntityId);
      } catch {
        // Silently handle - group join will be retried on reconnect
      }
    },
    [],
  );

  const leaveEntityGroup = useCallback(
    async (connection: HubConnection, oldEntityId: string) => {
      if (connection.state !== HubConnectionState.Connected) return;
      try {
        await connection.invoke('LeaveEntityGroup', oldEntityId);
      } catch {
        // Silently handle
      }
    },
    [],
  );

  // -----------------------------------------------------------------------
  // Connection lifecycle
  // -----------------------------------------------------------------------
  useEffect(() => {
    if (!enabled) {
      // Disconnect if disabled
      kpiHubRef.current?.stop();
      alertHubRef.current?.stop();
      kpiHubRef.current = null;
      alertHubRef.current = null;
      setConnectionStatus('disconnected');
      clearAll();
      return;
    }

    const kpiHub = buildConnection('/hubs/kpi');
    const alertHub = buildConnection('/hubs/alerts');

    // -- KPI Hub handlers --
    kpiHub.on('KpiUpdated', (update: KpiUpdateMessage) => {
      mergeKpiUpdate(update);
      // Invalidate the dashboard query so it picks up fresh data
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.dashboard(update.entityId),
      });
    });

    kpiHub.onreconnecting(() => setConnectionStatus('reconnecting'));
    kpiHub.onreconnected(async () => {
      setConnectionStatus('connected');
      // Re-join entity group after reconnect
      if (currentEntityRef.current) {
        await joinEntityGroup(kpiHub, currentEntityRef.current);
      }
    });
    kpiHub.onclose(() => setConnectionStatus('disconnected'));

    // -- Alert Hub handlers --
    alertHub.on('AlertTriggered', (alert: AlertDto) => {
      addAlert(alert);
      if (alert.kpiId) {
        queryClient.invalidateQueries({
          queryKey: queryKeys.kpi.alerts(alert.kpiId),
        });
      }
      queryClient.invalidateQueries({
        queryKey: queryKeys.kpi.all,
      });
    });

    alertHub.on('AlertAcknowledged', (alertId: string) => {
      useKpiStore.getState().acknowledgeAlert(alertId);
    });

    alertHub.on('AlertResolved', (alertId: string) => {
      useKpiStore.getState().resolveAlert(alertId);
    });

    alertHub.onreconnecting(() => {
      // KPI hub already manages the shared status
    });
    alertHub.onreconnected(async () => {
      if (currentEntityRef.current) {
        await joinEntityGroup(alertHub, currentEntityRef.current);
      }
    });

    // -- Start connections --
    const startConnections = async () => {
      const results = await Promise.allSettled([
        kpiHub.start(),
        alertHub.start(),
      ]);
      const anyConnected = results.some((r) => r.status === 'fulfilled');
      setConnectionStatus(anyConnected ? 'connected' : 'disconnected');

      // Join entity group if we already have an entityId
      if (entityId) {
        currentEntityRef.current = entityId;
        if (results[0].status === 'fulfilled')
          await joinEntityGroup(kpiHub, entityId);
        if (results[1].status === 'fulfilled')
          await joinEntityGroup(alertHub, entityId);
      }
    };

    kpiHubRef.current = kpiHub;
    alertHubRef.current = alertHub;
    startConnections();

    return () => {
      kpiHub.stop();
      alertHub.stop();
      kpiHubRef.current = null;
      alertHubRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled]);

  // -----------------------------------------------------------------------
  // Handle entity changes (join new group, leave old group)
  // -----------------------------------------------------------------------
  useEffect(() => {
    const prev = currentEntityRef.current;
    if (prev === entityId) return;

    const switchGroups = async () => {
      const kpiHub = kpiHubRef.current;
      const alertHub = alertHubRef.current;

      if (prev) {
        if (kpiHub) await leaveEntityGroup(kpiHub, prev);
        if (alertHub) await leaveEntityGroup(alertHub, prev);
      }

      if (entityId) {
        if (kpiHub) await joinEntityGroup(kpiHub, entityId);
        if (alertHub) await joinEntityGroup(alertHub, entityId);
      }

      currentEntityRef.current = entityId;
      clearAll();
    };

    switchGroups();
  }, [entityId, joinEntityGroup, leaveEntityGroup, clearAll]);

  return {
    isConnected: connectionStatus === 'connected',
  };
}
