# Umsetzungskonzept Dokumentprozess nach Upload (Frontend + API-Integration)

Stand: 2026-03-13  
Bezug auf aktuellen Repository-Stand, insbesondere:

- Frontend: `src/frontend/src/features/documents/DocumentUpload.tsx`, `DocumentArchive.tsx`, `DocumentDetail.tsx`, `src/frontend/src/hooks/useDocuments.ts`, `src/frontend/src/types/document.ts`, `src/frontend/src/hooks/useSignalR.ts`
- Backend/API: `src/backend/src/ClarityBoard.API/Controllers/DocumentController.cs`, `AccountingController.cs`, `src/backend/src/ClarityBoard.Application/Features/Document/DTOs/DocumentDtos.cs`, `GetDocumentDetailQuery.cs`, `DocumentExtractedDataReader.cs`, `src/backend/src/ClarityBoard.API/Services/AccountingHubNotifier.cs`

## 1. Ziel und Problemstellung

Der Backend-Prozess für Dokumente ist bereits weitgehend vorhanden: Upload, asynchrone Verarbeitung, OCR-/Vision-Pfad, strukturierte Extraktion, Buchungsvorschlag, Review-/Fehlerstatus.

Die zentrale Lücke liegt aktuell **nach dem Upload**:

- Das Backend verarbeitet Dokumente fachlich sinnvoll weiter.
- Das Frontend bildet den Folgeprozess nur teilweise ab.
- Mehrere API-Verträge sind zwischen Frontend und Backend nicht sauber verdrahtet.
- `Review` ist technisch vorhanden, aber noch kein durchgängig benutzbarer UI-Arbeitsmodus.

Zielbild ist ein aus Nutzersicht vollständig bedienbarer Dokumentprozess von **Upload → Verarbeitung → Review/Entscheidung → Abschluss oder Reprocess**.

## 2. Zielprozess aus Nutzersicht

### Ziel-Flow

1. Nutzer lädt ein Dokument hoch.
2. UI bestätigt den Upload und führt direkt in die Dokument-Detailansicht.
3. Während der Hintergrundverarbeitung sieht der Nutzer den aktuellen Status und automatische Aktualisierungen.
4. Bei `Review` sieht der Nutzer die Gründe, OCR-/Extraktionsdaten und den Buchungsvorschlag.
5. Der Nutzer kann den Fall abschließen durch:
   - Partner bestätigen/zuweisen
   - Buchungsvorschlag freigeben
   - Buchungsvorschlag anpassen
   - Buchungsvorschlag ablehnen
   - bei Bedarf Reprocess oder Übergabe in manuellen Folgepfad
6. Bei `Failed` sieht der Nutzer den Fehlerstatus und kann den Prozess erneut anstoßen.

### Sicht- und Aktionsmodell je Status

| Status | Nutzer sieht | Nutzer kann |
|---|---|---|
| `Processing` | Verarbeitungszustand, letzte Aktualisierung, Hinweis auf laufende AI-/OCR-Verarbeitung | warten, Archiv öffnen, Detail offen lassen; keine Abschlussaktionen |
| `Review` | Review-Gründe, OCR-Text/OCR-Metadaten, extrahierte Felder, Partnerstatus, Buchungsvorschlag, Confidence | Partner bestätigen/zuweisen, Buchungsvorschlag freigeben, anpassen oder ablehnen; ggf. Reprocess |
| `Extracted` | erfolgreich extrahierte Kerndaten, Buchungsvorschlag, Confidence | Buchungsvorschlag freigeben, anpassen oder ablehnen; Detail/Download |
| `Booked` | Abschlussstatus, gebuchte Werte, Verweis auf Journal Entry | Detail ansehen, Download, Sprung zum Buchungssatz |
| `Failed` | Fehlerstatus, letzte bekannte Verarbeitungsstufe, Hilfetext | Reprocess starten, Download, Archiv zurück |

## 3. Priorisiertes Gap-Backlog

### P0 = blockierend für einen benutzbaren Review-/Abschlussprozess

| ID | Priorität | Schicht | Betroffene Artefakte | Aktueller Ist-Zustand | Konkrete Lücke | Empfohlene Maßnahme | Erwarteter Nutzen |
|---|---|---|---|---|---|---|---|
| G1 | P0 | beides | `useDocuments.ts`, `DocumentController.cs` | Detail-, Download- und Buchungs-Calls rufen `/documents/...` ohne `entityId` auf; Backend verlangt `entityId` als Query-Parameter | Kernaktionen sind nicht zuverlässig funktionsfähig | Alle Hooks auf `entityId` umstellen: `useDocument(id, entityId)`, `useDocumentDownloadUrl(id, entityId)`, `useApproveBooking`, `useModifyBooking`, `useRejectBooking`, `useReprocessDocument`; Query Keys auf `entityId` erweitern | Detail, Download und Abschlussaktionen funktionieren stabil |
| G2 | P0 | beides | `useDocuments.ts`, `DocumentArchive.tsx`, `DocumentController.GetList` | Frontend sendet `search`, API akzeptiert aktuell `vendorName` | Suche/Filter ist fachlich irreführend oder wirkungslos | Einheitlichen Suchvertrag festlegen: bevorzugt `search` end-to-end; Controller, Query und Frontend angleichen | Listenfilter arbeitet erwartbar und testbar |
| G3 | P0 | Frontend | `DocumentUpload.tsx`, `DocumentDetail.tsx`, Router | Upload endet in statischem Erfolgscard mit künstlichem Status `uploaded`; kein geführter Folgeprozess | Nutzer wird nach Upload nicht in den eigentlichen Arbeitsfluss geführt | Nach Upload direkt auf `/documents/:id` navigieren; dort Status-Tracking starten; Upload-Seite nur für Auswahl und initiale Bestätigung verwenden | Nutzbarer Einstieg in den Folgeprozess |
| G4 | P0 | beides | `DocumentDetailDto`, `types/document.ts`, `DocumentDetail.tsx` | Backend liefert `Fields`, `Confidence`, `ProcessedAt`, `DocumentType`, `BookedJournalEntryId`; Frontend-Typ `Document` bildet Detailmodell unvollständig ab | Detailseite arbeitet mit unpräzisem Typmodell und blendet wichtige Daten aus | Dedizierte Frontend-Typen `DocumentListItem` und `DocumentDetail`; DTO-Mapping konsequent an Detail-/Listenbedarf ausrichten | Saubere API-Integration, weniger implizite Felder, bessere UI-Entscheidungen |
| G5 | P0 | beides | `DocumentDetailDto`, `GetDocumentDetailQuery.cs`, `DocumentDetail.tsx` | Review-Gründe werden geliefert, OCR-Text und OCR-Metadaten nicht; `Fields` werden nicht angezeigt | `Review` ist nicht nachvollziehbar und kaum bearbeitbar | Detail-Response um `ocrText`, `ocrMetadata`, `extractedFieldsSummary` erweitern; Detailseite um Review-Panel ergänzen | Review-Fälle werden fachlich verständlich und bearbeitbar |
| G6 | P0 | beides | `AccountingHubNotifier.cs`, `AccountingHub.cs`, `useSignalR.ts` | Backend sendet `DocumentStatusChanged` über `/hubs/accounting`; Frontend hört nur KPI-/Alert-Hubs | Statuswechsel nach Upload und während `Processing` kommen im UI nicht an | Entweder `useSignalR` um Accounting-Hub erweitern oder dedizierten `useDocumentStatusUpdates`-Hook bauen; zusätzlich Polling-Fallback | Statuswechsel werden ohne manuelles Reload sichtbar |
| G7 | P0 | Frontend | `DocumentDetail.tsx`, `DocumentArchive.tsx` | Kein UI-Pfad für `Failed` → `reprocess`; Download-Button im Archiv ohne Aktion | Fehlgeschlagene Fälle sind nicht sauber behandelbar | `useReprocessDocument` ergänzen, CTA in Detail/Archiv anzeigen, Download verdrahten | `Failed`-Fälle bleiben nicht im Leerlauf hängen |

### P1 = wichtig für produktive Nutzbarkeit

| ID | Priorität | Schicht | Betroffene Artefakte | Aktueller Ist-Zustand | Konkrete Lücke | Empfohlene Maßnahme | Erwarteter Nutzen |
|---|---|---|---|---|---|---|---|
| G8 | P1 | Frontend | `DocumentDetail.tsx` | Review-Gründe werden als rohe Codes dargestellt | Ursache und nächste Handlung sind für Nutzer nicht selbsterklärend | Übersetzbare Review-Reason-Mapping-Tabelle mit Text + empfohlener Aktion einführen | Weniger Supportbedarf, schnellere Bearbeitung |
| G9 | P1 | Frontend | `DocumentDetail.tsx` | Buchungsvorschlag ist sichtbar, aber Abschlusszustände wirken uneinheitlich | Nach Approve/Modify/Reject fehlt ein klarer Abschluss- und Weiterleitungszustand | Erfolgsbanner, Status-Refresh, optionale Navigation zum Journal Entry, klare CTA nach Abschluss | Verständlicher Abschluss statt „stillem“ Statuswechsel |
| G10 | P1 | beides | `DocumentDetailDto`, `DocumentDetail.tsx`, `AccountingController.cs` | Partnerzuordnung/-bestätigung ist vorhanden, aber nicht sauber in Review-Flow eingebettet | Partnerarbeit und Buchungsabschluss stehen nebeneinander statt in einer Reihenfolge | Review-Workspace in Schritte gliedern: 1) Partner, 2) Extraktion prüfen, 3) Buchung entscheiden | Weniger UI-Brüche, bessere Bedienbarkeit |
| G11 | P1 | Frontend | `DocumentArchive.tsx`, `DocumentUpload.tsx` | Archiv zeigt keine Prozesshinweise; Upload und Detail sind nicht gekoppelt | Nutzer verliert Kontext zwischen Upload und späterem Status | Archiv um Schnellfilter `Offen für Review`, `Fehlgeschlagen`, `Gerade in Verarbeitung` ergänzen; Deep Links aus Upload/Toast | Produktiv nutzbare Arbeitsliste |
| G12 | P1 | API | `DocumentDetailDto`, `DocumentListDto` | Zahlen/Felder sind verteilt; `businessPartnerNumber` / `suggestedBusinessPartnerNumber` fehlen im Detail-DTO, obwohl Frontend sie erwartet | Inkonsistente Anzeige und unnötige Frontend-Annahmen | DTOs vereinheitlichen und fehlende Partnernummern explizit mitliefern | Konsistente Anzeige in Detail und Liste |

### P2 = sinnvoller Ausbau danach

| ID | Priorität | Schicht | Betroffene Artefakte | Aktueller Ist-Zustand | Konkrete Lücke | Empfohlene Maßnahme | Erwarteter Nutzen |
|---|---|---|---|---|---|---|---|
| G13 | P2 | beides | Dokument-Detailmodell, Review-UI | `Fields` sind vorhanden, aber es gibt keinen fachlichen Verifizierungsworkflow | Keine manuelle Feldkorrektur/Verifikation in der UI | Eigene Review-Feldkomponente mit `isVerified` / `correctedValue`; bei Bedarf API zum Persistieren von Korrekturen | Besserer Review-Prozess bei OCR-/Extraktionsunsicherheit |
| G14 | P2 | Frontend | Detail-/Archiv-UX | Kein Status-Timeline-/Historienmodell | Prozessverlauf ist schwer nachvollziehbar | Zeitachse für Upload, Processing, Review, Booking, Reprocess | Höhere Transparenz und Auditierbarkeit |
| G15 | P2 | API | Detail-Response | Rohdaten liegen implizit in `ExtractedData`; UI braucht nur sichere Teilmengen | Gefahr von Frontend-Parsing aus Backend-JSON | Strikt typisierte Detail-Subobjekte (`ocrMetadata`, `review`, `extraction`) statt generischem Raw JSON | Stabilere Verträge, weniger Kopplung |

## 4. API-/Backend-Integrationsbedarf

### Bereits vorhandene Endpunkte, die sauber angebunden werden müssen

- `POST /api/documents/upload`
- `GET /api/documents?entityId=...`
- `GET /api/documents/{id}?entityId=...`
- `GET /api/documents/{id}/download?entityId=...`
- `POST /api/documents/{id}/approve-booking?entityId=...`
- `POST /api/documents/{id}/modify-booking?entityId=...`
- `POST /api/documents/{id}/reject-booking?entityId=...`
- `POST /api/documents/{id}/reprocess?entityId=...`
- `POST /api/accounting/documents/{id}/assign-partner`
- `POST /api/accounting/documents/{id}/confirm-partner`

### Zusätzlicher Integrationsbedarf an DTOs/Responses

Für Phase 1 soll **kein neuer großer Endpunktbaum** entstehen. Stattdessen soll `GET /api/documents/{id}` zum vollständigen Review-/Detailvertrag ausgebaut werden.

Empfohlene Erweiterungen an `DocumentDetailDto`:

- `OcrText: string?`
- `OcrMetadata: { source, confidence, usedVision, usedProvider, warnings[], nativeTextLength, visionTextLength }`
- `ReviewReasons: string[]` beibehalten
- `Fields` weiterverwenden, aber im Frontend tatsächlich anzeigen
- `Confidence`, `ProcessedAt`, `DocumentType`, `BookedJournalEntryId` aktiv nutzen
- `BusinessPartnerNumber`, `SuggestedBusinessPartnerNumber` ergänzen

Empfohlene Klärungen an bestehenden Verträgen:

- Listenvertrag von `vendorName` auf echten Freitext-`search` umstellen oder Frontend bewusst auf `vendorName` zurückbauen; bevorzugt ist `search`
- Upload-Response kann minimal bleiben (`documentId`), solange das Frontend danach direkt in die Detailansicht navigiert
- Für Echtzeit-Updates den bestehenden Hub-Event `DocumentStatusChanged` weiterverwenden; Payload `DocumentId`, `Status` reicht für Cache-Invalidierung im MVP

### Transportprinzip

- Keine Frontend-Auswertung von rohem `ExtractedData`-JSON
- Server mappt `ExtractedData` in explizite DTO-Felder
- Frontend verwendet getrennte Typen für Liste und Detail
- Statusänderungen invalidieren gezielt `documents.list(entityId)` und `documents.detail(entityId, id)`

## 5. Frontend-Umsetzung

### Bestehende Artefakte, die erweitert oder korrigiert werden sollten

- `DocumentUpload.tsx`
  - nach erfolgreichem Upload direkt auf Detailseite navigieren
  - Erfolgszustand nur kurz als Übergang zeigen
- `useDocuments.ts`
  - alle relevanten Hooks mit `entityId` verdrahten
  - `useReprocessDocument` ergänzen
  - optional `useDocumentStatusPolling` oder `useDocumentStatusUpdates`
- `types/document.ts`
  - in `DocumentListItem`, `DocumentDetail`, `DocumentOcrMetadata`, `DocumentField` aufteilen
- `DocumentDetail.tsx`
  - als zentrale Arbeitsfläche für `Processing`, `Review`, `Extracted`, `Booked`, `Failed`
  - `Fields`, `Confidence`, `ProcessedAt`, `BookedJournalEntryId` und OCR-Daten anzeigen
- `DocumentArchive.tsx`
  - Download verdrahten
  - Schnellfilter/Badges für offene Arbeitsfälle ergänzen

### Empfohlene UI-Struktur für `DocumentDetail`

1. **Statuskopf**
   - Statusbadge, letzte Aktualisierung, Confidence, CTA je Status
2. **Dokumentenübersicht**
   - Dateiname, Typ, Rechnungsdaten, Betrag, Partner
3. **Review-Panel**
   - Review-Gründe mit Textmapping
   - nächste empfohlene Aktion je Grund
4. **OCR-/Extraktionspanel**
   - OCR-Text
   - OCR-Metadaten
   - extrahierte Felder mit Confidence
5. **Buchungsvorschlag**
   - Konten, Betrag, Steuer, Reasoning, Status
6. **Aktionsleiste**
   - Partner bestätigen/zuweisen
   - Freigeben / Anpassen / Ablehnen
   - Reprocess bei `Failed` und optional bei Review-Sonderfällen

### Konkrete Komponenten-/Hook-Vorschläge

- `DocumentStatusPanel`
- `DocumentReviewReasons`
- `DocumentOcrPanel`
- `DocumentFieldsCard`
- `DocumentActionBar`
- `useDocument(entityId, id)`
- `useDocumentDownloadUrl(entityId, id)`
- `useReprocessDocument()`
- `useDocumentStatusUpdates(entityId, documentId?)`

## 6. UX- und Zustandsverhalten

### Asynchrone Verarbeitung nach Upload

- Nach Upload sofort Redirect auf Detailseite
- Solange Status `uploaded` oder `processing` ist:
  - Polling im 3-5-Sekunden-Takt **oder** Accounting-Hub-Event
  - zusätzlich Timeout-/Fallback-Hinweis nach längerer Wartezeit
- Bei Statuswechsel auf terminalen Zustand (`review`, `extracted`, `booked`, `failed`) Polling stoppen

### Ladezustände

- Skeleton für erste Detailladung
- Inline-Spinner für Mutationen (Approve/Modify/Reject/Reprocess)
- Sichtbare „aktualisiert gerade“-Hinweise bei Live-Refresh

### Fehlerzustände

- API-Fehler mit fachlich verständlicher Meldung statt nur Toast
- `Failed` zeigt klaren CTA `Erneut verarbeiten`
- nicht vorhandenes Dokument: leerer Not-Found-State mit Rückweg ins Archiv

### Leerstates

- Kein Buchungsvorschlag vorhanden: expliziter Hinweis und nächste mögliche Aktion
- Keine Review-Gründe vorhanden: positive Rückmeldung „keine manuelle Prüfung erforderlich“
- Keine OCR-Daten vorhanden: Hinweis, ob Dokument nativ verarbeitet wurde oder Verarbeitung fehlgeschlagen ist

### Verhalten bei `Review`

- Review nicht nur als Badge, sondern als Arbeitsmodus mit priorisierten Schritten
- Aktionen nur dann aktivieren, wenn die notwendigen Daten vorhanden sind
- bei rohen Codes immer verständliche Erklärung daneben anzeigen

### Verhalten bei `Failed`

- Fehlerstatus prominent anzeigen
- Reprocess direkt aus Detail und optional aus Archiv
- nach Reprocess sofort zurück in Status-Tracking

### Verhalten bei erfolgreichem Buchungsabschluss

- Erfolgsbanner und Aktualisierung auf `booked`
- Verweis auf gebuchten Journal Entry
- CTA: „Zum Buchungssatz“, „Zurück ins Archiv“, „Weiteres Dokument hochladen“

## 7. Umsetzungsphasen

### Phase 1: MVP für einen tatsächlich benutzbaren Review-Prozess

Zwingende Deliverables:

- API-/Hook-Mismatch für `entityId` beheben
- Listen-Suchvertrag bereinigen
- Upload → Detail-Redirect einführen
- Detailseite um Status-Tracking, `Failed`-Handling und Reprocess erweitern
- `DocumentDetailDto` + Frontend-Typen für `Fields`, `Confidence`, `ProcessedAt`, `BookedJournalEntryId` angleichen
- Review-Panel mit Review-Gründen, OCR-Text, OCR-Metadaten und Buchungsvorschlag anzeigen
- Approve/Modify/Reject/Partner-Aktionen robust verdrahten
- Polling oder Accounting-Hub-Integration für Statuswechsel aktivieren

### Phase 2: Produktive Nutzbarkeit

Deliverables:

- übersetzte Review-Gründe mit Handlungsempfehlung
- Archiv als Arbeitsliste für offene Review-/Failed-Fälle
- klarer Abschlusszustand nach Approve/Modify/Reject
- Download-Aktion in Archiv und Detail
- konsistente Deep Links zum Journal Entry / Business Partner

### Phase 3: Ausbau

Deliverables:

- manuelle Feldverifikation/Korrektur mit Persistenz
- Statushistorie / Timeline
- stärkere Trennung in explizite Review-/Extraction-DTOs
- weitergehende Echtzeit-Optimierung ohne Polling-Fallback

## 8. Test- und Validierungsstrategie

### Frontend-Tests

- Hook-Tests für `useDocuments.ts`
  - `entityId` wird in allen Requests korrekt gesendet
  - richtige Invalidierung der Query Keys
- Komponententests für `DocumentUpload.tsx`
  - Redirect auf Detail nach Upload
- Komponententests für `DocumentDetail.tsx`
  - Rendering je Status
  - Review-Panel, OCR-Panel, Buchungsaktionen
  - `Failed` → Reprocess-CTA

### API-/Integrations-Tests

- `GET /api/documents/{id}` liefert erweitertes Detailmodell korrekt
- Buchungsaktionen funktionieren mit `entityId`-Query zuverlässig
- `reprocess` nur für `uploaded`/`failed`
- Listen-Suche entspricht dem vereinbarten Query-Parameter

### End-to-End-Tests

- digitales PDF: Upload → `Extracted` oder `Review` → Abschluss
- gescanntes PDF: Upload → `Review` mit OCR-Metadaten sichtbar
- Bilddatei: Upload → Vision-OCR → Review/Extracted
- `Failed`: Upload/Mockfehler → Failed-Detail → Reprocess
- erfolgreicher Abschluss: Approve/Modify → `Booked` + Link zum Journal Entry

### Nachweis der Benutzbarkeit aus UI-Sicht

Der Prozess gilt erst dann als ausreichend umgesetzt, wenn ein Nutzer ohne API-Konsole:

1. ein Dokument hochladen,
2. den Fortschritt beobachten,
3. einen `Review`-Fall verstehen,
4. eine Entscheidung treffen,
5. einen `Failed`-Fall erneut anstoßen,
6. und einen erfolgreichen Abschluss in der UI nachvollziehen kann.

## 9. Klare Empfehlung / nächster Umsetzungsschnitt

Der nächste konkrete Umsetzungsschnitt soll **Phase 1 vollständig und ohne Seitenausbau nebenbei** liefern.

Empfohlener erster Schnitt:

1. **P0-Vertragskorrekturen** umsetzen (`entityId`, Suche, Query Keys, Download/Reprocess-Hooks)
2. **Upload → Detail → Status-Tracking** schließen
3. **Detail-Response und Detail-UI** für Review wirklich nutzbar machen (`Fields`, `ocrText`, `ocrMetadata`, `reviewReasons`, `confidence`)
4. **Approve/Modify/Reject/Partner/Reprocess** stabil verdrahten und mit klaren UI-Zuständen abschließen

Blockierend und daher P0 sind insbesondere:

- fehlende `entityId`-Verdrahtung in den Dokument-Hooks
- fehlender geführter Folgeprozess nach Upload
- fehlende nutzbare Review-Darstellung in der Detailansicht
- fehlende Statusaktualisierung während `Processing`
- fehlender UI-Pfad für `Failed`/`reprocess`

Bewusst nachrangig sind:

- Feldkorrektur mit Persistenz
- Timeline/Historie
- weitergehende UX-Veredelung

Damit ist die empfohlene Reihenfolge eindeutig: **zuerst Integrations- und Review-MVP schließen, danach Komfort und Ausbau.**