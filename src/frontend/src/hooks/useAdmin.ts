import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { PaginatedResponse } from '@/types/api';
import type {
  AdminUser,
  CreateUserRequest,
  CreateUserResponse,
  UpdateUserRequest,
  AuditLogEntry,
  AuditLogParams,
  MailConfig,
  UpsertMailConfigRequest,
  SendTestEmailRequest,
  SendTestEmailResult,
} from '@/types/admin';

// ---------------------------------------------------------------------------
// Users
// ---------------------------------------------------------------------------

interface UserListParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export function useUsers(params?: UserListParams) {
  return useQuery({
    queryKey: [...queryKeys.admin.users(), params],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<AdminUser>>(
        '/admin/users',
        { params },
      );
      return data;
    },
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateUserRequest) => {
      const { data } = await api.post<CreateUserResponse>(
        '/admin/users',
        request,
      );
      return data;
    },
    onSuccess: (data) => {
      toast.success(
        `User created. Temporary password: ${data.temporaryPassword}`,
        { duration: 15000 },
      );
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to create user');
    },
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpdateUserRequest) => {
      await api.put(`/admin/users/${request.id}`, request);
    },
    onSuccess: () => {
      toast.success('User updated');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to update user');
    },
  });
}

export function useDeactivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      await api.post(`/admin/users/${userId}/deactivate`);
    },
    onSuccess: () => {
      toast.success('User deactivated');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to deactivate user');
    },
  });
}

// ---------------------------------------------------------------------------
// Roles
// ---------------------------------------------------------------------------

export function useRoles() {
  return useQuery({
    queryKey: queryKeys.admin.roles(),
    queryFn: async () => {
      const { data } = await api.get<{ id: string; name: string }[]>(
        '/admin/roles',
      );
      return data;
    },
  });
}

export function useAssignRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      userId,
      roleId,
      entityId,
    }: {
      userId: string;
      roleId: string;
      entityId: string;
    }) => {
      await api.post(`/admin/users/${userId}/roles`, { roleId, entityId });
    },
    onSuccess: () => {
      toast.success('Role assigned');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to assign role');
    },
  });
}

export function useRemoveRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      userId,
      roleId,
      entityId,
    }: {
      userId: string;
      roleId: string;
      entityId: string;
    }) => {
      await api.delete(`/admin/users/${userId}/roles`, { params: { roleId, entityId } });
    },
    onSuccess: () => {
      toast.success('Role removed');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to remove role');
    },
  });
}

export function useReactivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      await api.post(`/admin/users/${userId}/reactivate`);
    },
    onSuccess: () => {
      toast.success('User reactivated');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error('Failed to reactivate user');
    },
  });
}

// ---------------------------------------------------------------------------
// Password Reset
// ---------------------------------------------------------------------------

export function useResetPassword() {
  return useMutation({
    mutationFn: async (userId: string) => {
      const { data } = await api.post<{ temporaryPassword: string }>(
        `/admin/users/${userId}/reset-password`,
      );
      return data;
    },
    onSuccess: (data) => {
      toast.success(
        `Password reset. New temporary password: ${data.temporaryPassword}`,
        { duration: 15000 },
      );
    },
    onError: () => {
      toast.error('Failed to reset password');
    },
  });
}

// ---------------------------------------------------------------------------
// Audit Logs
// ---------------------------------------------------------------------------

export function useAuditLogs(params?: AuditLogParams) {
  return useQuery({
    queryKey: [...queryKeys.admin.auditLogs(), params],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<AuditLogEntry>>(
        '/admin/audit-logs',
        { params },
      );
      return data;
    },
  });
}

export function useExportAuditLogs() {
  return useMutation({
    mutationFn: async (
      params: Omit<AuditLogParams, 'page' | 'pageSize'> & {
        format?: string;
      },
    ) => {
      const { data } = await api.get('/admin/audit-logs/export', {
        params: { format: 'csv', ...params },
        responseType: 'blob',
      });
      return data;
    },
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob as Blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `audit-log-${new Date().toISOString().slice(0, 10)}.csv`;
      anchor.click();
      window.URL.revokeObjectURL(url);
      toast.success('Audit log exported');
    },
    onError: () => {
      toast.error('Failed to export audit logs');
    },
  });
}

// ---------------------------------------------------------------------------
// Mail Config
// ---------------------------------------------------------------------------

export function useMailConfig() {
  return useQuery({
    queryKey: queryKeys.admin.mailConfig(),
    queryFn: async () => {
      const { data } = await api.get<MailConfig | null>('/admin/mail/config');
      return data;
    },
  });
}

export function useUpsertMailConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpsertMailConfigRequest) => {
      await api.put('/admin/mail/config', request);
    },
    onSuccess: () => {
      toast.success('Mail configuration saved');
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.mailConfig() });
    },
    onError: () => {
      toast.error('Failed to save mail configuration');
    },
  });
}

// ---------------------------------------------------------------------------
// Mail Test
// ---------------------------------------------------------------------------

export function useSendTestEmail() {
  return useMutation({
    mutationFn: async (request: SendTestEmailRequest) => {
      const { data } = await api.post<SendTestEmailResult>(
        '/admin/mail/test',
        request,
      );
      return data;
    },
  });
}
