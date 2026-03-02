import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type {
  UserProfile,
  UpdateProfileRequest,
  ChangePasswordRequest,
  Disable2FARequest,
} from '@/types/settings';

export function useProfile() {
  return useQuery({
    queryKey: queryKeys.settings.profile(),
    queryFn: async () => {
      const { data } = await api.get<UserProfile>('/me');
      return data;
    },
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpdateProfileRequest) => {
      await api.put('/me', request);
    },
    onSuccess: () => {
      toast.success('Profile updated');
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.profile() });
    },
    onError: () => {
      toast.error('Failed to update profile');
    },
  });
}

export function useUploadAvatar() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      const { data } = await api.post<{ avatarUrl: string }>('/me/avatar', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return data;
    },
    onSuccess: () => {
      toast.success('Avatar uploaded');
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.profile() });
    },
    onError: () => {
      toast.error('Failed to upload avatar');
    },
  });
}

export function useDeleteAvatar() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      await api.delete('/me/avatar');
    },
    onSuccess: () => {
      toast.success('Avatar removed');
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.profile() });
    },
    onError: () => {
      toast.error('Failed to remove avatar');
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: async (request: ChangePasswordRequest) => {
      await api.post('/me/change-password', request);
    },
    onSuccess: () => {
      toast.success('Password changed');
    },
    onError: () => {
      toast.error('Failed to change password');
    },
  });
}

export function useDisable2FA() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: Disable2FARequest) => {
      await api.post('/me/2fa/disable', request);
    },
    onSuccess: () => {
      toast.success('Two-factor authentication disabled');
      queryClient.invalidateQueries({ queryKey: queryKeys.settings.profile() });
    },
    onError: () => {
      toast.error('Failed to disable two-factor authentication');
    },
  });
}
