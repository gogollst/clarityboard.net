import * as XLSX from 'xlsx';
import type { EmployeeImportRow } from '@/types/hr';

const FIELD_MAP: Record<string, keyof Omit<EmployeeImportRow, 'rowNumber'>> = {
  EmployeeNumber: 'employeeNumber',
  EmployeeType: 'employeeType',
  FirstName: 'firstName',
  LastName: 'lastName',
  DateOfBirth: 'dateOfBirth',
  TaxId: 'taxId',
  HireDate: 'hireDate',
  Gender: 'gender',
  Nationality: 'nationality',
  Position: 'position',
  EmploymentType: 'employmentType',
  WorkEmail: 'workEmail',
  PersonalEmail: 'personalEmail',
  PersonalPhone: 'personalPhone',
  SocialSecurityNumber: 'socialSecurityNumber',
  IBAN: 'iban',
  BIC: 'bic',
  // Contract / Payroll fields
  ContractType: 'contractType',
  SalaryType: 'salaryType',
  GrossAmount: 'grossAmount',
  WeeklyHours: 'weeklyHours',
  WorkdaysPerWeek: 'workdaysPerWeek',
  ContractStartDate: 'contractStartDate',
  ContractEndDate: 'contractEndDate',
  AnnualVacationDays: 'annualVacationDays',
  Has13thSalary: 'has13thSalary',
  HasVacationBonus: 'hasVacationBonus',
};

const DATE_FIELDS: ReadonlySet<string> = new Set([
  'dateOfBirth', 'hireDate', 'contractStartDate', 'contractEndDate',
]);

function excelDateToISO(value: unknown): string {
  if (typeof value === 'number') {
    // Excel serial date number
    const parsed = XLSX.SSF.parse_date_code(value);
    if (parsed) {
      const y = String(parsed.y).padStart(4, '0');
      const m = String(parsed.m).padStart(2, '0');
      const d = String(parsed.d).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }
  }
  return String(value ?? '').trim();
}

export function parseExcel(file: File): Promise<EmployeeImportRow[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target?.result as ArrayBuffer);
        const wb = XLSX.read(data, { type: 'array' });

        const ws = wb.Sheets['Data'] ?? wb.Sheets[wb.SheetNames[0]];
        if (!ws) {
          reject(new Error('No worksheet found'));
          return;
        }

        const raw: unknown[][] = XLSX.utils.sheet_to_json(ws, {
          header: 1,
          raw: true,
          defval: '',
        });

        if (raw.length < 1) {
          resolve([]);
          return;
        }

        // Row 1 (index 0) = machine keys
        const headerRow = raw[0].map((h) => String(h).trim());

        // Build column index → field key mapping
        const colMap: { col: number; field: keyof Omit<EmployeeImportRow, 'rowNumber'> }[] = [];
        headerRow.forEach((header, col) => {
          const field = FIELD_MAP[header];
          if (field) colMap.push({ col, field });
        });

        const rows: EmployeeImportRow[] = [];

        // Data starts at row 3 (index 2) — row 2 is translated labels
        for (let i = 2; i < raw.length; i++) {
          const rowData = raw[i];
          if (!rowData || rowData.every((c) => String(c ?? '').trim() === '')) continue;

          const row: EmployeeImportRow = {
            rowNumber: i + 1, // 1-based Excel row number
            employeeNumber: '',
            employeeType: '',
            firstName: '',
            lastName: '',
            dateOfBirth: '',
            taxId: '',
            hireDate: '',
          };

          for (const { col, field } of colMap) {
            const cellValue = rowData[col];
            if (DATE_FIELDS.has(field)) {
              (row as unknown as Record<string, string>)[field] = excelDateToISO(cellValue);
            } else {
              (row as unknown as Record<string, string>)[field] = String(cellValue ?? '').trim();
            }
          }

          rows.push(row);
        }

        resolve(rows);
      } catch (err) {
        reject(err);
      }
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsArrayBuffer(file);
  });
}
