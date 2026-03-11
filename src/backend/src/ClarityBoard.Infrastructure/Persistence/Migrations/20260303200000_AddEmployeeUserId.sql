-- Migration: 20260303200000_AddEmployeeUserId
-- Links hr.employees to public.users so employees can look up their own profile.
-- Apply on server: psql -U <user> -d clarityboard -f 20260303200000_AddEmployeeUserId.sql

BEGIN;

ALTER TABLE hr.employees
    ADD COLUMN IF NOT EXISTS "UserId" uuid REFERENCES public.users("Id") ON DELETE SET NULL;

CREATE UNIQUE INDEX IF NOT EXISTS idx_employees_user_id
    ON hr.employees ("UserId")
    WHERE "UserId" IS NOT NULL;

COMMIT;
