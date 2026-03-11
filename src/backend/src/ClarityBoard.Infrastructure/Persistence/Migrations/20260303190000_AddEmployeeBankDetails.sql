-- Migration: 20260303190000_AddEmployeeBankDetails
-- Adds IBAN and BIC columns to hr.employees
-- Apply on server: psql -U <user> -d clarityboard -f 20260303190000_AddEmployeeBankDetails.sql

BEGIN;

ALTER TABLE hr.employees
    ADD COLUMN IF NOT EXISTS "Iban" character varying(34),
    ADD COLUMN IF NOT EXISTS "Bic"  character varying(11);

COMMIT;
