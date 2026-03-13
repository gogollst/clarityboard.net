-- Add classification fields to BookingSuggestions for enriched AI invoice classification
ALTER TABLE document.booking_suggestions
    ADD COLUMN IF NOT EXISTS "InvoiceType" varchar(50) NULL,
    ADD COLUMN IF NOT EXISTS "TaxKey" varchar(10) NULL,
    ADD COLUMN IF NOT EXISTS "VatTreatmentType" varchar(50) NULL;

-- Add index for filtering by invoice type
CREATE INDEX IF NOT EXISTS "IX_booking_suggestions_InvoiceType"
    ON document.booking_suggestions ("InvoiceType")
    WHERE "InvoiceType" IS NOT NULL;
