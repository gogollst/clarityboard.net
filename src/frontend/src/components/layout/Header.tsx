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

  const connectionDot = {
    connected: 'bg-green-500',
    reconnecting: 'bg-yellow-500 animate-pulse',
    disconnected: 'bg-red-500',
  }[connectionStatus];

  const connectionLabel = {
    connected: 'Connected',
    reconnecting: 'Reconnecting...',
    disconnected: 'Disconnected',
  }[connectionStatus];

  return (
    <header className="flex h-14 items-center justify-between border-b border-border bg-card px-4">
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
        <Building2 className="h-4 w-4 text-muted-foreground" />
        <select
          value={selectedEntityId ?? ''}
          onChange={(e) => switchEntity(e.target.value)}
          className="h-8 rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
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

      {/* Right section */}
      <div className="flex items-center gap-3">
        {/* Connection Status */}
        <div className="flex items-center gap-1.5" title={connectionLabel}>
          {connectionStatus === 'disconnected' ? (
            <WifiOff className="h-4 w-4 text-muted-foreground" />
          ) : (
            <Wifi className="h-4 w-4 text-muted-foreground" />
          )}
          <span
            className={cn('h-2 w-2 rounded-full', connectionDot)}
            aria-label={connectionLabel}
          />
        </div>

        {/* Notification Bell */}
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="h-5 w-5" />
          {/* Badge count - hardcoded for now, will be dynamic */}
        </Button>

        {/* User Dropdown */}
        <div className="relative" ref={userMenuRef}>
          <Button
            variant="ghost"
            className="flex items-center gap-2 px-2"
            onClick={() => setUserMenuOpen(!userMenuOpen)}
          >
            <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10">
              <User className="h-4 w-4 text-primary" />
            </div>
            <span className="hidden text-sm font-medium md:inline-block">
              {user?.name ?? 'User'}
            </span>
            <ChevronDown className="h-3 w-3 text-muted-foreground" />
          </Button>

          {userMenuOpen && (
            <div className="absolute right-0 top-full z-50 mt-1 w-56 rounded-md border border-border bg-popover p-1 shadow-lg">
              <div className="px-3 py-2">
                <p className="text-sm font-medium">{user?.name}</p>
                <p className="text-xs text-muted-foreground">{user?.email}</p>
                <p className="mt-0.5 text-xs capitalize text-muted-foreground">
                  {user?.role}
                </p>
              </div>
              <div className="my-1 border-t border-border" />
              <Link
                to="/settings"
                className="flex w-full items-center gap-2 rounded-sm px-3 py-1.5 text-sm hover:bg-accent"
                onClick={() => setUserMenuOpen(false)}
              >
                <Settings className="h-4 w-4" />
                Settings
              </Link>
              <button
                onClick={() => {
                  setUserMenuOpen(false);
                  logout();
                }}
                className="flex w-full items-center gap-2 rounded-sm px-3 py-1.5 text-sm text-destructive hover:bg-accent"
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
