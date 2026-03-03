import axios from 'axios';
import { useEntityStore } from '@/stores/entityStore';

export const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

let accessToken: string | null = null;
let refreshToken: string | null = null;

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
        if (data.refreshToken) {
          refreshToken = data.refreshToken;
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
