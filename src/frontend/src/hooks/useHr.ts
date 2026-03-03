import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import type { PaginatedResponse } from '@/types/api';
import type {
  EmployeeListItem,
  EmployeeDetail,
  SalaryEntry,
  ContractEntry,
  Department,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  TerminateEmployeeRequest,
  UpdateSalaryRequest,
  CreateContractRequest,
  CreateDepartmentRequest,
  EmployeeListParams,
} from '@/types/hr';

// ---------------------------------------------------------------------------
// Employees
// ---------------------------------------------------------------------------

export function useEmployees(params?: EmployeeListParams) {
  return useQuery({
    queryKey: [...queryKeys.hr.employees(), params],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<EmployeeListItem>>(
        '/hr/employees',
        { params },
      );
      return data;
    },
  });
}

export function useEmployee(id: string) {
  return useQuery({
    queryKey: queryKeys.hr.employee(id),
    queryFn: async () => {
      const { data } = await api.get<EmployeeDetail>(`/hr/employees/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateEmployeeRequest) => {
      const { data } = await api.post<EmployeeDetail>('/hr/employees', request);
      return data;
    },
    onSuccess: () => {
      toast.success('Employee created');
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error('Failed to create employee');
    },
  });
}

export function useUpdateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      ...request
    }: UpdateEmployeeRequest & { id: string }) => {
      await api.put(`/hr/employees/${id}`, request);
    },
    onSuccess: (_data, variables) => {
      toast.success('Employee updated');
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employee(variables.id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error('Failed to update employee');
    },
  });
}

export function useTerminateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      ...request
    }: TerminateEmployeeRequest & { id: string }) => {
      await api.post(`/hr/employees/${id}/terminate`, request);
    },
    onSuccess: (_data, variables) => {
      toast.success('Employee terminated');
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employee(variables.id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error('Failed to terminate employee');
    },
  });
}

// ---------------------------------------------------------------------------
// Salary History
// ---------------------------------------------------------------------------

export function useSalaryHistory(employeeId: string) {
  return useQuery({
    queryKey: queryKeys.hr.salaryHistory(employeeId),
    queryFn: async () => {
      const { data } = await api.get<SalaryEntry[]>(
        `/hr/employees/${employeeId}/salary-history`,
      );
      return data;
    },
    enabled: !!employeeId,
  });
}

export function useUpdateSalary() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      employeeId,
      ...request
    }: UpdateSalaryRequest & { employeeId: string }) => {
      await api.post(`/hr/employees/${employeeId}/salary`, request);
    },
    onSuccess: (_data, variables) => {
      toast.success('Salary updated');
      queryClient.invalidateQueries({
        queryKey: queryKeys.hr.salaryHistory(variables.employeeId),
      });
    },
    onError: () => {
      toast.error('Failed to update salary');
    },
  });
}

// ---------------------------------------------------------------------------
// Contracts
// ---------------------------------------------------------------------------

export function useContractHistory(employeeId: string) {
  return useQuery({
    queryKey: queryKeys.hr.contracts(employeeId),
    queryFn: async () => {
      const { data } = await api.get<ContractEntry[]>(
        `/hr/employees/${employeeId}/contracts`,
      );
      return data;
    },
    enabled: !!employeeId,
  });
}

export function useCreateContract() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      employeeId,
      ...request
    }: CreateContractRequest & { employeeId: string }) => {
      await api.post(`/hr/employees/${employeeId}/contracts`, request);
    },
    onSuccess: (_data, variables) => {
      toast.success('Contract created');
      queryClient.invalidateQueries({
        queryKey: queryKeys.hr.contracts(variables.employeeId),
      });
    },
    onError: () => {
      toast.error('Failed to create contract');
    },
  });
}

// ---------------------------------------------------------------------------
// Departments
// ---------------------------------------------------------------------------

export function useDepartments(entityId?: string) {
  return useQuery({
    queryKey: queryKeys.hr.departments(entityId),
    queryFn: async () => {
      const { data } = await api.get<Department[]>('/hr/departments', {
        params: entityId ? { entityId } : undefined,
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateDepartment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateDepartmentRequest) => {
      const { data } = await api.post<Department>('/hr/departments', request);
      return data;
    },
    onSuccess: () => {
      toast.success('Department created');
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.departments() });
    },
    onError: () => {
      toast.error('Failed to create department');
    },
  });
}
