import { useEffect, useRef, useState } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { useEntityStore } from '@/stores/entityStore';
import { useEntity, useSwitchEntity } from '@/hooks/useEntity';
import { useSignalR } from '@/hooks/useSignalR';
import { getAccessToken } from '@/lib/api';
import Sidebar from '@/components/layout/Sidebar';
import Header from '@/components/layout/Header';

function getEntityIdFromJwt(): string | null {
  const token = getAccessToken();
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.entity_id ?? null;
  } catch {
    return null;
  }
}

export default function DashboardLayout() {
  const { isAuthenticated, user } = useAuthStore();
  const { selectedEntityId } = useEntity();
  const { mutate: switchEntity } = useSwitchEntity();
  const syncedRef = useRef(false);
  const [entitySynced, setEntitySynced] = useState(() => {
    // Check if JWT already matches on initial render
    if (!selectedEntityId) return true;
    const jwtEntityId = getEntityIdFromJwt();
    return !jwtEntityId || jwtEntityId === selectedEntityId;
  });

  const userEntityIds = user?.entities?.map((e) => e.entityId) ?? [];
  const selectedEntityValid = !selectedEntityId || userEntityIds.includes(selectedEntityId);

  useSignalR({ entityId: selectedEntityId, enabled: isAuthenticated && entitySynced });

  // Stale selectedEntityId bereinigen: wenn nicht in User-Entities, auf erste Entity setzen
  useEffect(() => {
    if (!isAuthenticated || !user?.entities?.length) return;
    const ids = user.entities.map((e) => e.entityId);
    if (selectedEntityId && !ids.includes(selectedEntityId)) {
      useEntityStore.getState().setSelectedEntity(user.entities[0].entityId);
    }
  }, [isAuthenticated, user?.entities, selectedEntityId]);

  // Sync JWT entity_id with selected entity on mount
  useEffect(() => {
    if (!isAuthenticated || !selectedEntityId || !selectedEntityValid || syncedRef.current) return;
    const jwtEntityId = getEntityIdFromJwt();
    if (jwtEntityId && jwtEntityId !== selectedEntityId) {
      syncedRef.current = true;
      setEntitySynced(false);
      switchEntity(selectedEntityId, {
        onSettled: () => setEntitySynced(true),
      });
    } else {
      setEntitySynced(true);
    }
  }, [isAuthenticated, selectedEntityId, selectedEntityValid, switchEntity]);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Sidebar */}
      <Sidebar />

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Header */}
        <Header />

        {/* Page content */}
        <main className="scrollbar-thin flex-1 overflow-y-auto bg-background p-6">
          <div className="page-enter">
            {entitySynced ? <Outlet /> : (
              <div className="flex h-64 items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
