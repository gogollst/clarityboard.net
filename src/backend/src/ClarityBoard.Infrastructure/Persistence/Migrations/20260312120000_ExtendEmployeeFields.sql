-- Migration: ExtendEmployeeFields
-- Date: 2026-03-12
-- Description: Add Gender, Nationality, Position, EmploymentType, contact fields, and CostCenterId to employees

ALTER TABLE hr.employees
  ADD COLUMN IF NOT EXISTS "Gender" varchar(20) NOT NULL DEFAULT 'NotSpecified',
  ADD COLUMN IF NOT EXISTS "Nationality" varchar(100),
  ADD COLUMN IF NOT EXISTS "Position" varchar(200),
  ADD COLUMN IF NOT EXISTS "EmploymentType" varchar(20),
  ADD COLUMN IF NOT EXISTS "WorkEmail" varchar(254),
  ADD COLUMN IF NOT EXISTS "PersonalEmail" varchar(254),
  ADD COLUMN IF NOT EXISTS "PersonalPhone" varchar(50),
  ADD COLUMN IF NOT EXISTS "CostCenterId" uuid;
