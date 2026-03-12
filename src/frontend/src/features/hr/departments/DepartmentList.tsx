import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDepartments, useDeleteDepartment, useEmployees } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
import { useAuth } from '@/hooks/useAuth';
import type { Department } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/components/ui/dialog';
import { Skeleton } from '@/components/ui/skeleton';
import { Plus, Pencil, Trash2, Loader2 } from 'lucide-react';
import DepartmentFormDialog from './DepartmentFormDialog';

export function Component() {
  const { t } = useTranslation('hr');
  const { selectedEntityId } = useEntity();
  const { hasPermission } = useAuth();
  const { data: departments, isLoading } = useDepartments(selectedEntityId ?? undefined);
  const { data: employeesData } = useEmployees({ entityId: selectedEntityId ?? undefined, pageSize: 100 });
  const deleteDepartment = useDeleteDepartment();

  const [createOpen, setCreateOpen] = useState(false);
  const [editDepartment, setEditDepartment] = useState<Department | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Department | null>(null);

  const canManage = hasPermission('entity.manage');
  const employees = employeesData?.items ?? [];

  if (!selectedEntityId) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {t('employees.noEntitySelected')}
      </div>
    );
  }

  const handleDelete = () => {
    if (!deleteTarget) return;
    deleteDepartment.mutate(
      { id: deleteTarget.id, entityId: selectedEntityId },
      { onSuccess: () => setDeleteTarget(null) },
    );
  };

  return (
    <div>
      <PageHeader
        title={t('departments.title')}
        actions={
          canManage ? (
            <Button size="sm" onClick={() => setCreateOpen(true)}>
              <Plus className="mr-1 h-4 w-4" />
              {t('departments.newDepartment')}
            </Button>
          ) : undefined
        }
      />

      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full rounded" />
          ))}
        </div>
      ) : !departments || departments.length === 0 ? (
        <p className="py-8 text-center text-sm text-muted-foreground">
          {t('departments.noDepartments')}
        </p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('departments.columns.code')}</TableHead>
              <TableHead>{t('departments.columns.name')}</TableHead>
              <TableHead>{t('departments.columns.manager')}</TableHead>
              <TableHead>{t('departments.columns.status')}</TableHead>
              {canManage && <TableHead className="w-24" />}
            </TableRow>
          </TableHeader>
          <TableBody>
            {departments.map((dept) => (
              <TableRow key={dept.id}>
                <TableCell className="font-mono text-sm">{dept.code}</TableCell>
                <TableCell className="font-medium">{dept.name}</TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {dept.managerName ?? '—'}
                </TableCell>
                <TableCell>
                  {dept.isActive ? (
                    <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
                      {t('departments.status.active')}
                    </Badge>
                  ) : (
                    <Badge variant="secondary">{t('departments.status.inactive')}</Badge>
                  )}
                </TableCell>
                {canManage && (
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setEditDepartment(dept)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleteTarget(dept)}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create Dialog */}
      <DepartmentFormDialog
        mode="create"
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        entityId={selectedEntityId}
        departments={departments ?? []}
        employees={employees}
      />

      {/* Edit Dialog */}
      {editDepartment && (
        <DepartmentFormDialog
          mode="edit"
          open={!!editDepartment}
          onClose={() => setEditDepartment(null)}
          entityId={selectedEntityId}
          department={editDepartment}
          departments={departments ?? []}
          employees={employees}
        />
      )}

      {/* Delete Confirmation */}
      <Dialog open={!!deleteTarget} onOpenChange={(v) => { if (!v) setDeleteTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('departments.deleteTitle')}</DialogTitle>
            <DialogDescription>
              {t('departments.deleteDescription', { name: deleteTarget?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteDepartment.isPending}
            >
              {deleteDepartment.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('common:buttons.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
