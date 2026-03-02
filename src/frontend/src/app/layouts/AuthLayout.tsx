import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { BarChart3 } from 'lucide-react';

export default function AuthLayout() {
  const { isAuthenticated } = useAuthStore();

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="relative flex min-h-screen bg-[#faf9f4]">
      {/* Subtle warm grain texture overlay */}
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.03]"
        style={{
          backgroundImage:
            'url("data:image/svg+xml,%3Csvg viewBox=\'0 0 256 256\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cfilter id=\'noise\'%3E%3CfeTurbulence type=\'fractalNoise\' baseFrequency=\'0.9\' numOctaves=\'4\' stitchTiles=\'stitch\'/%3E%3C/filter%3E%3Crect width=\'100%25\' height=\'100%25\' filter=\'url(%23noise)\'/%3E%3C/svg%3E")',
        }}
      />

      {/* Left panel – branding (hidden on mobile) */}
      <div className="hidden lg:flex lg:w-[480px] lg:flex-col lg:justify-between lg:border-r lg:border-[#e5e0d8] lg:bg-[#f5f3ec] lg:px-12 lg:py-12">
        {/* Logo */}
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-[#d97757]/15 ring-1 ring-[#d97757]/25">
            <BarChart3 className="h-4 w-4 text-[#d97757]" />
          </div>
          <span className="font-display text-base font-medium tracking-tight text-[#131314]">
            Clarity Board
          </span>
        </div>

        {/* Tagline */}
        <div className="space-y-4">
          <h1 className="font-display text-3xl font-light leading-[1.1] tracking-[-0.04em] text-[#131314]">
            Financial clarity,<br />
            <em className="not-italic text-[#d97757]">at a glance.</em>
          </h1>
          <p className="text-sm leading-relaxed text-[#6b6865]">
            KPIs, cash flow, and planning tools built for teams that care about the details.
          </p>
        </div>

        {/* Footer note */}
        <p className="text-xs text-[#9b958f]">
          © {new Date().getFullYear()} Clarity Board
        </p>
      </div>

      {/* Right panel – form */}
      <div className="flex flex-1 items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm">
          {/* Mobile-only logo */}
          <div className="mb-8 flex items-center gap-2.5 lg:hidden">
            <div className="flex h-7 w-7 items-center justify-center rounded-md bg-[#d97757]/15 ring-1 ring-[#d97757]/25">
              <BarChart3 className="h-3.5 w-3.5 text-[#d97757]" />
            </div>
            <span className="font-display text-sm font-medium text-[#131314]">
              Clarity Board
            </span>
          </div>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
