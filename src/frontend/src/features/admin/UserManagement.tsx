import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import {
  useUsers,
  useCreateUser,
  useUpdateUser,
  useDeactivateUser,
  useReactivateUser,
  useResetPassword,
  useResendInvitation,
  useRoles,
  useAssignRole,
  useRemoveRole,
} from '@/hooks/useAdmin';
import type { AdminUser } from '@/types/admin';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import StatusBadge from '@/components/shared/StatusBadge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Plus, Loader2, Edit, UserX, UserCheck, KeyRound, Mail, X, Search } from 'lucide-react';

interface FormState {
  firstName: string;
  lastName: string;
  email: string;
  roleId: string;
  entityIds: string[];
}

const emptyForm: FormState = {
  firstName: '',
  lastName: '',
  email: '',
  roleId: '',
  entityIds: [],
};

function useDebounced<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

export function Component() {
  const { t, i18n } = useTranslation('admin');
  const { entities } = useEntity();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounced(search, 300);

  // Reset to page 1 when search changes
  const prevSearch = useRef(debouncedSearch);
  useEffect(() => {
    if (prevSearch.current !== debouncedSearch) {
      setPage(1);
      prevSearch.current = debouncedSearch;
    }
  }, [debouncedSearch]);

  const { data: usersData, isLoading } = useUsers({
    page,
    search: debouncedSearch || undefined,
  });
  const { data: roles } = useRoles();
  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const deactivateUser = useDeactivateUser();
  const reactivateUser = useReactivateUser();
  const resetPassword = useResetPassword();
  const resendInvitation = useResendInvitation();
  const assignRole = useAssignRole();
  const removeRole = useRemoveRole();

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [confirmDeactivate, setConfirmDeactivate] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [formErrors, setFormErrors] = useState<Partial<FormState>>({});
  const [dialogError, setDialogError] = useState<string | null>(null);

  // Role add state inside edit dialog
  const [newRoleId, setNewRoleId] = useState('');
  const [newEntityId, setNewEntityId] = useState('');

  const users: AdminUser[] = Array.isArray(usersData)
    ? usersData
    : (usersData?.items ?? []);

  const totalCount = (usersData as { totalCount?: number })?.totalCount ?? 0;
  const pageSize = (usersData as { pageSize?: number })?.pageSize ?? 25;

  const resetForm = () => {
    setForm(emptyForm);
    setFormErrors({});
    setDialogError(null);
  };

  const validateForm = (): boolean => {
    const errors: Partial<FormState> = {};
    if (!form.firstName.trim()) errors.firstName = t('users.validation.required');
    if (!form.lastName.trim()) errors.lastName = t('users.validation.required');
    if (!form.email.trim()) errors.email = t('users.validation.required');
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = () => {
    if (!validateForm()) return;
    setDialogError(null);
    createUser.mutate(
      {
        email: form.email,
        firstName: form.firstName,
        lastName: form.lastName,
        roleIds: form.roleId ? [form.roleId] : [],
        entityIds: form.entityIds,
      },
      {
        onSuccess: () => {
          setIsAddOpen(false);
          resetForm();
        },
        onError: (err: unknown) => {
          const msg =
            (err as { response?: { data?: { message?: string } } })?.response?.data
              ?.message ?? t('users.dialogs.addUser.errorFallback');
          setDialogError(msg);
        },
      },
    );
  };

  const handleUpdate = () => {
    if (!editingUser) return;
    const errors: Partial<FormState> = {};
    if (!form.firstName.trim()) errors.firstName = t('users.validation.required');
    if (!form.lastName.trim()) errors.lastName = t('users.validation.required');
    setFormErrors(errors);
    if (Object.keys(errors).length > 0) return;
    setDialogError(null);
    updateUser.mutate(
      {
        id: editingUser.id,
        firstName: form.firstName,
        lastName: form.lastName,
      },
      {
        onSuccess: () => {
          setIsEditOpen(false);
          setEditingUser(null);
          resetForm();
        },
        onError: (err: unknown) => {
          const msg =
            (err as { response?: { data?: { message?: string } } })?.response?.data
              ?.message ?? t('users.dialogs.editUser.errorFallback');
          setDialogError(msg);
        },
      },
    );
  };

  const openEdit = (user: AdminUser) => {
    setEditingUser(user);
    setForm({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      roleId: user.roles?.[0]?.roleId ?? '',
      entityIds: user.roles?.map((r) => r.entityId) ?? [],
    });
    setFormErrors({});
    setDialogError(null);
    setNewRoleId('');
    setNewEntityId('');
    setIsEditOpen(true);
  };

  const handleDeactivate = (userId: string) => {
    deactivateUser.mutate(userId, {
      onSuccess: () => setConfirmDeactivate(null),
    });
  };

  const handleReactivate = (userId: string) => {
    reactivateUser.mutate(userId);
  };

  const handleResetPassword = (userId: string) => {
    resetPassword.mutate(userId);
  };

  const toggleEntityForForm = (entityId: string) => {
    setForm((f) => ({
      ...f,
      entityIds: f.entityIds.includes(entityId)
        ? f.entityIds.filter((id) => id !== entityId)
        : [...f.entityIds, entityId],
    }));
  };

  const handleAddRole = () => {
    if (!editingUser || !newRoleId || !newEntityId) return;
    assignRole.mutate(
      { userId: editingUser.id, roleId: newRoleId, entityId: newEntityId },
      {
        onSuccess: () => {
          setNewRoleId('');
          setNewEntityId('');
          // Update local editingUser roles from cache is handled by query invalidation
        },
      },
    );
  };

  const handleRemoveRole = (roleId: string, entityId: string) => {
    if (!editingUser) return;
    removeRole.mutate({ userId: editingUser.id, roleId, entityId });
  };

  const columns = [
    {
      key: 'name',
      header: t('users.columns.name'),
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">
          {String(item.firstName ?? '')} {String(item.lastName ?? '')}
        </span>
      ),
    },
    {
      key: 'email',
      header: t('users.columns.email'),
    },
    {
      key: 'roles',
      header: t('users.columns.role'),
      render: (item: Record<string, unknown>) => {
        const userRoles = item.roles as AdminUser['roles'] | undefined;
        const roleNames = [...new Set(userRoles?.map((r) => r.roleName) ?? [])];
        return (
          <div className="flex gap-1">
            {roleNames.map((name) => (
              <Badge key={name} variant="secondary" className="capitalize">
                {name}
              </Badge>
            ))}
            {roleNames.length === 0 && (
              <span className="text-sm text-muted-foreground">{t('users.noRole')}</span>
            )}
          </div>
        );
      },
    },
    {
      key: 'entities',
      header: t('users.columns.entities'),
      render: (item: Record<string, unknown>) => {
        const userRoles = item.roles as AdminUser['roles'] | undefined;
        const entityNames = [...new Set(userRoles?.map((r) => r.entityName) ?? [])];
        return (
          <span className="text-sm text-muted-foreground">
            {entityNames.length > 0 ? entityNames.join(', ') : t('users.noEntities')}
          </span>
        );
      },
    },
    {
      key: 'twoFactorEnabled',
      header: t('users.columns.twoFa'),
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={item.twoFactorEnabled ? 'Enabled' : 'Disabled'}
          variantMap={{ enabled: 'success', disabled: 'default' }}
        />
      ),
    },
    {
      key: 'status',
      header: t('users.columns.status'),
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={String(item.status ?? 'Active')}
          variantMap={{ active: 'success', invited: 'warning', inactive: 'destructive' }}
        />
      ),
    },
    {
      key: 'lastLoginAt',
      header: t('users.columns.lastLogin'),
      render: (item: Record<string, unknown>) => {
        const date = item.lastLoginAt as string | undefined;
        return date ? new Date(date).toLocaleDateString(i18n.language) : t('users.neverLoggedIn');
      },
    },
    {
      key: 'actions',
      header: t('users.columns.actions'),
      render: (item: Record<string, unknown>) => {
        const user = item as unknown as AdminUser;
        return (
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              title={t('users.tooltips.edit')}
              onClick={(e) => {
                e.stopPropagation();
                openEdit(user);
              }}
            >
              <Edit className="h-4 w-4" />
            </Button>
            {user.status === 'Invited' ? (
              <Button
                variant="ghost"
                size="sm"
                title={t('users.tooltips.resendInvitation')}
                onClick={(e) => {
                  e.stopPropagation();
                  resendInvitation.mutate(user.id);
                }}
              >
                <Mail className="h-4 w-4 text-amber-500" />
              </Button>
            ) : (
              <Button
                variant="ghost"
                size="sm"
                title={t('users.tooltips.resetPassword')}
                onClick={(e) => {
                  e.stopPropagation();
                  handleResetPassword(user.id);
                }}
              >
                <KeyRound className="h-4 w-4" />
              </Button>
            )}
            {user.isActive ? (
              <Button
                variant="ghost"
                size="sm"
                title={t('users.tooltips.deactivate')}
                onClick={(e) => {
                  e.stopPropagation();
                  setConfirmDeactivate(user.id);
                }}
              >
                <UserX className="h-4 w-4 text-red-500" />
              </Button>
            ) : (
              <Button
                variant="ghost"
                size="sm"
                title={t('users.tooltips.reactivate')}
                onClick={(e) => {
                  e.stopPropagation();
                  handleReactivate(user.id);
                }}
              >
                <UserCheck className="h-4 w-4 text-green-500" />
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div>
      <PageHeader
        title={t('users.title')}
        actions={
          <Button onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            {t('users.addUser')}
          </Button>
        }
      />

      {/* Search */}
      <div className="mb-4 flex items-center gap-2">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder={t('users.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      <DataTable
        columns={columns}
        data={users as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('users.noUsers')}
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />

      {/* Add User Dialog */}
      <Dialog open={isAddOpen} onOpenChange={(open) => { setIsAddOpen(open); if (!open) resetForm(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('users.dialogs.addUser.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>{t('users.dialogs.addUser.firstName')}</Label>
                <Input
                  value={form.firstName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, firstName: e.target.value }))
                  }
                  placeholder={t('users.dialogs.addUser.firstNamePlaceholder')}
                  aria-invalid={!!formErrors.firstName}
                />
                {formErrors.firstName && (
                  <p className="mt-1 text-xs text-destructive">{formErrors.firstName}</p>
                )}
              </div>
              <div>
                <Label>{t('users.dialogs.addUser.lastName')}</Label>
                <Input
                  value={form.lastName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, lastName: e.target.value }))
                  }
                  placeholder={t('users.dialogs.addUser.lastNamePlaceholder')}
                  aria-invalid={!!formErrors.lastName}
                />
                {formErrors.lastName && (
                  <p className="mt-1 text-xs text-destructive">{formErrors.lastName}</p>
                )}
              </div>
            </div>
            <div>
              <Label>{t('users.dialogs.addUser.email')}</Label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) =>
                  setForm((f) => ({ ...f, email: e.target.value }))
                }
                placeholder="user@company.com"
                aria-invalid={!!formErrors.email}
              />
              {formErrors.email && (
                <p className="mt-1 text-xs text-destructive">{formErrors.email}</p>
              )}
            </div>
            <div>
              <Label>{t('users.dialogs.addUser.role')}</Label>
              <Select
                value={form.roleId}
                onValueChange={(v) => setForm((f) => ({ ...f, roleId: v }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('users.dialogs.addUser.roleSelectPlaceholder')} />
                </SelectTrigger>
                <SelectContent>
                  {(roles ?? []).map((role) => (
                    <SelectItem key={role.id} value={role.id}>
                      <span className="capitalize">{role.name}</span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>{t('users.dialogs.addUser.entities')}</Label>
              <div className="mt-1 flex flex-wrap gap-2">
                {entities.map((entity) => (
                  <Badge
                    key={entity.id}
                    variant={form.entityIds.includes(entity.id) ? 'default' : 'outline'}
                    className="cursor-pointer"
                    onClick={() => toggleEntityForForm(entity.id)}
                  >
                    {entity.name}
                  </Badge>
                ))}
                {entities.length === 0 && (
                  <span className="text-sm text-muted-foreground">{t('users.dialogs.addUser.noEntities')}</span>
                )}
              </div>
            </div>
            {dialogError && (
              <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {dialogError}
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddOpen(false)}>
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button onClick={handleCreate} disabled={createUser.isPending}>
              {createUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('users.dialogs.addUser.createUser')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit User Dialog */}
      <Dialog open={isEditOpen} onOpenChange={(open) => { setIsEditOpen(open); if (!open) { setEditingUser(null); resetForm(); } }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('users.dialogs.editUser.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>{t('users.dialogs.addUser.firstName')}</Label>
                <Input
                  value={form.firstName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, firstName: e.target.value }))
                  }
                  aria-invalid={!!formErrors.firstName}
                />
                {formErrors.firstName && (
                  <p className="mt-1 text-xs text-destructive">{formErrors.firstName}</p>
                )}
              </div>
              <div>
                <Label>{t('users.dialogs.addUser.lastName')}</Label>
                <Input
                  value={form.lastName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, lastName: e.target.value }))
                  }
                  aria-invalid={!!formErrors.lastName}
                />
                {formErrors.lastName && (
                  <p className="mt-1 text-xs text-destructive">{formErrors.lastName}</p>
                )}
              </div>
            </div>
            <div>
              <Label>{t('users.dialogs.editUser.email')}</Label>
              <Input type="email" value={form.email} disabled />
            </div>

            {/* Role management */}
            <div>
              <Label>{t('users.dialogs.editUser.roles')}</Label>
              <div className="mt-2 space-y-2">
                {(editingUser?.roles ?? []).length === 0 && (
                  <p className="text-sm text-muted-foreground">{t('users.dialogs.editUser.noRoles')}</p>
                )}
                {(editingUser?.roles ?? []).map((r) => (
                  <div key={`${r.roleId}-${r.entityId}`} className="flex items-center justify-between rounded-md border border-border px-3 py-1.5">
                    <span className="text-sm capitalize">
                      {r.roleName}
                      <span className="ml-2 text-xs text-muted-foreground">@ {r.entityName}</span>
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-6 w-6 p-0"
                      disabled={removeRole.isPending}
                      onClick={() => handleRemoveRole(r.roleId, r.entityId)}
                    >
                      <X className="h-3 w-3" />
                    </Button>
                  </div>
                ))}
                <div className="flex gap-2 pt-1">
                  <Select value={newRoleId} onValueChange={setNewRoleId}>
                    <SelectTrigger className="flex-1">
                      <SelectValue placeholder={t('users.dialogs.editUser.rolePlaceholder')} />
                    </SelectTrigger>
                    <SelectContent>
                      {(roles ?? []).map((role) => (
                        <SelectItem key={role.id} value={role.id}>
                          <span className="capitalize">{role.name}</span>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select value={newEntityId} onValueChange={setNewEntityId}>
                    <SelectTrigger className="flex-1">
                      <SelectValue placeholder={t('users.dialogs.editUser.entityPlaceholder')} />
                    </SelectTrigger>
                    <SelectContent>
                      {entities.map((entity) => (
                        <SelectItem key={entity.id} value={entity.id}>
                          {entity.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!newRoleId || !newEntityId || assignRole.isPending}
                    onClick={handleAddRole}
                  >
                    {assignRole.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <Plus className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>
            </div>

            {dialogError && (
              <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {dialogError}
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditOpen(false)}>
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button onClick={handleUpdate} disabled={updateUser.isPending}>
              {updateUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('users.dialogs.editUser.saveChanges')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Deactivate Confirmation Dialog */}
      <Dialog
        open={!!confirmDeactivate}
        onOpenChange={() => setConfirmDeactivate(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('users.dialogs.confirmDeactivate.title')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {t('users.dialogs.confirmDeactivate.message')}
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDeactivate(null)}>
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button
              variant="destructive"
              onClick={() => confirmDeactivate && handleDeactivate(confirmDeactivate)}
              disabled={deactivateUser.isPending}
            >
              {deactivateUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('users.dialogs.confirmDeactivate.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
