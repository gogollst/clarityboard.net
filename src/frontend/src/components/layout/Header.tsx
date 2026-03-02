import {
  Menu,
  Bell,
  User,
  LogOut,
  Settings,
  ChevronDown,
  Building2,
  Wifi,
  WifiOff,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/useAuth';
import { useEntity } from '@/hooks/useEntity';
import { useUiStore } from '@/stores/uiStore';
import { Button } from '@/components/ui/button';
import { Link } from 'react-router-dom';
import { useState, useRef, useEffect } from 'react';

export default function Header() {
  const { user, logout } = useAuth();
  const { entities, selectedEntityId, switchEntity } = useEntity();
  const { toggleSidebar, connectionStatus } = useUiStore();

  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // Close user menu on outside click
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        userMenuRef.current &&
        !userMenuRef.current.contains(event.target as Node)
      ) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const connectionConfig = {
    connected: {
      dot: 'bg-emerald-500',
      label: 'Connected',
    },
    reconnecting: {
      dot: 'bg-amber-500 pulse-ring',
      label: 'Reconnecting...',
    },
    disconnected: {
      dot: 'bg-red-500',
      label: 'Disconnected',
    },
  }[connectionStatus];

  return (
    <header className="flex h-14 items-center justify-between border-b border-border bg-card px-4 [box-shadow:0_1px_3px_rgba(0,0,0,0.06)]">
      {/* Left section */}
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleSidebar}
          aria-label="Toggle sidebar"
        >
          <Menu className="h-5 w-5" />
        </Button>
      </div>

      {/* Center: Entity Selector */}
      <div className="flex items-center gap-2">
        <div className="flex items-center gap-1.5 rounded-lg border border-border bg-secondary px-3 py-1.5">
          <Building2 className="h-3.5 w-3.5 text-muted-foreground" />
          <select
            value={selectedEntityId ?? ''}
            onChange={(e) => switchEntity(e.target.value)}
            className="bg-transparent text-sm font-medium text-foreground focus:outline-none"
          >
            {entities.length === 0 && (
              <option value="" disabled>
                No entities
              </option>
            )}
            {entities.map((entity) => (
              <option key={entity.id} value={entity.id}>
                {entity.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Right section */}
      <div className="flex items-center gap-2">
        {/* Connection Status */}
        <div
          className="flex items-center gap-1.5 rounded-md px-2 py-1"
          title={connectionConfig.label}
        >
          {connectionStatus === 'disconnected' ? (
            <WifiOff className="h-3.5 w-3.5 text-red-500" />
          ) : (
            <Wifi className="h-3.5 w-3.5 text-muted-foreground" />
          )}
          <span
            className={cn('h-2 w-2 rounded-full', connectionConfig.dot)}
            aria-label={connectionConfig.label}
          />
        </div>

        {/* Notification Bell */}
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="h-4.5 w-4.5" />
        </Button>

        {/* User Dropdown */}
        <div className="relative" ref={userMenuRef}>
          <Button
            variant="ghost"
            className="flex items-center gap-2 rounded-lg px-2 py-1.5"
            onClick={() => setUserMenuOpen(!userMenuOpen)}
          >
            <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 ring-1 ring-primary/20">
              <User className="h-3.5 w-3.5 text-primary" />
            </div>
            <span className="hidden text-sm font-medium md:inline-block">
              {user ? `${user.firstName} ${user.lastName}` : 'User'}
            </span>
            <ChevronDown className="h-3 w-3 text-muted-foreground" />
          </Button>

          {userMenuOpen && (
            <div className="absolute right-0 top-full z-50 mt-1.5 w-56 rounded-xl border border-border bg-popover p-1 shadow-card-hover">
              <div className="px-3 py-2.5">
                <p className="text-sm font-semibold">{user ? `${user.firstName} ${user.lastName}` : ''}</p>
                <p className="text-xs text-muted-foreground">{user?.email}</p>
                <span className="mt-1 inline-flex rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-medium capitalize text-primary">
                  {user?.roles?.[0]}
                </span>
              </div>
              <div className="my-1 border-t border-border" />
              <Link
                to="/settings"
                className="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-sm text-foreground transition-colors hover:bg-secondary"
                onClick={() => setUserMenuOpen(false)}
              >
                <Settings className="h-4 w-4 text-muted-foreground" />
                Settings
              </Link>
              <button
                onClick={() => {
                  setUserMenuOpen(false);
                  logout();
                }}
                className="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-sm text-destructive transition-colors hover:bg-red-50"
              >
                <LogOut className="h-4 w-4" />
                Logout
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
