-- Add SuggestedEntityId column to BookingSuggestions for AI-detected target entity
ALTER TABLE document.booking_suggestions
    ADD COLUMN IF NOT EXISTS "SuggestedEntityId" uuid NULL;

-- Add FK constraint
ALTER TABLE document.booking_suggestions
    ADD CONSTRAINT "FK_booking_suggestions_legal_entities_SuggestedEntityId"
    FOREIGN KEY ("SuggestedEntityId") REFERENCES entity.legal_entities ("Id")
    ON DELETE SET NULL;
