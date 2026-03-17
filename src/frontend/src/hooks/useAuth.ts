import { useMutation } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/authStore';
import { useEntityStore } from '@/stores/entityStore';
import {
  api,
  setAccessToken,
  setRefreshToken,
  storeTokens,
  storeUser,
  getStoredRememberMe,
} from '@/lib/api';

export function useAuth() {
  const { user, isAuthenticated, setUser, logout: clearAuth } = useAuthStore();

  const login = async (email: string, password: string, rememberMe = false) => {
    const { data } = await api.post('/auth/login', {
      email,
      password,
      deviceFingerprint: navigator.userAgent,
      rememberMe,
    });

    if (data.requires2FA) {
      // Persist rememberMe so verify2FA can use it
      sessionStorage.setItem('cb_pending_remember_me', rememberMe ? 'true' : 'false');
      return { requires2FA: true, challengeToken: data.challengeToken };
    }

    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken ?? null);
    storeTokens(data.accessToken, data.refreshToken ?? null, rememberMe);
    storeUser(data.user, rememberMe);
    setUser(data.user);
    if (data.user.entities?.length > 0) {
      useEntityStore.getState().setSelectedEntity(data.user.entities[0].entityId);
    }
    return { requires2FA: false };
  };

  const verify2FA = async (challengeToken: string, totpCode: string) => {
    const rememberMe =
      sessionStorage.getItem('cb_pending_remember_me') === 'true' || getStoredRememberMe();
    sessionStorage.removeItem('cb_pending_remember_me');

    const { data } = await api.post('/auth/verify-2fa', { challengeToken, totpCode });
    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken ?? null);
    storeTokens(data.accessToken, data.refreshToken ?? null, rememberMe);
    storeUser(data.user, rememberMe);
    setUser(data.user);
    if (data.user.entities?.length > 0) {
      useEntityStore.getState().setSelectedEntity(data.user.entities[0].entityId);
    }
  };

  const logout = async () => {
    try {
      await api.post('/auth/logout');
    } finally {
      setAccessToken(null);
      setRefreshToken(null);
      clearAuth();
    }
  };

  const forgotPassword = async (email: string) => {
    await api.post('/auth/forgot-password', { email });
  };

  const resetPasswordViaToken = async (token: string, newPassword: string) => {
    await api.post('/auth/reset-password', { token, newPassword });
  };

  const hasPermission = (permission: string): boolean => {
    if (!user?.permissions) return false;
    if (permission.endsWith('.*')) {
      const prefix = permission.slice(0, -1); // "admin.*" → "admin."
      return user.permissions.some((p) => p.startsWith(prefix));
    }
    return user.permissions.includes(permission);
  };

  const EXECUTIVE_ROLES = ['ceo', 'cfo', 'cso', 'chro', 'coo'];

  const isExecutive =
    user?.roles?.some((r) => EXECUTIVE_ROLES.includes(r.toLowerCase())) ||
    user?.permissions?.includes('executive.view') ||
    false;

  return {
    user,
    isAuthenticated,
    login,
    verify2FA,
    logout,
    forgotPassword,
    resetPasswordViaToken,
    hasPermission,
    role: user?.roles?.[0] ?? null,
    isExecutive,
  };
}

export function useAcceptInvitation() {
  return useMutation({
    mutationFn: async ({ token, password }: { token: string; password: string }) => {
      await api.post('/auth/accept-invitation', { token, password });
    },
  });
}
