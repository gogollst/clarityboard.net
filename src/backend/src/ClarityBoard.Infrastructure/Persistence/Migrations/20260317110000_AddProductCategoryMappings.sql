-- Migration: 20260317110000_AddProductCategoryMappings
-- Sprint 2: Ausgangsrechnungen - Produktkategorie-Mappings für Erlöskontenselektion

CREATE TABLE IF NOT EXISTS accounting.product_category_mappings (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "EntityId" UUID NOT NULL REFERENCES entity.legal_entities("Id"),
    "ProductNamePattern" VARCHAR(500) NOT NULL,
    "ProductCategory" VARCHAR(50) NOT NULL,
    "RevenueAccountId" UUID REFERENCES accounting.accounts("Id"),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_product_category_mappings_entity
    ON accounting.product_category_mappings("EntityId", "IsActive");

-- Migration History
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260317110000_AddProductCategoryMappings', '10.0.0')
ON CONFLICT DO NOTHING;
