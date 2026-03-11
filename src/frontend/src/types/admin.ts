export interface UserRoleDto {
  roleId: string;
  roleName: string;
  entityId: string;
  entityName: string;
  assignedAt: string;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  status: 'Active' | 'Invited' | 'Inactive';
  twoFactorEnabled: boolean;
  createdAt: string;
  lastLoginAt?: string;
  roles: UserRoleDto[];
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  roleIds: string[];
  entityIds: string[];
}

export interface UpdateUserRequest {
  id: string;
  firstName: string;
  lastName: string;
  locale?: string;
  timezone?: string;
}

export interface CreateUserResponse {
  userId: string;
  email: string;
}

export interface AuditLogEntry {
  id: string;
  userId?: string;
  userEmail?: string;
  action: string;
  tableName: string;
  recordId?: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
}

export interface AuditLogParams {
  page?: number;
  pageSize?: number;
  userId?: string;
  action?: string;
  startDate?: string;
  endDate?: string;
}

export interface DatevExport {
  id: string;
  entityId: string;
  startDate: string;
  endDate: string;
  status: 'pending' | 'completed' | 'failed';
  fileName?: string;
  createdAt: string;
}

export interface TriggerDatevExportRequest {
  entityId: string;
  startDate: string;
  endDate: string;
}

export interface MailConfig {
  id: string;
  host: string;
  port: number;
  username: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  isActive: boolean;
  updatedAt: string;
}

export interface UpsertMailConfigRequest {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
}

export interface SendTestEmailRequest {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  recipientEmail: string;
}

export interface SendTestEmailResult {
  success: boolean;
  errorMessage: string | null;
}

export interface AuthConfig {
  tokenLifetimeHours: number;
  rememberMeTokenLifetimeDays: number;
  updatedAt: string;
}

export interface UpsertAuthConfigRequest {
  tokenLifetimeHours: number;
  rememberMeTokenLifetimeDays: number;
}
