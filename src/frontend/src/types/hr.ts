// List DTO
export interface EmployeeListItem {
  id: string;
  employeeNumber: string;
  fullName: string;
  employeeType: string;   // "Employee" | "Contractor"
  status: string;         // "Active" | "OnLeave" | "Terminated"
  departmentName?: string;
  managerName?: string;
  position?: string;
  hireDate: string;       // ISO date "YYYY-MM-DD"
  entityId: string;
}

// Detail DTO
export interface EmployeeDetail {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  employeeType: string;
  status: string;
  dateOfBirth: string;
  taxId: string;
  socialSecurityNumber?: string;
  hireDate: string;
  terminationDate?: string;
  terminationReason?: string;
  managerId?: string;
  managerName?: string;
  departmentId?: string;
  departmentName?: string;
  iban?: string;
  bic?: string;
  gender: string;
  nationality?: string;
  position?: string;
  employmentType?: string;
  workEmail?: string;
  personalEmail?: string;
  personalPhone?: string;
  costCenterId?: string;
  costCenterName?: string;
  entityId: string;
  createdAt: string;
}

// Salary DTO
export interface SalaryEntry {
  id: string;
  salaryType: string;         // "Monthly" | "Hourly" | "DailyRate"
  grossAmountCents: number;
  currencyCode: string;
  bonusAmountCents: number;
  bonusCurrencyCode: string;
  paymentCycleMonths: number;
  validFrom: string;          // ISO datetime
  validTo?: string;
  changeReason: string;
  isCurrent: boolean;
}

// Contract DTO
export interface ContractEntry {
  id: string;
  contractType: string;       // "Permanent" | "FixedTerm" | "Freelance" | "WorkingStudent"
  weeklyHours: number;
  workdaysPerWeek: number;
  startDate: string;
  endDate?: string;
  probationEndDate?: string;
  noticeWeeks: number;
  validFrom: string;
  validTo?: string;
  changeReason: string;
  isCurrent: boolean;
}

// Department DTO
export interface Department {
  id: string;
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  managerId?: string;
  managerName?: string;
  isActive: boolean;
}

// Create/Update request types
export interface CreateEmployeeRequest {
  entityId: string;
  employeeNumber: string;
  employeeType: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  taxId: string;
  hireDate: string;
  managerId?: string;
  departmentId?: string;
  gender?: string;
  nationality?: string;
  position?: string;
  employmentType?: string;
  workEmail?: string;
  personalEmail?: string;
  personalPhone?: string;
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  taxId: string;
  managerId?: string;
  departmentId?: string;
  iban?: string;
  bic?: string;
  entityId?: string;
  socialSecurityNumber?: string;
  gender?: string;
  nationality?: string;
  position?: string;
  employmentType?: string;
  workEmail?: string;
  personalEmail?: string;
  personalPhone?: string;
}

export interface TerminateEmployeeRequest {
  terminationDate: string;
  reason: string;
}

export interface UpdateSalaryRequest {
  grossAmountCents: number;
  currencyCode: string;
  bonusAmountCents?: number;
  bonusCurrencyCode?: string;
  salaryType: string;
  paymentCycleMonths?: number;
  validFrom: string;
  changeReason: string;
}

export interface CreateContractRequest {
  contractType: string;
  weeklyHours: number;
  workdaysPerWeek: number;
  startDate: string;
  endDate?: string;
  probationEndDate?: string;
  noticeWeeks: number;
  validFrom: string;
  changeReason: string;
}

export interface CreateDepartmentRequest {
  entityId: string;
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  managerId?: string;
}

export interface UpdateDepartmentRequest {
  entityId: string;
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  managerId?: string;
}

// Leave request query params
export interface LeaveRequestParams {
  status?: string;
  year?: number;
  page?: number;
  pageSize?: number;
  employeeId?: string;
}

// List query params
export interface EmployeeListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  employeeType?: string;
  departmentId?: string;
  entityId?: string;
}

// Leave Types
export interface LeaveType {
  id: string;
  entityId: string;
  name: string;
  code: string;
  requiresApproval: boolean;
  isDeductedFromBalance: boolean;
  maxDaysPerYear?: number;
  color: string; // hex color
  isActive: boolean;
}

// Leave Requests
export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeFullName: string;
  leaveTypeName: string;
  startDate: string; // ISO date
  endDate: string;
  workingDays: number;
  halfDay: boolean;
  status: string; // 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'
  notes?: string;
  rejectionReason?: string;
  requestedAt: string;
  approvedAt?: string;
}

// Leave Balances
export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  year: number;
  entitlementDays: number;
  usedDays: number;
  pendingDays: number;
  carryOverDays: number;
  remainingDays: number;
}

// Work Time
export interface WorkTimeEntry {
  id: string;
  employeeId: string;
  date: string; // ISO date
  startTime?: string; // 'HH:mm'
  endTime?: string;
  breakMinutes: number;
  totalMinutes: number;
  entryType: string; // 'Work' | 'Overtime' | 'Oncall'
  projectCode?: string;
  notes?: string;
  status: string; // 'Open' | 'Locked'
}

// Request types
export interface SubmitLeaveRequestRequest {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  halfDay: boolean;
  notes?: string;
}

export interface LogWorkTimeRequest {
  employeeId: string;
  date: string;
  startTime?: string;
  endTime?: string;
  breakMinutes?: number;
  totalMinutes?: number;
  entryType: string;
  projectCode?: string;
  notes?: string;
}

export interface CreateLeaveTypeRequest {
  entityId: string;
  name: string;
  code: string;
  requiresApproval: boolean;
  isDeductedFromBalance: boolean;
  maxDaysPerYear?: number;
  color: string;
  isActive: boolean;
}

export interface TravelExpenseReport {
  id: string;
  employeeId: string;
  employeeFullName: string;
  title: string;
  tripStartDate: string;
  tripEndDate: string;
  destination: string;
  businessPurpose: string;
  status: string; // 'Draft' | 'Submitted' | 'Approved' | 'Reimbursed' | 'Rejected'
  totalAmountCents: number;
  currencyCode: string;
  createdAt: string;
}

export interface TravelExpenseItem {
  id: string;
  reportId: string;
  expenseType: string; // 'Accommodation' | 'Transport' | 'Meal' | 'Other'
  expenseDate: string;
  description: string;
  originalAmountCents: number;
  originalCurrencyCode: string;
  exchangeRate: number;
  exchangeRateDate: string;
  amountCents: number;
  currencyCode: string;
  vatRatePercent?: number;
  isDeductible: boolean;
}

export interface TravelExpenseReportDetail extends TravelExpenseReport {
  items: TravelExpenseItem[];
}

export interface CreateTravelExpenseReportRequest {
  employeeId: string;
  title: string;
  tripStartDate: string;
  tripEndDate: string;
  destination: string;
  businessPurpose: string;
}

export interface AddTravelExpenseItemRequest {
  expenseType: string;
  expenseDate: string;
  description: string;
  originalAmountCents: number;
  originalCurrencyCode: string;
  exchangeRate: number;
  exchangeRateDate: string;
  vatRatePercent?: number;
  isDeductible: boolean;
}

export interface TravelExpenseListParams {
  employeeId?: string;
  status?: string;
  entityId?: string;
  page?: number;
  pageSize?: number;
}

// Employee Documents
export interface EmployeeDocument {
  id: string;
  employeeId: string;
  documentType: string;
  title: string;
  fileName: string;
  mimeType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  expiresAt?: string;
  isConfidential: boolean;
  deletionScheduledAt?: string;
}

// DSGVO Deletion Requests
export interface DeletionRequest {
  id: string;
  employeeId: string;
  employeeFullName: string;
  requestedBy: string;
  requestedAt: string;
  scheduledDeletionAt: string;
  status: string; // 'Pending' | 'Completed' | 'Blocked'
  blockReason?: string;
  completedAt?: string;
}

// Performance Reviews

export interface PerformanceReview {
  id: string;
  employeeId: string;
  employeeFullName: string;
  reviewerId: string;
  reviewerFullName: string;
  reviewPeriodStart: string;
  reviewPeriodEnd: string;
  reviewType: string; // 'Annual' | 'Probation' | 'Quarterly' | 'ThreeSixty'
  status: string; // 'Draft' | 'InProgress' | 'Completed'
  overallRating?: number;
  createdAt: string;
}

export interface FeedbackEntry {
  id: string;
  reviewId: string;
  respondentType: string;
  isAnonymous: boolean;
  rating: number;
  comments?: string;
  competencyScores?: Record<string, number>;
  submittedAt?: string;
}

export interface PerformanceReviewDetail extends PerformanceReview {
  strengthsNotes?: string;
  improvementNotes?: string;
  goalsNotes?: string;
  completedAt?: string;
  feedbackEntries: FeedbackEntry[];
}

export interface ReviewListParams {
  employeeId?: string;
  reviewType?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

// HR Statistics

export interface HeadcountMonthDto {
  month: string;
  count: number;
}

export interface HeadcountStats {
  totalActive: number;
  totalContractors: number;
  totalEmployees: number;
  monthlyTrend: HeadcountMonthDto[];
}

export interface TurnoverMonthDto {
  month: string;
  terminations: number;
  newHires: number;
}

export interface TurnoverStats {
  monthlyTurnover: TurnoverMonthDto[];
  averageTurnoverRate: number;
}

export interface SalaryBandDto {
  label: string;
  count: number;
}

export interface SalaryBands {
  minSalaryCents: number | null;
  maxSalaryCents: number | null;
  avgSalaryCents: number | null;
  medianSalaryCents: number | null;
  bands: SalaryBandDto[];
}

// Onboarding Checklists

export interface OnboardingChecklistSummary {
  id: string;
  title: string;
  status: string; // 'InProgress' | 'Completed'
  totalTasks: number;
  completedTasks: number;
  createdAt: string;
  completedAt?: string;
}

export interface OnboardingTask {
  id: string;
  title: string;
  description?: string;
  isCompleted: boolean;
  completedAt?: string;
  dueDate?: string;
  sortOrder: number;
}

export interface OnboardingChecklistDetail {
  id: string;
  employeeId: string;
  title: string;
  status: string;
  createdAt: string;
  completedAt?: string;
  tasks: OnboardingTask[];
}

export interface CreateOnboardingChecklistRequest {
  employeeId: string;
  title: string;
  tasks?: Array<{
    title: string;
    description?: string;
    dueDate?: string;
    sortOrder: number;
  }>;
}

export interface AddOnboardingTaskRequest {
  title: string;
  description?: string;
  dueDate?: string;
  sortOrder: number;
}
