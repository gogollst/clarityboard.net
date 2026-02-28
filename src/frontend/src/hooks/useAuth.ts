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
    hasPermission,
    role: user?.roles?.[0] ?? null,
  };
}
