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
  LeaveRequestParams,
  LeaveType,
  LeaveRequest,
  LeaveBalance,
  WorkTimeEntry,
  SubmitLeaveRequestRequest,
  LogWorkTimeRequest,
  CreateLeaveTypeRequest,
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

// ---------------------------------------------------------------------------
// Leave Types
// ---------------------------------------------------------------------------

export function useLeaveTypes(entityId?: string) {
  return useQuery({
    queryKey: queryKeys.hr.leaveTypes(entityId),
    queryFn: async () => {
      const { data } = await api.get<LeaveType[]>('/hr/leave-types', { params: { entityId } });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateLeaveType() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateLeaveTypeRequest) => {
      const { data } = await api.post<{ id: string }>('/hr/leave-types', request);
      return data;
    },
    onSuccess: () => {
      toast.success('Urlaubstyp erstellt');
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.leaveTypes() });
    },
    onError: () => toast.error('Fehler beim Erstellen des Urlaubstyps'),
  });
}

// ---------------------------------------------------------------------------
// Leave Requests
// ---------------------------------------------------------------------------

export function useLeaveRequests(params?: LeaveRequestParams) {
  return useQuery({
    queryKey: [...queryKeys.hr.leaveRequests(), params ?? {}],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<LeaveRequest>>('/hr/leave-requests', { params });
      return data;
    },
  });
}

export function useSubmitLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: SubmitLeaveRequestRequest) => {
      const { data } = await api.post<{ id: string }>('/hr/leave-requests', request);
      return data;
    },
    onSuccess: () => {
      toast.success('Urlaubsantrag eingereicht');
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error('Fehler beim Einreichen des Urlaubsantrags'),
  });
}

export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.put(`/hr/leave-requests/${id}/approve`);
    },
    onSuccess: () => {
      toast.success('Urlaubsantrag genehmigt');
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error('Fehler bei der Genehmigung'),
  });
}

export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await api.put(`/hr/leave-requests/${id}/reject`, { reason });
    },
    onSuccess: () => {
      toast.success('Urlaubsantrag abgelehnt');
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error('Fehler bei der Ablehnung'),
  });
}

// ---------------------------------------------------------------------------
// Leave Balances
// ---------------------------------------------------------------------------

export function useLeaveBalance(employeeId: string, year?: number) {
  return useQuery({
    queryKey: queryKeys.hr.leaveBalance(employeeId, year),
    queryFn: async () => {
      const { data } = await api.get<LeaveBalance[]>(`/hr/leave-balances/${employeeId}`, {
        params: year ? { year } : undefined,
      });
      return data;
    },
    enabled: !!employeeId,
  });
}

// ---------------------------------------------------------------------------
// Work Time
// ---------------------------------------------------------------------------

export function useWorkTime(employeeId: string, month?: string) {
  return useQuery({
    queryKey: queryKeys.hr.workTime(employeeId, month),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<WorkTimeEntry>>(`/hr/work-time/${employeeId}`, {
        params: { month, page: 1, pageSize: 500 }, // a full month should not exceed this; all entries needed for accurate summary
      });
      return data;
    },
    enabled: !!employeeId,
  });
}

export function useLogWorkTime() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: LogWorkTimeRequest) => {
      const { data } = await api.post<{ id: string }>('/hr/work-time', request);
      return data;
    },
    onSuccess: (_, variables) => {
      toast.success('Arbeitszeit eingetragen');
      const month = variables.date.substring(0, 7);
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.workTime(variables.employeeId, month) });
    },
    onError: () => toast.error('Fehler beim Eintragen der Arbeitszeit'),
  });
}
