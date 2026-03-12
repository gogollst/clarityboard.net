import { useEffect, useRef } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
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
  const { isAuthenticated } = useAuthStore();
  const { selectedEntityId } = useEntity();
  const { mutate: switchEntity } = useSwitchEntity();
  const syncedRef = useRef(false);

  useSignalR({ entityId: selectedEntityId, enabled: isAuthenticated });

  // Sync JWT entity_id with selected entity on mount
  useEffect(() => {
    if (!isAuthenticated || !selectedEntityId || syncedRef.current) return;
    const jwtEntityId = getEntityIdFromJwt();
    if (jwtEntityId && jwtEntityId !== selectedEntityId) {
      syncedRef.current = true;
      switchEntity(selectedEntityId);
    }
  }, [isAuthenticated, selectedEntityId, switchEntity]);

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
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
