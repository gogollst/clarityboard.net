import { useState } from 'react';
import { useEntity } from '@/hooks/useEntity';
import {
  useUsers,
  useCreateUser,
  useUpdateUser,
  useDeactivateUser,
  useResetPassword,
  useRoles,
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
import { Plus, Loader2, Edit, UserX, KeyRound } from 'lucide-react';

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

export function Component() {
  const { entities } = useEntity();
  const { data: usersData, isLoading } = useUsers();
  const { data: roles } = useRoles();
  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const deactivateUser = useDeactivateUser();
  const resetPassword = useResetPassword();

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [confirmDeactivate, setConfirmDeactivate] = useState<string | null>(
    null,
  );
  const [form, setForm] = useState<FormState>(emptyForm);

  const users: AdminUser[] = Array.isArray(usersData)
    ? usersData
    : usersData?.items ?? [];

  const resetForm = () => setForm(emptyForm);

  const handleCreate = () => {
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
      },
    );
  };

  const handleUpdate = () => {
    if (!editingUser) return;
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
    setIsEditOpen(true);
  };

  const handleDeactivate = (userId: string) => {
    deactivateUser.mutate(userId, {
      onSuccess: () => setConfirmDeactivate(null),
    });
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

  const columns = [
    {
      key: 'name',
      header: 'Name',
      render: (item: Record<string, unknown>) => (
        <span className="font-medium">
          {String(item.firstName ?? '')} {String(item.lastName ?? '')}
        </span>
      ),
    },
    {
      key: 'email',
      header: 'Email',
    },
    {
      key: 'roles',
      header: 'Role',
      render: (item: Record<string, unknown>) => {
        const userRoles = item.roles as AdminUser['roles'] | undefined;
        const roleNames = [
          ...new Set(userRoles?.map((r) => r.roleName) ?? []),
        ];
        return (
          <div className="flex gap-1">
            {roleNames.map((name) => (
              <Badge key={name} variant="secondary" className="capitalize">
                {name}
              </Badge>
            ))}
            {roleNames.length === 0 && (
              <span className="text-sm text-muted-foreground">No role</span>
            )}
          </div>
        );
      },
    },
    {
      key: 'entities',
      header: 'Entities',
      render: (item: Record<string, unknown>) => {
        const userRoles = item.roles as AdminUser['roles'] | undefined;
        const entityNames = [
          ...new Set(userRoles?.map((r) => r.entityName) ?? []),
        ];
        return (
          <span className="text-sm text-muted-foreground">
            {entityNames.length > 0 ? entityNames.join(', ') : 'None'}
          </span>
        );
      },
    },
    {
      key: 'twoFactorEnabled',
      header: '2FA',
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={item.twoFactorEnabled ? 'Enabled' : 'Disabled'}
          variantMap={{ enabled: 'success', disabled: 'default' }}
        />
      ),
    },
    {
      key: 'isActive',
      header: 'Status',
      render: (item: Record<string, unknown>) => (
        <StatusBadge
          status={item.isActive ? 'Active' : 'Inactive'}
          variantMap={{ active: 'success', inactive: 'destructive' }}
        />
      ),
    },
    {
      key: 'lastLoginAt',
      header: 'Last Login',
      render: (item: Record<string, unknown>) => {
        const date = item.lastLoginAt as string | undefined;
        return date
          ? new Date(date).toLocaleDateString('de-DE')
          : 'Never';
      },
    },
    {
      key: 'actions',
      header: 'Actions',
      render: (item: Record<string, unknown>) => {
        const user = item as unknown as AdminUser;
        return (
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              title="Edit"
              onClick={(e) => {
                e.stopPropagation();
                openEdit(user);
              }}
            >
              <Edit className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              title="Reset Password"
              onClick={(e) => {
                e.stopPropagation();
                handleResetPassword(user.id);
              }}
            >
              <KeyRound className="h-4 w-4" />
            </Button>
            {user.isActive && (
              <Button
                variant="ghost"
                size="sm"
                title="Deactivate"
                onClick={(e) => {
                  e.stopPropagation();
                  setConfirmDeactivate(user.id);
                }}
              >
                <UserX className="h-4 w-4 text-red-500" />
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
        title="User Management"
        actions={
          <Button onClick={() => { resetForm(); setIsAddOpen(true); }}>
            <Plus className="mr-1 h-4 w-4" />
            Add User
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={users as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage="No users found."
      />

      {/* Add User Dialog */}
      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add User</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>First Name</Label>
                <Input
                  value={form.firstName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, firstName: e.target.value }))
                  }
                  placeholder="First name"
                />
              </div>
              <div>
                <Label>Last Name</Label>
                <Input
                  value={form.lastName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, lastName: e.target.value }))
                  }
                  placeholder="Last name"
                />
              </div>
            </div>
            <div>
              <Label>Email</Label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) =>
                  setForm((f) => ({ ...f, email: e.target.value }))
                }
                placeholder="user@company.com"
              />
            </div>
            <div>
              <Label>Role</Label>
              <Select
                value={form.roleId}
                onValueChange={(v) => setForm((f) => ({ ...f, roleId: v }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select a role" />
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
              <Label>Entities</Label>
              <div className="mt-1 flex flex-wrap gap-2">
                {entities.map((entity) => (
                  <Badge
                    key={entity.id}
                    variant={
                      form.entityIds.includes(entity.id)
                        ? 'default'
                        : 'outline'
                    }
                    className="cursor-pointer"
                    onClick={() => toggleEntityForForm(entity.id)}
                  >
                    {entity.name}
                  </Badge>
                ))}
                {entities.length === 0 && (
                  <span className="text-sm text-muted-foreground">
                    No entities available.
                  </span>
                )}
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddOpen(false)}
            >
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={createUser.isPending}>
              {createUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Create User
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit User Dialog */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit User</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>First Name</Label>
                <Input
                  value={form.firstName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, firstName: e.target.value }))
                  }
                />
              </div>
              <div>
                <Label>Last Name</Label>
                <Input
                  value={form.lastName}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, lastName: e.target.value }))
                  }
                />
              </div>
            </div>
            <div>
              <Label>Email</Label>
              <Input
                type="email"
                value={form.email}
                disabled
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditOpen(false)}
            >
              Cancel
            </Button>
            <Button onClick={handleUpdate} disabled={updateUser.isPending}>
              {updateUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Save Changes
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
            <DialogTitle>Confirm Deactivation</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Are you sure you want to deactivate this user? They will no longer
            be able to sign in.
          </p>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setConfirmDeactivate(null)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() =>
                confirmDeactivate && handleDeactivate(confirmDeactivate)
              }
              disabled={deactivateUser.isPending}
            >
              {deactivateUser.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Deactivate
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
