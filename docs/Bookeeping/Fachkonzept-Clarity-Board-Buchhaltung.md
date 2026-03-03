# Fach- und Datenkonzept: Clarity Board – Buchhaltungsmodul

**Version:** 1.0
**Datum:** 03.03.2026
**Status:** Final Draft
**Autor:** Clarity Board Architecture Team
**Zielgruppe:** CTO, Lead-Architekten, Steuerberater

---

## 1. Executive Summary

Clarity Board wird die komplette Finanzbuchhaltung der Aqua Cloud GmbH abbilden – von der Belegerfassung bis zum fertigen DATEV-Export. Das System basiert auf PostgreSQL, verwendet den SKR 04 als Kontenrahmen und unterstützt drei Sprachen (DE/EN/RU) auf allen kontenbezogenen Ebenen.

**Kernfakten aus der Analyse:**

- Aqua Cloud operiert auf SKR 04 (Bilanzrichtlinie-Umsetzungsgesetz, gültig 2026)
- Jahresumsatz 2025: ca. 2,19 Mio. EUR, Jahresverlust: ca. -374 TEUR
- 57 aktive Sachkonten, 16 Kostenstellen (davon 8 Abteilungs-KSt, 8 Mitarbeiter-KSt)
- 2.250 Buchungszeilen im Jahr 2025
- Dominante Kostenblöcke: Fremdleistungen Dev (Konto 5900: 526 TEUR), Personal (606 TEUR), Lizenzen (80 TEUR), Beratung (141 TEUR)
- Erlösstruktur: SaaS/Miete (734 TEUR), Kauf (372 TEUR), M&S (701 TEUR), §13b (94 TEUR), Drittland (189 TEUR)
- Konzernstruktur: Clearing-Konten zu Holding, andagon people, a4b vorhanden
- Bankkonten: SSK, Commerzbank, Deutsche Bank, moss (Kreditkarten), UniCredit
- Kreditkarten: 2 Mastercards (S.Gogoll, Y.Gogoll)

**Designziele:**

1. 100% DATEV-Export ohne manuelle Nachbearbeitung
2. Echte doppelte Buchführung mit GoBD-konformer Unveränderbarkeit
3. Parallele Ist-Buchungen, Plan-Daten und beliebige Szenarien in einem Datenmodell
4. Dreisprachiger Kontenrahmen (DE/EN/RU), Währung immer EUR
5. Direkte Cash-Flow-, KPI- und Forecast-Berechnung auf dem Datenmodell

---

## 2. Analyse der drei Dokumente

### 2.1 DATEV-Kontenrahmen SKR 04 (Art.-Nr. 11175, gültig 2026)

Der SKR 04 folgt dem Abschlussgliederungsprinzip der Bilanzrichtlinie:

| Klasse | Bereich | Kontenbereich | Aqua-Cloud-Relevanz |
|--------|---------|---------------|---------------------|
| 0 | Anlagevermögen | 0001–0999 | Immaterielle VG (148), Büroeinrichtung (650), BGA (690) |
| 1 | Umlaufvermögen | 1000–1999 | Forderungen (1200), Vorsteuer (1401/1406/1407), Bank (1800–1851), ARAP (1900) |
| 2 | Eigenkapital | 2000–2999 | Gezeichnetes Kapital (2900), Kapitalrücklage (2920), Verlustvortrag (2978) |
| 3 | Fremdkapital | 3000–3999 | Rückstellungen (3071/3079/3095/3096), Verb. L+L (3300), Clearing (3420–3424), Kreditkarten (3610/3611), Lohnverbindlichkeiten (3720–3790), USt (3806/3837/3865), PRAP (3900) |
| 4 | Erträge | 4000–4999 | Erlöse §13b (4337), Drittland (4338), aqua SaaS (4402), Kauf (4403), Sonstige (4406), M&S (4409), Fremdlizenzen (4410), Sachbezüge (4946/4947) |
| 5 | Materialaufwand | 5000–5999 | Fremdleistungen (5900/5901), EU-Leistungen (5923) |
| 6 | Personalaufwand & sonstige | 6000–6999 | Gehälter (6020), Sozialversicherung (6110), AfA (6201/6220), Versicherungen (6400), Lizenzen (6425), Fahrzeug (6530), Marketing (6601/6603), Fremdarbeit Vertrieb/Marketing (6780–6782), Hosting (6811), Beratung (6825/6826), Buchführung (6830), Bankgebühren (6855) |
| 7 | Finanzergebnis | 7000–7999 | Zinserträge (7100) |

**Wichtige Erkenntnisse:**

- Aqua Cloud nutzt individuelle Konten (I-Kennzeichen im Kontenplan): 4402, 4403, 4406, 4409, 4410, 5901, 5907, 6421, 6423, 6425 u.v.m.
- Der SKR 04 definiert für jedes Konto: S/H-Kennzeichen, Zusatzfunktionen (USt-Automatik), HF-Typ, Funktionsschlüssel, Anlagenspiegelfunktion, Kontenzweck
- DATEV-Automatikkonten (AV-Kennzeichen) steuern die Vorsteuer-/USt-Ermittlung automatisch
- Die Programmverbindungen (Bilanzposten/GuV-Posten) sind fest definiert und müssen im Export korrekt zugeordnet werden

### 2.2 Buchungsdaten Aqua Cloud 2025 (Stand 13.02.2026)

**Sheet "Buchungen" (2.250 Zeilen):**

Struktur pro Zeile: Konto/BWA | Datum | Buchungstext | Belegfeld1 | Ausgaben | Einnahmen | Kostenstellen | Leistungsdatum | Kostenstelle

Kernbefunde:
- Nur Aufwandsseite erfasst (Konten 5900–7100), keine Erlösbuchungen im Sheet – diese kommen offenbar aus einem anderen System
- Konto 5900 (Fremdleistungen) dominiert mit 178 Buchungen – vorwiegend monatliche Freelancer-Abrechnungen für das Dev-Team
- Konto 6425 (Lizenzgebühren) hat 677 Buchungen – sehr granulare SaaS-Lizenzbuchungen
- Konto 6855 (Bankgebühren) mit 272 Buchungen zeigt hohe Transaktionsfrequenz
- Gehaltsbuchungen (6020) sind pro Mitarbeiter und Monat aufgeschlüsselt (108 Buchungen, 8 Mitarbeiter × ca. 12 Monate + Sonderbuchungen)
- Leistungsdatum wird konsequent gepflegt – wichtig für periodengerechte Abgrenzung
- Belegfeld1 enthält strukturierte Referenzen (Rechnungsnummern, Datumsformate)

**Sheet "BWA":**

Vollständige BWA nach DATEV-Standard mit Zeilen 1020–1380:
- Gesamtleistung 2025: 2.187.895 EUR
- Materialaufwand: 612.971 EUR (28% der Gesamtleistung)
- Rohertrag: 1.574.925 EUR (72%)
- Personalkosten: 606.429 EUR (28%)
- Gesamtkosten: 1.941.547 EUR
- Betriebsergebnis: -350.394 EUR
- Vorläufiges Ergebnis: -374.091 EUR

**Sheet "Kostenstellen" (16 Einträge):**

| Code | Typ | Bezeichnung |
|------|-----|-------------|
| 200 | Abteilung | Gemeinkosten aqua cloud |
| 201 | Abteilung | Indirekte Kosten |
| 202 | Abteilung | Recruiting |
| 210 | Abteilung | Development |
| 220 | Abteilung | Product |
| 230 | Abteilung | Sales |
| 235 | Abteilung | Marketing |
| 240 | Abteilung | Ranorex |
| 2001–2018 | Mitarbeiter | Koch, Elsner, Weingartz, Buzik, McDade, Hartmann, Ali Lara |

### 2.3 Kontenplan Aqua Cloud (119 Konten)

Der individuelle Kontenplan enthält 119 Zeilen mit folgenden Spalten: Konto von, Konto bis, S/K/I-Kennzeichen, Beschriftung, Zusatzfunktion, HFTyp, Funktion, FE, Faktor 2, Anlagenspiegelfunktion, Kontenzweck.

Kritische Details:
- S = Standardkonto, I = Individualkonto, K = Kapitalgesellschafts-spezifisch
- 5 Bankkonten aktiv (SSK, Commerzbank, Deutsche Bank, moss, UniCredit)
- 2 Kreditkartenkonten (3610, 3611)
- Intercompany über Clearing-Konten (3420 Holding, 3422 people, 3424 a4b)
- Erlöskonten nach Produktlinie differenziert (4402 SaaS, 4403 Kauf, 4406 Sonstige, 4409 M&S, 4410 Fremdlizenzen)
- Aufwandskonten nach Vertriebskanal differenziert (6780 Vertrieb, 6781 Marketing, 6782 allgemein)
- USt-Automatik über Zusatzfunktionen gesteuert (z.B. Konto 1406: Funktion 30, FE 190 = 19% Vorsteuer)

---

## 3. Empfohlener Kontenrahmen + Begründung

### Empfehlung: SKR 04 beibehalten

**Begründung:**
1. Aqua Cloud bucht seit Gründung auf SKR 04 – eine Migration wäre aufwändig und fehleranfällig
2. Der Steuerberater arbeitet mit SKR 04 (BWA-Format bestätigt dies)
3. SKR 04 folgt dem Abschlussgliederungsprinzip – die Kontenklassen korrespondieren direkt mit der HGB-Bilanz und GuV
4. Für DATEV-Export ist SKR 04 vollständig dokumentiert
5. Die bestehende Clarity-Board-Dokumentation beschreibt SKR 03 und SKR 04 – beide bleiben als Option, SKR 04 wird Default für Aqua Cloud

### Kontenrahmen-Strategie in Clarity Board

Clarity Board speichert den vollständigen DATEV-Standardkontenrahmen SKR 04 als Basis und erlaubt darauf aufbauend individuelle Konten pro Entity:

- **Standardkonten** (ca. 2.500 im SKR 04): systemverwaltet, nicht löschbar, über DATEV-Updates aktualisierbar
- **Individualkonten** (I-Kennzeichen): entity-spezifisch, frei konfigurierbar innerhalb der Kontenklassen-Regeln
- **Deaktivierte Konten**: nicht mehr bebuchbare Konten bleiben für historische Auswertungen erhalten

### Aqua-Cloud-spezifische Individualkonten

Die folgenden 25+ Individualkonten aus dem Kontenplan werden als Entity-spezifische Konten angelegt:

| Konto | Bezeichnung | Begründung |
|-------|------------|------------|
| 4402 | Erlöse aqua Miete/SaaS | Produktlinie SaaS |
| 4403 | Erlöse 19% USt aqua Kauf | Produktlinie Kauflizenzen |
| 4406 | Erlöse aqua sonstige | Sonstige Erlöse |
| 4409 | Erlöse aqua M&S | Maintenance & Support |
| 4410 | Erlöse Fremdlizenzen | Weiterverkauf Fremdlizenzen |
| 5901 | Fremdleistungen AI | AI-spezifische Entwicklungskosten |
| 5907 | Allgemeine Umlage | Konzernumlage |
| 6421 | HR other | HR-Nebenkosten |
| 6423 | Beiträge, Lizenzen (Marketing) | Marketing-Tools |
| 6425 | Lizenzgebühren | Software-Lizenzen allgemein |
| 3420 | Clearing Holding | IC Holding |
| 3422 | Clearing people | IC andagon people |
| 3424 | Clearing a4b | IC a4b |
| 1800 | SSK 1934553841 | Sparkasse |
| 1820 | Commerzbank andagon | Commerzbank |
| 1830 | Deutsche Bank 727777500 | Deutsche Bank |
| 1850 | moss | Kreditkarten-Clearing |
| 1851 | UniCredit | UniCredit |
| 3610 | Mastercard S.Gogoll | Kreditkarte GF |
| 3611 | Mastercard Y.Gogoll | Kreditkarte |

---

## 4. Vollständiges Postgres-Datenmodell

### 4.1 Schema-Übersicht

```
clarityboard (database)
├── core                        # Mandanten, User, Stammdaten
├── accounting                  # Doppelte Buchführung, Kontenrahmen
├── i18n                        # Mehrsprachigkeit
├── document                    # Belegerfassung und -verwaltung
├── planning                    # Plan-Daten, Szenarien, Forecasts
├── cashflow                    # Cash-Flow-Engine
├── kpi                         # KPI-Berechnungen
├── datev                       # DATEV-Export
├── audit                       # GoBD-konformer Audit-Trail
```

### 4.2 Schema: core

```sql
-- ============================================================
-- CORE: Mandanten und Stammdaten
-- ============================================================

CREATE TABLE core.legal_entities (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code                VARCHAR(20) NOT NULL UNIQUE,      -- z.B. 'AQUA'
    name                VARCHAR(200) NOT NULL,
    tax_number          VARCHAR(30),                       -- Steuernummer
    vat_id              VARCHAR(20),                       -- USt-IdNr.
    trade_register      VARCHAR(50),                       -- HRB-Nummer
    chart_of_accounts   VARCHAR(10) NOT NULL DEFAULT 'SKR04', -- SKR03 | SKR04
    fiscal_year_start   SMALLINT NOT NULL DEFAULT 1,       -- Monat Geschäftsjahresbeginn
    base_currency       CHAR(3) NOT NULL DEFAULT 'EUR',
    datev_consultant_nr VARCHAR(10),                       -- DATEV Beraternummer
    datev_client_nr     VARCHAR(10),                       -- DATEV Mandantennummer
    is_active           BOOLEAN NOT NULL DEFAULT true,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE core.fiscal_periods (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    year                SMALLINT NOT NULL,
    month               SMALLINT NOT NULL,                 -- 1-12, 13=Abschluss, 0=Eröffnung
    start_date          DATE NOT NULL,
    end_date            DATE NOT NULL,
    status              VARCHAR(20) NOT NULL DEFAULT 'open',
                        -- open | soft_closed | exported | hard_closed
    closed_at           TIMESTAMPTZ,
    closed_by           UUID,
    export_count        INT NOT NULL DEFAULT 0,
    UNIQUE (entity_id, year, month)
);

CREATE INDEX idx_fiscal_periods_open
    ON core.fiscal_periods (entity_id, year, month)
    WHERE status = 'open';

CREATE TABLE core.cost_centers (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    code                VARCHAR(20) NOT NULL,              -- z.B. '210'
    short_name          VARCHAR(50) NOT NULL,              -- z.B. 'Dev'
    long_name           VARCHAR(200),                      -- z.B. 'Development'
    cc_type             VARCHAR(20) NOT NULL DEFAULT 'department',
                        -- department | employee | project
    parent_id           UUID REFERENCES core.cost_centers(id),
    responsible_user    UUID,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    valid_from          DATE NOT NULL DEFAULT CURRENT_DATE,
    valid_to            DATE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, code)
);

CREATE TABLE core.business_partners (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    partner_type        VARCHAR(20) NOT NULL,              -- customer | vendor | both
    datev_number        INT,                               -- 10000-69999 Debitoren, 70000-99999 Kreditoren
    company_name        VARCHAR(300),
    tax_number          VARCHAR(30),
    vat_id              VARCHAR(20),
    country_code        CHAR(2) NOT NULL DEFAULT 'DE',
    address_street      VARCHAR(200),
    address_city        VARCHAR(100),
    address_zip         VARCHAR(10),
    payment_terms_days  INT DEFAULT 30,
    iban                VARCHAR(34),
    bic                 VARCHAR(11),
    is_active           BOOLEAN NOT NULL DEFAULT true,
    is_intercompany     BOOLEAN NOT NULL DEFAULT false,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, datev_number)
);

CREATE TABLE core.bank_accounts (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    account_id          UUID NOT NULL,                     -- FK zu accounting.accounts
    bank_name           VARCHAR(200) NOT NULL,
    iban                VARCHAR(34),
    bic                 VARCHAR(11),
    account_holder      VARCHAR(200),
    is_main_account     BOOLEAN NOT NULL DEFAULT false,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE core.projects (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    code                VARCHAR(30) NOT NULL,
    name                VARCHAR(200) NOT NULL,
    description         TEXT,
    cost_center_id      UUID REFERENCES core.cost_centers(id),
    budget_amount       NUMERIC(18,2),
    start_date          DATE,
    end_date            DATE,
    status              VARCHAR(20) NOT NULL DEFAULT 'active',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, code)
);
```

### 4.3 Schema: accounting

```sql
-- ============================================================
-- ACCOUNTING: Kontenrahmen und doppelte Buchführung
-- ============================================================

-- Standardkontenrahmen (systemverwaltet)
CREATE TABLE accounting.standard_accounts (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chart_type          VARCHAR(10) NOT NULL,              -- SKR03 | SKR04
    account_number      VARCHAR(10) NOT NULL,
    account_class       SMALLINT NOT NULL,                 -- 0-9
    s_h_indicator       CHAR(1) NOT NULL,                  -- S=Soll, H=Haben
    vat_auto_function   SMALLINT,                          -- DATEV Zusatzfunktion
    hf_type             SMALLINT,                          -- DATEV HF-Typ
    function_code       SMALLINT,                          -- DATEV Funktionsschlüssel
    fe_factor           NUMERIC(5,1),                      -- DATEV FE/Faktor
    balance_sheet_line  VARCHAR(20),                       -- Bilanzposten
    pnl_line            VARCHAR(20),                       -- GuV-Posten
    asset_function      VARCHAR(20),                       -- Anlagenspiegelfunktion
    account_purpose     VARCHAR(10) NOT NULL DEFAULT 'S',  -- S=Standard, B=Besonders, I=Individuell
    valid_from          DATE NOT NULL DEFAULT '2026-01-01',
    valid_to            DATE,
    UNIQUE (chart_type, account_number, valid_from)
);

-- Mehrsprachige Kontenbeschriftungen (Standard)
CREATE TABLE accounting.standard_account_labels (
    account_id          UUID NOT NULL REFERENCES accounting.standard_accounts(id),
    language_code       CHAR(2) NOT NULL,                  -- de, en, ru
    name                VARCHAR(200) NOT NULL,
    long_name           VARCHAR(500),
    PRIMARY KEY (account_id, language_code)
);

-- Entity-spezifischer Kontenplan (vereinigt Standard + Individual)
CREATE TABLE accounting.accounts (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    account_number      VARCHAR(10) NOT NULL,
    standard_account_id UUID REFERENCES accounting.standard_accounts(id),
    account_class       SMALLINT NOT NULL,
    account_type        VARCHAR(20) NOT NULL,
                        -- asset | liability | equity | revenue | expense | statistical
    s_h_indicator       CHAR(1) NOT NULL,
    is_individual       BOOLEAN NOT NULL DEFAULT false,    -- I-Konto
    vat_auto_function   SMALLINT,
    hf_type             SMALLINT,
    function_code       SMALLINT,
    fe_factor           NUMERIC(5,1),
    vat_default_code    VARCHAR(10),                       -- Default BU-Schlüssel
    is_auto_account     BOOLEAN NOT NULL DEFAULT false,    -- DATEV Automatikkonto
    is_cost_center_required BOOLEAN NOT NULL DEFAULT false,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    valid_from          DATE NOT NULL DEFAULT CURRENT_DATE,
    valid_to            DATE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, account_number)
);

CREATE INDEX idx_accounts_entity_class
    ON accounting.accounts (entity_id, account_class)
    WHERE is_active = true;

-- Entity-spezifische Kontenbeschriftungen
CREATE TABLE accounting.account_labels (
    account_id          UUID NOT NULL REFERENCES accounting.accounts(id),
    language_code       CHAR(2) NOT NULL,                  -- de, en, ru
    name                VARCHAR(200) NOT NULL,
    long_name           VARCHAR(500),
    PRIMARY KEY (account_id, language_code)
);

-- ============================================================
-- JOURNAL ENTRIES: GoBD-konforme Buchungssätze
-- ============================================================

CREATE TABLE accounting.journal_entries (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    entry_number        BIGINT NOT NULL,                   -- Lückenlos, pro Entity
    fiscal_period_id    UUID NOT NULL REFERENCES core.fiscal_periods(id),
    entry_date          DATE NOT NULL,                     -- Buchungsdatum
    document_date       DATE,                              -- Belegdatum
    service_date        DATE,                              -- Leistungsdatum
    posting_date        DATE NOT NULL,                     -- Erfassungsdatum
    description         VARCHAR(500) NOT NULL,             -- Buchungstext
    document_ref        VARCHAR(100),                      -- Belegfeld 1
    document_ref2       VARCHAR(100),                      -- Belegfeld 2
    document_id         UUID,                              -- FK -> document.documents
    partner_id          UUID REFERENCES core.business_partners(id),
    source_type         VARCHAR(30) NOT NULL DEFAULT 'manual',
                        -- manual | bank_import | invoice | recurring | ai_suggestion | migration
    source_ref          VARCHAR(200),
    status              VARCHAR(20) NOT NULL DEFAULT 'draft',
                        -- draft | posted | reversed
    is_reversal         BOOLEAN NOT NULL DEFAULT false,
    reversal_of_id      UUID REFERENCES accounting.journal_entries(id),
    is_opening_balance  BOOLEAN NOT NULL DEFAULT false,
    is_closing          BOOLEAN NOT NULL DEFAULT false,
    -- GoBD: Unveränderbarkeit
    hash                VARCHAR(64) NOT NULL,              -- SHA-256
    previous_hash       VARCHAR(64),                       -- Verkettung
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID NOT NULL,
    posted_at           TIMESTAMPTZ,
    posted_by           UUID,
    version             INT NOT NULL DEFAULT 1,
    UNIQUE (entity_id, entry_number)
);

CREATE INDEX idx_je_entity_date ON accounting.journal_entries (entity_id, entry_date DESC);
CREATE INDEX idx_je_entity_period ON accounting.journal_entries (entity_id, fiscal_period_id);
CREATE INDEX idx_je_status ON accounting.journal_entries (entity_id, status) WHERE status = 'draft';
CREATE INDEX idx_je_document ON accounting.journal_entries (document_id) WHERE document_id IS NOT NULL;
CREATE INDEX idx_je_partner ON accounting.journal_entries (partner_id) WHERE partner_id IS NOT NULL;

CREATE TABLE accounting.journal_entry_lines (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id    UUID NOT NULL REFERENCES accounting.journal_entries(id) ON DELETE CASCADE,
    line_number         SMALLINT NOT NULL,
    account_id          UUID NOT NULL REFERENCES accounting.accounts(id),
    contra_account_id   UUID REFERENCES accounting.accounts(id),  -- Gegenkonto (für DATEV)
    debit_amount        NUMERIC(18,2) NOT NULL DEFAULT 0,
    credit_amount       NUMERIC(18,2) NOT NULL DEFAULT 0,
    tax_code            VARCHAR(10),                       -- BU-Schlüssel (DATEV)
    tax_amount          NUMERIC(18,2) DEFAULT 0,
    tax_account_id      UUID REFERENCES accounting.accounts(id),
    cost_center_id      UUID REFERENCES core.cost_centers(id),
    project_id          UUID REFERENCES core.projects(id),
    description         VARCHAR(300),
    -- Fremdwährung
    currency            CHAR(3) NOT NULL DEFAULT 'EUR',
    original_amount     NUMERIC(18,2),                     -- Betrag in Fremdwährung
    exchange_rate       NUMERIC(12,6) DEFAULT 1.000000,
    -- Dimensionen (erweiterbar)
    dimension_1         VARCHAR(50),                       -- z.B. Produktlinie
    dimension_2         VARCHAR(50),                       -- z.B. Region
    tags                JSONB DEFAULT '[]',

    UNIQUE (journal_entry_id, line_number),
    CHECK (debit_amount >= 0 AND credit_amount >= 0),
    CHECK (NOT (debit_amount > 0 AND credit_amount > 0))
);

CREATE INDEX idx_jel_account ON accounting.journal_entry_lines (account_id);
CREATE INDEX idx_jel_cost_center ON accounting.journal_entry_lines (cost_center_id)
    WHERE cost_center_id IS NOT NULL;
CREATE INDEX idx_jel_project ON accounting.journal_entry_lines (project_id)
    WHERE project_id IS NOT NULL;

-- Constraint: Buchungssatz muss ausgeglichen sein
CREATE OR REPLACE FUNCTION accounting.check_journal_balance()
RETURNS TRIGGER AS $$
BEGIN
    IF (SELECT ABS(SUM(debit_amount) - SUM(credit_amount))
        FROM accounting.journal_entry_lines
        WHERE journal_entry_id = NEW.journal_entry_id) > 0.005
    THEN
        RAISE EXCEPTION 'Journal entry is not balanced (debit <> credit)';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================
-- RECURRING ENTRIES: Wiederkehrende Buchungen
-- ============================================================

CREATE TABLE accounting.recurring_entries (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    name                VARCHAR(200) NOT NULL,
    description         TEXT,
    frequency           VARCHAR(20) NOT NULL,              -- monthly | quarterly | annually
    day_of_month        SMALLINT DEFAULT 1,
    template_lines      JSONB NOT NULL,                    -- Array von Buchungszeilen-Templates
    start_date          DATE NOT NULL,
    end_date            DATE,
    next_execution      DATE NOT NULL,
    last_executed       DATE,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    auto_post           BOOLEAN NOT NULL DEFAULT false,    -- Automatisch buchen oder Draft
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ============================================================
-- VAT: USt-Verwaltung
-- ============================================================

CREATE TABLE accounting.vat_codes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    code                VARCHAR(10) NOT NULL,              -- BU-Schlüssel
    datev_bu_key        VARCHAR(4) NOT NULL,               -- DATEV BU-Schlüssel numerisch
    description_key     VARCHAR(100) NOT NULL,
    rate                NUMERIC(5,2) NOT NULL,             -- z.B. 19.00, 7.00, 0.00
    vat_type            VARCHAR(30) NOT NULL,
                        -- output | input | reverse_charge | intra_eu | exempt | zero_rated
    output_account_id   UUID REFERENCES accounting.accounts(id),
    input_account_id    UUID REFERENCES accounting.accounts(id),
    is_active           BOOLEAN NOT NULL DEFAULT true,
    UNIQUE (entity_id, code)
);
```

### 4.4 Schema: document

```sql
-- ============================================================
-- DOCUMENT: Belegerfassung und -verwaltung
-- ============================================================

CREATE TABLE document.documents (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    document_type       VARCHAR(30) NOT NULL,
                        -- incoming_invoice | outgoing_invoice | credit_note |
                        -- bank_statement | credit_card_statement | receipt |
                        -- contract | payroll | tax_notice | other
    document_number     VARCHAR(100),                      -- Belegnummer
    external_ref        VARCHAR(200),                      -- Externe Referenz
    partner_id          UUID REFERENCES core.business_partners(id),
    document_date       DATE,
    due_date            DATE,
    service_period_from DATE,                              -- Leistungszeitraum von
    service_period_to   DATE,                              -- Leistungszeitraum bis
    net_amount          NUMERIC(18,2),
    tax_amount          NUMERIC(18,2),
    gross_amount        NUMERIC(18,2),
    currency            CHAR(3) NOT NULL DEFAULT 'EUR',
    payment_status      VARCHAR(20) DEFAULT 'open',
                        -- open | partial | paid | overdue | cancelled
    payment_reference   VARCHAR(200),                      -- Verwendungszweck
    -- OCR/AI Ergebnisse
    ocr_raw_text        TEXT,
    ai_extraction       JSONB,                             -- Strukturierte AI-Erkennung
    ai_confidence       NUMERIC(5,2),
    -- Datei-Referenz
    file_storage_key    VARCHAR(500),                      -- S3/MinIO Key
    file_name           VARCHAR(300),
    file_mime_type      VARCHAR(100),
    file_size_bytes     BIGINT,
    -- Status
    status              VARCHAR(20) NOT NULL DEFAULT 'uploaded',
                        -- uploaded | processing | extracted | validated |
                        -- booked | archived | rejected
    journal_entry_id    UUID REFERENCES accounting.journal_entries(id),
    -- GoBD
    retention_until     DATE,                              -- Aufbewahrungsfrist
    is_archived         BOOLEAN NOT NULL DEFAULT false,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID NOT NULL,
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_doc_entity_type ON document.documents (entity_id, document_type, document_date DESC);
CREATE INDEX idx_doc_status ON document.documents (entity_id, status) WHERE status NOT IN ('archived', 'rejected');
CREATE INDEX idx_doc_partner ON document.documents (partner_id) WHERE partner_id IS NOT NULL;
CREATE INDEX idx_doc_payment ON document.documents (entity_id, payment_status, due_date)
    WHERE payment_status IN ('open', 'partial', 'overdue');

-- Belegpositionen (Zeilen einer Rechnung)
CREATE TABLE document.document_lines (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id         UUID NOT NULL REFERENCES document.documents(id) ON DELETE CASCADE,
    line_number         SMALLINT NOT NULL,
    description         VARCHAR(500),
    quantity            NUMERIC(12,4),
    unit_price          NUMERIC(18,4),
    net_amount          NUMERIC(18,2) NOT NULL,
    tax_rate            NUMERIC(5,2),
    tax_amount          NUMERIC(18,2),
    account_id          UUID REFERENCES accounting.accounts(id),  -- Vorschlag/Zuordnung
    cost_center_id      UUID REFERENCES core.cost_centers(id),
    project_id          UUID REFERENCES core.projects(id),
    UNIQUE (document_id, line_number)
);

-- Zahlungszuordnungen
CREATE TABLE document.payment_allocations (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id         UUID NOT NULL REFERENCES document.documents(id),
    journal_entry_id    UUID NOT NULL REFERENCES accounting.journal_entries(id),
    allocated_amount    NUMERIC(18,2) NOT NULL,
    allocation_date     DATE NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 4.5 Schema: planning (Szenarien-Engine)

```sql
-- ============================================================
-- PLANNING: Szenarien, Plandaten, Budgets
-- ============================================================

-- Szenario-Definitionen
CREATE TABLE planning.scenarios (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    name                VARCHAR(200) NOT NULL,
    scenario_type       VARCHAR(30) NOT NULL,
                        -- actual | budget | forecast | best_case | worst_case | custom
    parent_scenario_id  UUID REFERENCES planning.scenarios(id),  -- Versionierung
    version             INT NOT NULL DEFAULT 1,
    description         TEXT,
    year                SMALLINT NOT NULL,
    is_locked           BOOLEAN NOT NULL DEFAULT false,
    is_approved         BOOLEAN NOT NULL DEFAULT false,
    approved_by         UUID,
    approved_at         TIMESTAMPTZ,
    parameters          JSONB DEFAULT '{}',                -- Szenario-Parameter
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID NOT NULL,
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, name, version)
);

-- Plan-Buchungszeilen (gleiche Struktur wie Ist, aber pro Szenario)
CREATE TABLE planning.plan_entries (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scenario_id         UUID NOT NULL REFERENCES planning.scenarios(id),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    account_id          UUID NOT NULL REFERENCES accounting.accounts(id),
    period_year         SMALLINT NOT NULL,
    period_month        SMALLINT NOT NULL,                 -- 1-12
    amount              NUMERIC(18,2) NOT NULL,            -- positiv=Soll-typisch, negativ=Haben-typisch
    cost_center_id      UUID REFERENCES core.cost_centers(id),
    project_id          UUID REFERENCES core.projects(id),
    description         VARCHAR(300),
    dimension_1         VARCHAR(50),
    dimension_2         VARCHAR(50),
    source              VARCHAR(30) DEFAULT 'manual',
                        -- manual | formula | import | ai_generated
    formula             TEXT,                              -- Berechnungsformel falls source=formula
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (scenario_id, account_id, period_year, period_month,
            COALESCE(cost_center_id, '00000000-0000-0000-0000-000000000000'::UUID))
);

CREATE INDEX idx_plan_scenario_period
    ON planning.plan_entries (scenario_id, period_year, period_month);
CREATE INDEX idx_plan_account
    ON planning.plan_entries (account_id, scenario_id);

-- Szenario-Vergleichsergebnisse (materialisiert)
CREATE TABLE planning.comparison_snapshots (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    comparison_name     VARCHAR(200) NOT NULL,
    scenario_ids        UUID[] NOT NULL,                   -- Array der verglichenen Szenarien
    result_data         JSONB NOT NULL,                    -- Vorberechnetes Ergebnis
    generated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    generated_by        UUID NOT NULL
);

-- Szenario-Parameter-Templates
CREATE TABLE planning.scenario_parameters (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scenario_id         UUID NOT NULL REFERENCES planning.scenarios(id),
    parameter_key       VARCHAR(100) NOT NULL,
    parameter_type      VARCHAR(30) NOT NULL,
                        -- revenue_growth | cost_change | headcount | churn_rate |
                        -- price_change | one_time_event | custom
    value_numeric       NUMERIC(18,4),
    value_json          JSONB,
    effective_from      DATE,
    effective_to        DATE,
    description         VARCHAR(300),
    UNIQUE (scenario_id, parameter_key, effective_from)
);
```

### 4.6 Schema: cashflow

```sql
-- ============================================================
-- CASHFLOW: Cash-Flow-Engine
-- ============================================================

CREATE TABLE cashflow.cash_positions (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    bank_account_id     UUID NOT NULL REFERENCES core.bank_accounts(id),
    position_date       DATE NOT NULL,
    balance             NUMERIC(18,2) NOT NULL,
    source              VARCHAR(20) NOT NULL DEFAULT 'bank_import',
                        -- bank_import | manual | calculated
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, bank_account_id, position_date)
);

CREATE TABLE cashflow.cash_flow_items (
    id                  UUID NOT NULL DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    item_date           DATE NOT NULL,
    category            VARCHAR(30) NOT NULL,
                        -- operating_in | operating_out | investing_in | investing_out |
                        -- financing_in | financing_out
    subcategory         VARCHAR(50) NOT NULL,
                        -- customer_receipt | vendor_payment | salary | tax |
                        -- capex | loan | dividend | etc.
    amount              NUMERIC(18,2) NOT NULL,
    certainty           VARCHAR(20) NOT NULL DEFAULT 'confirmed',
                        -- confirmed | recurring | probable | planned | speculative
    certainty_weight    NUMERIC(5,2) NOT NULL DEFAULT 1.00,
    source_type         VARCHAR(30),
                        -- journal_entry | document | recurring | forecast | manual
    source_ref          UUID,
    partner_id          UUID REFERENCES core.business_partners(id),
    description         VARCHAR(500),
    is_recurring        BOOLEAN NOT NULL DEFAULT false,
    scenario_id         UUID REFERENCES planning.scenarios(id),  -- NULL = Ist
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id, item_date)
) PARTITION BY RANGE (item_date);

-- Partitionen erstellen (Beispiel)
CREATE TABLE cashflow.cash_flow_items_2025 PARTITION OF cashflow.cash_flow_items
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
CREATE TABLE cashflow.cash_flow_items_2026 PARTITION OF cashflow.cash_flow_items
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');

CREATE INDEX idx_cf_entity_date ON cashflow.cash_flow_items (entity_id, item_date DESC);
CREATE INDEX idx_cf_scenario ON cashflow.cash_flow_items (scenario_id, entity_id, item_date)
    WHERE scenario_id IS NOT NULL;
```

### 4.7 Schema: i18n (Mehrsprachigkeit)

```sql
-- ============================================================
-- I18N: Mehrsprachigkeit für alle geschäftsrelevanten Texte
-- ============================================================

CREATE TABLE i18n.translations (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID,                              -- NULL = systemweit
    translation_key     VARCHAR(200) NOT NULL,             -- z.B. 'account.4402.name'
    language_code       CHAR(2) NOT NULL,                  -- de, en, ru
    text                TEXT NOT NULL,
    context             VARCHAR(50),                       -- account | cost_center | report | ui
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (entity_id, translation_key, language_code)
);

-- Partialindex für schnelle Lookups
CREATE INDEX idx_i18n_key_lang
    ON i18n.translations (translation_key, language_code);
CREATE INDEX idx_i18n_entity
    ON i18n.translations (entity_id, context, language_code)
    WHERE entity_id IS NOT NULL;
```

### 4.8 Schema: datev

```sql
-- ============================================================
-- DATEV: Export-Verwaltung
-- ============================================================

CREATE TABLE datev.exports (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id           UUID NOT NULL REFERENCES core.legal_entities(id),
    fiscal_period_id    UUID NOT NULL REFERENCES core.fiscal_periods(id),
    export_type         VARCHAR(30) NOT NULL,
                        -- full | delta | correction
    status              VARCHAR(20) NOT NULL DEFAULT 'pending',
                        -- pending | generating | validating | ready | downloaded | failed
    file_count          INT,
    record_count        INT,
    validation_result   JSONB,                             -- Validierungsergebnis
    errors              JSONB DEFAULT '[]',
    warnings            JSONB DEFAULT '[]',
    checksums           JSONB,                             -- SHA-256 pro Datei
    file_storage_keys   JSONB,                             -- S3/MinIO Keys der Exportdateien
    generated_at        TIMESTAMPTZ,
    generated_by        UUID NOT NULL,
    downloaded_at       TIMESTAMPTZ,
    downloaded_by       UUID
);

CREATE TABLE datev.export_log (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    export_id           UUID NOT NULL REFERENCES datev.exports(id),
    log_level           VARCHAR(10) NOT NULL,              -- info | warning | error
    message             TEXT NOT NULL,
    details             JSONB,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 4.9 Schema: audit

```sql
-- ============================================================
-- AUDIT: GoBD-konformer Audit-Trail
-- ============================================================

CREATE TABLE audit.change_log (
    id                  BIGSERIAL,
    entity_id           UUID,
    table_schema        VARCHAR(50) NOT NULL,
    table_name          VARCHAR(100) NOT NULL,
    record_id           UUID NOT NULL,
    action              VARCHAR(10) NOT NULL,              -- INSERT | UPDATE | DELETE
    old_values          JSONB,
    new_values          JSONB,
    changed_fields      TEXT[],
    user_id             UUID NOT NULL,
    ip_address          INET,
    user_agent          VARCHAR(500),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

-- 10 Jahre Aufbewahrung (GoBD)
-- Monatliche Partitionen, automatisch erstellt
```

### 4.10 Materialisierte Views

```sql
-- ============================================================
-- MATERIALIZED VIEWS für Performance
-- ============================================================

-- Saldenliste (Trial Balance)
CREATE MATERIALIZED VIEW accounting.mv_trial_balance AS
SELECT
    je.entity_id,
    fp.year,
    fp.month,
    jel.account_id,
    a.account_number,
    a.account_class,
    a.account_type,
    a.s_h_indicator,
    jel.cost_center_id,
    SUM(jel.debit_amount) AS total_debit,
    SUM(jel.credit_amount) AS total_credit,
    SUM(jel.debit_amount) - SUM(jel.credit_amount) AS saldo
FROM accounting.journal_entries je
JOIN accounting.journal_entry_lines jel ON je.id = jel.journal_entry_id
JOIN accounting.accounts a ON jel.account_id = a.id
JOIN core.fiscal_periods fp ON je.fiscal_period_id = fp.id
WHERE je.status = 'posted'
GROUP BY je.entity_id, fp.year, fp.month, jel.account_id,
         a.account_number, a.account_class, a.account_type, a.s_h_indicator,
         jel.cost_center_id
WITH DATA;

CREATE UNIQUE INDEX idx_mv_tb ON accounting.mv_trial_balance
    (entity_id, year, month, account_id, COALESCE(cost_center_id, '00000000-0000-0000-0000-000000000000'::UUID));

-- BWA (Betriebswirtschaftliche Auswertung)
CREATE MATERIALIZED VIEW accounting.mv_bwa AS
SELECT
    tb.entity_id,
    tb.year,
    tb.month,
    CASE
        WHEN tb.account_class = 4 THEN '1020'  -- Umsatzerlöse
        WHEN tb.account_class = 5 THEN '1060'  -- Materialaufwand
        WHEN tb.account_number LIKE '60%' THEN '1100'  -- Personalkosten
        WHEN tb.account_number LIKE '64%' THEN '1150'  -- Versicherungen/Beiträge
        WHEN tb.account_number LIKE '653%' THEN '1180'  -- Fahrzeugkosten
        WHEN tb.account_number LIKE '66%' THEN '1200'  -- Werbe-/Reisekosten
        WHEN tb.account_number LIKE '678%' THEN '1220'  -- Kosten Warenabgabe
        WHEN tb.account_number LIKE '62%' THEN '1240'  -- Abschreibungen
        ELSE '1260'  -- Sonstige Kosten
    END AS bwa_line,
    tb.account_number,
    SUM(tb.saldo) AS amount
FROM accounting.mv_trial_balance tb
GROUP BY tb.entity_id, tb.year, tb.month,
         CASE
             WHEN tb.account_class = 4 THEN '1020'
             WHEN tb.account_class = 5 THEN '1060'
             WHEN tb.account_number LIKE '60%' THEN '1100'
             WHEN tb.account_number LIKE '64%' THEN '1150'
             WHEN tb.account_number LIKE '653%' THEN '1180'
             WHEN tb.account_number LIKE '66%' THEN '1200'
             WHEN tb.account_number LIKE '678%' THEN '1220'
             WHEN tb.account_number LIKE '62%' THEN '1240'
             ELSE '1260'
         END,
         tb.account_number
WITH DATA;

-- Soll/Ist-Vergleich View
CREATE OR REPLACE VIEW planning.v_plan_actual_comparison AS
SELECT
    a.entity_id,
    a.year,
    a.month,
    a.account_id,
    acc.account_number,
    a.cost_center_id,
    a.total_debit AS actual_debit,
    a.total_credit AS actual_credit,
    a.saldo AS actual_saldo,
    COALESCE(p.amount, 0) AS plan_amount,
    a.saldo - COALESCE(p.amount, 0) AS deviation,
    CASE WHEN COALESCE(p.amount, 0) <> 0
         THEN ROUND(((a.saldo - p.amount) / ABS(p.amount)) * 100, 2)
         ELSE NULL
    END AS deviation_pct,
    s.id AS scenario_id,
    s.name AS scenario_name
FROM accounting.mv_trial_balance a
JOIN accounting.accounts acc ON a.account_id = acc.id
CROSS JOIN planning.scenarios s
LEFT JOIN planning.plan_entries p ON p.scenario_id = s.id
    AND p.account_id = a.account_id
    AND p.period_year = a.year
    AND p.period_month = a.month
    AND COALESCE(p.cost_center_id, '00000000-0000-0000-0000-000000000000'::UUID)
      = COALESCE(a.cost_center_id, '00000000-0000-0000-0000-000000000000'::UUID)
WHERE s.scenario_type IN ('budget', 'forecast');
```

---

## 5. Beleg- und Buchungserfassungslogik

### 5.1 Erfassungsprozess

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│  1. UPLOAD   │────▶│  2. OCR/AI   │────▶│ 3. VALIDIERUNG│────▶│ 4. BUCHUNG  │
│  Beleg       │     │  Extraktion  │     │   + Zuordnung │     │   Erstellen │
└─────────────┘     └──────────────┘     └──────────────┘     └─────────────┘
                                                                      │
                                                               ┌──────▼──────┐
                                                               │  5. FREIGABE│
                                                               │  + Posting  │
                                                               └─────────────┘
```

**Schritt 1 – Upload:** Beleg wird hochgeladen (PDF, Bild, E-Mail-Anhang) oder via API/Webhook empfangen (Bank, Kreditkarte, moss). System erstellt `document.documents`-Eintrag mit Status `uploaded`.

**Schritt 2 – OCR/AI-Extraktion:** AI extrahiert: Belegtyp, Lieferant, Belegnummer, Datum, Leistungszeitraum, Netto/Brutto/USt, Einzelpositionen, IBAN, Verwendungszweck. Ergebnis wird in `ai_extraction` (JSONB) gespeichert.

**Schritt 3 – Validierung und Zuordnung:** System schlägt vor: Sachkonto (basierend auf Lieferant-Historie, Kontenbeschriftung, AI), Kostenstelle, Projekt, BU-Schlüssel. Pflichtfelder-Check: Belegnummer, Datum, mindestens eine Position mit Konto.

**Schritt 4 – Buchungssatz erstellen:** Aus dem validierten Beleg wird ein `journal_entry` mit Zeilen generiert. Bei Eingangsrechnungen z.B.:
```
Soll  5900 Fremdleistungen       2.800,00 EUR  (KSt 210)
Soll  1407 Vorsteuer §13b 19%      532,00 EUR
Haben 3300 Verb. L+L              2.800,00 EUR
Haben 3837 USt §13b 19%             532,00 EUR
```

**Schritt 5 – Freigabe:** Je nach Betrag und Konfiguration: Automatisch posten oder Freigabe-Workflow. Beim Posten: SHA-256 Hash berechnen, mit vorherigem Hash verketten, Status auf `posted` setzen, Buchungsnummer vergeben (lückenlos).

### 5.2 Belegtypen und ihre Buchungslogik

| Belegtyp | Soll-Konten | Haben-Konten | Besonderheiten |
|----------|-------------|--------------|----------------|
| Eingangsrechnung Inland | Aufwandskonto + Vorsteuer 19% (1406) | Verb. L+L (3300) | Standard |
| Eingangsrechnung EU §13b | Aufwandskonto + VSt §13b (1407) | Verb. L+L (3300) + USt §13b (3837) | Reverse Charge |
| Eingangsrechnung Drittland | Aufwandskonto | Verb. L+L (3300) | Keine USt |
| Ausgangsrechnung 19% | Forderungen (1200) | Erlöskonto (4402-4410) + USt 19% (3806) | |
| Bankbuchung Eingang | Bank (1800-1851) | Forderungen (1200) | Zahlungszuordnung |
| Bankbuchung Ausgang | Verb. L+L (3300) | Bank (1800-1851) | Zahlungszuordnung |
| Kreditkartenbeleg | Aufwandskonto + ggf. Vorsteuer | Kreditkarte (3610/3611) | |
| Gehaltsbuchung | Gehälter (6020) + SV AG (6110) + ... | Verb. Lohn (3720) + Verb. LSt (3730) + Verb. SV (3740) | Lohnbuchhaltung |
| Abschreibung | AfA (6201/6220) | Anlagenkonto | Monatlich automatisch |
| Intercompany | Clearing (3420/3422/3424) | Bank oder Aufwand | IC-Abstimmung |

---

## 6. Szenarien-Engine

### 6.1 Architektur

Die Szenarien-Engine trennt sauber zwischen:

1. **Ist-Daten** (`accounting.journal_entries` + `journal_entry_lines`): Echte Buchungen, unveränderbar nach Posting
2. **Plan-Daten** (`planning.plan_entries`): Pro Szenario, Konto, Periode, Kostenstelle
3. **Szenario-Parameter** (`planning.scenario_parameters`): Steuergrößen wie Wachstumsraten, Personalveränderungen

### 6.2 Szenario-Typen

| Typ | scenario_type | Beschreibung |
|-----|---------------|-------------|
| Ist | actual | Automatisch aus gebuchten Journal Entries aggregiert |
| Budget | budget | Jahresplanung, typischerweise 1 aktive Version pro Jahr |
| Forecast | forecast | Rollierender Forecast, monatlich aktualisiert |
| Best Case | best_case | Optimistische Annahmen |
| Worst Case | worst_case | Pessimistische Annahmen / Stresstest |
| Custom | custom | Beliebige What-If-Szenarien |

### 6.3 Szenario-Workflow

```
1. Szenario anlegen (Name, Typ, Jahr, Beschreibung)
2. Optional: Bestehendes Szenario als Basis kopieren
3. Parameter definieren (Wachstumsraten, Kostenänderungen, etc.)
4. Plan-Entries befüllen:
   a) Manuell pro Konto/Monat
   b) Import aus Excel
   c) Formel-basiert (z.B. "Konto 5900 = Vormonat * 1.03")
   d) AI-generiert aus historischen Mustern
5. Szenario sperren (is_locked = true) für Vergleiche
6. Vergleich generieren: Ist vs. Budget vs. Forecast vs. Custom
```

### 6.4 Soll/Ist-Vergleich (Query)

```sql
-- Soll/Ist-Vergleich auf BWA-Ebene
SELECT
    bwa_line,
    SUM(actual_amount) AS ist,
    SUM(plan_amount) AS soll,
    SUM(actual_amount) - SUM(plan_amount) AS abweichung_abs,
    ROUND(
        CASE WHEN SUM(plan_amount) <> 0
        THEN ((SUM(actual_amount) - SUM(plan_amount)) / ABS(SUM(plan_amount))) * 100
        ELSE NULL END, 1
    ) AS abweichung_pct
FROM planning.v_plan_actual_comparison
WHERE entity_id = :entity_id
  AND year = :year
  AND scenario_id = :scenario_id
GROUP BY bwa_line
ORDER BY bwa_line;
```

---

## 7. Cash-Flow-, KPI- und Forecasting-Logik

### 7.1 Cash-Flow-Berechnung

Die Cash-Flow-Berechnung basiert auf zwei Quellen:

**Direkte Methode (aus Banktransaktionen):**
```sql
-- Operativer Cash Flow
SELECT
    DATE_TRUNC('month', item_date) AS monat,
    SUM(CASE WHEN category = 'operating_in' THEN amount ELSE 0 END) AS einnahmen_operativ,
    SUM(CASE WHEN category = 'operating_out' THEN amount ELSE 0 END) AS ausgaben_operativ,
    SUM(CASE WHEN category IN ('operating_in','operating_out') THEN amount ELSE 0 END) AS ocf
FROM cashflow.cash_flow_items
WHERE entity_id = :entity_id AND scenario_id IS NULL
GROUP BY DATE_TRUNC('month', item_date);
```

**Indirekte Methode (aus GuV + Bilanzveränderungen):**
```
Jahresüberschuss/-fehlbetrag (aus Kontenklasse 4-7)
+ Abschreibungen (6201, 6220)
+ Zuführung Rückstellungen (3071, 3079, 3095, 3096)
- Auflösung Rückstellungen (4930)
+/- Veränderung Forderungen (1200)
+/- Veränderung Verbindlichkeiten (3300)
+/- Veränderung ARAP/PRAP (1900, 3900)
= Operativer Cash Flow
```

### 7.2 KPIs (direkt aus dem Datenmodell berechenbar)

| KPI | Formel | Konten (SKR 04) |
|-----|--------|-----------------|
| Umsatz | Summe Klasse 4 (Haben) | 4337+4338+4402+4403+4406+4409+4410 |
| Rohertrag | Umsatz - Materialaufwand | Umsatz - (5900+5901+5923) |
| Rohertragsmarge | Rohertrag / Umsatz * 100 | |
| Personalkosten-Quote | Personal / Umsatz * 100 | (6020+6039+6060+6072+6090+6110+6120+6130+6140) / Umsatz |
| EBITDA | Betriebsergebnis + AfA | BWA 1300 + (6201+6220) |
| Burn Rate | Monatliche Gesamtausgaben | Summe Soll aller Aufwandskonten (5+6) |
| Runway | Kassenbestand / Burn Rate | Bank (1800-1851) / Burn Rate |
| Freelancer-Quote | Fremdleistungen / (Fremdl. + Personal) | 5900 / (5900+6020+6110) |

### 7.3 13-Wochen-Liquiditätsplanung

Basiert auf `cashflow.cash_flow_items` mit Certainty-Gewichtung:
```sql
SELECT
    DATE_TRUNC('week', item_date) AS woche,
    SUM(amount * certainty_weight) AS gewichteter_cashflow,
    SUM(CASE WHEN amount > 0 THEN amount * certainty_weight ELSE 0 END) AS zufluss,
    SUM(CASE WHEN amount < 0 THEN amount * certainty_weight ELSE 0 END) AS abfluss
FROM cashflow.cash_flow_items
WHERE entity_id = :entity_id
  AND item_date BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '13 weeks'
GROUP BY DATE_TRUNC('week', item_date)
ORDER BY woche;
```

---

## 8. DATEV-Export-Spezifikation

### 8.1 Exportpaket

| Datei | Format | Inhalt |
|-------|--------|--------|
| EXTF_Buchungsstapel_YYYYMM.csv | DATEV EXTF v700 | Alle gebuchten Journal Entries der Periode |
| EXTF_Kontenbeschriftungen_YYYYMM.csv | DATEV EXTF | Individuelle Kontennamen |
| EXTF_Debitoren_YYYYMM.csv | DATEV EXTF | Neue/geänderte Debitoren |
| EXTF_Kreditoren_YYYYMM.csv | DATEV EXTF | Neue/geänderte Kreditoren |
| export_metadata.json | JSON | Checksummen, Statistiken, Validierungsergebnis |

### 8.2 Buchungsstapel: Header (3 Zeilen)

```
"EXTF";700;21;"Buchungsstapel";12;{Timestamp};;"CB";"";;{Beraternr};{Mandantnr};{GJBeginn};4;{PeriodeVon};{PeriodeBis};"Clarity Board Export {Monat/Jahr}";"";1;0;;;"EUR";"";;""
```

Felder des Headers:
- Formatkennung: "EXTF"
- Versionsnummer: 700
- Datenkategorie: 21 (Buchungsstapel)
- Formatname: "Buchungsstapel"
- Formatversion: 12
- Erzeugt am: YYYYMMDDHHMMSSmmm
- Beraternummer: aus `legal_entities.datev_consultant_nr`
- Mandantennummer: aus `legal_entities.datev_client_nr`
- WJ-Beginn: YYYYMMDD
- Sachkontenlänge: 4
- Datum von/bis: YYYYMMDD

### 8.3 Buchungsstapel: Feld-Mapping

| Pos | DATEV-Feld | Typ | Quelle in Clarity Board | Validierung |
|-----|-----------|-----|------------------------|-------------|
| 1 | Umsatz (Betrag) | Numeric(18,2) | `ABS(jel.debit_amount)` oder `ABS(jel.credit_amount)` | > 0, max 2 Dezimalstellen |
| 2 | Soll/Haben-Kennzeichen | "S" / "H" | `debit_amount > 0 → "S"`, `credit_amount > 0 → "H"` | Pflicht |
| 3 | WKZ Umsatz | Char(3) | `jel.currency` | ISO 4217, Default "EUR" |
| 4 | Kurs | Numeric | `jel.exchange_rate` wenn ≠ 1.0, sonst leer | |
| 5 | Basis-Umsatz | Numeric | `jel.original_amount` wenn Fremdwährung, sonst leer | |
| 6 | Konto | VARCHAR(9) | `a.account_number` (Sachkonto) | Muss im Kontenplan existieren |
| 7 | Gegenkonto (mit BU-Schlüssel) | VARCHAR(9) | `contra_account.account_number` | Muss im Kontenplan existieren |
| 8 | BU-Schlüssel | VARCHAR(4) | `jel.tax_code` → `vat_codes.datev_bu_key` | Gültiger DATEV BU-Schlüssel |
| 9 | Belegdatum | DDMM | `je.document_date` formatiert | Pflicht, innerhalb der Periode |
| 10 | Belegfeld 1 | VARCHAR(36) | `je.document_ref` | Max 36 Zeichen |
| 11 | Belegfeld 2 | VARCHAR(12) | `je.document_ref2` | Max 12 Zeichen |
| 12 | Skonto | Numeric | Aus Zahlungszuordnung, falls Skonto | |
| 13 | Buchungstext | VARCHAR(60) | `je.description` oder `jel.description` | Max 60 Zeichen, Windows-1252 |
| 14–17 | EU-Felder | diverse | USt-ID Partner, EU-Land, etc. | Nur bei EU-Geschäftsvorfall |
| 20 | Beleglink | VARCHAR(210) | URL zum Beleg in Clarity Board | Optional |
| 36 | Kostenstelle 1 | VARCHAR(8) | `cc.code` | Muss als KSt in DATEV existieren |
| 37 | Kostenstelle 2 | VARCHAR(8) | Leer oder zweite Dimension | |
| 39 | Leistungsdatum | DDMMYYYY | `je.service_date` | Wenn abweichend vom Belegdatum |

### 8.4 Kodierung und Format

- **Zeichensatz:** Windows-1252 (ANSI), nicht UTF-8
- **Trennzeichen:** Semikolon (;)
- **Textqualifizierer:** Doppelte Anführungszeichen (")
- **Dezimaltrennzeichen:** Komma (,) für deutsche DATEV-Installation
- **Datumsformat:** DDMM (Belegdatum), DDMMYYYY (Leistungsdatum), YYYYMMDD (Header)
- **Zeilenumbruch:** CR+LF (\r\n)
- **Betrag immer positiv:** Vorzeichen über S/H-Kennzeichen

### 8.5 Validierungen vor Export

| Prüfung | Schwere | Aktion |
|---------|---------|--------|
| Summe Soll = Summe Haben pro Entry | FEHLER | Export blockiert |
| Alle Konten im Kontenplan vorhanden | FEHLER | Export blockiert |
| BU-Schlüssel konsistent zum Konto | FEHLER | Export blockiert |
| Alle Buchungen innerhalb der Exportperiode | FEHLER | Export blockiert |
| Windows-1252 Encoding möglich | FEHLER | Export blockiert |
| Buchungstext max. 60 Zeichen | WARNUNG | Automatisch abschneiden |
| Debitoren-/Kreditoren-Nr. im gültigen Bereich | WARNUNG | Anzeigen |
| Kostenstelle bei Aufwandskonten vorhanden | WARNUNG | Anzeigen |
| Leistungsdatum bei periodenübergreifenden Belegen | WARNUNG | Anzeigen |
| Keine doppelten Belegnummern | INFO | Anzeigen |

### 8.6 Export-Prozess

```
1. User klickt "DATEV Export" für Periode YYYY-MM
2. System prüft: Alle offenen Buchungen gebucht? Periode soft_closed?
3. Validierungsrunde (siehe 8.5)
4. Generierung der CSV-Dateien (Windows-1252 Encoding)
5. SHA-256 Checksumme pro Datei
6. ZIP-Archiv erstellen
7. Export-Metadaten speichern (datev.exports)
8. Download bereitstellen
9. Periode-Status auf "exported" setzen
10. Export-Count inkrementieren
```

---

## 9. Mehrsprachigkeits-Konzept

### 9.1 Architektur

Drei Ebenen der Mehrsprachigkeit:

**Ebene 1 – Kontenrahmen (systemverwaltet):**
- Tabelle `accounting.standard_account_labels` enthält DE/EN/RU für jeden SKR-04-Standardkonto
- Wird bei System-Updates aktualisiert
- Beispiel: Konto 5900 → DE: "Fremdleistungen", EN: "External services", RU: "Услуги субподрядчиков"

**Ebene 2 – Entity-spezifische Konten:**
- Tabelle `accounting.account_labels` enthält DE/EN/RU für Individualkonten
- Beispiel: Konto 4402 → DE: "Erlöse aqua Miete/SaaS", EN: "Revenue aqua Rent/SaaS", RU: "Выручка aqua Аренда/SaaS"

**Ebene 3 – Allgemeine Übersetzungen:**
- Tabelle `i18n.translations` für Kostenstellen, Berichte, UI-Texte, Reports
- Key-basiert, z.B. `cost_center.210.name` → DE: "Development", EN: "Development", RU: "Разработка"

### 9.2 Lookup-Logik

```sql
-- Kontennamen in der gewünschten Sprache abrufen
-- Fallback: Entity-Label → Standard-Label → account_number
SELECT
    a.account_number,
    COALESCE(
        al.name,                    -- Entity-spezifisches Label
        sal.name,                   -- Standard-Label
        a.account_number            -- Fallback: Kontonummer
    ) AS display_name
FROM accounting.accounts a
LEFT JOIN accounting.account_labels al
    ON al.account_id = a.id AND al.language_code = :lang
LEFT JOIN accounting.standard_account_labels sal
    ON sal.account_id = a.standard_account_id AND sal.language_code = :lang
WHERE a.entity_id = :entity_id;
```

### 9.3 DATEV-Export: Immer Deutsch

Der DATEV-Export verwendet immer die deutschen Kontenbeschriftungen. Die Mehrsprachigkeit betrifft nur die Clarity-Board-GUI und Reports.

---

## 10. Erweiterbarkeit, Skalierbarkeit und Roadmap

### 10.1 Erweiterbarkeit

| Dimension | Mechanismus |
|-----------|------------|
| Neue Konten | Individualkonten pro Entity, jederzeit anlegbar |
| Neue Kostenstellen | Hierarchisch, mit Gültigkeitszeitraum |
| Neue Dimensionen | `dimension_1`, `dimension_2` auf Journal Entry Lines + JSONB `tags` für beliebige Erweiterungen |
| Neue Belegtypen | `document_type` erweiterbar, AI-Extraction anpassbar |
| Neue Szenarien | Unbegrenzt, mit Versionierung und Parametrisierung |
| Neue Sprachen | Zeile in `*_labels` / `i18n.translations` hinzufügen |
| Neue Entities | Mandantenfähig ab Tag 1 |
| Konsolidierung | Über `entity_relationships` + IC-Eliminierung auf Clearing-Konten |
| Weitere Kontenrahmen | SKR 03 als zweite Option bereits im Modell vorgesehen |

### 10.2 Skalierbarkeit

| Maßnahme | Detail |
|----------|--------|
| Partitionierung | `cash_flow_items` und `audit.change_log` nach Datum partitioniert |
| Materialisierte Views | `mv_trial_balance` und `mv_bwa` für Dashboard-Performance |
| Indizes | Composite-Indizes auf (entity_id, date), Partial-Indizes für aktive Records |
| Connection Pooling | PgBouncer im Transaction-Mode |
| Archivierung | GoBD-konform: 10 Jahre online, danach Cold Storage |

### 10.3 Roadmap

| Phase | Umfang | Zeitrahmen |
|-------|--------|------------|
| Phase 1 | Kontenrahmen, Journal Entries, Belegerfassung, DATEV-Export | Q1 2026 |
| Phase 2 | Szenarien-Engine, Plan-Daten, Soll/Ist-Vergleiche | Q2 2026 |
| Phase 3 | Cash-Flow-Engine, 13-Wochen-Forecast, KPI-Dashboard | Q2-Q3 2026 |
| Phase 4 | Bankintegration (HBCI/FinTS), automatischer Zahlungsabgleich | Q3 2026 |
| Phase 5 | AI-gestützte Kontozuordnung, Anomalie-Erkennung | Q3-Q4 2026 |
| Phase 6 | Konsolidierung, Multi-Entity-Reporting, IC-Abstimmung | Q4 2026 |

---

## 11. Offene Punkte und Empfehlungen

### 11.1 Offene Punkte

| Nr. | Punkt | Priorität | Empfehlung |
|-----|-------|-----------|------------|
| 1 | DATEV Beraternummer und Mandantennummer für Aqua Cloud erfragen | Hoch | Ohne diese Daten kein gültiger DATEV-Export |
| 2 | Erlösbuchungen fehlen in den Buchungsdaten – kommen diese aus einem CRM/Billing-System? | Hoch | Klären, wie Erlöse in Clarity Board fließen (API, Import, manuell) |
| 3 | Anlagenspiegel: Konto 148 (Immat. VG in Entwicklung) mit monatlich 15.726 EUR AfA → welche Anlagegüter genau? | Mittel | Anlagenbuchhaltung mit Inventardetails aufbauen |
| 4 | Intercompany-Prozess: Wie werden Clearing-Konten (3420/3422/3424) abgestimmt? | Mittel | IC-Abstimmungsmodul planen |
| 5 | moss-Kreditkartenbelege: Kommt hier eine API-Integration oder manueller Import? | Mittel | moss-API prüfen (Clarity Board doc 22 bereits vorhanden) |
| 6 | Lohnbuchhaltung: Wird diese in Clarity Board geführt oder extern (DATEV Lohn)? | Hoch | Bei externer Lohnbuchhaltung: Import-Schnittstelle für Summenbuchungen |
| 7 | Ranorex-Kostenstelle (240): Ist das eine eigene Produktlinie mit separater Ergebnis-rechnung? | Niedrig | Segment-Reporting klären |
| 8 | Kontenplan-Update-Prozess: Wie werden DATEV-SKR-Updates (jährlich) eingespielt? | Mittel | Automatisierter Import der DATEV-Stammdaten-Updates |

### 11.2 Empfehlungen

1. **Sofort starten mit Phase 1:** Kontenrahmen-Import (die 119 Konten aus dem Kontenplan) und Migrations-Import aller 2.250 Buchungen aus 2025 als Eröffnungsdaten.

2. **DATEV-Export als Qualitätsgate:** Erst wenn ein exportierter Buchungsstapel vom Steuerberater erfolgreich importiert wird, gilt das Modul als produktionsreif.

3. **Leistungsdatum konsequent pflegen:** Die Buchungsdaten zeigen, dass Leistungsdaten vorhanden sind – diese müssen in Clarity Board Pflichtfeld für alle periodenübergreifenden Buchungen sein.

4. **Kostenstellen-Hierarchie aufbauen:** Die flache Liste (200-240 + 2001-2018) sollte hierarchisch werden: Abteilung → Mitarbeiter. So können Reports sowohl auf Abteilungs- als auch auf Mitarbeiterebene aggregieren.

5. **BU-Schlüssel-Mapping priorisieren:** Die korrekte Zuordnung der USt-Automatik (Zusatzfunktionen aus dem Kontenplan) ist der kritischste Punkt für einen fehlerfreien DATEV-Export.

6. **Szenario-Engine frühzeitig mit Ist-Daten testen:** Die BWA-Daten zeigen ein Unternehmen mit -374 TEUR Jahresverlust. Szenarien für Break-Even-Planung und Liquiditäts-Stresstests haben unmittelbaren Business-Value.
