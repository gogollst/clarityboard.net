import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';

export default function AuthLayout() {
  const { isAuthenticated } = useAuthStore();

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-900 [background-image:radial-gradient(ellipse_at_top,rgba(79,70,229,0.15)_0%,transparent_60%)]">
      <div className="w-full max-w-md px-4">
        <Outlet />
      </div>
    </div>
  );
}
