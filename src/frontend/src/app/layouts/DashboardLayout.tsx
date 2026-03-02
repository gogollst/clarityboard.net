import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { useEntity } from '@/hooks/useEntity';
import { useSignalR } from '@/hooks/useSignalR';
import Sidebar from '@/components/layout/Sidebar';
import Header from '@/components/layout/Header';

export default function DashboardLayout() {
  const { isAuthenticated } = useAuthStore();
  const { selectedEntityId } = useEntity();

  useSignalR({ entityId: selectedEntityId, enabled: isAuthenticated });

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
