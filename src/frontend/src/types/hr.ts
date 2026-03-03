// List DTO
export interface EmployeeListItem {
  id: string;
  employeeNumber: string;
  fullName: string;
  employeeType: string;   // "Employee" | "Contractor"
  status: string;         // "Active" | "OnLeave" | "Terminated"
  departmentName?: string;
  managerName?: string;
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
  hireDate: string;
  terminationDate?: string;
  terminationReason?: string;
  managerId?: string;
  managerName?: string;
  departmentId?: string;
  departmentName?: string;
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
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  taxId: string;
  managerId?: string;
  departmentId?: string;
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
