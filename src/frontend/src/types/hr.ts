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
