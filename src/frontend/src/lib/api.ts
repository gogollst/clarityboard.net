import axios from 'axios';
import { useEntityStore } from '@/stores/entityStore';

export const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

const KEYS = {
  accessToken: 'cb_access_token',
  refreshToken: 'cb_refresh_token',
  user: 'cb_user',
  rememberMe: 'cb_remember_me',
} as const;

export function getStoredRememberMe(): boolean {
  return localStorage.getItem(KEYS.rememberMe) === 'true';
}

function getStorage(): Storage {
  return getStoredRememberMe() ? localStorage : sessionStorage;
}

export function storeTokens(access: string, refresh: string | null, rememberMe: boolean): void {
  const storage = rememberMe ? localStorage : sessionStorage;
  storage.setItem(KEYS.accessToken, access);
  if (refresh) storage.setItem(KEYS.refreshToken, refresh);
  localStorage.setItem(KEYS.rememberMe, rememberMe ? 'true' : 'false');
}

export function storeUser(user: unknown, rememberMe: boolean): void {
  const storage = rememberMe ? localStorage : sessionStorage;
  storage.setItem(KEYS.user, JSON.stringify(user));
}

export function getStoredUser(): unknown {
  const raw = getStorage().getItem(KEYS.user);
  return raw ? JSON.parse(raw) : null;
}

export function clearStoredTokens(): void {
  [localStorage, sessionStorage].forEach((s) => {
    s.removeItem(KEYS.accessToken);
    s.removeItem(KEYS.refreshToken);
    s.removeItem(KEYS.user);
  });
  localStorage.removeItem(KEYS.rememberMe);
}

// Module-level token cache — seeded from storage on init
let accessToken: string | null = getStorage().getItem(KEYS.accessToken);
let refreshToken: string | null = getStorage().getItem(KEYS.refreshToken);

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function setRefreshToken(token: string | null) {
  refreshToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

// Request interceptor: attach JWT
api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Response interceptor: auto-refresh on 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;

      try {
        const entityId = useEntityStore.getState().selectedEntityId;
        const { data } = await api.post('/auth/refresh', {
          refreshToken,
          deviceFingerprint: navigator.userAgent,
          entityId: entityId ?? undefined,
        });
        accessToken = data.accessToken;
        const storage = getStorage();
        storage.setItem(KEYS.accessToken, data.accessToken);
        if (data.refreshToken) {
          refreshToken = data.refreshToken;
          storage.setItem(KEYS.refreshToken, data.refreshToken);
        }
        error.config.headers.Authorization = `Bearer ${accessToken}`;
        return api(error.config);
      } catch {
        accessToken = null;
        window.location.href = '/login';
        return Promise.reject(error);
      }
    }
    return Promise.reject(error);
  },
);
