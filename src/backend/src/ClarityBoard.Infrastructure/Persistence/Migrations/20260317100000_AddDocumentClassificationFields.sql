-- Migration: 20260317100000_AddDocumentClassificationFields
-- Sprint 1: Ausgangsrechnungen - Klassifikationsfelder
-- Adds IBAN/BIC to legal_entities, document_direction to documents and recurring_patterns

-- 1. IBAN/BIC auf LegalEntity (für Klassifikator-Aussteller-Match)
ALTER TABLE entity.legal_entities
    ADD COLUMN IF NOT EXISTS "Iban" VARCHAR(34),
    ADD COLUMN IF NOT EXISTS "Bic" VARCHAR(11);

-- 2. Klassifikationsfelder auf Documents
ALTER TABLE document.documents
    ADD COLUMN IF NOT EXISTS document_direction VARCHAR(20) NOT NULL DEFAULT 'incoming',
    ADD COLUMN IF NOT EXISTS classification_confidence DECIMAL(3,2),
    ADD COLUMN IF NOT EXISTS due_date DATE,
    ADD COLUMN IF NOT EXISTS order_number VARCHAR(255),
    ADD COLUMN IF NOT EXISTS reverse_charge BOOLEAN NOT NULL DEFAULT FALSE;

-- 3. Richtungsfeld auf RecurringPatterns (für richtungsabhängiges Pattern-Matching)
ALTER TABLE document.recurring_patterns
    ADD COLUMN IF NOT EXISTS document_direction VARCHAR(20) NOT NULL DEFAULT 'incoming';

-- 4. Index für Richtungsfilter auf Documents
CREATE INDEX IF NOT EXISTS idx_documents_entity_direction
    ON document.documents("EntityId", document_direction);

-- 5. Migration History
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260317100000_AddDocumentClassificationFields', '10.0.0')
ON CONFLICT DO NOTHING;
