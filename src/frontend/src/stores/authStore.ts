import { create } from 'zustand';
import { getStoredUser, clearStoredTokens } from '@/lib/api';

export interface EntityAccess {
  entityId: string;
  entityName: string;
  role: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  locale: string;
  timezone: string;
  twoFactorEnabled: boolean;
  entities: EntityAccess[];
  roles: string[];
  permissions: string[];
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  setUser: (user: User | null) => void;
  logout: () => void;
}

const _storedUser = getStoredUser() as User | null;

export const useAuthStore = create<AuthState>((set) => ({
  user: _storedUser,
  isAuthenticated: !!_storedUser,
  setUser: (user) => set({ user, isAuthenticated: !!user }),
  logout: () => {
    clearStoredTokens();
    set({ user: null, isAuthenticated: false });
  },
}));
