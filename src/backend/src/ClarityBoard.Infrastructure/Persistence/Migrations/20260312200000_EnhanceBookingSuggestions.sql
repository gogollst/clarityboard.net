ALTER TABLE document.booking_suggestions
    ADD COLUMN IF NOT EXISTS "HrEmployeeId" UUID REFERENCES hr.employees("Id") ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS "ModifiedBy" UUID REFERENCES public.users("Id"),
    ADD COLUMN IF NOT EXISTS "ModifiedAt" TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS "RejectedBy" UUID REFERENCES public.users("Id"),
    ADD COLUMN IF NOT EXISTS "RejectedAt" TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS "RejectionReason" VARCHAR(500),
    ADD COLUMN IF NOT EXISTS "IsAutoBooked" BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE document.recurring_patterns
    ADD COLUMN IF NOT EXISTS "HrEmployeeId" UUID REFERENCES hr.employees("Id") ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS "AutoBookThreshold" INTEGER NOT NULL DEFAULT 3,
    ADD COLUMN IF NOT EXISTS "AutoBookEnabled" BOOLEAN NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS "IX_recurring_patterns_AutoBook"
    ON document.recurring_patterns("EntityId", "AutoBookEnabled")
    WHERE "IsActive" = true AND "AutoBookEnabled" = true;

UPDATE document.recurring_patterns
SET "AutoBookEnabled" = true
WHERE "MatchCount" >= 3 AND "IsActive" = true;
