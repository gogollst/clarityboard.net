# Invoice Classification Integration — Übergabepaket

## KONTEXT

Dieses Paket kommt aus einer Claude.ai-Session und enthält Referenz-Implementierungen 
und einen optimierten Prompt für die automatische Belegklassifizierung via Anthropic API. 

**WICHTIG: Die Dateien in diesem Paket sind REFERENZMATERIAL, keine Drop-in-Replacements.**
Das bestehende Projekt hat seine eigene Architektur, Namespaces, Patterns und Konventionen. 
Deine Aufgabe ist es, die INHALTE sinnvoll in die bestehende Struktur einzuarbeiten — 
NICHT die Dateien 1:1 zu übernehmen.

---

## DATEIEN IN DIESEM PAKET

| Datei | Zweck | Priorität |
|-------|-------|-----------|
| `prompt_invoice_classification_system_v1.xml` | **Produktions-Prompt** — System Prompt für die Anthropic API. Enthält vollständigen SKR 04 Kontenplan, 12 Steuerregeln, 7-Schritt-Entscheidungslogik, DATEV-Steuerschlüssel, Few-Shot-Examples und Output-Schema. | **HOCH** — Direkt in die Prompt-Datenbank übernehmen |
| `prompt_invoice_classification_user_v1.xml` | **User Prompt Template** — Template mit `{{PLATZHALTERN}}` für OCR-Daten. | **HOCH** — Als Template in die Prompt-Datenbank |
| `InvoiceClassificationService.cs` | **Referenz-Service** — Zeigt die Architektur: Prompt-Construction, API-Call, JSON-Extraktion, Validierung, Batch-Verarbeitung. | **MITTEL** — Als Referenz für Integration |
| `Models.cs` | **Datenmodelle** — Input (OcrInvoiceData) und Output (BookingProposal) Models inkl. aller Sub-Types. | **MITTEL** — Modelle an bestehende DTOs anpassen |
| `UsageExamples.cs` | **Beispiele** — Konkrete Szenarien (M&A-Beratung, Reverse Charge EU, Batch). | **NIEDRIG** — Nur als Referenz/Tests |
| `README.md` | **Doku** — Architekturübersicht und Empfehlungen. | **NIEDRIG** — Zur Orientierung |

---

## INTEGRATIONS-ANLEITUNG

### Phase 1: Prompt-Dateien einpflegen

1. Lies `prompt_invoice_classification_system_v1.xml` vollständig.
2. Identifiziere, wo im Projekt Prompts gespeichert werden (Datenbank, Config-Dateien, Konstanten).
3. Übernimm den System Prompt in das bestehende Prompt-Storage-System.
4. Die Platzhalter im System Prompt (`{{HOLDING_NAME}}`, `{{ENTITIES}}`, `{{COST_CENTERS}}`) 
   müssen zur Laufzeit befüllt werden — prüfe, ob es dafür bereits einen Mechanismus gibt.
5. Übernimm den User Prompt Template (`prompt_invoice_classification_user_v1.xml`) analog.
6. Die `{{PLATZHALTER}}` im User Prompt Template müssen auf die bestehenden OCR-Datenfelder 
   gemappt werden.

### Phase 2: Datenmodelle abgleichen

1. Lies `Models.cs` und vergleiche mit den bestehenden DTOs/Models im Projekt.
2. **NICHT** die Models 1:1 übernehmen. Stattdessen:
   - Identifiziere welche Felder bereits existieren
   - Ergänze fehlende Felder im bestehenden Model (z.B. `flags.maTransaction`, `flags.maPhase`,
     `vatTreatment.legalBasis`, `flags.periodAccrualNeeded`)
   - Passe Namenskonventionen an das bestehende Projekt an (camelCase vs PascalCase, etc.)
3. Das Output-Schema im System Prompt (`<output_schema>`) und die C#-Models müssen 
   synchron sein — wenn du ein Feld im Model änderst, passe auch das Schema im Prompt an.

### Phase 3: Service-Logik integrieren

1. Lies `InvoiceClassificationService.cs` als Referenz.
2. Prüfe, ob es bereits einen Service/Handler für die Belegklassifizierung gibt.
3. Übernimm die LOGIK, nicht die Klasse:
   - **JSON-Extraktion** (`ExtractJson`-Methode): Robustes Parsing der Claude-Antwort
   - **Validierung** (`ValidateBookingProposal`): Kontenprüfung, Confidence-Check
   - **Batch mit Semaphore**: Falls Batch-Verarbeitung benötigt wird
4. Passe den API-Call an das bestehende Anthropic SDK Setup an (DI, HttpClient, etc.)
5. Temperatur 0.1 ist bewusst niedrig gewählt für konsistente Buchungsvorschläge.

---

## WAS DU NICHT TUN SOLLST

- **KEINE neuen Projekte/Assemblies anlegen** — alles in bestehende Struktur einarbeiten
- **KEINE bestehenden Models überschreiben** — nur erweitern
- **KEINE Namespaces ändern** — bestehende Konventionen beibehalten  
- **KEINE bestehende API-Client-Konfiguration ändern** — vorhandenes Setup nutzen
- **KEINE Tests löschen oder umschreiben** — nur neue ergänzen
- **NICHT den gesamten Service 1:1 kopieren** — Logik extrahieren und integrieren

---

## REIHENFOLGE DER ARBEIT

1. **Zuerst** die bestehende Projektstruktur verstehen (Ordner, Namespaces, Patterns)
2. **Dann** die Prompt-Dateien ins Prompt-Storage einpflegen
3. **Dann** die Models erweitern (nicht ersetzen)
4. **Dann** die Service-Logik integrieren (Validierung, JSON-Parsing)
5. **Zuletzt** die UsageExamples als Basis für Integration-Tests verwenden

---

## TECHNISCHE DETAILS

- **Modell**: `claude-sonnet-4-6` als Default, Eskalation auf `claude-opus-4-6` bei 
  `flags.needsManualReview == true` und `confidence < 0.6` ist optional aber empfohlen
- **Temperatur**: 0.1 (niedrig für Konsistenz)
- **Max Tokens**: 2048 reicht für das Output-Schema
- **SDK**: Offizielles Anthropic C# SDK (NuGet: `Anthropic`, v12+)
- **Prompt Caching**: Der System Prompt ist für Caching optimiert — die dynamischen 
  Teile (`{{ENTITIES}}`, `{{COST_CENTERS}}`) sollten sich selten ändern
- **Kontenrahmen**: SKR 04 (DATEV)

---

## FRAGEN BEI UNKLARHEITEN

Wenn du unsicher bist, wie etwas einzusortieren ist:
1. Frag den Benutzer, bevor du größere Änderungen machst
2. Bevorzuge minimale, chirurgische Änderungen
3. Erstelle lieber ein separates TODO/Issue als etwas falsch einzubauen
