-- Add translated name columns to accounts table
ALTER TABLE accounting.accounts
  ADD COLUMN IF NOT EXISTS "NameDe" varchar(200),
  ADD COLUMN IF NOT EXISTS "NameEn" varchar(200),
  ADD COLUMN IF NOT EXISTS "NameRu" varchar(200);

-- Backfill: copy existing Name to NameDe (assuming German as default)
UPDATE accounting.accounts SET "NameDe" = "Name" WHERE "NameDe" IS NULL;
