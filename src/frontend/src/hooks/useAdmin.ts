import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import i18n from '@/i18n';
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
  AuthConfig,
  UpsertAuthConfigRequest,
  ProductCategoryMapping,
  UpsertProductMappingRequest,
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
    onSuccess: () => {
      toast.success(i18n.t('admin:users.toast.created'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.createFailed'));
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
      toast.success(i18n.t('admin:users.toast.updated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.updateFailed'));
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
      toast.success(i18n.t('admin:users.toast.deactivated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.deactivateFailed'));
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
      toast.success(i18n.t('admin:users.toast.roleAssigned'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.roleAssignFailed'));
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
      toast.success(i18n.t('admin:users.toast.roleRemoved'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.roleRemoveFailed'));
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
      toast.success(i18n.t('admin:users.toast.reactivated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.reactivateFailed'));
    },
  });
}

// ---------------------------------------------------------------------------
// Password Reset
// ---------------------------------------------------------------------------

export function useResetPassword() {
  return useMutation({
    mutationFn: async (userId: string) => {
      await api.post(`/admin/users/${userId}/reset-password`);
    },
    onSuccess: () => {
      toast.success(i18n.t('admin:users.toast.passwordReset'));
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.passwordResetFailed'));
    },
  });
}

export function useResendInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      await api.post(`/admin/users/${userId}/resend-invitation`);
    },
    onSuccess: () => {
      toast.success(i18n.t('admin:users.toast.invitationResent'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users() });
    },
    onError: () => {
      toast.error(i18n.t('admin:users.toast.invitationResendFailed'));
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
      toast.success(i18n.t('admin:audit.toast.exported'));
    },
    onError: () => {
      toast.error(i18n.t('admin:audit.toast.exportFailed'));
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
      toast.success(i18n.t('admin:mail.toast.saved'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.mailConfig() });
    },
    onError: () => {
      toast.error(i18n.t('admin:mail.toast.saveFailed'));
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

// ---------------------------------------------------------------------------
// Auth Config
// ---------------------------------------------------------------------------

export function useAuthConfig() {
  return useQuery({
    queryKey: queryKeys.admin.authConfig(),
    queryFn: async () => {
      const { data } = await api.get<AuthConfig>('/admin/auth-config');
      return data;
    },
  });
}

export function useUpsertAuthConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpsertAuthConfigRequest) => {
      await api.put('/admin/auth-config', request);
    },
    onSuccess: () => {
      toast.success(i18n.t('admin:auth.toast.saved'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.authConfig() });
    },
    onError: () => {
      toast.error(i18n.t('admin:auth.toast.saveFailed'));
    },
  });
}

// ---------------------------------------------------------------------------
// Product Category Mappings
// ---------------------------------------------------------------------------

export function useProductMappings(entityId: string) {
  return useQuery({
    queryKey: queryKeys.admin.productMappings(entityId),
    queryFn: async () => {
      const { data } = await api.get<ProductCategoryMapping[]>(
        '/admin/product-mappings',
        { params: { entityId } },
      );
      return data;
    },
    enabled: !!entityId,
  });
}

export function useUpsertProductMapping() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: UpsertProductMappingRequest) => {
      const { data } = await api.put<string>('/admin/product-mappings', request);
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('admin:productMappings.toast.saved'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.productMappings(variables.entityId) });
    },
    onError: () => {
      toast.error(i18n.t('admin:productMappings.toast.saveFailed'));
    },
  });
}

export function useDeleteProductMapping() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ mappingId, entityId }: { mappingId: string; entityId: string }) => {
      await api.delete(`/admin/product-mappings/${mappingId}`, { params: { entityId } });
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('admin:productMappings.toast.deleted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.productMappings(variables.entityId) });
    },
    onError: () => {
      toast.error(i18n.t('admin:productMappings.toast.deleteFailed'));
    },
  });
}
