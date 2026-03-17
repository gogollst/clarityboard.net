import * as XLSX from 'xlsx';
import type { TFunction } from 'i18next';

const FIELD_KEYS = [
  // Employee fields
  'EmployeeNumber',
  'EmployeeType',
  'FirstName',
  'LastName',
  'DateOfBirth',
  'TaxId',
  'HireDate',
  'Gender',
  'Nationality',
  'Position',
  'EmploymentType',
  'WorkEmail',
  'PersonalEmail',
  'PersonalPhone',
  'SocialSecurityNumber',
  'IBAN',
  'BIC',
  // Contract / Payroll fields
  'ContractType',
  'SalaryType',
  'GrossAmount',
  'WeeklyHours',
  'WorkdaysPerWeek',
  'ContractStartDate',
  'ContractEndDate',
  'AnnualVacationDays',
  'Has13thSalary',
  'HasVacationBonus',
] as const;

const LABEL_KEYS: Record<string, string> = {
  EmployeeNumber: 'employees.fields.employeeNumber',
  EmployeeType: 'employees.fields.employeeType',
  FirstName: 'employees.fields.firstName',
  LastName: 'employees.fields.lastName',
  DateOfBirth: 'employees.fields.dateOfBirth',
  TaxId: 'employees.fields.taxId',
  HireDate: 'employees.fields.hireDate',
  Gender: 'employees.fields.gender',
  Nationality: 'employees.fields.nationality',
  Position: 'employees.fields.position',
  EmploymentType: 'employees.fields.employmentType',
  WorkEmail: 'employees.fields.workEmail',
  PersonalEmail: 'employees.fields.personalEmail',
  PersonalPhone: 'employees.fields.personalPhone',
  SocialSecurityNumber: 'employees.fields.socialSecurityNumber',
  IBAN: 'employees.bankDetails.iban',
  BIC: 'employees.bankDetails.bic',
  ContractType: 'employees.import.contractFields.contractType',
  SalaryType: 'employees.import.contractFields.salaryType',
  GrossAmount: 'employees.import.contractFields.grossAmount',
  WeeklyHours: 'employees.import.contractFields.weeklyHours',
  WorkdaysPerWeek: 'employees.import.contractFields.workdaysPerWeek',
  ContractStartDate: 'employees.import.contractFields.contractStartDate',
  ContractEndDate: 'employees.import.contractFields.contractEndDate',
  AnnualVacationDays: 'employees.import.contractFields.annualVacationDays',
  Has13thSalary: 'employees.import.contractFields.has13thSalary',
  HasVacationBonus: 'employees.import.contractFields.hasVacationBonus',
};

const EXAMPLE_ROW: Record<string, string> = {
  EmployeeNumber: 'EMP-001',
  EmployeeType: 'Employee',
  FirstName: 'Max',
  LastName: 'Mustermann',
  DateOfBirth: '1990-05-15',
  TaxId: '12345678901',
  HireDate: '2026-04-01',
  Gender: 'Male',
  Nationality: 'German',
  Position: 'Software Developer',
  EmploymentType: 'FullTime',
  WorkEmail: 'max@company.de',
  PersonalEmail: 'max@example.com',
  PersonalPhone: '+49 170 1234567',
  SocialSecurityNumber: '12 150590 M 001',
  IBAN: 'DE89 3704 0044 0532 0130 00',
  BIC: 'DEUTDEDB',
  ContractType: 'Permanent',
  SalaryType: 'Monthly',
  GrossAmount: '5000.00',
  WeeklyHours: '40',
  WorkdaysPerWeek: '5',
  ContractStartDate: '2026-04-01',
  ContractEndDate: '',
  AnnualVacationDays: '30',
  Has13thSalary: 'No',
  HasVacationBonus: 'No',
};

export function generateTemplate(t: TFunction) {
  const wb = XLSX.utils.book_new();

  // Row 1: machine keys, Row 2: translated labels, Row 3: example
  const row1 = FIELD_KEYS.map((k) => k);
  const row2 = FIELD_KEYS.map((k) => t(LABEL_KEYS[k]));
  const row3 = FIELD_KEYS.map((k) => EXAMPLE_ROW[k] ?? '');

  const ws = XLSX.utils.aoa_to_sheet([row1, row2, row3]);

  // Set column widths
  ws['!cols'] = FIELD_KEYS.map((k) => ({
    wch: Math.max(k.length, (EXAMPLE_ROW[k] ?? '').length, 16),
  }));

  // Valid Values reference sheet
  const validValuesData = [
    ['EmployeeType', 'Gender', 'EmploymentType', 'ContractType', 'SalaryType', 'Yes/No'],
    ['Employee', 'Male', 'FullTime', 'Permanent', 'Monthly', 'Yes'],
    ['Contractor', 'Female', 'PartTime', 'FixedTerm', 'Hourly', 'No'],
    ['', 'Diverse', 'WorkingStudent', 'Freelance', 'DailyRate', ''],
    ['', 'NotSpecified', 'MiniJob', 'WorkingStudent', '', ''],
    ['', '', 'Internship', '', '', ''],
  ];
  const wsValid = XLSX.utils.aoa_to_sheet(validValuesData);
  wsValid['!cols'] = [{ wch: 16 }, { wch: 16 }, { wch: 18 }, { wch: 18 }, { wch: 14 }, { wch: 10 }];

  XLSX.utils.book_append_sheet(wb, ws, 'Data');
  XLSX.utils.book_append_sheet(wb, wsValid, 'Valid Values');

  XLSX.writeFile(wb, 'employee-import-template.xlsx');
}

export { FIELD_KEYS };
