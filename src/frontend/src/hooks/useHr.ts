import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import i18n from '@/i18n';
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
  CreateContractRequest,
  UpdateContractRequest,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  EmployeeListParams,
  LeaveRequestParams,
  LeaveType,
  LeaveRequest,
  LeaveBalance,
  WorkTimeEntry,
  SubmitLeaveRequestRequest,
  LogWorkTimeRequest,
  CreateLeaveTypeRequest,
  TravelExpenseReport,
  TravelExpenseReportDetail,
  TravelExpenseListParams,
  CreateTravelExpenseReportRequest,
  AddTravelExpenseItemRequest,
  PerformanceReview,
  PerformanceReviewDetail,
  ReviewListParams,
  EmployeeDocument,
  DeletionRequest,
  HeadcountStats,
  TurnoverStats,
  SalaryBands,
  OnboardingChecklistSummary,
  OnboardingChecklistDetail,
  CreateOnboardingChecklistRequest,
  AddOnboardingTaskRequest,
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

export function useMyEmployee() {
  return useQuery({
    queryKey: [...queryKeys.hr.all, 'me'],
    queryFn: async () => {
      const { data } = await api.get<EmployeeDetail>('/hr/employees/me');
      return data;
    },
    retry: false,
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
      toast.success(i18n.t('hr:toast.employeeCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.employeeCreateError'));
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
      toast.success(i18n.t('hr:toast.employeeUpdated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employee(variables.id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.employeeUpdateError'));
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
      toast.success(i18n.t('hr:toast.employeeTerminated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employee(variables.id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.employees() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.employeeTerminateError'));
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
      const { data } = await api.post<{ id: string }>(`/hr/employees/${employeeId}/contracts`, request);
      return data;
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('hr:toast.contractCreated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.hr.contracts(variables.employeeId),
      });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.contractCreateError'));
    },
  });
}

export function useUpdateContract() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      employeeId,
      contractId,
      ...request
    }: UpdateContractRequest & { employeeId: string; contractId: string }) => {
      await api.put(`/hr/employees/${employeeId}/contracts/${contractId}`, request);
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('hr:toast.contractUpdated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.hr.contracts(variables.employeeId),
      });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.contractUpdateError'));
    },
  });
}

export function useAttachDocToContract() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ employeeId, contractId, docId }: { employeeId: string; contractId: string; docId: string }) => {
      await api.post(`/hr/employees/${employeeId}/contracts/${contractId}/documents/${docId}`);
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('hr:toast.documentAttached'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.contracts(variables.employeeId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.documents(variables.employeeId) });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.documentAttachError'));
    },
  });
}

export function useDetachDocFromContract() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ employeeId, contractId, docId }: { employeeId: string; contractId: string; docId: string }) => {
      await api.delete(`/hr/employees/${employeeId}/contracts/${contractId}/documents/${docId}`);
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('hr:toast.documentDetached'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.contracts(variables.employeeId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.documents(variables.employeeId) });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.documentDetachError'));
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
      toast.success(i18n.t('hr:toast.departmentCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.departments() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.departmentCreateError'));
    },
  });
}

export function useUpdateDepartment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      ...request
    }: UpdateDepartmentRequest & { id: string }) => {
      await api.put(`/hr/departments/${id}`, request);
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.departmentUpdated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.departments() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.departmentUpdateError'));
    },
  });
}

export function useDeleteDepartment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, entityId }: { id: string; entityId: string }) => {
      await api.delete(`/hr/departments/${id}`, { params: { entityId } });
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.departmentDeleted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.departments() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.departmentDeleteError'));
    },
  });
}

export function useDeactivateDepartment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, entityId }: { id: string; entityId: string }) => {
      await api.post(`/hr/departments/${id}/deactivate`, null, { params: { entityId } });
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.departmentDeactivated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.departments() });
    },
    onError: () => {
      toast.error(i18n.t('hr:toast.departmentDeactivatedError'));
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
      toast.success(i18n.t('hr:toast.leaveTypeCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.leaveTypes() });
    },
    onError: () => toast.error(i18n.t('hr:toast.leaveTypeCreateError')),
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
      toast.success(i18n.t('hr:toast.leaveSubmitted'));
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error(i18n.t('hr:toast.leaveSubmitError')),
  });
}

export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.put(`/hr/leave-requests/${id}/approve`);
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.leaveApproved'));
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error(i18n.t('hr:toast.leaveApproveError')),
  });
}

export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await api.put(`/hr/leave-requests/${id}/reject`, { reason });
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.leaveRejected'));
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-requests'] });
      queryClient.invalidateQueries({ queryKey: ['hr', 'leave-balance'] });
    },
    onError: () => toast.error(i18n.t('hr:toast.leaveRejectError')),
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
      toast.success(i18n.t('hr:toast.workTimeLogged'));
      const month = variables.date.substring(0, 7);
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.workTime(variables.employeeId, month) });
    },
    onError: () => toast.error(i18n.t('hr:toast.workTimeLogError')),
  });
}

// ---------------------------------------------------------------------------
// Travel Expenses
// ---------------------------------------------------------------------------

export function useTravelExpenses(params?: TravelExpenseListParams) {
  return useQuery({
    queryKey: [...queryKeys.hr.travelExpenses(), params ?? {}],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<TravelExpenseReport>>('/hr/travel-expenses', { params });
      return data;
    },
  });
}

export function useTravelExpense(id: string) {
  return useQuery({
    queryKey: queryKeys.hr.travelExpense(id),
    queryFn: async () => {
      const { data } = await api.get<TravelExpenseReportDetail>(`/hr/travel-expenses/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateTravelExpenseReport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateTravelExpenseReportRequest) => {
      const { data } = await api.post<{ id: string }>('/hr/travel-expenses', request);
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.travelReportCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpenses() });
    },
    onError: () => toast.error(i18n.t('hr:toast.travelReportCreateError')),
  });
}

export function useAddTravelExpenseItem(reportId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: AddTravelExpenseItemRequest) => {
      const { data } = await api.post<{ id: string }>(`/hr/travel-expenses/${reportId}/items`, request);
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.travelItemAdded'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpense(reportId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpenses() });
    },
    onError: () => toast.error(i18n.t('hr:toast.travelItemAddError')),
  });
}

export function useSubmitTravelExpense() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (reportId: string) => {
      await api.post(`/hr/travel-expenses/${reportId}/submit`);
    },
    onSuccess: (_data, reportId) => {
      toast.success(i18n.t('hr:toast.travelSubmitted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpense(reportId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpenses() });
    },
    onError: () => toast.error(i18n.t('hr:toast.travelSubmitError')),
  });
}

export function useApproveTravelExpense() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (reportId: string) => {
      await api.put(`/hr/travel-expenses/${reportId}/approve`);
    },
    onSuccess: (_data, reportId) => {
      toast.success(i18n.t('hr:toast.travelApproved'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpense(reportId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.travelExpenses() });
    },
    onError: () => toast.error(i18n.t('hr:toast.travelApproveError')),
  });
}

// ---------------------------------------------------------------------------
// Performance Reviews
// ---------------------------------------------------------------------------

export function useReviews(params?: ReviewListParams) {
  return useQuery({
    queryKey: [...queryKeys.hr.reviews(), params ?? {}],
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<PerformanceReview>>('/hr/reviews', { params });
      return data;
    },
  });
}

export function useReview(id: string) {
  return useQuery({
    queryKey: queryKeys.hr.review(id),
    queryFn: async () => {
      const { data } = await api.get<PerformanceReviewDetail>(`/hr/reviews/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateReview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: {
      employeeId: string;
      reviewerId: string;
      reviewPeriodStart: string;
      reviewPeriodEnd: string;
      reviewType: string;
    }) => {
      const { data } = await api.post<{ id: string }>('/hr/reviews', request);
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.reviewCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.reviews() });
    },
    onError: () => toast.error(i18n.t('hr:toast.reviewCreateError')),
  });
}

export function useSubmitFeedback(reviewId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: {
      respondentType: string;
      isAnonymous: boolean;
      rating: number;
      comments?: string;
      competencyScores?: Record<string, number>;
    }) => {
      const { data } = await api.post<{ id: string }>(`/hr/reviews/${reviewId}/feedback`, request);
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.feedbackSubmitted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.review(reviewId) });
    },
    onError: () => toast.error(i18n.t('hr:toast.feedbackSubmitError')),
  });
}

export function useCompleteReview(reviewId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: {
      overallRating: number;
      strengthsNotes: string;
      improvementNotes: string;
      goalsNotes: string;
    }) => {
      await api.put(`/hr/reviews/${reviewId}/complete`, request);
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.reviewCompleted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.review(reviewId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.reviews() });
    },
    onError: () => toast.error(i18n.t('hr:toast.reviewCompleteError')),
  });
}

// ---------------------------------------------------------------------------
// Employee Documents
// ---------------------------------------------------------------------------

export function useEmployeeDocuments(employeeId: string) {
  return useQuery({
    queryKey: queryKeys.hr.documents(employeeId),
    queryFn: async () => {
      const { data } = await api.get<EmployeeDocument[]>(`/hr/employees/${employeeId}/documents`);
      return data;
    },
    enabled: !!employeeId,
  });
}

export function useDeleteDocument(employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (docId: string) => {
      await api.delete(`/hr/employees/${employeeId}/documents/${docId}`);
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.documentDeleted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.documents(employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:toast.documentDeleteError')),
  });
}

export function useUploadDocument(employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (formData: FormData) => {
      const { data } = await api.post(
        `/hr/employees/${employeeId}/documents`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      );
      return data;
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.documentUploaded'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.documents(employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:toast.documentUploadError')),
  });
}

// ---------------------------------------------------------------------------
// DSGVO / Deletion Requests
// ---------------------------------------------------------------------------

export function useDeletionRequests() {
  return useQuery({
    queryKey: queryKeys.hr.deletionRequests(),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResponse<DeletionRequest>>('/hr/deletion-requests');
      return data;
    },
  });
}

export function useScheduleDeletion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (employeeId: string) => {
      await api.post(`/hr/employees/${employeeId}/schedule-deletion`);
    },
    onSuccess: () => {
      toast.success(i18n.t('hr:toast.deletionScheduled'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.deletionRequests() });
    },
    onError: () => toast.error(i18n.t('hr:toast.deletionScheduleError')),
  });
}

// ---------------------------------------------------------------------------
// HR Statistics
// ---------------------------------------------------------------------------

export function useHeadcountStats(entityId: string, departmentId?: string) {
  return useQuery({
    queryKey: [...queryKeys.hr.headcountStats(entityId), departmentId ?? ''],
    queryFn: async () => {
      const { data } = await api.get<HeadcountStats>('/hr/stats/headcount', {
        params: { entityId, departmentId: departmentId || undefined },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useTurnoverStats(entityId: string, departmentId?: string) {
  return useQuery({
    queryKey: [...queryKeys.hr.turnoverStats(entityId), departmentId ?? ''],
    queryFn: async () => {
      const { data } = await api.get<TurnoverStats>('/hr/stats/turnover', {
        params: { entityId, departmentId: departmentId || undefined },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useSalaryBands(entityId: string, departmentId?: string) {
  return useQuery({
    queryKey: [...queryKeys.hr.salaryBands(entityId), departmentId ?? ''],
    queryFn: async () => {
      const { data } = await api.get<SalaryBands>('/hr/stats/salary-bands', {
        params: { entityId, departmentId: departmentId || undefined },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

// ---------------------------------------------------------------------------
// Onboarding Checklists
// ---------------------------------------------------------------------------

export function useOnboardingChecklists(employeeId: string) {
  return useQuery({
    queryKey: queryKeys.hr.onboardingChecklists(employeeId),
    queryFn: async () => {
      const { data } = await api.get<OnboardingChecklistSummary[]>('/hr/onboarding', {
        params: { employeeId },
      });
      return data;
    },
    enabled: !!employeeId,
  });
}

export function useOnboardingChecklist(id: string) {
  return useQuery({
    queryKey: queryKeys.hr.onboardingChecklist(id),
    queryFn: async () => {
      const { data } = await api.get<OnboardingChecklistDetail>(`/hr/onboarding/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateOnboardingChecklist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateOnboardingChecklistRequest) => {
      const { data } = await api.post<{ id: string }>('/hr/onboarding', body);
      return data.id;
    },
    onSuccess: (_, variables) => {
      toast.success(i18n.t('hr:onboarding.created'));
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklists(variables.employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:onboarding.createError')),
  });
}

export function useAddOnboardingTask(checklistId: string, employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: AddOnboardingTaskRequest) => {
      await api.post(`/hr/onboarding/${checklistId}/tasks`, body);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklist(checklistId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklists(employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:onboarding.taskError')),
  });
}

export function useCompleteOnboardingTask(checklistId: string, employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskId: string) => {
      await api.put(`/hr/onboarding/${checklistId}/tasks/${taskId}/complete`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklist(checklistId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklists(employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:onboarding.taskError')),
  });
}

export function useReopenOnboardingTask(checklistId: string, employeeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskId: string) => {
      await api.put(`/hr/onboarding/${checklistId}/tasks/${taskId}/reopen`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklist(checklistId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.hr.onboardingChecklists(employeeId) });
    },
    onError: () => toast.error(i18n.t('hr:onboarding.taskError')),
  });
}
