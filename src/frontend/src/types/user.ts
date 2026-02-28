export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  entityIds: string[];
  permissions: string[];
  is2FAEnabled: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export type UserRole =
  | 'admin'
  | 'executive'
  | 'finance'
  | 'sales'
  | 'marketing'
  | 'hr'
  | 'auditor';
