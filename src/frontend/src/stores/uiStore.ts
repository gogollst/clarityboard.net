import { create } from 'zustand';

type Theme = 'light' | 'dark' | 'system';
type Locale = 'de' | 'en';
type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected';

interface UiState {
  sidebarOpen: boolean;
  theme: Theme;
  locale: Locale;
  connectionStatus: ConnectionStatus;
  toggleSidebar: () => void;
  setTheme: (theme: Theme) => void;
  setLocale: (locale: Locale) => void;
  setConnectionStatus: (status: ConnectionStatus) => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarOpen: true,
  theme: 'system',
  locale: 'en',
  connectionStatus: 'disconnected',
  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  setTheme: (theme) => set({ theme }),
  setLocale: (locale) => set({ locale }),
  setConnectionStatus: (status) => set({ connectionStatus: status }),
}));
