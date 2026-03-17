import { create } from 'zustand';

type Theme = 'light' | 'dark' | 'system';
type Locale = 'de' | 'en';
type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected';

interface UiState {
  sidebarOpen: boolean;
  theme: Theme;
  locale: Locale;
  connectionStatus: ConnectionStatus;
  showFullNav: boolean;
  toggleSidebar: () => void;
  setTheme: (theme: Theme) => void;
  setLocale: (locale: Locale) => void;
  setConnectionStatus: (status: ConnectionStatus) => void;
  toggleFullNav: () => void;
}

const THEME_KEY = 'cb-theme';

/** Liest OS-Präferenz */
const prefersDark = () =>
  window.matchMedia('(prefers-color-scheme: dark)').matches;

/** Schreibt .dark-Klasse auf <html> — einzige Quelle der Wahrheit */
function applyTheme(theme: Theme): void {
  const shouldBeDark =
    theme === 'dark' || (theme === 'system' && prefersDark());
  document.documentElement.classList.toggle('dark', shouldBeDark);
}

/** Persistiertes Theme aus localStorage, Fallback: 'system' */
const initialTheme =
  (localStorage.getItem(THEME_KEY) as Theme | null) ?? 'system';

export const useUiStore = create<UiState>((set) => ({
  sidebarOpen: true,
  theme: initialTheme,
  locale: 'en',
  connectionStatus: 'disconnected',
  showFullNav: false,

  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  toggleFullNav: () => set((s) => ({ showFullNav: !s.showFullNav })),

  setTheme: (theme) => {
    localStorage.setItem(THEME_KEY, theme);
    applyTheme(theme);
    set({ theme });
  },

  setLocale: (locale) => set({ locale }),
  setConnectionStatus: (status) => set({ connectionStatus: status }),
}));

// Theme sofort beim Laden anwenden (vor erstem React-Render)
applyTheme(initialTheme);

// OS-Präferenz live verfolgen wenn theme === 'system'
window
  .matchMedia('(prefers-color-scheme: dark)')
  .addEventListener('change', () => {
    if (useUiStore.getState().theme === 'system') {
      applyTheme('system');
    }
  });
