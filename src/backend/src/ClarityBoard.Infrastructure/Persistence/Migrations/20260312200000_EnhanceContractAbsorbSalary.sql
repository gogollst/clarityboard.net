-- Step 1: Neue Spalten auf hr.contracts
ALTER TABLE hr.contracts
  ADD COLUMN "EmploymentType"           varchar(20),
  ADD COLUMN "SalaryType"               varchar(20) NOT NULL DEFAULT 'Monthly',
  ADD COLUMN "GrossAmountCents"         integer NOT NULL DEFAULT 0,
  ADD COLUMN "CurrencyCode"             varchar(3) NOT NULL DEFAULT 'EUR',
  ADD COLUMN "BonusAmountCents"         integer NOT NULL DEFAULT 0,
  ADD COLUMN "BonusCurrencyCode"        varchar(3) NOT NULL DEFAULT 'EUR',
  ADD COLUMN "PaymentCycleMonths"       integer NOT NULL DEFAULT 12,
  ADD COLUMN "EmployerNoticeWeeks"      integer NOT NULL DEFAULT 4,
  ADD COLUMN "AnnualVacationDays"       integer NOT NULL DEFAULT 20,
  ADD COLUMN "FixedTermReason"          varchar(500),
  ADD COLUMN "FixedTermExtensionCount"  integer NOT NULL DEFAULT 0,
  ADD COLUMN "Has13thSalary"            boolean NOT NULL DEFAULT false,
  ADD COLUMN "HasVacationBonus"         boolean NOT NULL DEFAULT false,
  ADD COLUMN "VariablePayCents"         integer NOT NULL DEFAULT 0,
  ADD COLUMN "VariablePayDescription"   varchar(500),
  ADD COLUMN "Notes"                    varchar(2000);

-- Step 2: Rename NoticeWeeks → EmployeeNoticeWeeks
ALTER TABLE hr.contracts RENAME COLUMN "NoticeWeeks" TO "EmployeeNoticeWeeks";

-- Step 3: Contract FK auf employee_documents
ALTER TABLE hr.employee_documents
  ADD COLUMN "ContractId" uuid REFERENCES hr.contracts("Id") ON DELETE SET NULL;

-- Step 4: Gehaltsdaten aus salary_history in Verträge migrieren
UPDATE hr.contracts c
SET
  "SalaryType"         = COALESCE(s."SalaryType", 'Monthly'),
  "GrossAmountCents"   = COALESCE(s."GrossAmountCents", 0),
  "CurrencyCode"       = COALESCE(s."CurrencyCode", 'EUR'),
  "BonusAmountCents"   = COALESCE(s."BonusAmountCents", 0),
  "BonusCurrencyCode"  = COALESCE(s."BonusCurrencyCode", 'EUR'),
  "PaymentCycleMonths" = COALESCE(s."PaymentCycleMonths", 12)
FROM (
  SELECT DISTINCT ON (sh."EmployeeId", c2."Id")
    c2."Id" AS contract_id,
    sh."SalaryType"::varchar(20),
    sh."GrossAmountCents",
    sh."CurrencyCode",
    sh."BonusAmountCents",
    sh."BonusCurrencyCode",
    sh."PaymentCycleMonths"
  FROM hr.salary_history sh
  JOIN hr.contracts c2 ON sh."EmployeeId" = c2."EmployeeId"
    AND sh."ValidFrom" <= c2."ValidFrom"
  ORDER BY sh."EmployeeId", c2."Id", sh."ValidFrom" DESC
) s
WHERE s.contract_id = c."Id";

-- Step 5: Indexes
CREATE INDEX "IX_contracts_EmployeeId_Active"
  ON hr.contracts ("EmployeeId") WHERE "ValidTo" IS NULL;

CREATE INDEX "IX_employee_documents_ContractId"
  ON hr.employee_documents ("ContractId") WHERE "ContractId" IS NOT NULL;

-- Permissions
GRANT ALL ON hr.contracts TO app;
GRANT ALL ON hr.employee_documents TO app;
