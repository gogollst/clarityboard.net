-- Migration: 20260303210000_AddOnboarding
-- Adds onboarding checklists and tasks to the hr schema
-- Apply on server: psql -U <user> -d clarityboard -f 20260303210000_AddOnboarding.sql

BEGIN;

CREATE TABLE IF NOT EXISTS hr.onboarding_checklists (
    "Id"          uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "EmployeeId"  uuid         NOT NULL REFERENCES hr.employees("Id") ON DELETE CASCADE,
    "Title"       varchar(200) NOT NULL,
    "Status"      varchar(20)  NOT NULL DEFAULT 'InProgress',
    "CreatedBy"   uuid         NOT NULL,
    "CreatedAt"   timestamptz  NOT NULL DEFAULT now(),
    "CompletedAt" timestamptz
);

CREATE INDEX IF NOT EXISTS idx_onboarding_checklists_employee
    ON hr.onboarding_checklists ("EmployeeId");

CREATE TABLE IF NOT EXISTS hr.onboarding_tasks (
    "Id"          uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "ChecklistId" uuid         NOT NULL REFERENCES hr.onboarding_checklists("Id") ON DELETE CASCADE,
    "Title"       varchar(200) NOT NULL,
    "Description" varchar(1000),
    "IsCompleted" boolean      NOT NULL DEFAULT false,
    "CompletedAt" timestamptz,
    "CompletedBy" uuid,
    "DueDate"     date,
    "SortOrder"   integer      NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_onboarding_tasks_checklist
    ON hr.onboarding_tasks ("ChecklistId");

GRANT SELECT, INSERT, UPDATE, DELETE ON hr.onboarding_checklists TO app;
GRANT SELECT, INSERT, UPDATE, DELETE ON hr.onboarding_tasks TO app;

COMMIT;
