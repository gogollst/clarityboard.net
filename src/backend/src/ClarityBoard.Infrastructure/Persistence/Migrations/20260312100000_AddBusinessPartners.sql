-- Business Partners table
CREATE TABLE IF NOT EXISTS accounting.business_partners (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL REFERENCES public.entities(id),
    partner_number VARCHAR(20) NOT NULL,
    name VARCHAR(200) NOT NULL,
    tax_id VARCHAR(50),
    vat_number VARCHAR(50),
    street VARCHAR(200),
    city VARCHAR(100),
    postal_code VARCHAR(20),
    country VARCHAR(2),
    email VARCHAR(200),
    phone VARCHAR(50),
    bank_name VARCHAR(200),
    iban VARCHAR(34),
    bic VARCHAR(11),
    is_creditor BOOLEAN NOT NULL DEFAULT false,
    is_debtor BOOLEAN NOT NULL DEFAULT false,
    default_expense_account_id UUID REFERENCES accounting.accounts(id),
    default_revenue_account_id UUID REFERENCES accounting.accounts(id),
    contact_employee_id UUID REFERENCES hr.employees(id) ON DELETE SET NULL,
    payment_term_days INTEGER NOT NULL DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT true,
    notes VARCHAR(2000),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    UNIQUE(entity_id, partner_number)
);

CREATE INDEX IF NOT EXISTS ix_business_partners_entity ON accounting.business_partners(entity_id);
CREATE INDEX IF NOT EXISTS ix_business_partners_name ON accounting.business_partners(entity_id, name);
CREATE INDEX IF NOT EXISTS ix_business_partners_tax_id ON accounting.business_partners(entity_id, tax_id) WHERE tax_id IS NOT NULL;

-- Add business_partner_id and suggested_business_partner_id FK to documents
ALTER TABLE document.documents ADD COLUMN IF NOT EXISTS business_partner_id UUID REFERENCES accounting.business_partners(id);
ALTER TABLE document.documents ADD COLUMN IF NOT EXISTS suggested_business_partner_id UUID REFERENCES accounting.business_partners(id);
CREATE INDEX IF NOT EXISTS ix_documents_business_partner ON document.documents(business_partner_id) WHERE business_partner_id IS NOT NULL;

-- Add business_partner_id FK to recurring_patterns
ALTER TABLE document.recurring_patterns ADD COLUMN IF NOT EXISTS business_partner_id UUID REFERENCES accounting.business_partners(id);

-- Add business_partner_id FK to journal_entries
ALTER TABLE accounting.journal_entries ADD COLUMN IF NOT EXISTS business_partner_id UUID REFERENCES accounting.business_partners(id);
