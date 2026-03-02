export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  locale: string;
  timezone: string;
  bio: string | null;
  avatarUrl: string | null;
  twoFactorEnabled: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  locale: string;
  timezone: string;
  bio: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface Disable2FARequest {
  password: string;
}
