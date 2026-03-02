import { create } from 'zustand';

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

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  setUser: (user) => set({ user, isAuthenticated: !!user }),
  logout: () => set({ user: null, isAuthenticated: false }),
}));
