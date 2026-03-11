# Employee Import Feature — Concept

## 1. Overview

HR admins can bulk-import employees from an Excel (.xlsx) file. The feature covers:

- Template download endpoint (pre-built XLSX with headers, dropdowns, example row)
- Import endpoint (multipart upload, strict or partial mode, ≤ 1 000 rows)
- Per-row validation with structured error reporting
- Automatic user-account creation (temp password, BCrypt-hashed, returned once in response)
- SalaryHistory entry creation for rows that include salary data
- One AuditLog entry per import operation
- 4-step frontend wizard (Download → Upload → Import → Results)

---

## 2. Excel Library

**ClosedXML** (`ClosedXML` NuGet, MIT license).

- Add to `ClarityBoard.Infrastructure/ClarityBoard.Infrastructure.csproj`
- EPPlus ruled out (requires commercial license for commercial use)
- DocumentFormat.OpenXml ruled out (low-level, verbose)

---

## 3. Excel Template

### 3.1 Columns

| # | Header           | Required | Type    | Notes |
|---|------------------|----------|---------|-------|
| 1 | FirstName        | ✓        | string  | max 100 |
| 2 | LastName         | ✓        | string  | max 100 |
| 3 | Email            | ✓        | string  | Creates user account; must be unique globally |
| 4 | EmployeeNumber   | ✓        | string  | Unique per entity |
| 5 | EmploymentType   | ✓        | enum    | `Employee` \| `Contractor` (dropdown) |
| 6 | DateOfBirth      | ✓        | date    | Format: YYYY-MM-DD |
| 7 | TaxId            | ✓        | string  | |
| 8 | HireDate         | ✓        | date    | Format: YYYY-MM-DD |
| 9 | Department       |          | string  | Matched by Name or Code; empty = no department |
| 10| ManagerEmail     |          | string  | Email of existing employee's linked user account |
| 11| SalaryAmount     |          | decimal | > 0 when provided |
| 12| SalaryCurrency   |          | string  | ISO 4217 (default: EUR when SalaryAmount given) |
| 13| SalaryPayCycle   |          | enum    | `Monthly` \| `Annual` (dropdown); Annual → ÷12 → stored as Monthly |
| 14| IBAN             |          | string  | max 34 chars |
| 15| BIC              |          | string  | max 11 chars |

### 3.2 Template Styling

- Row 1: bold headers, light-blue background (`#DDEEFF`)
- Row 2: example data row, grey italic text (to be deleted by user before upload)
- Hidden sheet `_Enums` holds the two dropdown source lists
- Data validation (in-cell dropdown) on columns E (EmploymentType) and M (SalaryPayCycle)
- Column widths auto-sized after population

### 3.3 Enum Mapping Decisions

**EmploymentType** — The domain `EmployeeType` enum has `Employee | Contractor`.
The template dropdown shows exactly these values. (Prior spec drafts mentioned FullTime/PartTime;
those are not modelled in the domain and would require a migration + downstream changes,
so they are excluded from this feature.)

**SalaryPayCycle** — The domain `SalaryType` enum has `Monthly | Hourly | DailyRate`.
`Annual` does not exist in the domain. Import handler behavior:
- `Monthly` → `SalaryType.Monthly`, amount used as-is
- `Annual`  → `SalaryType.Monthly`, amount ÷ 12 (rounded to nearest cent),
  `ChangeReason = "Imported (Annual salary converted to monthly)"`

---

## 4. Backend Architecture

### 4.1 New Files

```
ClarityBoard.Application/Features/Hr/Commands/ImportEmployees/
├── ImportEmployeesCommand.cs         # IRequest<ImportResultDto> + handler
├── ImportEmployeeRowDto.cs           # Parsed row data (internal)
├── ImportEmployeeResultDto.cs        # Public result DTOs
└── ImportEmployeesValidator.cs       # FluentValidation on the command

ClarityBoard.Application/Common/Interfaces/
└── IEmployeeImportTemplateService.cs # Returns Stream of the template XLSX

ClarityBoard.Infrastructure/Services/
└── EmployeeImportTemplateService.cs  # ClosedXML implementation
```

`HrController.cs` receives two new endpoints (no new controller file needed).

### 4.2 New Endpoints

```
GET  /api/hr/employees/import/template
     Permission : hr.manage
     Response   : application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
                  Content-Disposition: attachment; filename="employee_import_template.xlsx"

POST /api/hr/employees/import
     Permission : hr.manage
     Content-Type: multipart/form-data
     Fields     : file (.xlsx), mode ("strict" | "partial")
     Response 200: ImportResultDto
     Response 422: row count > 1 000
```

### 4.3 ImportEmployeesCommand

```csharp
[RequirePermission("hr.manage")]
public record ImportEmployeesCommand : IRequest<ImportResultDto>
{
    public required Stream  FileStream { get; init; }
    public required string  FileName   { get; init; }
    public required string  Mode       { get; init; } // "strict" | "partial"
    public required Guid    EntityId   { get; init; }
}
```

### 4.4 Handler Algorithm

```
1.  Open workbook (ClosedXML), read all data rows → List<ParsedRow>
2.  If count > 1 000 → throw ValidationException (HTTP 422)
3.  Bulk-load lookups (single queries each):
      • Departments by EntityId   → dict[Name.ToLower()] + dict[Code.ToLower()]
      • CostCenters by EntityId   → dict[Code.ToLower()]
      • Existing user emails      → HashSet<string> (global, for duplicate check)
      • Existing EmployeeNumbers  → HashSet<string> (scoped to EntityId)
      • Employees with UserId     → dict[userId] for manager resolution
4.  Validate all rows → collect List<RowError> per row index
      (Unknown FK = row error, not exception; see §4.6 for all rules)
5.  Begin IDbContextTransaction
6.  For each valid row:
      a. Generate temp password (16-char random, see §4.5)
      b. User.Create(email, BCrypt.HashPassword(tempPassword), firstName, lastName)
         → UserStatus.Active
      c. Assign Role "Employee" to new user for EntityId
         (role already seeded in DefaultRoles; lookup by name)
      d. Employee.Create(entityId, employeeNumber, employeeType, firstName, lastName,
                         dateOfBirth, taxId, hireDate, managerId?, departmentId?)
         → employee.UserId = newUser.Id
      e. Auto-create CostCenter with code = "E" + employeeNumber
         (mirrors existing CreateEmployeeCommand behavior)
      f. If SalaryAmount > 0:
           → Create SalaryHistory entry:
             SalaryType = Monthly, GrossAmountCents = amount×100 (Annual÷12×100),
             CurrencyCode, ValidFrom = HireDate, ChangeReason (see §3.3)
      g. db.Employees.Add(employee); db.Users.Add(user); etc.
      h. Track { employeeId, tempPassword (plain text) } in successRows
7.  Strict mode : if ANY row has validation errors → rollback → return all errors, successCount=0
    Partial mode: insert valid rows, skip failed rows, commit
8.  Commit transaction
9.  Write one AuditLog entry (see §4.7)
10. Return ImportResultDto (plain-text temp passwords included for successful rows)
```

### 4.5 Temporary Password Generation

```csharp
private static string GenerateTempPassword()
{
    const string chars =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%^&*";
    Span<byte> buf = stackalloc byte[16];
    RandomNumberGenerator.Fill(buf);
    return new string(buf.ToArray().Select(b => chars[b % chars.Length]).ToArray());
}
```

- Plain-text password appears in the result DTO and **nowhere else**
- Never logged, never stored — only the BCrypt hash is persisted
- Minimum effective entropy: ≈95 bits

### 4.6 Per-Row Validation Rules

| Rule | Fields | Error message |
|------|--------|---------------|
| Required | FirstName, LastName, Email, EmployeeNumber, EmploymentType, DateOfBirth, TaxId, HireDate | `"{Field} is required"` |
| Valid email format | Email | `"Invalid email format"` |
| Unique within file | Email | `"Duplicate email in import file"` |
| Not already in DB | Email | `"Email already registered"` |
| Unique within file | EmployeeNumber | `"Duplicate employee number in import file"` |
| Not already in DB | EmployeeNumber | `"Employee number '{value}' already exists"` |
| Valid enum | EmploymentType | `"Must be 'Employee' or 'Contractor'"` |
| Valid enum (if provided) | SalaryPayCycle | `"Must be 'Monthly' or 'Annual'"` |
| Valid date | DateOfBirth, HireDate | `"Invalid date format — use YYYY-MM-DD"` |
| Amount > 0 (if provided) | SalaryAmount | `"Salary amount must be greater than 0"` |
| Valid ISO 4217 (if provided) | SalaryCurrency | `"Invalid currency code"` |
| FK resolves | Department | `"Department '{value}' not found"` |
| FK resolves | ManagerEmail | `"No employee found for manager email '{value}'"` |
| max 34 chars | IBAN | `"IBAN must not exceed 34 characters"` |
| max 11 chars | BIC | `"BIC must not exceed 11 characters"` |

### 4.7 Audit Log Entry

```csharp
await _auditService.LogAsync(
    entityId  : command.EntityId,
    action    : "EmployeeImport",
    tableName : "employees",
    recordId  : null,
    oldValues : null,
    newValues : JsonSerializer.Serialize(new {
        totalRows    = result.TotalRows,
        successCount = result.SuccessCount,
        failedCount  = result.FailedCount,
        mode         = command.Mode,
        fileName     = command.FileName
    }),
    userId    : _currentUser.UserId,
    ipAddress : null,
    userAgent : null,
    ct);
```

### 4.8 Result DTOs

```csharp
public record ImportResultDto
{
    public int TotalRows    { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount  { get; init; }
    public required List<ImportRowResultDto> Rows { get; init; }
}

public record ImportRowResultDto
{
    public int     RowNumber          { get; init; }
    public string  Status             { get; init; } // "success" | "failed"
    public Guid?   EmployeeId         { get; init; }
    public string? EmployeeNumber     { get; init; }
    public string? FullName           { get; init; }
    public string? Email              { get; init; }
    public string? TemporaryPassword  { get; init; } // plain text, success rows only
    public List<ImportRowErrorDto> Errors { get; init; } = [];
}

public record ImportRowErrorDto
{
    public string Field   { get; init; } = "";
    public string Message { get; init; } = "";
}
```

### 4.9 Salary Storage Decision

The existing `SalaryHistory` table stores `GrossAmountCents` as a plain `integer`.
Encrypting this column would require a migration changing the column type to `text` and
updates to all salary queries, display components, and salary-band statistics.

**Decision:** Use the existing unencrypted SalaryHistory structure for salary import rows,
consistent with the rest of the salary subsystem. Salary data is protected at the API layer
by the `hr.salary.view` permission requirement and by row-level EntityId isolation. Full
at-rest encryption of the salary subsystem is deferred to a future holistic change.

---

## 5. Frontend Architecture

### 5.1 New Files

```
src/features/hr/employees/
└── EmployeeImport.tsx       # 4-step wizard, lazy-loaded page

src/hooks/
└── useEmployeeImport.ts     # useDownloadImportTemplate + useImportEmployees
```

### 5.2 Types (additions to `src/types/hr.ts`)

```typescript
export type ImportMode = 'strict' | 'partial';

export interface ImportRowError {
  field:   string;
  message: string;
}

export interface ImportRowResult {
  rowNumber:          number;
  status:             'success' | 'failed';
  employeeId?:        string;
  employeeNumber?:    string;
  fullName?:          string;
  email?:             string;
  temporaryPassword?: string;  // only on success rows
  errors:             ImportRowError[];
}

export interface ImportResult {
  totalRows:    number;
  successCount: number;
  failedCount:  number;
  rows:         ImportRowResult[];
}
```

### 5.3 `useEmployeeImport.ts` Hook

Follows the same `useMutation` + `sonner` toast pattern as `useHr.ts`.

```typescript
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import api from '@/lib/api';
import type { ImportResult } from '@/types/hr';

export function useDownloadImportTemplate() {
  const { t } = useTranslation('hr');
  return useMutation({
    mutationFn: async () => {
      const res = await api.get('/hr/employees/import/template', { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a   = document.createElement('a');
      a.href     = url;
      a.download = 'employee_import_template.xlsx';
      a.click();
      URL.revokeObjectURL(url);
    },
    onError: () => toast.error(t('import.downloadError')),
  });
}

export function useImportEmployees() {
  const { t } = useTranslation('hr');
  return useMutation({
    mutationFn: async ({ file, mode }: { file: File; mode: 'strict' | 'partial' }) => {
      const form = new FormData();
      form.append('file', file);
      form.append('mode', mode);
      const res = await api.post<ImportResult>('/hr/employees/import', form);
      return res.data;
    },
    onError: () => toast.error(t('import.importError')),
  });
}
```

### 5.4 `EmployeeImport.tsx` — Wizard

Local `useState` drives a 4-step wizard (no external step library needed).

**Step 1 — Download Template**
- Heading + short explanation of what each column means
- "Download Template (.xlsx)" button → `useDownloadImportTemplate().mutate()`
- "Next →" button always enabled (downloading is optional if the admin already has the template)

**Step 2 — Upload & Options**
- `<input type="file" accept=".xlsx">` (no drag-drop; keeps implementation simple)
- Shows selected filename and formatted file size after selection
- Import mode radio group with descriptions:
  - `strict` — "Roll back everything if any row fails"
  - `partial` — "Import valid rows, skip failed rows"
- Soft warning shown if file size exceeds ~5 MB: "Large file — import may take a few seconds"
- Row-limit error (> 1 000 rows) returned as HTTP 422 and shown inline
- "Import" button → `useImportEmployees().mutate({ file, mode })`; disabled while loading

**Step 3 — Loading** (shown during mutation)
- Spinner + "Importing employees…" message
- Automatically transitions to Step 4 on mutation settlement

**Step 4 — Results**
- Summary: "{successCount} imported · {failedCount} failed"
- **Success table** (green-tinted header):
  - Columns: Row #, Full Name, Email, Temporary Password
  - Temporary Password: monospace font, copy-to-clipboard button (`navigator.clipboard.writeText`)
- **Failed table** (red-tinted header, only rendered when failedCount > 0):
  - Columns: Row #, Field, Error Message
- "Import Another File" button → resets wizard to Step 1 (clears file + result state)
- "Download Errors as CSV" button (client-side generation) — only shown when failedCount > 0

### 5.5 Router Integration

In `src/app/router.tsx`, add inside the HR children array **before** the `:id` catch-all route:

```typescript
{ path: 'employees/import', lazy: () => import('@/features/hr/employees/EmployeeImport') },
```

Follows the existing `lazy()` pattern used for all feature pages.

### 5.6 Employee List Integration

In `src/features/hr/employees/EmployeeList.tsx`, add an "Import" button in the page header
next to the existing "Add Employee" button:

```tsx
<Button variant="outline" onClick={() => navigate('/hr/employees/import')}>
  <Upload className="mr-2 h-4 w-4" />
  {t('hr:import.importButton')}
</Button>
```

### 5.7 i18n Keys

Add to `en/hr.json`, `de/hr.json`, and `ru/hr.json` under the `"import"` key:

```json
"import": {
  "title":            "Import Employees",
  "step1Title":       "Download Template",
  "step2Title":       "Upload & Options",
  "step3Title":       "Importing…",
  "step4Title":       "Results",
  "templateDesc":     "Download the Excel template, fill in one employee per row, then upload it here.",
  "downloadTemplate": "Download Template (.xlsx)",
  "selectFile":       "Select .xlsx file",
  "selectedFile":     "{{filename}} ({{size}})",
  "modeLabel":        "Import mode",
  "modeStrict":       "Strict — roll back all rows on any error",
  "modePartial":      "Partial — import valid rows, skip failed",
  "importButton":     "Import",
  "importing":        "Importing employees…",
  "summarySuccess":   "{{count}} employees imported successfully",
  "summaryFailed":    "{{count}} rows failed",
  "colPassword":      "Temporary Password",
  "copyPassword":     "Copy",
  "importAnother":    "Import Another File",
  "downloadErrors":   "Download Errors as CSV",
  "rowLimitExceeded": "File exceeds the 1,000-row limit",
  "fileTooLarge":     "Large file — import may take a few seconds",
  "downloadError":    "Failed to download template",
  "importError":      "Import failed — please check your file and try again"
}
```

---

## 6. No Migration Required

The import reuses all existing tables:

| Table | Usage |
|-------|-------|
| `hr.employees` | Created for each successful row |
| `hr.salary_histories` | Created if salary columns are provided |
| `public.users` | New user account per row |
| `public.user_roles` | Employee role assignment (scoped to EntityId) |
| `accounting.cost_centers` | Auto-created with code `E{EmployeeNumber}` |
| `public.audit_logs` | One entry written per import run |

---

## 7. Security & Tenant Isolation

- Both endpoints gated by `[RequirePermission("hr.manage")]`
- All `Employee` records created with `EntityId = _currentUser.EntityId`
- All `UserRole` records scoped to `_currentUser.EntityId`
- Department and CostCenter lookups filtered by EntityId
- Manager resolution restricted to employees within the same EntityId
- Temporary passwords: plain text in the response DTO only; BCrypt hash persisted in DB; never written to logs or audit trail content

---

## 8. Critical Files

| File | Change |
|------|--------|
| `src/backend/src/ClarityBoard.Infrastructure/ClarityBoard.Infrastructure.csproj` | Add `ClosedXML` NuGet |
| `src/backend/src/ClarityBoard.Application/Common/Interfaces/IEmployeeImportTemplateService.cs` | New interface |
| `src/backend/src/ClarityBoard.Application/Features/Hr/Commands/ImportEmployees/` | New folder, 4 files |
| `src/backend/src/ClarityBoard.Infrastructure/Services/EmployeeImportTemplateService.cs` | New ClosedXML implementation |
| `src/backend/src/ClarityBoard.Infrastructure/DependencyInjection.cs` | Register `IEmployeeImportTemplateService` |
| `src/backend/src/ClarityBoard.API/Controllers/HrController.cs` | 2 new endpoints |
| `src/frontend/src/app/router.tsx` | Add `employees/import` lazy route |
| `src/frontend/src/features/hr/employees/EmployeeImport.tsx` | New wizard page |
| `src/frontend/src/features/hr/employees/EmployeeList.tsx` | Add Import button |
| `src/frontend/src/hooks/useEmployeeImport.ts` | New hook file |
| `src/frontend/src/types/hr.ts` | Add 4 new types |
| `src/frontend/src/locales/en/hr.json` | Add `import` keys |
| `src/frontend/src/locales/de/hr.json` | Add `import` keys |
| `src/frontend/src/locales/ru/hr.json` | Add `import` keys |

---

## 9. Verification Checklist

1. `GET /api/hr/employees/import/template` returns an `.xlsx` binary with 15 columns,
   in-cell dropdown validation on EmploymentType and SalaryPayCycle, and an example row
2. `POST /api/hr/employees/import` with a valid 3-row file in `partial` mode
   → `successCount = 3`, each success row contains a non-empty `temporaryPassword`
3. Same endpoint, `strict` mode, one invalid row mixed in → full rollback, `successCount = 0`
4. File with > 1 000 data rows → HTTP 422
5. File with a duplicate email on two rows → row-level error on the second occurrence
6. File referencing a non-existent department → row error `"Department 'X' not found"`
7. Audit log entry written with `action = "EmployeeImport"` and correct metadata JSON
8. Imported user can authenticate with the returned temporary password
9. `/hr/employees/import` route renders the wizard; "Import" button visible in employee list
10. Step 4 displays temporary passwords in monospace font; copy-to-clipboard button works
