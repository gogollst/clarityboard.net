import type { EmployeeImportRow, ImportRowValidation } from '@/types/hr';
import type { TFunction } from 'i18next';

const EMPLOYEE_TYPES = ['Employee', 'Contractor'];
const GENDERS = ['Male', 'Female', 'Diverse', 'NotSpecified'];
const EMPLOYMENT_TYPES = ['FullTime', 'PartTime', 'WorkingStudent', 'MiniJob', 'Internship'];
const CONTRACT_TYPES = ['Permanent', 'FixedTerm', 'Freelance', 'WorkingStudent'];
const SALARY_TYPES = ['Monthly', 'Hourly', 'DailyRate'];
const YES_NO = ['Yes', 'No', 'Ja', 'Nein', 'Да', 'Нет', '1', '0', 'true', 'false'];

const ISO_DATE_RE = /^\d{4}-\d{2}-\d{2}$/;
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function isValidDate(s: string): boolean {
  if (!ISO_DATE_RE.test(s)) return false;
  const d = new Date(s);
  return !isNaN(d.getTime());
}

function hasContractData(row: EmployeeImportRow): boolean {
  return !!(row.grossAmount || row.salaryType || row.contractType);
}

export function validateRows(
  rows: EmployeeImportRow[],
  t: TFunction,
): ImportRowValidation[] {
  const seenNumbers = new Map<string, number>();
  const results: ImportRowValidation[] = [];

  for (const row of rows) {
    const errors: string[] = [];
    const warnings: string[] = [];

    // ── Employee fields ──

    // Required fields
    if (!row.employeeNumber) {
      errors.push(t('employees.import.validation.employeeNumberRequired'));
    } else if (row.employeeNumber.length > 20) {
      errors.push(t('employees.import.validation.employeeNumberMaxLength'));
    }

    if (!row.firstName) {
      errors.push(t('employees.import.validation.firstNameRequired'));
    } else if (row.firstName.length > 100) {
      errors.push(t('employees.import.validation.firstNameMaxLength'));
    }

    if (!row.lastName) {
      errors.push(t('employees.import.validation.lastNameRequired'));
    } else if (row.lastName.length > 100) {
      errors.push(t('employees.import.validation.lastNameMaxLength'));
    }

    if (!row.dateOfBirth) {
      errors.push(t('employees.import.validation.dateOfBirthRequired'));
    } else if (!isValidDate(row.dateOfBirth)) {
      errors.push(t('employees.import.validation.invalidDate', { field: t('employees.fields.dateOfBirth') }));
    }

    if (!row.taxId) {
      errors.push(t('employees.import.validation.taxIdRequired'));
    } else if (row.taxId.length > 50) {
      errors.push(t('employees.import.validation.taxIdMaxLength'));
    }

    if (!row.hireDate) {
      errors.push(t('employees.import.validation.hireDateRequired'));
    } else if (!isValidDate(row.hireDate)) {
      errors.push(t('employees.import.validation.invalidDate', { field: t('employees.fields.hireDate') }));
    }

    if (!row.employeeType) {
      errors.push(t('employees.import.validation.employeeTypeRequired'));
    } else if (!EMPLOYEE_TYPES.includes(row.employeeType)) {
      errors.push(t('employees.import.validation.invalidEmployeeType'));
    }

    // Optional enums
    if (row.gender && !GENDERS.includes(row.gender)) {
      errors.push(t('employees.import.validation.invalidGender'));
    }

    if (row.employmentType && !EMPLOYMENT_TYPES.includes(row.employmentType)) {
      errors.push(t('employees.import.validation.invalidEmploymentType'));
    }

    // Emails
    if (row.workEmail) {
      if (!EMAIL_RE.test(row.workEmail)) {
        errors.push(t('employees.import.validation.invalidEmail', { field: t('employees.fields.workEmail') }));
      } else if (row.workEmail.length > 254) {
        errors.push(t('employees.import.validation.emailMaxLength', { field: t('employees.fields.workEmail') }));
      }
    }

    if (row.personalEmail) {
      if (!EMAIL_RE.test(row.personalEmail)) {
        errors.push(t('employees.import.validation.invalidEmail', { field: t('employees.fields.personalEmail') }));
      } else if (row.personalEmail.length > 254) {
        errors.push(t('employees.import.validation.emailMaxLength', { field: t('employees.fields.personalEmail') }));
      }
    }

    // Batch duplicate check
    if (row.employeeNumber) {
      const prev = seenNumbers.get(row.employeeNumber.toLowerCase());
      if (prev !== undefined) {
        errors.push(t('employees.import.validation.duplicateInBatch', { row: prev }));
      } else {
        seenNumbers.set(row.employeeNumber.toLowerCase(), row.rowNumber);
      }
    }

    // ── Contract / Payroll fields ──
    if (hasContractData(row)) {
      // GrossAmount is required when contract data is present
      if (!row.grossAmount) {
        errors.push(t('employees.import.validation.grossAmountRequired'));
      } else {
        const amount = parseFloat(row.grossAmount);
        if (isNaN(amount) || amount <= 0) {
          errors.push(t('employees.import.validation.grossAmountPositive'));
        }
      }

      // SalaryType: required when contract data is present
      if (!row.salaryType) {
        errors.push(t('employees.import.validation.salaryTypeRequired'));
      } else if (!SALARY_TYPES.includes(row.salaryType)) {
        errors.push(t('employees.import.validation.invalidSalaryType'));
      }

      // ContractType: optional, defaults to Permanent on backend
      if (row.contractType && !CONTRACT_TYPES.includes(row.contractType)) {
        errors.push(t('employees.import.validation.invalidContractType'));
      }

      // WeeklyHours
      if (row.weeklyHours) {
        const hours = parseFloat(row.weeklyHours);
        if (isNaN(hours) || hours <= 0 || hours > 60) {
          errors.push(t('employees.import.validation.weeklyHoursRange'));
        }
      }

      // WorkdaysPerWeek
      if (row.workdaysPerWeek) {
        const days = parseInt(row.workdaysPerWeek, 10);
        if (isNaN(days) || days < 1 || days > 7) {
          errors.push(t('employees.import.validation.workdaysPerWeekRange'));
        }
      }

      // ContractStartDate
      if (row.contractStartDate && !isValidDate(row.contractStartDate)) {
        errors.push(t('employees.import.validation.invalidDate', {
          field: t('employees.import.contractFields.contractStartDate'),
        }));
      }

      // ContractEndDate
      if (row.contractEndDate && !isValidDate(row.contractEndDate)) {
        errors.push(t('employees.import.validation.invalidDate', {
          field: t('employees.import.contractFields.contractEndDate'),
        }));
      }

      // FixedTerm requires EndDate
      if (row.contractType === 'FixedTerm' && !row.contractEndDate) {
        errors.push(t('employees.import.validation.fixedTermRequiresEndDate'));
      }

      // AnnualVacationDays
      if (row.annualVacationDays) {
        const days = parseInt(row.annualVacationDays, 10);
        if (isNaN(days) || days < 0 || days > 365) {
          errors.push(t('employees.import.validation.vacationDaysRange'));
        }
      }

      // Yes/No fields
      if (row.has13thSalary && !YES_NO.includes(row.has13thSalary)) {
        errors.push(t('employees.import.validation.invalidYesNo', {
          field: t('employees.import.contractFields.has13thSalary'),
        }));
      }
      if (row.hasVacationBonus && !YES_NO.includes(row.hasVacationBonus)) {
        errors.push(t('employees.import.validation.invalidYesNo', {
          field: t('employees.import.contractFields.hasVacationBonus'),
        }));
      }
    } else {
      // No contract data — warn that payroll won't work
      warnings.push(t('employees.import.validation.noContractDataWarning'));
    }

    results.push({ rowNumber: row.rowNumber, errors, warnings });
  }

  return results;
}
