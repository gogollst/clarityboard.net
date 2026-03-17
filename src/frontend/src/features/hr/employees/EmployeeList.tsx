import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEmployees, useDepartments } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
import { useDebounced } from '@/hooks/useDebounced';
import type { EmployeeListItem } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import DataTable from '@/components/shared/DataTable';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { Plus, Search, Upload } from 'lucide-react';

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [employeeType, setEmployeeType] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const debouncedSearch = useDebounced(search, 300);

  const { data: departmentsData } = useDepartments(selectedEntityId ?? undefined);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, status, employeeType, departmentId]);

  const { data, isLoading } = useEmployees({
    page,
    pageSize: 25,
    search: debouncedSearch || undefined,
    status: status || undefined,
    employeeType: employeeType || undefined,
    departmentId: departmentId || undefined,
    entityId: selectedEntityId ?? undefined,
  });

  const employees: EmployeeListItem[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 25;

  function getStatusBadge(empStatus: string) {
    switch (empStatus) {
      case 'Active':
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
            {t('employees.status.Active')}
          </Badge>
        );
      case 'OnLeave':
        return (
          <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
            {t('employees.status.OnLeave')}
          </Badge>
        );
      case 'Terminated':
        return (
          <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
            {t('employees.status.Terminated')}
          </Badge>
        );
      default:
        return <Badge variant="secondary">{empStatus}</Badge>;
    }
  }

  function getTypeBadge(empType: string) {
    if (empType === 'Contractor') {
      return <Badge variant="outline">{t('employees.type.Contractor')}</Badge>;
    }
    return <Badge variant="secondary">{t('employees.type.Employee')}</Badge>;
  }

  const columns = [
    {
      key: 'employeeNumber',
      header: t('employees.columns.employeeNumber'),
    },
    {
      key: 'fullName',
      header: t('employees.columns.name'),
      render: (item: Record<string, unknown>) => (
        <button
          className="font-medium text-left hover:underline text-foreground"
          onClick={() => navigate(`/hr/employees/${String(item.id ?? '')}`)}
        >
          {String(item.fullName ?? '')}
        </button>
      ),
    },
    {
      key: 'employeeType',
      header: t('employees.columns.type'),
      render: (item: Record<string, unknown>) =>
        getTypeBadge(String(item.employeeType ?? '')),
    },
    {
      key: 'status',
      header: t('employees.columns.status'),
      render: (item: Record<string, unknown>) =>
        getStatusBadge(String(item.status ?? '')),
    },
    {
      key: 'departmentName',
      header: t('employees.columns.department'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.departmentName ?? '—')}
        </span>
      ),
    },
    {
      key: 'managerName',
      header: t('employees.columns.manager'),
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.managerName ?? '—')}
        </span>
      ),
    },
    {
      key: 'hireDate',
      header: t('employees.columns.hireDate'),
      render: (item: Record<string, unknown>) => {
        const raw = item.hireDate as string | undefined;
        if (!raw) return '—';
        const d = new Date(raw);
        return d.toLocaleDateString(i18n.language);
      },
    },
  ];

  return (
    <div>
      <PageHeader
        title={t('employees.title')}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate('/hr/employees/import')}>
              <Upload className="mr-1 h-4 w-4" />
              {t('employees.import.button')}
            </Button>
            <Button onClick={() => navigate('/hr/employees/new')}>
              <Plus className="mr-1 h-4 w-4" />
              {t('employees.newEmployee')}
            </Button>
          </div>
        }
      />

      {/* Filter bar */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder={t('employees.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <Select
          value={status || 'all'}
          onValueChange={(v) => setStatus(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('employees.filterStatus')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('employees.allStatuses')}</SelectItem>
            <SelectItem value="Active">{t('employees.status.Active')}</SelectItem>
            <SelectItem value="OnLeave">{t('employees.status.OnLeave')}</SelectItem>
            <SelectItem value="Terminated">{t('employees.status.Terminated')}</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={employeeType || 'all'}
          onValueChange={(v) => setEmployeeType(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder={t('employees.filterType')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('employees.allTypes')}</SelectItem>
            <SelectItem value="Employee">{t('employees.type.Employee')}</SelectItem>
            <SelectItem value="Contractor">{t('employees.type.Contractor')}</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={departmentId || 'all'}
          onValueChange={(v) => setDepartmentId(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t('employees.filterDepartment')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('employees.allDepartments')}</SelectItem>
            {(departmentsData ?? []).filter(d => d.isActive).map((dept) => (
              <SelectItem key={dept.id} value={dept.id}>{dept.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={employees as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage={t('employees.noEmployees')}
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />
    </div>
  );
}
