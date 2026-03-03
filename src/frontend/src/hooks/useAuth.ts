import { useMutation } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/authStore';
import { api, setAccessToken, setRefreshToken } from '@/lib/api';

export function useAuth() {
  const { user, isAuthenticated, setUser, logout: clearAuth } = useAuthStore();

  const login = async (email: string, password: string) => {
    const { data } = await api.post('/auth/login', {
      email,
      password,
      deviceFingerprint: navigator.userAgent,
    });

    if (data.requires2FA) {
      return { requires2FA: true, challengeToken: data.challengeToken };
    }

    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken ?? null);
    setUser(data.user);
    return { requires2FA: false };
  };

  const verify2FA = async (challengeToken: string, totpCode: string) => {
    const { data } = await api.post('/auth/verify-2fa', { challengeToken, totpCode });
    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken ?? null);
    setUser(data.user);
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
  };
}

export function useAcceptInvitation() {
  return useMutation({
    mutationFn: async ({ token, password }: { token: string; password: string }) => {
      await api.post('/auth/accept-invitation', { token, password });
    },
  });
}
