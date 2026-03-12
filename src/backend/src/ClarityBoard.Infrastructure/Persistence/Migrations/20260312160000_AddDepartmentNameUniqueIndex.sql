-- Migration: AddDepartmentNameUniqueIndex
-- Date: 2026-03-12
-- Description: Add unique index on (EntityId, Name) for departments table

CREATE UNIQUE INDEX IF NOT EXISTS "IX_departments_EntityId_Name"
    ON hr.departments ("EntityId", "Name");
