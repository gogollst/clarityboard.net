-- Migration: 20260303180000_AddHrModule
-- Creates the HR module schema and all associated tables

BEGIN;

-- ============================================================
-- 1. Create schema
-- ============================================================
CREATE SCHEMA IF NOT EXISTS hr;

-- ============================================================
-- 2. hr.departments (no ManagerId FK yet — employees not created)
-- ============================================================
CREATE TABLE hr.departments (
    "Id"                uuid                        NOT NULL,
    "EntityId"          uuid                        NOT NULL,
    "Name"              character varying(200)      NOT NULL,
    "Code"              character varying(50)       NOT NULL,
    "ParentDepartmentId" uuid,
    "ManagerId"         uuid,
    "IsActive"          boolean                     NOT NULL DEFAULT true,
    "CreatedAt"         timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_departments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_departments_ParentDepartment" FOREIGN KEY ("ParentDepartmentId")
        REFERENCES hr.departments ("Id") ON DELETE SET NULL
);

-- ============================================================
-- 3. hr.employees
-- ============================================================
CREATE TABLE hr.employees (
    "Id"                     uuid                        NOT NULL,
    "EntityId"               uuid                        NOT NULL,
    "EmployeeNumber"         character varying(20)       NOT NULL,
    "EmployeeType"           character varying(20)       NOT NULL,
    "FirstName"              character varying(100)      NOT NULL,
    "LastName"               character varying(100)      NOT NULL,
    "DateOfBirth"            date                        NOT NULL,
    "TaxId"                  character varying(50)       NOT NULL,
    "SocialSecurityNumber"   character varying(200),
    "Status"                 character varying(20)       NOT NULL,
    "HireDate"               date                        NOT NULL,
    "TerminationDate"        date,
    "TerminationReason"      character varying(500),
    "ManagerId"              uuid,
    "DepartmentId"           uuid,
    "CreatedAt"              timestamp with time zone    NOT NULL,
    "UpdatedAt"              timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_employees" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_employees_EntityId_EmployeeNumber" UNIQUE ("EntityId", "EmployeeNumber"),
    CONSTRAINT "FK_employees_Manager" FOREIGN KEY ("ManagerId")
        REFERENCES hr.employees ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_employees_Department" FOREIGN KEY ("DepartmentId")
        REFERENCES hr.departments ("Id") ON DELETE SET NULL
);

-- ============================================================
-- 4. Add ManagerId FK on departments now that employees exists
-- ============================================================
ALTER TABLE hr.departments
    ADD CONSTRAINT "FK_departments_Manager" FOREIGN KEY ("ManagerId")
        REFERENCES hr.employees ("Id") ON DELETE SET NULL;

-- ============================================================
-- 5. hr.employee_address_history
-- ============================================================
CREATE TABLE hr.employee_address_history (
    "Id"            uuid                        NOT NULL,
    "EmployeeId"    uuid                        NOT NULL,
    "AddressType"   character varying(20)       NOT NULL,
    "Street"        character varying(200)      NOT NULL,
    "HouseNumber"   character varying(20)       NOT NULL,
    "PostalCode"    character varying(20)       NOT NULL,
    "City"          character varying(100)      NOT NULL,
    "CountryCode"   character varying(2)        NOT NULL,
    "ValidFrom"     timestamp with time zone    NOT NULL,
    "ValidTo"       timestamp with time zone,
    "CreatedBy"     uuid                        NOT NULL,
    "ChangeReason"  character varying(500)      NOT NULL,
    CONSTRAINT "PK_employee_address_history" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_employee_address_history_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 6. hr.employee_contact_history
-- ============================================================
CREATE TABLE hr.employee_contact_history (
    "Id"                    uuid                        NOT NULL,
    "EmployeeId"            uuid                        NOT NULL,
    "Email"                 character varying(320)      NOT NULL,
    "PhoneWork"             character varying(50),
    "PhoneMobile"           character varying(50),
    "EmergencyContactName"  character varying(200),
    "EmergencyContactPhone" character varying(50),
    "ValidFrom"             timestamp with time zone    NOT NULL,
    "ValidTo"               timestamp with time zone,
    "CreatedBy"             uuid                        NOT NULL,
    "ChangeReason"          character varying(500)      NOT NULL,
    CONSTRAINT "PK_employee_contact_history" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_employee_contact_history_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 7. hr.contracts
-- ============================================================
CREATE TABLE hr.contracts (
    "Id"                uuid                        NOT NULL,
    "EmployeeId"        uuid                        NOT NULL,
    "ContractType"      character varying(20)       NOT NULL,
    "WeeklyHours"       numeric(5,2)                NOT NULL,
    "WorkdaysPerWeek"   integer                     NOT NULL,
    "StartDate"         date                        NOT NULL,
    "EndDate"           date,
    "ProbationEndDate"  date,
    "NoticeWeeks"       integer                     NOT NULL DEFAULT 4,
    "ValidFrom"         timestamp with time zone    NOT NULL,
    "ValidTo"           timestamp with time zone,
    "CreatedBy"         uuid                        NOT NULL,
    "ChangeReason"      character varying(500)      NOT NULL,
    CONSTRAINT "PK_contracts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_contracts_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 8. hr.salary_history
-- ============================================================
CREATE TABLE hr.salary_history (
    "Id"                  uuid                        NOT NULL,
    "EmployeeId"          uuid                        NOT NULL,
    "SalaryType"          character varying(20)       NOT NULL,
    "GrossAmountCents"    integer                     NOT NULL,
    "CurrencyCode"        character varying(3)        NOT NULL DEFAULT 'EUR',
    "BonusAmountCents"    integer                     NOT NULL DEFAULT 0,
    "BonusCurrencyCode"   character varying(3)        NOT NULL DEFAULT 'EUR',
    "PaymentCycleMonths"  integer                     NOT NULL DEFAULT 12,
    "ValidFrom"           timestamp with time zone    NOT NULL,
    "ValidTo"             timestamp with time zone,
    "CreatedBy"           uuid                        NOT NULL,
    "ChangeReason"        character varying(500)      NOT NULL,
    CONSTRAINT "PK_salary_history" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_salary_history_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 9. hr.leave_types
-- ============================================================
CREATE TABLE hr.leave_types (
    "Id"                    uuid                        NOT NULL,
    "EntityId"              uuid                        NOT NULL,
    "Name"                  character varying(100)      NOT NULL,
    "Code"                  character varying(20)       NOT NULL,
    "RequiresApproval"      boolean                     NOT NULL DEFAULT true,
    "IsDeductedFromBalance" boolean                     NOT NULL DEFAULT true,
    "MaxDaysPerYear"        integer,
    "Color"                 character varying(7)        NOT NULL DEFAULT '#3b82f6',
    "IsActive"              boolean                     NOT NULL DEFAULT true,
    CONSTRAINT "PK_leave_types" PRIMARY KEY ("Id")
);

-- ============================================================
-- 10. hr.leave_balances
-- ============================================================
CREATE TABLE hr.leave_balances (
    "Id"              uuid                        NOT NULL,
    "EmployeeId"      uuid                        NOT NULL,
    "LeaveTypeId"     uuid                        NOT NULL,
    "Year"            integer                     NOT NULL,
    "EntitlementDays" numeric(5,2)                NOT NULL DEFAULT 0,
    "UsedDays"        numeric(5,2)                NOT NULL DEFAULT 0,
    "PendingDays"     numeric(5,2)                NOT NULL DEFAULT 0,
    "CarryOverDays"   numeric(5,2)                NOT NULL DEFAULT 0,
    "UpdatedAt"       timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_leave_balances" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_leave_balances_Employee_LeaveType_Year" UNIQUE ("EmployeeId", "LeaveTypeId", "Year"),
    CONSTRAINT "FK_leave_balances_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_leave_balances_LeaveType" FOREIGN KEY ("LeaveTypeId")
        REFERENCES hr.leave_types ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 11. hr.leave_requests
-- ============================================================
CREATE TABLE hr.leave_requests (
    "Id"              uuid                        NOT NULL,
    "EmployeeId"      uuid                        NOT NULL,
    "LeaveTypeId"     uuid                        NOT NULL,
    "StartDate"       date                        NOT NULL,
    "EndDate"         date                        NOT NULL,
    "WorkingDays"     numeric(5,2)                NOT NULL,
    "HalfDay"         boolean                     NOT NULL DEFAULT false,
    "Status"          character varying(20)       NOT NULL DEFAULT 'pending',
    "RequestedAt"     timestamp with time zone    NOT NULL,
    "ApprovedBy"      uuid,
    "ApprovedAt"      timestamp with time zone,
    "RejectionReason" character varying(500),
    "Notes"           character varying(1000),
    CONSTRAINT "PK_leave_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_leave_requests_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_leave_requests_LeaveType" FOREIGN KEY ("LeaveTypeId")
        REFERENCES hr.leave_types ("Id")
);

-- ============================================================
-- 12. hr.work_time_entries
-- ============================================================
CREATE TABLE hr.work_time_entries (
    "Id"            uuid                        NOT NULL,
    "EmployeeId"    uuid                        NOT NULL,
    "Date"          date                        NOT NULL,
    "StartTime"     time without time zone,
    "EndTime"       time without time zone,
    "BreakMinutes"  integer                     NOT NULL DEFAULT 0,
    "TotalMinutes"  integer                     NOT NULL,
    "EntryType"     character varying(20)       NOT NULL DEFAULT 'work',
    "ProjectCode"   character varying(50),
    "Notes"         character varying(500),
    "Status"        character varying(20)       NOT NULL DEFAULT 'open',
    "CreatedAt"     timestamp with time zone    NOT NULL,
    "UpdatedBy"     uuid                        NOT NULL,
    CONSTRAINT "PK_work_time_entries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_work_time_entries_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 13. hr.performance_reviews
-- ============================================================
CREATE TABLE hr.performance_reviews (
    "Id"                uuid                        NOT NULL,
    "EmployeeId"        uuid                        NOT NULL,
    "ReviewerId"        uuid                        NOT NULL,
    "ReviewPeriodStart" date                        NOT NULL,
    "ReviewPeriodEnd"   date                        NOT NULL,
    "ReviewType"        character varying(20)       NOT NULL,
    "Status"            character varying(20)       NOT NULL DEFAULT 'draft',
    "OverallRating"     integer,
    "StrengthsNotes"    character varying(2000),
    "ImprovementNotes"  character varying(2000),
    "GoalsNotes"        character varying(2000),
    "CompletedAt"       timestamp with time zone,
    "CreatedAt"         timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_performance_reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_performance_reviews_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 14. hr.feedback_entries
-- ============================================================
CREATE TABLE hr.feedback_entries (
    "Id"               uuid                        NOT NULL,
    "ReviewId"         uuid                        NOT NULL,
    "RespondentId"     uuid                        NOT NULL,
    "RespondentType"   character varying(20)       NOT NULL,
    "IsAnonymous"      boolean                     NOT NULL DEFAULT false,
    "Rating"           integer                     NOT NULL,
    "Comments"         character varying(2000),
    "CompetencyScores" jsonb,
    "SubmittedAt"      timestamp with time zone,
    "CreatedAt"        timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_feedback_entries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_feedback_entries_Review" FOREIGN KEY ("ReviewId")
        REFERENCES hr.performance_reviews ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 15. hr.travel_expense_reports
-- ============================================================
CREATE TABLE hr.travel_expense_reports (
    "Id"              uuid                        NOT NULL,
    "EmployeeId"      uuid                        NOT NULL,
    "Title"           character varying(200)      NOT NULL,
    "TripStartDate"   date                        NOT NULL,
    "TripEndDate"     date                        NOT NULL,
    "Destination"     character varying(200)      NOT NULL,
    "BusinessPurpose" character varying(500)      NOT NULL,
    "Status"          character varying(20)       NOT NULL DEFAULT 'draft',
    "TotalAmountCents" integer                    NOT NULL DEFAULT 0,
    "CurrencyCode"    character varying(3)        NOT NULL DEFAULT 'EUR',
    "ApprovedBy"      uuid,
    "ApprovedAt"      timestamp with time zone,
    "ReimbursedAt"    timestamp with time zone,
    "CreatedAt"       timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_travel_expense_reports" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_travel_expense_reports_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 16. hr.travel_expense_items
-- ============================================================
CREATE TABLE hr.travel_expense_items (
    "Id"                     uuid                        NOT NULL,
    "ReportId"               uuid                        NOT NULL,
    "ExpenseType"            character varying(50)       NOT NULL,
    "ExpenseDate"            date                        NOT NULL,
    "Description"            character varying(500)      NOT NULL,
    "OriginalAmountCents"    integer                     NOT NULL,
    "OriginalCurrencyCode"   character varying(3)        NOT NULL,
    "ExchangeRate"           numeric(18,6)               NOT NULL DEFAULT 1,
    "ExchangeRateDate"       date                        NOT NULL,
    "AmountCents"            integer                     NOT NULL,
    "CurrencyCode"           character varying(3)        NOT NULL DEFAULT 'EUR',
    "ReceiptDocumentId"      uuid,
    "VatRatePercent"         numeric(5,2),
    "IsDeductible"           boolean                     NOT NULL DEFAULT true,
    CONSTRAINT "PK_travel_expense_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_travel_expense_items_Report" FOREIGN KEY ("ReportId")
        REFERENCES hr.travel_expense_reports ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 17. hr.employee_documents
-- ============================================================
CREATE TABLE hr.employee_documents (
    "Id"                    uuid                        NOT NULL,
    "EmployeeId"            uuid                        NOT NULL,
    "DocumentType"          character varying(50)       NOT NULL,
    "Title"                 character varying(200)      NOT NULL,
    "FileName"              character varying(500)      NOT NULL,
    "StoragePath"           character varying(1000)     NOT NULL,
    "MimeType"              character varying(100)      NOT NULL,
    "FileSizeBytes"         bigint                      NOT NULL,
    "UploadedBy"            uuid                        NOT NULL,
    "UploadedAt"            timestamp with time zone    NOT NULL,
    "ExpiresAt"             timestamp with time zone,
    "IsConfidential"        boolean                     NOT NULL DEFAULT false,
    "DeletionScheduledAt"   timestamp with time zone,
    CONSTRAINT "PK_employee_documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_employee_documents_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 18. hr.data_access_logs
-- ============================================================
CREATE TABLE hr.data_access_logs (
    "Id"                  uuid                        NOT NULL,
    "AccessedEmployeeId"  uuid                        NOT NULL,
    "AccessedBy"          uuid                        NOT NULL,
    "AccessType"          character varying(50)       NOT NULL,
    "ResourceType"        character varying(50)       NOT NULL,
    "ResourceId"          uuid,
    "IpAddress"           character varying(45),
    "UserAgent"           character varying(500),
    "AccessedAt"          timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_data_access_logs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_data_access_logs_Employee" FOREIGN KEY ("AccessedEmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE CASCADE
);

-- ============================================================
-- 19. hr.deletion_requests
-- ============================================================
CREATE TABLE hr.deletion_requests (
    "Id"                  uuid                        NOT NULL,
    "EmployeeId"          uuid                        NOT NULL,
    "RequestedBy"         uuid                        NOT NULL,
    "RequestedAt"         timestamp with time zone    NOT NULL,
    "ScheduledDeletionAt" timestamp with time zone    NOT NULL,
    "Status"              character varying(20)       NOT NULL DEFAULT 'pending',
    "BlockReason"         character varying(500),
    "CompletedAt"         timestamp with time zone,
    CONSTRAINT "PK_deletion_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_deletion_requests_Employee" FOREIGN KEY ("EmployeeId")
        REFERENCES hr.employees ("Id") ON DELETE RESTRICT
);

-- ============================================================
-- 20. hr.public_holidays
-- ============================================================
CREATE TABLE hr.public_holidays (
    "Id"          uuid                    NOT NULL,
    "EntityId"    uuid                    NOT NULL,
    "Date"        date                    NOT NULL,
    "Name"        character varying(200)  NOT NULL,
    "CountryCode" character varying(2)    NOT NULL DEFAULT 'DE',
    CONSTRAINT "PK_public_holidays" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_public_holidays_Entity_Date" UNIQUE ("EntityId", "Date")
);

-- ============================================================
-- 21. Indexes for common query patterns
-- ============================================================

-- employees
CREATE INDEX "IX_employees_EntityId"      ON hr.employees ("EntityId");
CREATE INDEX "IX_employees_Status"        ON hr.employees ("Status");
CREATE INDEX "IX_employees_DepartmentId"  ON hr.employees ("DepartmentId");
CREATE INDEX "IX_employees_ManagerId"     ON hr.employees ("ManagerId");

-- departments
CREATE INDEX "IX_departments_EntityId"   ON hr.departments ("EntityId");

-- leave_requests
CREATE INDEX "IX_leave_requests_EmployeeId"  ON hr.leave_requests ("EmployeeId");
CREATE INDEX "IX_leave_requests_Status"      ON hr.leave_requests ("Status");
CREATE INDEX "IX_leave_requests_StartDate"   ON hr.leave_requests ("StartDate");

-- work_time_entries
CREATE INDEX "IX_work_time_entries_EmployeeId_Date" ON hr.work_time_entries ("EmployeeId", "Date");

-- data_access_logs
CREATE INDEX "IX_data_access_logs_AccessedAt"        ON hr.data_access_logs ("AccessedAt");
CREATE INDEX "IX_data_access_logs_AccessedEmployeeId" ON hr.data_access_logs ("AccessedEmployeeId");

-- employee_documents
CREATE INDEX "IX_employee_documents_EmployeeId" ON hr.employee_documents ("EmployeeId");

-- performance_reviews
CREATE INDEX "IX_performance_reviews_EmployeeId" ON hr.performance_reviews ("EmployeeId");

-- salary_history
CREATE INDEX "IX_salary_history_EmployeeId" ON hr.salary_history ("EmployeeId");

-- ============================================================
-- 22. GRANT permissions to app user
-- ============================================================
GRANT USAGE ON SCHEMA hr TO app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA hr TO app;
ALTER DEFAULT PRIVILEGES IN SCHEMA hr GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app;

-- ============================================================
-- 23. EF Migrations History entry
-- ============================================================
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260303180000_AddHrModule', '10.0.3')
ON CONFLICT DO NOTHING;

-- ============================================================
-- Quality Fixes (applied 2026-03-03)
-- ============================================================

-- Fix 1: deletion_requests ON DELETE RESTRICT → CASCADE
-- RESTRICT blockiert DSGVO-Löschungen (Deadlock). CASCADE erlaubt
-- das Löschen eines Mitarbeiters inkl. seiner Löschanfragen.
ALTER TABLE hr.deletion_requests
  DROP CONSTRAINT IF EXISTS "FK_deletion_requests_Employee",
  ADD CONSTRAINT "FK_deletion_requests_Employee"
    FOREIGN KEY ("EmployeeId") REFERENCES hr.employees ("Id") ON DELETE CASCADE;

-- Fix 2: Fehlende Composite-Indexes auf History-Tabellen
-- ValidTo = NULL = aktueller Datensatz — häufigste Query-Pattern.
CREATE INDEX IF NOT EXISTS "IX_employee_address_history_EmployeeId_ValidTo"
  ON hr.employee_address_history ("EmployeeId", "ValidTo");
CREATE INDEX IF NOT EXISTS "IX_employee_contact_history_EmployeeId_ValidTo"
  ON hr.employee_contact_history ("EmployeeId", "ValidTo");
CREATE INDEX IF NOT EXISTS "IX_contracts_EmployeeId_ValidTo"
  ON hr.contracts ("EmployeeId", "ValidTo");
CREATE INDEX IF NOT EXISTS "IX_salary_history_EmployeeId_ValidTo"
  ON hr.salary_history ("EmployeeId", "ValidTo");

-- Fix 3: CHECK Constraint für work_time_entries
-- Verhindert negative TotalMinutes-Werte auf DB-Ebene.
ALTER TABLE hr.work_time_entries
  ADD CONSTRAINT "CHK_work_time_entries_TotalMinutes"
    CHECK ("TotalMinutes" >= 0);

-- Fix 4: Explizites ON DELETE RESTRICT auf leave_requests → leave_types
-- Macht die implizite Standard-Semantik explizit (kein Löschen eines
-- LeaveType solange noch Anträge dagegen existieren).
ALTER TABLE hr.leave_requests
  DROP CONSTRAINT IF EXISTS "FK_leave_requests_LeaveType",
  ADD CONSTRAINT "FK_leave_requests_LeaveType"
    FOREIGN KEY ("LeaveTypeId") REFERENCES hr.leave_types ("Id") ON DELETE RESTRICT;

COMMIT;
