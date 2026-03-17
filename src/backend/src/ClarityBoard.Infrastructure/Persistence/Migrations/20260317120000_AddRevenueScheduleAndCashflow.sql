-- Migration: 20260317120000_AddRevenueScheduleAndCashflow
-- Sprint 3: Ausgangsrechnungen - Erlösverteilung (Revenue Schedule) + Cashflow-Einträge

CREATE TABLE IF NOT EXISTS accounting.revenue_schedule_entries (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "EntityId" UUID NOT NULL REFERENCES entity.legal_entities("Id"),
    "DocumentId" UUID NOT NULL REFERENCES document.documents("Id"),
    "BookingSuggestionId" UUID REFERENCES document.booking_suggestions("Id"),
    "LineItemIndex" INT,
    "PeriodDate" DATE NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "RevenueAccountNumber" VARCHAR(10) NOT NULL,
    "RevenueAccountId" UUID REFERENCES accounting.accounts("Id"),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'planned',
    "JournalEntryId" UUID REFERENCES accounting.journal_entries("Id"),
    "PostedAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_revenue_schedule_entity_status
    ON accounting.revenue_schedule_entries("EntityId", "Status");

CREATE INDEX IF NOT EXISTS idx_revenue_schedule_period
    ON accounting.revenue_schedule_entries("PeriodDate", "Status");

CREATE INDEX IF NOT EXISTS idx_revenue_schedule_document
    ON accounting.revenue_schedule_entries("DocumentId");

CREATE TABLE IF NOT EXISTS accounting.cashflow_entries (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "EntityId" UUID NOT NULL REFERENCES entity.legal_entities("Id"),
    "DocumentId" UUID NOT NULL REFERENCES document.documents("Id"),
    "ExpectedDate" DATE NOT NULL,
    "ActualDate" DATE,
    "GrossAmount" DECIMAL(18,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'EUR',
    "Direction" VARCHAR(10) NOT NULL DEFAULT 'inflow',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'open',
    "JournalEntryId" UUID REFERENCES accounting.journal_entries("Id"),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cashflow_entries_entity_status
    ON accounting.cashflow_entries("EntityId", "Status");

CREATE INDEX IF NOT EXISTS idx_cashflow_entries_expected
    ON accounting.cashflow_entries("ExpectedDate", "Status");

-- Migration History
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260317120000_AddRevenueScheduleAndCashflow', '10.0.0')
ON CONFLICT DO NOTHING;
