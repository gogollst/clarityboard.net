import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useEmployees } from '@/hooks/useHr';
import { useEntity } from '@/hooks/useEntity';
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
import { Plus, Search } from 'lucide-react';

function useDebounced<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

function getStatusBadge(status: string) {
  switch (status) {
    case 'Active':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          Aktiv
        </Badge>
      );
    case 'OnLeave':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          Im Urlaub
        </Badge>
      );
    case 'Terminated':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          Gekündigt
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

function getTypeBadge(employeeType: string) {
  if (employeeType === 'Contractor') {
    return <Badge variant="outline">Contractor</Badge>;
  }
  return <Badge variant="secondary">Festangestellt</Badge>;
}

export function Component() {
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [employeeType, setEmployeeType] = useState('');
  const debouncedSearch = useDebounced(search, 300);

  // Reset to page 1 when filters change
  const prevSearch = useRef(debouncedSearch);
  const prevStatus = useRef(status);
  const prevType = useRef(employeeType);
  useEffect(() => {
    if (
      prevSearch.current !== debouncedSearch ||
      prevStatus.current !== status ||
      prevType.current !== employeeType
    ) {
      setPage(1);
      prevSearch.current = debouncedSearch;
      prevStatus.current = status;
      prevType.current = employeeType;
    }
  }, [debouncedSearch, status, employeeType]);

  const { data, isLoading } = useEmployees({
    page,
    pageSize: 25,
    search: debouncedSearch || undefined,
    status: status || undefined,
    employeeType: employeeType || undefined,
    entityId: selectedEntityId ?? undefined,
  });

  const employees: EmployeeListItem[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const pageSize = data?.pageSize ?? 25;

  const columns = [
    {
      key: 'employeeNumber',
      header: 'Personalnummer',
    },
    {
      key: 'fullName',
      header: 'Name',
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
      header: 'Typ',
      render: (item: Record<string, unknown>) =>
        getTypeBadge(String(item.employeeType ?? '')),
    },
    {
      key: 'status',
      header: 'Status',
      render: (item: Record<string, unknown>) =>
        getStatusBadge(String(item.status ?? '')),
    },
    {
      key: 'departmentName',
      header: 'Abteilung',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.departmentName ?? '—')}
        </span>
      ),
    },
    {
      key: 'managerName',
      header: 'Manager',
      render: (item: Record<string, unknown>) => (
        <span className="text-sm text-muted-foreground">
          {String(item.managerName ?? '—')}
        </span>
      ),
    },
    {
      key: 'hireDate',
      header: 'Einstellungsdatum',
      render: (item: Record<string, unknown>) => {
        const raw = item.hireDate as string | undefined;
        if (!raw) return '—';
        const d = new Date(raw);
        return d.toLocaleDateString('de-DE');
      },
    },
  ];

  return (
    <div>
      <PageHeader
        title="Mitarbeiter"
        actions={
          <Button onClick={() => navigate('/hr/employees/new')}>
            <Plus className="mr-1 h-4 w-4" />
            Neuer Mitarbeiter
          </Button>
        }
      />

      {/* Filter bar */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Name oder Personalnummer..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <Select
          value={status || 'all'}
          onValueChange={(v) => setStatus(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Alle Status</SelectItem>
            <SelectItem value="Active">Aktiv</SelectItem>
            <SelectItem value="OnLeave">Im Urlaub</SelectItem>
            <SelectItem value="Terminated">Gekündigt</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={employeeType || 'all'}
          onValueChange={(v) => setEmployeeType(v === 'all' ? '' : v)}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Anstellungsart" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Alle Typen</SelectItem>
            <SelectItem value="Employee">Festangestellt</SelectItem>
            <SelectItem value="Contractor">Contractor</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={employees as unknown as Record<string, unknown>[]}
        isLoading={isLoading}
        emptyMessage="Keine Mitarbeiter gefunden."
        pagination={
          totalCount > pageSize
            ? { page, pageSize, total: totalCount, onPageChange: setPage }
            : undefined
        }
      />
    </div>
  );
}
