import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDepartments, useDeleteDepartment, useDeactivateDepartment, useEmployees } from '@/hooks/useHr';
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
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Plus, Pencil, Trash2, Archive, Loader2 } from 'lucide-react';
import DepartmentFormDialog from './DepartmentFormDialog';

export function Component() {
  const { t } = useTranslation('hr');
  const { selectedEntityId } = useEntity();
  const { hasPermission } = useAuth();
  const { data: departments, isLoading } = useDepartments(selectedEntityId ?? undefined);
  const { data: employeesData } = useEmployees({ entityId: selectedEntityId ?? undefined, pageSize: 100 });
  const deleteDepartment = useDeleteDepartment();
  const deactivateDepartment = useDeactivateDepartment();

  const [createOpen, setCreateOpen] = useState(false);
  const [editDepartment, setEditDepartment] = useState<Department | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Department | null>(null);
  const [deactivateTarget, setDeactivateTarget] = useState<Department | null>(null);

  const canManage = hasPermission('hr.manage');
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

  const handleDeactivate = () => {
    if (!deactivateTarget) return;
    deactivateDepartment.mutate(
      { id: deactivateTarget.id, entityId: selectedEntityId },
      { onSuccess: () => setDeactivateTarget(null) },
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
              <TableHead className="text-right">{t('departments.columns.employeeCount')}</TableHead>
              <TableHead>{t('departments.columns.status')}</TableHead>
              {canManage && <TableHead className="w-32" />}
            </TableRow>
          </TableHeader>
          <TableBody>
            {departments.map((dept) => (
              <TableRow key={dept.id}>
                <TableCell className="font-mono text-sm">{dept.code}</TableCell>
                <TableCell>
                  <div>
                    <span className="font-medium">{dept.name}</span>
                    {dept.description && (
                      <p className="text-xs text-muted-foreground truncate max-w-xs">{dept.description}</p>
                    )}
                  </div>
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {dept.managerName ?? '—'}
                </TableCell>
                <TableCell className="text-right text-sm">{dept.employeeCount}</TableCell>
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
                      {dept.isActive && (
                        <TooltipProvider>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => setDeactivateTarget(dept)}
                              >
                                <Archive className="h-4 w-4 text-amber-600" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>{t('departments.deactivateTitle')}</TooltipContent>
                          </Tooltip>
                        </TooltipProvider>
                      )}
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

      {/* Deactivate Confirmation */}
      <Dialog open={!!deactivateTarget} onOpenChange={(v) => { if (!v) setDeactivateTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('departments.deactivateTitle')}</DialogTitle>
            <DialogDescription>
              {t('departments.deactivateDescription', { name: deactivateTarget?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeactivateTarget(null)}>
              {t('common:buttons.cancel')}
            </Button>
            <Button
              onClick={handleDeactivate}
              disabled={deactivateDepartment.isPending}
            >
              {deactivateDepartment.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
              {t('common:buttons.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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
