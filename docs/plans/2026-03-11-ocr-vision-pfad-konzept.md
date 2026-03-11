# Konzept: OCR-/Vision-Pfad für die AI-Dokumentenverarbeitung

**Version:** 1.0 | **Datum:** 11.03.2026 | **Status:** Umsetzungsgrundlage

---

## 1. Hintergrund und Zielsetzung

### Aktueller Stand

Der bestehende Dokumentenverarbeitungs-Stack enthält bereits die zentralen Bausteine für die textbasierte AI-Verarbeitung:

- `DocumentProcessingConsumer` als orchestrierende Pipeline in `src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs`
- `DocumentTextExtractor` als bestehende native Textextraktion für digitale PDFs und Plain-Text-Dateien
- prompt-basierte AI-Ausführung über `IPromptAiService` und `PromptAiService`
- `IAiService` als bestehende fachliche Fassade für Dokumentextraktion, Buchungsvorschlag und KPI-Analyse
- Prompt-Seeding in `AiPromptsSeed`
- Provider-Konfiguration in `AiProviderConfig` mit `BaseUrl`, `ModelDefault`, API-Key-Verschlüsselung und Health-Status
- Audit-/Kostenbasis über `AiCallLog`

Die strukturierte Dokumentextraktion ist bereits über Prompt Keys umgesetzt, insbesondere:

- `document_extraction`
- `document.ocr_extraction` (Legacy-/Kompatibilitäts-Key)
- `document.booking_suggestion`

### Wiederverwendbare Bausteine

Für die Erweiterung sollen folgende Komponenten ausdrücklich weiterverwendet werden:

- bestehender Dokument-Workflow im `DocumentProcessingConsumer`
- `DocumentTextExtractor` für native PDF-/Textpfade
- bestehende Prompt-Engine für die **nachgelagerte** Feldextraktion
- bestehendes Provider-Management und API-Key-Handling
- bestehende AI-Call-Logs und Prompt-Konfigurationen
- bestehende Persistenz des Extraktionsergebnisses in `Document.OcrText`, `Document.ExtractedData`, `Document.Confidence`

### Verbleibende Lücke

Die aktuell fehlende Kernfunktion ist der **echte multimodale OCR-/Vision-Pfad** für:

- Bilddateien (`image/png`, `image/jpeg`, perspektivisch `image/heic`, `image/tiff`)
- gescannte PDFs ohne brauchbaren eingebetteten Text
- schwache/native PDF-Extraktion mit unzureichender Textqualität

### Zielsetzung

Ziel ist ein zweistufiger, produktionsnaher Verarbeitungsweg:

1. **OCR-/Vision-Schritt** zur robusten Textgewinnung aus Bildern/Scans
2. **Strukturierte Feldextraktion** auf Basis des OCR-Ergebnisses über die bestehende Prompt-Engine

Die Implementierung soll den bestehenden Stack erweitern, ohne die bereits funktionierende textbasierte Prompt-Engine unnötig zu verkomplizieren.

---

## 2. Zielbild / End-to-End-Flow

### Ziel-Flow

1. Dokument wird hochgeladen und im bestehenden Prozess gespeichert
2. `DocumentProcessingConsumer` lädt das Dokument aus dem Storage
3. Native Textextraktion wird über `DocumentTextExtractor` versucht
4. Die Textqualität wird bewertet
5. Falls der native Text leer oder qualitativ unzureichend ist, wird der Vision-/OCR-Pfad aktiviert
6. Der Vision-/OCR-Pfad liefert ein normalisiertes OCR-Zwischenergebnis zurück
7. Das OCR-Zwischenergebnis wird in einen konsolidierten OCR-Text überführt
8. Der konsolidierte Text wird an die bestehende strukturierte Extraktion übergeben
9. Das strukturierte Extraktionsergebnis wird gespeichert und wie bisher für den Buchungsvorschlag verwendet
10. Dokumentstatus, Review-Gründe, Logs und AI-Call-Metriken werden aktualisiert

### Saubere fachliche Trennung

Die Verarbeitung wird ausdrücklich in zwei Verantwortlichkeiten getrennt:

- **OCR/Vision:** „Welcher lesbare Text steht im Dokument?"
- **Strukturierte Feldextraktion:** „Welche geschäftlich relevanten Felder leite ich aus dem Text ab?"

Diese Trennung ist verbindlich. Es wird **kein** einzelner End-to-End-Megaprompt empfohlen, der OCR und Strukturierung gleichzeitig übernehmen soll.

### Einbindung in den bestehenden Dokumentverarbeitungsfluss

Der bestehende Ablauf in `DocumentProcessingConsumer` wird erweitert, nicht ersetzt. Zielzustand der Stages:

1. `load_document`
2. `mark_processing`
3. `download_document`
4. `extract_native_text`
5. `evaluate_text_quality`
6. `run_vision_ocr` *(nur bei Bedarf)*
7. `merge_text_sources`
8. `extract_fields`
9. `persist_extraction`
10. `suggest_booking`
11. `persist_results`

---

## 3. Architekturentscheidung

### Option A: Vision direkt in die bestehende Prompt-Engine integrieren

**Idee:** `IPromptAiService` wird um multimodale Payloads erweitert, z. B. mit Attachments/Binary Inputs.

#### Vorteile

- einheitliches Prompt-Management
- gleiche Administrierbarkeit wie textbasierte Prompts
- direkte Wiederverwendung von Fallback- und Logging-Mechanismen
- alle AI-Aufrufe laufen durch eine gemeinsame Engine

#### Nachteile

- die aktuelle `IPromptAiService`-Signatur ist klar text- und templateorientiert (`Dictionary<string, string>`)
- multimodale Eingaben erfordern provider-spezifische Request-Aufbauten und Binär-/Bildhandling
- erhöhte Komplexität im Kernstück der bestehenden Prompt-Engine
- hohes Risiko, den stabilen Textpfad mit multimodalen Sonderfällen zu belasten

### Option B: Separater vorgelagerter OCR-/Vision-Service

**Idee:** Ein neuer, spezialisierter Service kapselt den Vision-Aufruf, liefert ein normalisiertes OCR-Ergebnis und übergibt dieses danach an die bestehende strukturierte Extraktion.

#### Vorteile

- klare Trennung von Verantwortlichkeiten
- geringes Risiko für die bestehende Prompt-Engine
- OCR-spezifische Themen wie PDF-Rasterisierung, Bildgrößen, Seitenhandling und Vision-Fallbacks bleiben gekapselt
- sauberer MVP mit begrenztem Eingriff in den Bestand
- strukturierte Feldextraktion kann unverändert wiederverwendet werden

#### Nachteile

- zusätzliche Service-Schicht im Backend
- OCR-spezifische Provider-Logik muss separat implementiert werden
- Wiederverwendung von Provider-Konfiguration und Logging muss bewusst organisiert werden

### Entscheidung

**Empfehlung: Option B – separater vorgelagerter OCR-/Vision-Service.**

#### Begründung

Diese Variante ist kurzfristig am realistischsten und am risikoärmsten, weil:

- der bestehende textbasierte Prompt-Stack stabil bleiben kann
- der OCR-/Vision-Pfad andere technische Anforderungen hat als textbasierte Prompt-Ausführung
- der Dokumentprozess klar in Textgewinnung und Strukturierung getrennt bleibt
- die bestehende `IAiService`-Fassade für die nachgelagerte Feldextraktion weiterverwendet werden kann

**Leitentscheidung:**

- Vision/OCR wird als eigener Service implementiert
- strukturierte Extraktion bleibt auf der bestehenden Prompt-Engine
- Provider-Konfiguration, Fallback-Prinzipien und Logging werden gemeinsam genutzt

---

## 4. Provider-Strategie

### Geeignete Vision-/OCR-Provider

Für den aktuellen Stack sind praktisch relevant:

- **Gemini** – bevorzugter Provider für multimodale OCR-/Vision-Eingaben
- **OpenAI** – sinnvoller Fallback für multimodale Verarbeitung
- **Anthropic** – perspektivisch ebenfalls als Vision-Fallback möglich, jedoch nicht erste Priorität im MVP

### Empfohlene Rolle von Gemini

Gemini soll im Vision-Pfad die Rolle des **Primary Providers** übernehmen.

#### Gründe

- gute Eignung für multimodale Eingaben
- bereits im bestehenden Provider-Set vorhanden
- bereits im aktuellen AI-Konzept als starker Kandidat für Dokumentverarbeitung etabliert

### Fallback-Strategie

Empfohlene Reihenfolge im MVP:

1. native Textextraktion
2. Vision-OCR über **Gemini**
3. Vision-OCR-Fallback über **OpenAI**
4. falls kein verwertbarer OCR-Text erzeugt werden kann: `review` oder `failed` je nach Fehlerart

### Anbindung an die bestehende Provider-Konfiguration

Die bestehende `AiProviderConfig` soll direkt weiterverwendet werden:

- `EncryptedApiKey`
- `BaseUrl`
- `ModelDefault`
- `IsActive`
- `IsHealthy`

### Konkrete Empfehlung zur Wiederverwendung

Ein gemeinsamer Resolver für Provider-Runtime-Daten wird empfohlen:

- **Interface:** `IAiProviderRuntimeResolver`
- **Implementierung:** `AiProviderRuntimeResolver`

Verantwortung:

- aktive Provider-Konfiguration laden
- API-Key entschlüsseln
- `BaseUrl` und `ModelDefault` bereitstellen
- optional Health-Prüfung zentralisieren

Dieser Resolver soll sowohl vom `PromptAiService` als auch vom neuen Vision-Service genutzt werden.

---

## 5. Konkrete Backend-Auswirkungen

### Bestehende Klassen/Services, die angepasst werden sollten

#### `DocumentProcessingConsumer`

Erweiterung um:

- native Textbewertung
- optionalen Vision-/OCR-Fallback
- erweiterte Review-Gründe und Statuslogik
- zusätzliche Observability-Stages

#### `DocumentTextExtractor`

Weiterverwendung für:

- digitale PDFs mit eingebettetem Text
- Textdateien

Kurzfristig keine vollständige Ablösung. Der Service bleibt der erste Versuchspfad.

#### `DocumentExtractedDataSerializer`

Erweiterung um:

- OCR-bezogene Review-Reasons
- optional OCR-Metadaten im serialisierten Extraktionspayload

#### `PromptAiService`

Keine direkte Umbaupflicht in einen multimodalen Service. Nur gezielte Wiederverwendung gemeinsamer Provider-/Logging-Bausteine.

### Neue Interfaces und Services

#### `IDocumentVisionService`

Zweck:

- Vision-/OCR-Aufrufe an externe Provider
- Rückgabe eines normalisierten OCR-Ergebnisses

Empfohlene Methode:

`Task<DocumentOcrResult> ExtractTextAsync(DocumentVisionRequest request, CancellationToken ct)`

#### `DocumentVisionService`

Implementiert:

- Provider-Auswahl und Fallback
- Vision-Request-Aufbau
- Response-Normalisierung
- AI-Call-Logging

#### `IDocumentPageRasterizer`

Zweck:

- PDF-Seiten in Bilder rendern

#### `PdfPageRasterizer`

Implementiert:

- PDF → PNG/JPEG pro Seite
- Seitengrenzen und Größenbegrenzung

#### `IDocumentTextAcquisitionService`

Empfehlung für saubere Orchestrierung.

Zweck:

- native Textextraktion durchführen
- Qualität bewerten
- bei Bedarf Vision-/OCR triggern
- konsolidiertes Textresultat an den Consumer zurückgeben

#### `DocumentTextAcquisitionService`

Implementiert:

- Entscheidung „native ausreichend?“
- Aufruf des Vision-Services bei Bedarf
- Zusammenführung von Text und OCR-Metadaten

### Neue DTOs/Modelle

#### `DocumentVisionRequest`

Empfohlene Felder:

- `DocumentId`
- `EntityId`
- `ContentType`
- `FileName` *(optional)*
- `PageImages` oder `BinaryPayloads`
- `PreferredPromptKey`

#### `DocumentOcrResult`

Empfohlene Felder:

- `FullText`
- `Pages`
- `Confidence`
- `Warnings`
- `UsedProvider`
- `UsedFallback`
- `Source`
- `DurationMs`

#### `DocumentOcrPageResult`

- `PageNumber`
- `Text`
- `Confidence`
- `Warnings`

#### `DocumentTextAcquisitionResult`

- `Text`
- `Source`
- `Confidence`
- `Warnings`
- `UsedVision`
- `UsedProvider`
- `NativeTextLength`
- `VisionTextLength`

### Einbindungspunkt im bestehenden Flow

Der OCR-/Vision-Schritt wird **vor** `IAiService.ExtractDocumentFieldsAsync(...)` eingebunden.

Die bestehende `IAiService`-Fassade bleibt erhalten und erhält weiterhin einen konsolidierten Textstring plus MIME-Type.

---

## 6. Prompt- und Datenfluss

### Input an den Vision-Schritt

Der Vision-Service soll folgende Inputs erhalten:

- Original-Bilddatei oder gerenderte PDF-Seitenbilder
- `mimeType`
- `documentId`
- `entityId`
- `pageNumber` je Bildseite
- optionale Hinweise:
  - `document_type` *(falls vorhanden)*
  - `language_hint = de`
  - `domain_hint = accounting_document`

### Zwischenformat des OCR-Ergebnisses

Der Vision-Schritt soll **kein** loses Freitextformat zurückgeben, sondern ein normales JSON-Zwischenformat.

Empfohlenes internes Format:

```json
{
  "full_text": "...",
  "confidence": 0.93,
  "warnings": ["page_2_low_quality"],
  "pages": [
    { "page_number": 1, "text": "...", "confidence": 0.96 },
    { "page_number": 2, "text": "...", "confidence": 0.82 }
  ]
}
```

### Übergabe an die bestehende strukturierte Extraktion

Die Übergabe bleibt bewusst einfach:

- `DocumentTextAcquisitionResult.Text` → `IAiService.ExtractDocumentFieldsAsync(...)`
- `mimeType` bleibt erhalten
- OCR-Metadaten fließen zusätzlich in Review-Gründe, Logs und optional serialisierte Zusatzinformationen ein

### Neue Prompt-Keys / Seed-Prompts

#### Verbindlich für MVP

- **`document.vision_ocr`**

Beschreibung:

- multimodale OCR für Bilder und gerenderte PDF-Seiten
- Ausgabe ausschließlich als JSON mit `full_text`, `pages`, `confidence`, `warnings`

Empfohlene Provider-Konfiguration:

- `PrimaryProvider = Gemini`
- `FallbackProvider = OpenAI`
- `Temperature = 0.0m` bis `0.1m`
- moderates `MaxTokens`, pro Dokument bzw. pro Seite begrenzt

#### Empfohlene spätere Erweiterungen

- `document.ocr_cleanup`
- `document.ocr_quality_assessment`

Diese Prompts sind **nicht** Teil des MVP, aber sinnvoll für spätere Qualitätsstufen.

### Wiederverwendete Prompt-Keys

Weiterhin aktiv und unverändert nutzbar:

- `document_extraction`
- `document.ocr_extraction` *(Legacy-Kompatibilität)*
- `document.booking_suggestion`

---

## 7. Fehlerbehandlung und Observability

### Zu behandelnde Fehlerfälle

- schlechtes oder unscharfes Bild
- leeres OCR-Ergebnis
- nur teilweise erkannte Seiten
- PDF-Rasterisierung schlägt fehl
- Provider-Timeout
- Provider-HTTP-Fehler
- Vision-JSON ungültig oder nicht parsebar
- Primärprovider fällt aus, Fallback springt ein

### Review- und Fehlercodes

Empfohlene Review-Reasons für den Dokumentprozess:

- `native_text_empty`
- `native_text_low_quality`
- `vision_ocr_failed`
- `vision_ocr_timeout`
- `vision_ocr_empty`
- `vision_ocr_low_confidence`
- `vision_ocr_partial_pages`
- `vision_provider_fallback_used`
- `ocr_text_cleanup_applied`
- `low_extraction_confidence`
- `low_booking_confidence`

### Statusverhalten im Dokumentprozess

Verbindliche Empfehlung:

- `processing` während der Pipeline
- `extracted` bei erfolgreicher Textgewinnung und strukturierter Feldextraktion
- `review` bei fachlich oder technisch unsicheren Ergebnissen
- `failed` bei technischem Totalausfall ohne verwertbares OCR-/Textresultat

### Logging

Folgende zusätzliche Stages sollen im `DocumentProcessingConsumer` geloggt werden:

- `extract_native_text`
- `evaluate_text_quality`
- `render_pdf_pages`
- `run_vision_ocr`
- `merge_text_sources`
- `extract_fields`

Zusätzlich loggen:

- `DocumentId`, `EntityId`
- Seitenanzahl
- Dateigröße
- Provider
- Fallback ja/nein
- OCR-Confidence
- Textlänge gesamt und pro Seite

### AI-Call-Logs und Metriken

`AiCallLog` soll auch für Vision-Calls verwendet werden.

Empfehlung:

- Vision-Aufrufe über den Prompt-Key `document.vision_ocr` logisch der bestehenden AI-Auditierung zuordnen
- mittelfristig `AiCallLog` optional um `DocumentId` erweitern

### Operative Kennzahlen

Empfohlene technische Metriken:

- OCR-Erfolgsquote
- Fallback-Quote
- durchschnittliche OCR-Latenz
- OCR-Kosten pro Dokumenttyp
- Anteil `review`
- Anteil leerer OCR-Ergebnisse

---

## 8. Sicherheit und Kosten

### Datenschutz und Umgang mit sensiblen Dokumentinhalten

Rechnungen und Belege enthalten potenziell:

- personenbezogene Daten
- Steuerdaten
- Zahlungsinformationen
- Geschäftspartnerdaten

Verbindliche Maßnahmen:

- nur notwendige Dokumentinhalte an externe Provider senden
- keine unnötigen Metadaten mitsenden
- API-Keys ausschließlich über `AiProviderConfig` und bestehende Verschlüsselung verwalten
- Provider-Einsatz dokumentieren und fachlich freigeben
- Payloads vor Versand größenmäßig begrenzen und normalisieren

### Kostenkontrolle

Verbindliche MVP-Regeln:

1. native Textextraktion immer zuerst versuchen
2. Vision nur bei leerem oder qualitativ unzureichendem Text aktivieren
3. PDF-Seitenlimit definieren (z. B. max. 10 Seiten im MVP)
4. Bildgrößen vor Versand normalisieren
5. Request-Timeouts konsequent setzen
6. Token-/Call-Daten über `AiCallLog` auswerten

### Wann lokales/vorgelagertes OCR vorzuziehen ist

Ein lokaler oder spezialisierter OCR-Pfad ist mittelfristig vorzuziehen, wenn:

- Datenschutzanforderungen keine externen Provider zulassen
- sehr hohe Dokumentvolumina entstehen
- die Kosten pro Vision-Request zu hoch werden
- deterministischere OCR-Ergebnisse benötigt werden
- Standard-Scanbelege in hoher Stückzahl verarbeitet werden

### Betriebsentscheidung

Für den **MVP** wird dennoch ein externer multimodaler Provider empfohlen, weil damit der Implementierungsaufwand gering und die Time-to-Value hoch bleibt.

---

## 9. Umsetzungsphasen

### Phase 1 – MVP (kurzfristig realistisch)

#### Ziel

Scans, Bilder und gescannte PDFs zuverlässig in den bestehenden Dokumentprozess integrieren, ohne die bestehende strukturierte Extraktion umzubauen.

#### Deliverables

- neuer Seed-Prompt `document.vision_ocr`
- `IDocumentVisionService` + `DocumentVisionService`
- `IDocumentPageRasterizer` + `PdfPageRasterizer`
- `IDocumentTextAcquisitionService` + `DocumentTextAcquisitionService`
- Erweiterung von `DocumentProcessingConsumer` um OCR-Routing
- erweiterte Review-Reasons und Logging
- Anbindung an bestehende Provider-Konfiguration
- Vision-Fallback: Gemini → OpenAI

### Phase 2 – Stabilisierung und Qualität

#### Ziel

Robustheit, Nachvollziehbarkeit und Konfigurierbarkeit erhöhen.

#### Deliverables

- `IAiProviderRuntimeResolver` als Shared-Komponente
- optional `DocumentId` in `AiCallLog`
- bessere Qualitätsbewertung des nativen PDF-Texts
- optionaler Prompt `document.ocr_cleanup`
- strengere JSON-Validierung und Retry-Strategie
- bessere Seitenteilung/Chunking bei großen Dokumenten

### Phase 3 – Ausbau

#### Ziel

Höhere Effizienz und langfristige Produktionsreife bei größerem Volumen.

#### Deliverables

- lokaler OCR- oder Spezial-OCR-Pfad als Alternative
- dokumenttypspezifisches Routing
- seitenselektive Verarbeitung
- erweiterte Tabellen-/Positionszeilenextraktion
- kosten- und qualitätsbasiertes Provider-Routing

---

## 10. Test- und Validierungsstrategie

### Unit-Tests

Erforderlich für:

- `DocumentVisionService`
- `DocumentTextAcquisitionService`
- `PdfPageRasterizer`
- JSON-Parsing des OCR-Zwischenformats
- Fallback-Logik Gemini → OpenAI
- Review-Reasons und Statusentscheidung

### Integrationstests

Erforderlich für:

- `DocumentProcessingConsumer` mit nativer Textgewinnung
- `DocumentProcessingConsumer` mit Vision-Fallback
- Fehlerfälle bei Provider-Timeouts oder leeren OCR-Ergebnissen
- Persistenz von `OcrText`, `ExtractedData`, `Confidence` und Review-Gründen

### End-to-End-Tests

Mindestens folgende Testdaten/Fälle abdecken:

- digitales PDF mit eingebettetem Text
- gescanntes PDF
- JPEG/PNG-Rechnung
- Foto eines Kassenbons
- mehrseitige Rechnung
- Dokument mit schlechter Qualität / Schatten / Schieflage
- Dokument mit unvollständigem Text
- Dokument ohne verwertbaren Text

### Fachliche Verifikation

Prüfen:

- VendorName, InvoiceNumber, InvoiceDate, Amount, Currency
- Zeilenpositionen soweit vom Extraktionsprompt erwartet
- Confidence und Review-Gründe
- korrekte Übergabe an `document.booking_suggestion`

### Technische Verifikation

Prüfen:

- OCR-Latenz pro Dokumenttyp
- OCR-Erfolgsquote
- Fallback-Verhalten
- Provider- und Timeout-Fehlerpfade
- Kosten- und Tokenentwicklung im Testbetrieb

---

## 11. Offene Entscheidungen und Risiken

### Vom Team vor Implementierungsstart festzulegen

1. Ist Gemini verbindlich der Primary Provider für Vision-OCR?
2. Welcher Provider ist der verbindliche Fallback – OpenAI oder Anthropic?
3. Welche Dokumenttypen dürfen extern an Vision-Provider übertragen werden?
4. Wie viele Seiten pro Dokument dürfen im MVP verarbeitet werden?
5. Wann soll bei OCR-Problemen `review` statt `failed` gesetzt werden?
6. Soll `AiCallLog` kurzfristig um `DocumentId` erweitert werden?
7. Welche PDF-/Bildformate werden im MVP verbindlich unterstützt?

### Technische Risiken

- PDF-Rasterisierung kann zusätzliche Abhängigkeiten oder Plattformanforderungen erzeugen
- multimodale Provider-Antworten können inkonsistente JSON-Strukturen liefern
- OCR-Qualität schwankt stark bei schlechten Scans und Smartphone-Fotos
- große Dokumente können Latenz und Kosten stark erhöhen

### Fachliche Risiken

- OCR-Fehler propagieren in die strukturierte Extraktion
- unscharfe Trennung zwischen „review“ und „failed“ kann zu operativem Mehraufwand führen
- zu aggressive Vision-Nutzung kann unnötige Kosten verursachen

### Betriebliche Risiken

- externe Provider-Verfügbarkeit
- Rate Limits und Timeouts
- Datenschutzanforderungen einzelner Kunden oder Märkte

---

## 12. Verbindliche Empfehlung für den nächsten Implementierungsschritt

Für den nächsten Umsetzungsschritt wird folgendes als verbindlicher Scope empfohlen:

1. neuen Seed-Prompt `document.vision_ocr` anlegen
2. `IDocumentVisionService` und `DocumentVisionService` implementieren
3. `IDocumentPageRasterizer` und `PdfPageRasterizer` implementieren
4. `IDocumentTextAcquisitionService` einführen
5. `DocumentProcessingConsumer` auf den neuen Textgewinnungsservice umstellen
6. Vision-Fallback-Strategie Gemini → OpenAI implementieren
7. neue Review-Reasons und Logging-Stages ergänzen
8. gezielte Unit- und Integrationstests für den neuen Pfad erstellen

### Nicht Teil des nächsten Schritts

Folgende Punkte werden ausdrücklich **nicht** in den ersten Umsetzungsabschnitt aufgenommen:

- vollständige multimodale Erweiterung von `IPromptAiService`
- lokaler OCR-Stack
- komplexes providerabhängiges Routing nach Dokumenttyp
- zusätzliche Cleanup-/Quality-Prompts außerhalb des MVP

Damit bleibt der Scope klein, umsetzbar und direkt anschlussfähig an den bestehenden Dokument- und Prompt-Stack.