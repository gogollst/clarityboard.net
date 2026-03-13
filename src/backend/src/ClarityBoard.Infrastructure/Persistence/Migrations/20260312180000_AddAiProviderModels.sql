-- Provider model registry: stores which models are available per AI provider.
-- Admins can add/remove models via the Admin UI without code changes.

CREATE TABLE IF NOT EXISTS ai.ai_provider_models (
    "Id"          uuid PRIMARY KEY,
    "Provider"    integer NOT NULL,
    "ModelId"     varchar(100) NOT NULL,
    "DisplayName" varchar(200) NOT NULL,
    "SortOrder"   integer NOT NULL DEFAULT 0,
    "Description" varchar(500),
    "IsActive"    boolean NOT NULL DEFAULT true,
    "CreatedAt"   timestamptz NOT NULL DEFAULT now()
);

-- Grant permissions to app user
GRANT ALL ON ai.ai_provider_models TO app;

-- Each model identifier must be unique within a provider
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ai_provider_models_Provider_ModelId"
    ON ai.ai_provider_models ("Provider", "ModelId");

-- Index for the common query: active models for a provider, ordered
CREATE INDEX IF NOT EXISTS "IX_ai_provider_models_Provider_Active_Sort"
    ON ai.ai_provider_models ("Provider", "SortOrder")
    WHERE "IsActive" = true;

-- ── Seed data ──────────────────────────────────────────────────────────────────

INSERT INTO ai.ai_provider_models ("Id", "Provider", "ModelId", "DisplayName", "SortOrder", "Description", "IsActive", "CreatedAt")
VALUES
    -- Anthropic (Provider = 1)
    (gen_random_uuid(), 1, 'claude-opus-4-6',        'Claude Opus 4.6',        10, 'SOTA-Reasoning, komplexeste Logik, Code-Architektur.',              true, now()),
    (gen_random_uuid(), 1, 'claude-sonnet-4-6',      'Claude Sonnet 4.6',      20, 'High-Speed Coding, Agenten-Workflows, bestes P/L.',                true, now()),
    (gen_random_uuid(), 1, 'claude-haiku-4-5',       'Claude Haiku 4.5',       30, 'Echtzeit-Chat, Klassifizierung, extrem niedrige Kosten.',           true, now()),

    -- OpenAI (Provider = 2)
    (gen_random_uuid(), 2, 'gpt-5.4',               'GPT-5.4',                10, 'Flaggschiff mit Deep-Reasoning für autonome Forschung.',             true, now()),
    (gen_random_uuid(), 2, 'gpt-5-mini',            'GPT-5 Mini',             20, 'Kompakter "Thinker" für Logik-Tasks bei geringer Latenz.',           true, now()),
    (gen_random_uuid(), 2, 'gpt-5-nano',            'GPT-5 Nano',             30, 'Günstigstes Reasoning-Modell für einfache Entscheidungen.',          true, now()),
    (gen_random_uuid(), 2, 'gpt-4.1',               'GPT-4.1',                40, 'Klassisches LLM ohne Denkpausen für kreative Texte.',               true, now()),
    (gen_random_uuid(), 2, 'gpt-4.1-mini',          'GPT-4.1 Mini',           50, 'Schnelle Standard-Verarbeitung ohne Reasoning-Logik.',              true, now()),

    -- Gemini / Google (Provider = 4)
    (gen_random_uuid(), 4, 'gemini-3.1-pro',         'Gemini 3.1 Pro',         10, 'Multimodales High-End Modell, 2M+ Kontextfenster.',                 true, now()),
    (gen_random_uuid(), 4, 'gemini-3-flash',          'Gemini 3 Flash',         20, 'Universeller Allrounder für Video-, Audio- & Textanalyse.',         true, now()),
    (gen_random_uuid(), 4, 'gemini-3.1-flash-lite',   'Gemini 3.1 Flash Lite',  30, 'Maximale Geschwindigkeit für High-Volume Processing.',              true, now()),
    (gen_random_uuid(), 4, 'gemini-3.1-flash-image',  'Gemini 3.1 Flash Image', 40, 'Dediziertes Modell für extrem schnelle Bildgenerierung.',           true, now()),

    -- X.AI / Grok (Provider = 3)
    (gen_random_uuid(), 3, 'grok-4.20-beta',        'Grok 4.20 Beta',         10, 'Echtzeit-Web-Zugriff (X), unzensiertes SOTA-Reasoning.',            true, now()),
    (gen_random_uuid(), 3, 'grok-4.1-fast',         'Grok 4.1 Fast',          20, 'Optimiert auf schnellen Output bei hoher Fakten-Treue.',            true, now()),
    (gen_random_uuid(), 3, 'grok-code-fast-1',      'Grok Code Fast 1',       30, 'Spezialmodell für Code-Generierung und Debugging.',                 true, now()),

    -- Z.AI / ZhipuAI (Provider = 5)
    (gen_random_uuid(), 5, 'glm-5',                 'GLM-5',                  10, 'Führendes chinesisches Modell für Agenten & komplexe Logik.',        true, now()),
    (gen_random_uuid(), 5, 'glm-4.7',               'GLM-4.7',                20, 'Stabiles Modell für allgemeine Sprachaufgaben (Mid-Tier).',          true, now()),
    (gen_random_uuid(), 5, 'glm-4.5-air',           'GLM-4.5 Air',            30, 'Leichtgewichtiges Modell für einfache API-Automatisierung.',         true, now()),

    -- Manus (Provider = 6)
    (gen_random_uuid(), 6, 'manus-1.6-max',         'Manus 1.6 Max',          10, 'Voll-autonomer Agent: Research, Coding & Web-Execution.',            true, now()),
    (gen_random_uuid(), 6, 'manus-1.6',             'Manus 1.6',              20, 'Standard-Agent für geführte Browser-Interaktionen.',                 true, now()),
    (gen_random_uuid(), 6, 'manus-1.6-lite',        'Manus 1.6 Lite',         30, 'Schnelle Ausführung einfacher Web- oder Daten-Tasks.',              true, now());

-- Note: DeepL (Provider = 7) has no chat models — it's a translation-only provider.
