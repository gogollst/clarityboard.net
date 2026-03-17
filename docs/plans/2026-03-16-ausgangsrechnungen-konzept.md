# Fachlich-technisches Konzept: Ausgangsrechnungen erkennen, klassifizieren und verbuchen

**Datum:** 16.03.2026
**Autor:** Claude (erstellt auf Basis der aqua cloud Invoices und Codebase-Analyse)
**Eingaben:** CFO-Konzept "Konzept_Ausgangsrechnungen_aqua_cloud.docx" v1.0 vom 16.03.2026
**Status:** Entwurf — konsolidierte Fassung

---

## 1. Management Summary

Die aktuelle Dokumentenverarbeitung ist ausschließlich auf Eingangsrechnungen ausgelegt. Die 74 analysierten Ausgangsrechnungen der aqua cloud GmbH werden daher fachlich falsch klassifiziert, was zu fehlerhaften Buchungsvorschlägen (Aufwand statt Erlös), fehlender Erlösabgrenzung und einer verzerrten Cashflow-Darstellung führt.

Dieses Konzept definiert eine konkrete Lösung für:
1. **Belegklassifikation** — Eingangsrechnung vs. Ausgangsrechnung (regelbasiert + AI-Fallback)
2. **Datenextraktion** — erweiterte Felder inkl. Leistungszeitraum, Produktkategorie, Abrechnungsintervall
3. **Periodengerechte Erlösverteilung** — lineare Abgrenzung über den Leistungszeitraum mit tagesgenauer Berechnung
4. **Cashflow-Trennung** — strikte Unterscheidung zwischen GuV-Erlös und Liquiditätswirkung
5. **Systemerweiterung** — konkrete Änderungen an Backend, Frontend und API

> **Empfohlener MVP-Umfang:** Dokumentklassifikation, Extraktion der Pflichtfelder inkl. Leistungszeitraum, Buchungsvorschlagslogik mit linearer Erlösverteilung, Cashflow-Trennung. **Geschätzter Aufwand: 4 Sprints.**

---

## 2. Analyse der aqua-cloud-Rechnungen

### 2.1 Datenbasis

74 Ausgangsrechnungen mit Rechnungsnummern 2026400001–2026400074, datiert zwischen Januar und März 2026. Alle von der aqua cloud GmbH ausgestellt.

### 2.2 Wiederkehrende Strukturmerkmale

Alle Rechnungen weisen ein identisches Layout in zwei Sprachvarianten auf:

| Merkmal | Deutsche Variante | Englische Variante |
|---|---|---|
| **Aussteller** | aqua cloud GmbH, Scheidtweilerstr. 4, 50933 Köln | (identisch) |
| **Steuernummer** | 223/5851/1437 | Tax number: 223/5851/1437 |
| **USt-IdNr** | DE321177152 | VAT ID number: DE321177152 |
| **IBAN** | DE92 3707 0060 0727 7775 00 | (identisch) |
| **BIC** | DEUTDEDKXXX | (identisch) |
| **Adressblock-Label** | Rechnungsadresse | Invoice To: |
| **Rechnungsnummer-Label** | Rechnungsnummer | Invoice# |
| **Datum-Label** | Rechnungsdatum | Invoice Date |
| **Fälligkeits-Label** | Fälligkeitsdatum | Due Date |
| **Bestell-Label** | Bestellnummer | Order number |
| **Positions-Header** | Artikel & Beschreibung | Item & Description |
| **Steuer-Label** | MwSt (19%) | MwSt. / VAT (0%) |
| **Summe-Label** | Gesamt | Total Gross |
| **Fußzeile** | MACHEN SIE QUALITÄT ZU IHREM BESTEN FEATURE | MAKE QUALITY YOUR BEST FEATURE |

### 2.3 Signale zur Erkennung als Ausgangsrechnung

#### 2.3.1 Primäre Erkennungssignale (hart)

1. **Rechnungssteller = eigene Firma:** USt-IdNr `DE321177152`, IBAN `DE92 3707 0060 0727 7775 00` oder Steuernummer `223/5851/1437` stimmen mit eigenen Entity-Stammdaten überein.
2. **Firmen-Logo/Header-Block:** Eigenes Logo und Firmenblock rechts oben. Kunde steht links unter "Rechnungsadresse" / "Invoice To".
3. **IBAN/BIC des Ausstellers:** Bankverbindung (Deutsche Bank Köln) gehört zur eigenen Firma, nicht zum Lieferanten.

#### 2.3.2 Sekundäre Erkennungssignale (weich)

4. **Produktnamen:** Alle Positionen beginnen mit "aqua" (aqua Rent Concurrent Pro, aqua Suite, aqua Private Cloud) — eigene Produkte, keine Fremdleistungen.
5. **Typische Formulierungen:** "Kosten pro Lizenz und Jahr", "aqua als Mietlösung (On Premises)", "Wartung & Support inklusive", "Inklusive Bereitstellung, Wartung & Support".
6. **Reverse-Charge-Hinweise:** "Steuerschuldnerschaft des Leistungsempfängers nach § 13b UStG" / "Tax liability of the service recipient according to § 13b USTG" — aqua cloud ist Leistungserbringer.
7. **Kontaktdaten:** Finance: invoice@aqua-cloud.io, Sales: sales@aqua-cloud.io — E-Mail-Domain = @aqua-cloud.io.

### 2.4 Positionstypen und Leistungszeiträume

| Positionstyp | Typische Laufzeit | Abrechnungsart | Beispiel |
|---|---|---|---|
| **SaaS-Lizenz (Cloud)** | 12 Monate | Jährlich | aqua Suite (cloud), aqua Scout |
| **On-Premises-Mietlizenz** | 12 Monate | Jährlich | aqua Rent Concurrent Pro, Named ALM |
| **Private-Cloud-Hosting** | 12 Monate | Jährlich | aqua Private Cloud (Azure Germany) |
| **M&S-Verträge (Ranorex)** | 12 Monate | Jährlich | M&S Concurrent Ranorex Enterprise |
| **Monats-Abonnement** | 1 Monat | Monatlich | aqua Suite - Monthly subscription |
| **Einmalleistung** | Kein Zeitraum | Einmalig | aqua Starter Kit (2 days) |
| **Staffelrabatt** | Laufzeit der Hauptpos. | Jährlich | Staffelrabatt (negativer Betrag) |
| **Gratisposition** | Wie Hauptposition | Jährlich | aqua Guest License (0,00 €) |
| **Teilzeitraum** | < 12 Monate | Anteilig | Leistungszeitraum 6 Monate |

### 2.5 Leistungszeitraum-Formate

| Format | Beispiel |
|---|---|
| `Laufzeit: DD.MM.YYYY - DD.MM.YYYY` | Laufzeit: 01.02.2026 - 31.01.2027 |
| `Laufzeit DD.MM.YYYY – DD.MM.YYYY` | Laufzeit 01.03.2026 – 28.02.2027 |
| `Leistungszeitraum: YYYY-MM-DD - YYYY-MM-DD` | Leistungszeitraum: 2026-01-01 - 2026-12-31 |
| `Service period: YYYY-MM-DD - YYYY-MM-DD` | Service period: 2026-03-11 - 2027-03-10 |
| `Laufzeit 12 Monate` (implizit, ohne Datum) | Laufzeit 12 Monate. |
| `Delivery date: YYYY-MM-DD` (Einmalleistung) | Delivery date: 2026-03-12 |
| `Liefertermin: DD.MM.YYYY` (Einmalleistung) | Liefertermin: 01.03.2026 |
| Fehlend | Bei Monatsabonnements teilweise kein expliziter Zeitraum |

### 2.6 Steuerliche Varianten

| Szenario | MwSt-Satz | Hinweis auf Rechnung |
|---|---|---|
| **Inlandskunde (DE)** | 19% | MwSt (19%) + Betrag |
| **EU-Ausland (z.B. AT)** | 0% | Steuerschuldnerschaft nach § 13b UStG |
| **Drittland (z.B. USA)** | 0% | Tax liability according to § 13b USTG / VAT due do the recipient |
| **Öffentl. Auftraggeber (DE, ohne USt-ID)** | 19% | Standard-Inlandsbesteuerung |

---

## 3. Soll-Prozess für Ausgangsrechnungen

### 3.1 Verarbeitungspipeline

Der bestehende Dokumentenprozess wird um eine vorgelagerte Klassifikationsstufe und eine nachgelagerte Erlösverteilungslogik erweitert:

```
1. Dokumenteingang         PDF-Upload (unverändert)
2. OCR/Vision-Extraktion   Texterkennung (unverändert)
3. NEU: Klassifikation     Eingangsrechnung vs. Ausgangsrechnung vs. Gutschrift
4. AI-Datenextraktion      Angepasst je nach Dokumenttyp (neue Felder)
5. Validierung             Erweiterte Plausibilitätsprüfungen (V-01 bis V-10)
6. NEU: Buchungsvorschlag  Periodengerechte Erlösverteilung bei Laufzeiten > 1 Monat
   mit Erlösverteilung
7. NEU: Cashflow-Buchung   Separate Erfassung des Zahlungszuflusses
8. Review/Freigabe         Erweiterte Review-Gründe
```

### 3.2 Erwartete Ergebnisse nach Verarbeitung

| Ergebnis | Beschreibung |
|---|---|
| **Klassifikation** | Typ = `OUTGOING_INVOICE` mit Confidence-Score |
| **Extrahierte Stammdaten** | Rechnungsnummer, Datum, Fälligkeit, Kunde, Bestellnummer, Gesamtbetrag, Währung, Steuerinfo |
| **Extrahierte Positionen** | Pro Position: Produktname, Menge, Einzelpreis, Betrag, Leistungszeitraum (von/bis), Produktkategorie, Abrechnungsintervall |
| **Buchungsvorschlag (Erlös)** | Aufgeteilt in monatliche Erlösbuchungen (Soll: Forderungen, Haben: Erlöse aus Lizenzen), ggf. mit passiver Rechnungsabgrenzung |
| **Buchungsvorschlag (Cashflow)** | Einmaliger Zahlungszufluss zum Fälligkeitsdatum (Soll: Bank, Haben: Forderungen) |
| **Review-Status** | `AUTO_APPROVED` oder `REVIEW_REQUIRED` mit Begründung |

---

## 4. Technische Lösungsarchitektur

### 4.1 Dokumentklassifikation

Die Klassifikation erfolgt als erster Schritt nach der OCR-Extraktion und vor der strukturierten Datenextraktion. Sie basiert auf einem **regelbasierten Scoring-Ansatz** mit optionalem AI-Fallback.

#### 4.1.1 Regelbasierter Klassifikator

Der Klassifikator prüft die folgenden Regeln in der angegebenen Reihenfolge. Jede Regel liefert einen Score (0–100). Der Gesamtscore bestimmt die Klassifikation.

| Regel | Gewicht | Prüflogik |
|---|---|---|
| **Aussteller-Match** | 40% | USt-IdNr, IBAN oder Firmenname des Ausstellers = eigene Entity-Stammdaten |
| **Produktname-Match** | 20% | Mindestens eine Position enthält bekannten Produktnamen (konfigurierbare Mapping-Tabelle) |
| **Layout-Signatur** | 15% | Aussteller rechts oben, Empfänger links ("Rechnungsadresse" / "Invoice To") |
| **Reverse-Charge** | 10% | Hinweis auf § 13b UStG = wir sind Leistungserbringer |
| **Kontaktdaten** | 10% | E-Mail-Domain = eigene Domain |
| **Negativ-Signal** | −30% | Eigene Firma steht im Empfängerblock → wahrscheinlich Eingangsrechnung |

**Entscheidungslogik:**

| Gesamtscore | Klassifikation | Aktion |
|---|---|---|
| ≥ 80 | `OUTGOING_INVOICE` | Auto-Approval (kein Review für Klassifikation) |
| 50–79 | `OUTGOING_INVOICE` | Review-Pflicht (`CLASSIFICATION_UNCERTAIN`) |
| < 50 | `INCOMING_INVOICE` | Bestehender Eingangsrechnungs-Pfad |

**AI-Fallback:** Wenn der Aussteller-Match keine Entscheidung erlaubt (z.B. neue Entity ohne hinterlegte Stammdaten), wird der Vision-Extraktor befragt: "Ist der Aussteller dieses Dokuments identisch mit [eigene Firmendaten]?"

**Technische Umsetzung:** Der Klassifikator wird als neue Komponente `DocumentClassifier` implementiert, die nach der OCR-/Vision-Textakquise und **vor** der AI-Feldextraktion aufgerufen wird. Das Ergebnis (`DocumentType` + `ClassificationConfidence`) wird an die Extraktion weitergegeben, damit der AI-Prompt die Richtung berücksichtigen kann.

> **Abgleich mit bestehendem Code:** Die bestehende `MatchEntityFields`-Methode im `DocumentProcessingConsumer` führt bereits einen Entity-Abgleich über USt-IdNr, VAT-ID und Firmenname durch. Diese Logik wird um den IBAN-Abgleich und die gewichtete Scoring-Logik erweitert und **vorgelagert** (vor Extraktion statt danach). Das bisherige Feld `DocumentDirection` (String `"incoming"/"outgoing"`) wird durch das formale Enum ersetzt.

#### 4.1.2 Neues Enum: `DocumentType`

```csharp
public enum DocumentType
{
    IncomingInvoice = 0,     // Eingangsrechnung (Default, bestehend)
    OutgoingInvoice = 1,     // Ausgangsrechnung (neu)
    CreditNoteIncoming = 2,  // Eingangs-Gutschrift (zukünftig)
    CreditNoteOutgoing = 3,  // Ausgangs-Gutschrift (zukünftig)
    Unknown = 99             // Nicht klassifizierbar → immer REVIEW
}
```

Dieses Enum ersetzt die bisherige String-basierte `DocumentDirection` (`"incoming"/"outgoing"`) in `DocumentExtractionResult` und das String-basierte `InvoiceType` in `BookingSuggestionResult`. Die Migration muss bestehende `"incoming"`-Werte auf `IncomingInvoice` und `"outgoing"` auf `OutgoingInvoice` mappen.

### 4.2 Datenextraktion — erweiterte Felder

#### 4.2.1 Neue Dokument-Level-Felder

| Feld | Typ | Pflicht | Beschreibung |
|---|---|---|---|
| `documentType` | `DocumentType` (Enum) | Ja | Ergebnis der Klassifikation |
| `classificationConfidence` | decimal | Ja | 0.0–1.0, Schwellwert Auto-Approval: 0.8 |
| `dueDate` | DateOnly | Ja | Fälligkeitsdatum |
| `orderNumber` | string | Nein | Bestellnummer / Order number |
| `reverseCharge` | bool | Ja | `true` wenn §13b-Hinweis vorhanden |
| `servicePeriodStart` | DateOnly | Bedingt | Frühester Leistungsbeginn aller Positionen |
| `servicePeriodEnd` | DateOnly | Bedingt | Spätestes Leistungsende aller Positionen |
| `isRecurringRevenue` | bool | Ja | `true` wenn mind. eine Lizenz-/Abo-Position |
| `recurringInterval` | string | Ja | `"ANNUAL"`, `"MONTHLY"`, `"ONE_TIME"` |

#### 4.2.2 Neue Positions-Level-Felder (`LineItemResult`)

| Feld | Typ | Pflicht | Beschreibung |
|---|---|---|---|
| `productCategory` | Enum | Ja | `SAAS_LICENSE`, `ON_PREM_LICENSE`, `HOSTING`, `MAINTENANCE`, `ONE_TIME_SERVICE`, `DISCOUNT` |
| `servicePeriodStart` | DateOnly | Bedingt | Beginn Leistungszeitraum (Pflicht bei Lizenzen) |
| `servicePeriodEnd` | DateOnly | Bedingt | Ende Leistungszeitraum (Pflicht bei Lizenzen) |
| `billingInterval` | Enum | Ja | `ANNUAL`, `MONTHLY`, `ONE_TIME` |
| `isRecurring` | bool | Ja | Ableitung aus `billingInterval` |

> **Annahme:** Die Produktkategorisierung (`productCategory`) wird initial über Keyword-Matching auf den Produktnamen abgebildet. Eine **konfigurierbare Mapping-Tabelle** (Produktname → Kategorie → Erlöskonto) wird im Backend gepflegt (`ProductMappingService`). Dies ermöglicht mandantenspezifische Anpassungen.

#### 4.2.3 Erweiterte C#-Datenstrukturen

```csharp
// Erweiterung von DocumentExtractionResult
public record DocumentExtractionResult
{
    // Bestehende Felder bleiben...

    // Neu: Klassifikation
    public DocumentType DocumentType { get; init; }
    public decimal ClassificationConfidence { get; init; }

    // Neu: Zusätzliche Kopfdaten
    public DateOnly? DueDate { get; init; }
    public string? OrderNumber { get; init; }
    public bool ReverseCharge { get; init; }

    // Neu: Leistungszeitraum (Belegebene, aggregiert)
    public DateOnly? ServicePeriodStart { get; init; }
    public DateOnly? ServicePeriodEnd { get; init; }
    public bool IsRecurringRevenue { get; init; }
    public string? RecurringInterval { get; init; } // "ANNUAL", "MONTHLY", "ONE_TIME"
}

// Erweiterung von LineItemResult
public record LineItemResult
{
    // Bestehende Felder bleiben...

    // Neu
    public string? ProductCategory { get; init; } // "SAAS_LICENSE", "ON_PREM_LICENSE", etc.
    public DateOnly? ServicePeriodStart { get; init; }
    public DateOnly? ServicePeriodEnd { get; init; }
    public string? BillingInterval { get; init; } // "ANNUAL", "MONTHLY", "ONE_TIME"
    public bool IsRecurring { get; init; }
}
```

#### 4.2.4 Anpassung des Extraction-Prompts

Der bestehende Prompt `document_extraction` in `AiPromptsSeed.cs` wird um folgende Anweisungen erweitert:

```
- Extract due_date (Fälligkeitsdatum / Due Date) in ISO format.
- Extract order_number (Bestellnummer / Order number) if present.
- Set reverse_charge to true if any reference to §13b UStG, "Steuerschuldnerschaft",
  "Tax liability of the service recipient" is found.
- For each line item:
  - Extract service_period_start and service_period_end if a service period is mentioned
    ("Laufzeit", "Leistungszeitraum", "Service period"). Parse all date formats to ISO.
    If only a duration is given (e.g., "Laufzeit 12 Monate"), derive dates from invoice_date.
  - Classify product_category: "SAAS_LICENSE" (cloud software), "ON_PREM_LICENSE" (on-premises,
    Mietlösung), "HOSTING" (Private Cloud, Azure), "MAINTENANCE" (M&S, Wartung & Support as
    standalone), "ONE_TIME_SERVICE" (Starter Kit, consulting, training), "DISCOUNT" (Staffelrabatt,
    negative amounts).
  - Set billing_interval: "ANNUAL" (12 months), "MONTHLY" (1 month), "ONE_TIME" (no recurrence).
  - Set is_recurring to true if billing_interval is "ANNUAL" or "MONTHLY".
- Set is_recurring_revenue to true if any line item has is_recurring = true.
- Set recurring_interval to the dominant billing_interval across all line items.
```

### 4.3 Validierungsregeln

Nach der Extraktion werden folgende Validierungen durchgeführt. Jede gescheiterte Validierung setzt den Status auf `REVIEW_REQUIRED` mit entsprechendem Grund:

| Regel-ID | Prüfung | Severity | Review-Grund |
|---|---|---|---|
| **V-01** | Rechnungsnummer vorhanden und plausibles Format | ERROR | `MISSING_INVOICE_NUMBER` |
| **V-02** | Rechnungsdatum liegt nicht in der Zukunft | ERROR | `INVALID_DATE` |
| **V-03** | Gesamtbetrag > 0 (außer bei Gutschrift) | ERROR | `INVALID_AMOUNT` |
| **V-04** | Leistungszeitraum bei Lizenzpositionen vorhanden | WARNING | `MISSING_SERVICE_PERIOD` |
| **V-05** | `servicePeriodEnd` > `servicePeriodStart` | ERROR | `INVALID_SERVICE_PERIOD` |
| **V-06** | Laufzeit ≤ 36 Monate (Plausibilität) | WARNING | `UNUSUAL_SERVICE_PERIOD` |
| **V-07** | Summe Einzelpositionen = Zwischensumme | ERROR | `AMOUNT_MISMATCH` |
| **V-08** | MwSt korrekt berechnet (Toleranz 0,02 €) | WARNING | `VAT_MISMATCH` |
| **V-09** | Bei `reverseCharge = true`: `vatRate = 0%` | ERROR | `REVERSE_CHARGE_VAT_CONFLICT` |
| **V-10** | Kunde hat USt-ID bei EU-Ausland | WARNING | `MISSING_CUSTOMER_VAT` |

> **Abgleich mit bestehendem Code:** Die bestehenden Validierungen im `DocumentProcessingConsumer` (Amount-Plausibility via `AmountReconciler`, Low-Confidence-Check mit Schwelle 0.70) bleiben erhalten. Die Regeln V-01 bis V-10 werden als zusätzliche Prüfungen für Ausgangsrechnungen hinzugefügt.

### 4.4 Buchungsvorschlagslogik

#### 4.4.1 Grundprinzip

Jede Ausgangsrechnung erzeugt **zwei voneinander unabhängige Buchungsstränge:**

1. **Erlösbuchungen (periodengerecht):** Verteilung des Nettoerlöses über den Leistungszeitraum
2. **Cashflow-Buchung (zahlungswirksam):** Einmaliger Zahlungszufluss bei Forderungsausgleich

#### 4.4.2 Erlöserfassung bei Jahreslizenzen (Normalfall)

**Beispiel:** Rechnung über 12.000,00 € netto, Leistungszeitraum 01.02.2026–31.01.2027, 19% MwSt.

**Buchung zum Rechnungsdatum:**

| Konto | Soll | Haben | Bemerkung |
|---|---|---|---|
| 1200 Forderungen aus L+L | 14.280,00 € | | Bruttobetrag (inkl. 19% MwSt) |
| 4400 Erlöse aus Lizenzen | | 1.000,00 € | Erlös Monat 1 (anteilig, sofort) |
| 4900 Passive Rechnungsabgrenzung (PRA) | | 11.000,00 € | Verbleibende 11 Monate |
| 1776 Umsatzsteuer 19% | | 2.280,00 € | MwSt sofort fällig |

**Monatliche Auflösung der PRA (Monate 2–12):**

| Konto | Soll | Haben | Bemerkung |
|---|---|---|---|
| 4900 Passive Rechnungsabgrenzung | 1.000,00 € | | Auflösung 1/12 |
| 4400 Erlöse aus Lizenzen | | 1.000,00 € | Periodengerechter Erlös |

> **Verteilungslogik:** Die Verteilung erfolgt linear pro Monat: `Nettobetrag / Anzahl Monate`. Bei **angebrochenen Monaten** wird **tagesgenau** abgegrenzt: `Nettobetrag / Gesamttage × Tage im Monat`. Die MwSt wird zum Rechnungsdatum vollständig gebucht, da die Steuerschuld mit Rechnungsstellung entsteht (deutsches UStG).

> **Kontenhinweis:** Die genannten Konten (1200, 4400, 4900, 1776) entsprechen dem SKR03-Standard. Bei SKR04-Mandanten gelten: 1400 Forderungen, 4400 Erlöse, 3900 PRA, 3806 USt. Das System muss den konfigurierten Kontenrahmen berücksichtigen.

#### 4.4.3 Buchungsschemata nach Steuervariante

**Standard Inland (19% MwSt):**
```
Soll  1200 Forderungen aus L+L          Bruttobetrag
Haben 4400 Erlöse (anteilig Monat 1)    Netto/Monate × Tage-Anteil
Haben 4900 PRA (verbleibend)            Netto − Erlös Monat 1
Haben 1776 Umsatzsteuer 19%             MwSt-Betrag (sofort, vollständig)
```

**Reverse Charge / EU-Ausland (0% MwSt):**
```
Soll  1200 Forderungen aus L+L          Nettobetrag (= Brutto)
Haben 4400 Erlöse (anteilig Monat 1)    Netto/Monate × Tage-Anteil
Haben 4900 PRA (verbleibend)            Netto − Erlös Monat 1
```
Keine USt auszuweisen. Steuerschuldnerschaft beim Empfänger (§ 13b UStG).

**Drittland (z.B. USA, 0% MwSt):**
```
Soll  1200 Forderungen aus L+L          Nettobetrag (= Brutto)
Haben 4400 Erlöse (anteilig Monat 1)    Netto/Monate × Tage-Anteil
Haben 4900 PRA (verbleibend)            Netto − Erlös Monat 1
```
Steuerfreie Ausfuhrlieferung. Analoges Schema wie Reverse Charge.

#### 4.4.4 Sonderfälle der Erlösverteilung

**a) Monatsabonnements (`billingInterval = MONTHLY`):**
Keine Abgrenzung. Der gesamte Nettobetrag wird im Rechnungsmonat als Erlös gebucht: Forderung an Erlöse.

**b) Einmalleistungen (`billingInterval = ONE_TIME`):**
Sofortige Erlöserfassung zum Rechnungsdatum. Kein Leistungszeitraum, keine Verteilung. Beispiel: aqua Starter Kit.

**c) Staffelrabatt (`productCategory = DISCOUNT`):**
Der Rabatt wird **anteilig auf die zugehörigen Lizenzpositionen** verteilt und mindert den monatlichen Erlös proportional. Die Laufzeit des Rabatts entspricht der Laufzeit der Hauptposition.

**d) Gratispositionen (`lineTotal = 0,00`):**
Werden extrahiert und dokumentiert, erzeugen aber **keine Buchung**. Sie dienen der Vertragsdokumentation.

**e) Teilzeiträume (< 12 Monate):**
Identische Logik wie Jahreslizenzen, nur mit kürzerem Verteilungszeitraum.

**f) Fehlender Leistungszeitraum bei Lizenzposition:**
Position geht in REVIEW. **Fallback-Annahme (konfigurierbar):** 12 Monate ab Rechnungsdatum. Der Reviewer muss den Zeitraum manuell bestätigen.

**g) Gemischte Rechnungen (Lizenzen + Einmalleistungen):**
Jede Position wird einzeln bewertet. Lizenzen → Abgrenzung, Einmalleistungen → Sofort-Erlös. Es werden **separate Buchungsvorschläge** pro Verteilungstyp generiert. Review-Flag `MIXED_REVENUE_TYPES`.

### 4.5 Cashflow-Abbildung

#### 4.5.1 Fachliche Trennung

| Dimension | GuV / Erlöserfassung | Cashflow / Liquidität |
|---|---|---|
| **Zeitpunkt** | Verteilt über Leistungszeitraum | Einmalig bei Zahlungseingang |
| **Betrag** | Netto, anteilig pro Monat | Brutto (inkl. MwSt), Gesamtbetrag |
| **Konto** | 4400 Erlöse / 4900 PRA | 1200 Forderungen / 1800 Bank |
| **Trigger** | Automatisch per Scheduler (monatlich) | Manuell oder per Bankabgleich |

#### 4.5.2 Buchung bei Zahlungseingang

| Konto | Soll | Haben | Bemerkung |
|---|---|---|---|
| 1800 Bank | 14.280,00 € | | Zahlungseingang brutto |
| 1200 Forderungen aus L+L | | 14.280,00 € | Forderung ausgeglichen |

#### 4.5.3 Systemmodell für die Trennung

Im Datenmodell werden **zwei separate Entitäten** geführt:

**`RevenueSchedule`** (Erlösverteilung):
- Enthält die monatlichen Erlösbuchungen (Soll-Plan)
- Felder: `invoiceId`, `lineItemId`, `month`, `amount`, `status` (`PLANNED`/`BOOKED`/`CANCELLED`)
- Wird automatisch aus dem Leistungszeitraum generiert
- Monatlicher Batch-Job löst fällige Einträge auf (PRA → Erlös)

**`CashflowEntry`** (Zahlungstracking):
- Enthält die Zahlungsinformation
- Felder: `invoiceId`, `expectedDate` (= dueDate), `actualDate` (bei Eingang), `grossAmount`, `status` (`OPEN`/`RECEIVED`/`OVERDUE`/`WRITTEN_OFF`)
- Wird aus dem Fälligkeitsdatum generiert
- Status wechselt bei tatsächlichem Geldeingang auf `RECEIVED`

Beide Entitäten referenzieren die Rechnung, sind aber **unabhängig** voneinander.

> **Abgleich mit bestehendem Code:** Der bestehende `BookingSuggestion`-Mechanismus wird um `RevenueSchedule` und `CashflowEntry` ergänzt. Die `BookingSuggestion` bleibt der zentrale Buchungsvorschlag, aber sie verweist jetzt zusätzlich auf den `RevenueSchedule` (wenn Abgrenzung nötig) und den `CashflowEntry` (für Zahlungstracking).

#### 4.5.4 Auswirkung auf die Cashflow-Berechnung

- Veränderungen des PRA-Kontos (4900/3900) = **nicht-cashflow-wirksam** (reine GuV-Buchungen)
- Veränderungen der Forderungen (1200/1400) = **Cashflow aus laufender Geschäftstätigkeit**
- Zahlungseingang (Bank-Buchung) = einziger **Cashflow-Zufluss**

Das `RevenueSchedule` erscheint **nicht** in der Cashflow-Übersicht. Die Abgrenzungsbuchungen (PRA ↔ Erlös) sind reine GuV-Buchungen ohne Liquiditätswirkung.

---

## 5. Konkrete Änderungsbedarfe im System

### 5.1 Backend-Komponenten

| Komponente | Änderung | Priorität |
|---|---|---|
| **DocumentClassifier (NEU)** | Neue Komponente nach OCR-Schritt. Regelbasierte Klassifikation mit gewichtetem Scoring (6 Regeln) + AI-Fallback. Output: `DocumentType` + `ClassificationConfidence`. | MVP |
| **ExtractionService** | Erweiterung des `document_extraction`-Prompts um Ausgangsrechnungs-Felder. Neue Felder: `customer.*`, `servicePeriod*`, `productCategory`, `billingInterval`, `reverseCharge`, `dueDate`. | MVP |
| **ValidationService (NEU)** | 10 Validierungsregeln (V-01 bis V-10) mit Severity und Review-Gründen. Wird nach Extraktion ausgeführt. | MVP |
| **BookingProposalService** | Erweiterte Buchungslogik: Erkennung Ausgangsrechnung → Erlöskonten statt Aufwandskonten. PRA-Berechnung bei Abgrenzung. Separate Buchungsvorschläge für Sofort-Erlös vs. Abgrenzung. | MVP |
| **RevenueScheduleService (NEU)** | Monatliche Erlösverteilung generieren und verwalten. Tagesgenaue Berechnung. Batch-Job für automatische Auflösung (fällige `PLANNED`-Einträge → JournalEntry → `BOOKED`). | MVP |
| **CashflowService (NEU)** | Zahlungszufluss-Tracking. Integration mit Bankabgleich. Mahnwesen-Trigger bei Überschreitung des Fälligkeitsdatums. | Phase 2 |
| **ProductMappingService (NEU)** | Konfigurierbare Zuordnung Produktname → Kategorie → Erlöskonto. Admin-UI für Pflege. | MVP |
| **Domänenmodell / DTOs** | Neue Entitäten: `RevenueScheduleEntry`, `CashflowEntry`. Erweiterung `Document` um `DocumentType`. Neues Enum `DocumentType`. Erweiterung `LineItemResult` um servicePeriod-Felder. | MVP |

> **Abgleich mit bestehendem Code:**
> - `DocumentProcessingConsumer.cs`: Die Verarbeitungspipeline wird um die Klassifikation (nach Textakquise, vor Extraktion) und die Erlösabgrenzung (nach Buchungsvorschlag) erweitert.
> - `MatchEntityFields`: Wird zum Kern des `DocumentClassifier` refactored. IBAN-Abgleich und gewichtetes Scoring werden ergänzt.
> - `AiPromptsSeed.cs`: Beide Prompts (`document_extraction`, `document.booking_suggestion`) werden erweitert.
> - `IAiService.cs`: `DocumentExtractionResult` und `LineItemResult` erhalten die neuen Felder.
> - `StoreDocumentFields`: Erweitert um Persistierung aller neuen Felder als `DocumentField`-Einträge.

### 5.2 API-Anpassungen

| Endpunkt | Änderung |
|---|---|
| `POST /api/documents/upload` | Response enthält neu: `documentType`, `classificationConfidence` |
| `GET /api/documents/{id}` | Erweitert um `documentType`, `customer`-Block, LineItems mit `servicePeriod`, `productCategory` |
| `GET /api/documents/{id}/booking-proposal` | Neues Response-Format: `revenueEntries[]` + `cashflowEntry` zusätzlich zu bisherigen `bookingEntries[]` |
| `GET /api/documents/{id}/revenue-schedule` | **NEU:** Liefert die monatliche Erlösverteilung |
| `POST /api/documents/{id}/review` | Erweitert um neue `reviewReasons` (s. Abschnitt 4.3) |
| `GET /api/accounting/revenue-schedules?month=YYYY-MM` | **NEU:** Alle fälligen Erlösauflösungen für einen Monat |
| `POST /api/accounting/revenue-schedules/{entryId}/post` | **NEU:** Manuelle Auflösung einer einzelnen Entry |
| `POST /api/accounting/revenue-schedules/post-all` | **NEU:** Alle fälligen Entries des Monats buchen |
| `GET /api/accounting/cashflow?from=...&to=...` | **NEU:** Cashflow-Übersicht für Zeitraum (Phase 2) |
| `GET /api/admin/product-mappings` | **NEU:** Produktkategorie-Zuordnungen verwalten |
| `PUT /api/admin/product-mappings` | **NEU:** Mapping-Tabelle aktualisieren |

### 5.3 Frontend-Anpassungen

| Änderung | Bereich | Beschreibung |
|---|---|---|
| **Dokumenttyp-Anzeige** | `DocumentDetail.tsx` | Farbliche Kennzeichnung Eingang/Ausgang. Kundenblock statt Lieferantenblock bei Ausgangsrechnungen. |
| **Positionsdetails** | `DocumentDetail.tsx` | Leistungszeitraum-Anzeige pro Position. Produktkategorie als Badge. |
| **Erlösverteilung** | `DocumentDetail.tsx` (neuer Abschnitt) | Tabellarische Darstellung der monatlichen Erlösverteilung. Visualisierung als Zeitleiste oder Balkendiagramm. |
| **Review-UI** | `DocumentDetail.tsx` | Neue Review-Gründe anzeigbar. Möglichkeit, fehlenden Leistungszeitraum manuell nachzutragen. |
| **Dokumentfilter** | `DocumentArchive.tsx` | Neuer Filter: Dokumenttyp (Eingang / Ausgang / Alle). |
| **Erlösübersicht** | `DeferredRevenueOverview.tsx` (NEU) | Alle aktiven Abgrenzungspläne, fällige Auflösungen, Batch-Posting. |
| **Dashboard** | Dashboard-Seite | Neues Widget: Offene Erlösabgrenzungen (PRA-Saldo). Cashflow-Prognose basierend auf offenen Forderungen. |
| **Admin: Produktmapping** | Admin-Bereich (NEU) | Konfigurierbare Zuordnung Produktname → Kategorie → Erlöskonto. |
| **Hooks** | `useDocuments.ts` | Neue Hooks: `useRevenueSchedule(documentId)`, `usePostRevenueEntry()`, `usePendingRevenueEntries()`, `useProductMappings()` |
| **Types** | `types/document.ts` | Neue Types: `RevenueScheduleEntry`, `CashflowEntry`, erweiterte `LineItem`, `DocumentType`-Enum |
| **Lokalisierung** | `locales/{de,en,ru}/documents.json` | Übersetzungsschlüssel für alle neuen Felder, Review-Gründe, Dokumenttypen |

### 5.4 Neue Felder, Stati und Flags

| Element | Typ | Werte / Beschreibung |
|---|---|---|
| `documentType` | Enum (Document) | `INCOMING_INVOICE`, `OUTGOING_INVOICE`, `CREDIT_NOTE_INCOMING`, `CREDIT_NOTE_OUTGOING`, `UNKNOWN` |
| `classificationConfidence` | decimal (Document) | 0.0–1.0, Schwellwert Auto-Approval: 0.8 |
| `productCategory` | Enum (LineItem) | `SAAS_LICENSE`, `ON_PREM_LICENSE`, `HOSTING`, `MAINTENANCE`, `ONE_TIME_SERVICE`, `DISCOUNT` |
| `billingInterval` | Enum (LineItem) | `ANNUAL`, `MONTHLY`, `ONE_TIME` |
| `servicePeriodStart` | DateOnly (LineItem) | Beginn des Leistungszeitraums |
| `servicePeriodEnd` | DateOnly (LineItem) | Ende des Leistungszeitraums |
| `reverseCharge` | bool (Document) | `true` bei EU-/Drittland-Lieferung |
| `revenueScheduleStatus` | Enum (RevenueScheduleEntry) | `PLANNED`, `BOOKED`, `CANCELLED` |
| `cashflowStatus` | Enum (CashflowEntry) | `OPEN`, `RECEIVED`, `OVERDUE`, `WRITTEN_OFF` |

**Neue Review-Gründe:**

| Key | Beschreibung | Severity |
|---|---|---|
| `CLASSIFICATION_UNCERTAIN` | Klassifikations-Score zwischen 50–79 | WARNING |
| `MISSING_INVOICE_NUMBER` | Rechnungsnummer fehlt oder ungültig | ERROR |
| `INVALID_DATE` | Rechnungsdatum in der Zukunft | ERROR |
| `INVALID_AMOUNT` | Gesamtbetrag ≤ 0 | ERROR |
| `MISSING_SERVICE_PERIOD` | Lizenzposition ohne Leistungszeitraum | WARNING |
| `INVALID_SERVICE_PERIOD` | Ende vor Start | ERROR |
| `UNUSUAL_SERVICE_PERIOD` | Laufzeit > 36 Monate | WARNING |
| `AMOUNT_MISMATCH` | Positionssumme ≠ Zwischensumme | ERROR |
| `VAT_MISMATCH` | MwSt-Berechnung weicht ab (> 0,02 €) | WARNING |
| `REVERSE_CHARGE_VAT_CONFLICT` | Reverse Charge aber MwSt > 0% | ERROR |
| `MISSING_CUSTOMER_VAT` | EU-Kunde ohne USt-ID | WARNING |
| `MIXED_REVENUE_TYPES` | Lizenzen + Einmalleistungen auf einer Rechnung | WARNING |
| `HIGH_VALUE_OUTGOING_INVOICE` | Betrag über konfigurierter Schwelle | WARNING |
| `INCONSISTENT_SERVICE_PERIODS` | Leistungszeiträume der Positionen weichen ab | WARNING |
| `FOREIGN_CURRENCY_OUTGOING` | Nicht-EUR-Ausgangsrechnung | WARNING |

### 5.5 BusinessPartner als Debitor bei Ausgangsrechnungen

#### 5.5.1 Ist-Zustand

Das `BusinessPartner`-Entity verfügt bereits über die Felder `IsCreditor` (bool) und `IsDebtor` (bool) sowie einen typbasierten Nummernkreis: `K-XXXXX` (Kreditor), `D-XXXXX` (Debitor), `KD-XXXXX` (beides). Zudem existiert ein `DefaultRevenueAccountId`, das für Ausgangsrechnungen genutzt werden kann.

**Problem:** Die Auto-Create-Logik im `DocumentProcessingConsumer` verwendet ausschließlich die Vendor-Daten der Extraktion und erstellt immer einen **Kreditor** (`isCreditor=true, isDebtor=false`, Prefix `K-`). Bei Ausgangsrechnungen ist der Vendor aber die eigene Firma — der relevante Partner ist der **Rechnungsempfänger (Debitor/Kunde)**.

#### 5.5.2 Lösung

Der Partner-Matching-/Erstellungscode im Consumer wird basierend auf `documentDirection` verzweigt:

```
isOutgoing = classification.Direction == "outgoing"

// Partner-Daten je nach Richtung
partnerName   = isOutgoing ? extraction.RecipientName   : extraction.VendorName
partnerTaxId  = isOutgoing ? extraction.RecipientTaxId  : extraction.VendorTaxId
partnerIban   = isOutgoing ? null                       : extraction.VendorIban
partnerStreet = isOutgoing ? extraction.RecipientStreet  : extraction.VendorStreet
partnerCity   = isOutgoing ? extraction.RecipientCity    : extraction.VendorCity
// ... analog für PostalCode, Country

// Bei Auto-Create (MatchType.None):
isCreditor = !isOutgoing    // false bei Ausgangsrechnung
isDebtor   = isOutgoing     // true bei Ausgangsrechnung
prefix     = isOutgoing ? "D-" : "K-"
```

**Keine Signaturänderung** an `IBusinessPartnerMatchingService.MatchPartnerAsync` nötig — der `vendorName`-Parameter akzeptiert bereits jeden Partnernamen. Die 5-Stufen-Matching-Pipeline (RecurringPattern → TaxId → IBAN → ExactName → FuzzyName) funktioniert richtungsunabhängig.

**Wichtig:** Bei Ausgangsrechnungen steht keine IBAN des Empfängers auf dem Beleg (der Kunde gibt seine IBAN nicht an). Daher wird `partnerIban = null` gesetzt, und das IBAN-Matching (Stufe 2) entfällt.

### 5.6 RecurringPattern für Ausgangsrechnungen

#### 5.6.1 Ist-Zustand

`RecurringPattern` matcht aktuell auf `VendorName` (case-insensitive, exakter Match). Das Feld speichert immer den Lieferantennamen. Für Ausgangsrechnungen muss stattdessen auf den **Kundennamen** gematcht werden.

#### 5.6.2 Lösung: `DocumentDirection`-Feld als Diskriminator

- Neues Feld `DocumentDirection` (string, Default `"incoming"`) auf `RecurringPattern`
- Das bestehende `VendorName`-Feld speichert je nach Richtung den Lieferanten- ODER Kundennamen
- Die Matching-Query in `TryAutoBookAsync` wird erweitert:

```
// Vorher:
p.VendorName.ToLower() == vendorName.ToLower()

// Nachher:
p.VendorName.ToLower() == partnerName.ToLower()
&& p.DocumentDirection == direction
```

- `BookingPatternLearnerService.LearnFromDecisionAsync` erhält einen neuen Parameter `documentDirection` und setzt diesen auf neu erstellte Patterns
- Bestehende Patterns behalten Default `"incoming"` → **kein Breaking Change**

**Auto-Booking für Ausgangsrechnungen im MVP:**
Nach 3 manuell freigegebenen Rechnungen desselben Kunden wird das Pattern aktiv und bucht automatisch — identisch zum bestehenden Verhalten bei Eingangsrechnungen.

### 5.7 SKR04-Kontenreferenz (verbindlich)

Alle Kontennummern in diesem Konzept beziehen sich auf den **SKR04-Kontenrahmen**. Der bestehende Booking-Suggestion-Prompt verwendet bereits SKR04 intern.

| Konto | Bezeichnung | Verwendung |
|---|---|---|
| **1400** | Forderungen aus L+L | Soll bei Ausgangsrechnung (Brutto) |
| **3400** | Verbindlichkeiten aus L+L | Haben bei Eingangsrechnung |
| **3806** | Umsatzsteuer 19% | Haben bei AR Inland |
| **3830** | USt §13b UStG | Reverse Charge (Ausgang EU/Drittland) |
| **3900** | Passive Rechnungsabgrenzung (PRA) | Erlösabgrenzung bei Laufzeit > 1 Monat |
| **4110** | Lizenz-/SaaS-Erlöse | Haben bei Software-Lizenzen, Hosting |
| **4400** | Erlöse 19% USt | Haben bei allgemeinen Erlösen |
| **1406** | Vorsteuer 19% | Soll bei Eingangsrechnung |
| **1200** | Bank | Soll bei Zahlungseingang |

> **Hinweis:** Die DB-Seeds verwenden aktuell SKR03-Konten. Der AI-Prompt enthält jedoch eine eigenständige SKR04-Referenz, die für die Buchungsvorschläge maßgeblich ist. Bei der Konto-Auflösung (`GetAccountAsync`) werden die Kontonummern gegen die tatsächlich in der Entity angelegten Konten geprüft.

### 5.8 Datenbank-Migrationen

**Neue Tabellen (Schema `accounting`):**

```sql
-- Erlösverteilungsplan
CREATE TABLE accounting.revenue_schedule_entries (
    id UUID PRIMARY KEY,
    entity_id UUID NOT NULL REFERENCES public.entities(id),
    document_id UUID NOT NULL REFERENCES public.documents(id),
    booking_suggestion_id UUID REFERENCES public.booking_suggestions(id),
    line_item_index INT, -- Referenz auf die Position
    period_date DATE NOT NULL, -- Erster Tag des Monats
    amount DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PLANNED', -- PLANNED, BOOKED, CANCELLED
    journal_entry_id UUID REFERENCES accounting.journal_entries(id),
    posted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Cashflow-Tracking
CREATE TABLE accounting.cashflow_entries (
    id UUID PRIMARY KEY,
    entity_id UUID NOT NULL REFERENCES public.entities(id),
    document_id UUID NOT NULL REFERENCES public.documents(id),
    expected_date DATE NOT NULL, -- Fälligkeitsdatum
    actual_date DATE, -- Tatsächlicher Zahlungseingang
    gross_amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN', -- OPEN, RECEIVED, OVERDUE, WRITTEN_OFF
    journal_entry_id UUID REFERENCES accounting.journal_entries(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Produkt-Kategorie-Mapping
CREATE TABLE accounting.product_category_mappings (
    id UUID PRIMARY KEY,
    entity_id UUID NOT NULL REFERENCES public.entities(id),
    product_name_pattern VARCHAR(500) NOT NULL, -- Regex oder Keyword
    product_category VARCHAR(50) NOT NULL, -- SAAS_LICENSE, ON_PREM_LICENSE, etc.
    revenue_account_id UUID REFERENCES accounting.accounts(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Bestehende Tabellen erweitern:**

```sql
-- Document-Tabelle
ALTER TABLE public.documents
    ADD COLUMN document_type INT NOT NULL DEFAULT 0, -- 0=IncomingInvoice
    ADD COLUMN classification_confidence DECIMAL(3,2),
    ADD COLUMN reverse_charge BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN due_date DATE,
    ADD COLUMN order_number VARCHAR(255);
```

---

## 6. Risiken und Sonderfälle

| Sonderfall | Risiko | Empfohlene Behandlung |
|---|---|---|
| **Fehlender Leistungszeitraum** | Keine periodengerechte Verteilung möglich | REVIEW + Fallback-Annahme 12 Monate ab Rechnungsdatum. Reviewer bestätigt/korrigiert. |
| **Unklare Laufzeit** ("Laufzeit 12 Monate" ohne Datum) | Start-/Enddatum nicht präzise | Ableitung: Start = Rechnungsdatum, Ende = Start + 12 Monate. REVIEW bei Abweichung. |
| **Teilzeiträume** (z.B. 6 Monate) | Falsche Verteilung wenn als 12 Monate angenommen | Explizite Extraktion der Datumsangaben. Bei Laufzeit < 6 oder > 24 Monate → REVIEW. |
| **Gutschriften** | Negative Beträge, invertierte Buchungslogik | Phase 2: Eigener DocumentType `CREDIT_NOTE_OUTGOING`. Storno der Erlösverteilung. |
| **Kündigungen während Laufzeit** | Bereits geplante Erlöse müssen storniert werden | Phase 2: Storno-Funktion auf RevenueSchedule. Offene `PLANNED`-Einträge → `CANCELLED`. |
| **Abweichende Abrechnungsintervalle** | Quartal, Halbjahr statt Jahr/Monat | `billingInterval` erweitern um `QUARTERLY`, `SEMI_ANNUAL`. Verteilungslogik analog. |
| **Netto/Brutto-Unklarheiten** | Falsche MwSt-Berechnung | Validierung V-08 (Toleranz 0,02 €). Bei Abweichung → REVIEW. |
| **Mehrere Währungen (EUR/USD)** | Wechselkursproblematik | Phase 2: Währungsfeld pro Rechnung. Umrechnung zum Tageskurs bei Nicht-EUR. Im MVP: REVIEW. |
| **Rechnungen mit Score 50–79** | Falsche Klassifikation | REVIEW-Pflicht. Manueller Override durch Sachbearbeiter mit Audit-Trail. |
| **Staffelrabatt ohne klare Zuordnung** | Rabatt keiner Position zuordenbar | Verteilung proportional auf alle Lizenzpositionen der gleichen Rechnung. |
| **Kostenlose Positionen (0,00 €)** | Keine buchhalterische Relevanz | Werden extrahiert, aber nicht verbucht. Dienen der Vertragsdokumentation. |
| **Angebrochene Monate** (z.B. Laufzeit 09.05–16.04) | Ungleiche Monatsbeträge | Tagesgenaue Berechnung: Nettobetrag / Gesamttage × Tage im Monat. |

---

## 7. Empfohlener Umsetzungsschnitt

### Phase 1: MVP (Sprint 1–4)

**Ziel:** Ausgangsrechnungen korrekt erkennen und mit periodengerechter Erlösverteilung buchen.

**Sprint 1 — Klassifikation:**
- `DocumentClassifier` implementieren mit den 6 gewichteten Regeln
- Neues `DocumentType`-Enum und Feld im Domänenmodell
- Anpassung der API (`POST /api/documents/upload`, `GET /api/documents/{id}`)
- Migration: `document_type`, `classification_confidence`, `reverse_charge`, `due_date` auf `documents`-Tabelle
- Frontend: Dokumenttyp-Anzeige in `DocumentDetail.tsx` und Filter in `DocumentArchive.tsx`

**Sprint 2 — Extraktion + Prompt-Formulierung:**
- Erweiterung `document_extraction`-Prompt um Ausgangsrechnungs-Template
- Neue Felder in `DocumentExtractionResult` und `LineItemResult`
- `ProductMappingService` mit initialer aqua-cloud-Konfiguration
- Migration: `product_category_mappings`-Tabelle
- Erweiterung `StoreDocumentFields` für alle neuen Felder
- Frontend: Positionsdetails mit Leistungszeitraum und Produktkategorie
- **Booking-Suggestion-Prompt ausformulieren** (Sprint-2-Deliverable, parallel zu Extraktion):
  - REGEL 13: Ausgangsrechnungen-Buchungslogik (Erlöskontenselektion 4110/4400, Forderungskonto 1400, USt 3806, Reverse Charge steuerfrei)
  - REGEL 14: Passive Rechnungsabgrenzung (PRA) bei Laufzeit > 1 Monat (1. Monatsrate sofort Erlös, Rest auf 3900 PRA, USt sofort vollständig)
  - BEISPIEL 6–8: Standard AR Inland 19%, AR mit 12-Monats-Lizenz → PRA, AR Reverse Charge EU
  - Output-Schema erweitern um `revenue_schedule`-Array, `deferred_revenue_account`, `service_period_start/end`
  - Prompt wird in `AiPromptsSeed.cs` → `document.booking_suggestion` ergänzt (nicht ersetzt)

**Sprint 3 — Buchungslogik:**
- `BookingProposalService` für Ausgangsrechnungen (Erlöskonten statt Aufwandskonten)
- `RevenueScheduleService`: Lineare Erlösverteilung mit tagesgenauer Berechnung
- Batch-Job für automatische monatliche Auflösung (fällige `PLANNED` → `BOOKED`)
- Validierungsregeln V-01 bis V-10
- Migration: `revenue_schedule_entries`-Tabelle
- API: `GET /api/documents/{id}/revenue-schedule`

**Sprint 4 — Integration & Review:**
- Erweiterte Review-Gründe im Frontend anzeigbar
- Möglichkeit, fehlenden Leistungszeitraum manuell nachzutragen
- Erlösverteilungs-Tabelle in `DocumentDetail.tsx`
- `DeferredRevenueOverview.tsx` (Monatsübersicht aller Abgrenzungen)
- End-to-End-Test mit allen 74 aqua-cloud-Rechnungen
- Lokalisierung (de, en, ru)
- Dashboard-Widget: Offene Erlösabgrenzungen (PRA-Saldo)

### Phase 2: Erweiterungen (Sprint 5–7)

**Sprint 5 — Cashflow-Integration:**
- `CashflowService` implementieren mit `CashflowEntry`-Entity
- Migration: `cashflow_entries`-Tabelle
- Anbindung an Bankabgleich
- API: `GET /api/accounting/cashflow?from=...&to=...`
- Dashboard-Widget: Cashflow-Prognose basierend auf offenen Forderungen

**Sprint 6 — Gutschriften & Storno:**
- `DocumentType.CreditNoteOutgoing` implementieren
- Storno-Logik für RevenueSchedule (offene `PLANNED` → `CANCELLED`)
- Umgang mit Kündigungen (manuelles Storno mit Audit-Trail)
- Teilgutschriften

**Sprint 7 — Reporting & Optimierung:**
- Erlösreporting pro Kunde / Produkt / Zeitraum
- AI-basierter Klassifikations-Fallback trainieren
- Performance-Optimierung Batch-Jobs
- Admin-UI für Produktkategorie-Mapping

### Phase 3: Skalierung (ab Sprint 8)

1. **Mehrwährungsfähigkeit:** Automatische Wechselkursumrechnung für USD-Rechnungen
2. **Weitere Dokumenttypen:** Angebote, Mahnungen, Verträge als zusätzliche `DocumentType`-Werte
3. **Mandantenfähigkeit:** Konfigurierbare Stammdaten pro Mandant für die Klassifikation (nicht nur aqua cloud)
4. **Erlösplanung:** Forecast auf Basis bestehender Verträge und historischer Verlängerungsquoten

---

## 8. Testplan und Akzeptanzkriterien

### 8.1 Testdatenbasis

Die 74 Ausgangsrechnungen aus `aqua cloud Invoices/` (Nummern 2026400001–2026400074) dienen als primäre Testdaten. Alle Akzeptanzkriterien beziehen sich auf diese Belege.

### 8.2 Sprint-1-Akzeptanzkriterien (Klassifikation)

| ID | Kriterium | Erwartung |
|---|---|---|
| T-01 | Alle 74 aqua-cloud-Rechnungen klassifizieren | 74/74 als `outgoing` erkannt |
| T-02 | Classification-Score | Score ≥ 80 bei allen 74 (Auto-Approval-Schwelle) |
| T-03 | Aussteller-Match-Regel | IBAN/TaxId/Name der LegalEntity matcht → +40 Punkte |
| T-04 | Eingangsrechnungen bleiben korrekt | Bestehende Eingangsrechnungen weiterhin als `incoming` klassifiziert |
| T-05 | BusinessPartner-Erstellung | Empfänger als Debitor angelegt (D-XXXXX, IsDebtor=true, IsCreditor=false) |
| T-06 | RecurringPattern-Richtung | Neues Pattern hat `DocumentDirection="outgoing"` |

### 8.3 Sprint-2-Akzeptanzkriterien (Extraktion + Prompt)

| ID | Kriterium | Erwartung |
|---|---|---|
| T-07 | Leistungszeitraum-Erkennung | ≥ 95% der Positionen mit korrekt extrahiertem `ServicePeriodStart/End` |
| T-08 | Fehlender Leistungszeitraum | → `MISSING_SERVICE_PERIOD` Review-Flag gesetzt |
| T-09 | Produktkategorie-Erkennung | aqua Lizenzen → `SAAS_LICENSE`, Hosting → `HOSTING`, Maintenance → `MAINTENANCE` |
| T-10 | Fälligkeitsdatum | Korrekt extrahiert (30 Tage nach Rechnungsdatum bei aqua cloud) |
| T-11 | Reverse-Charge-Erkennung | EU-Kunden mit 0% MwSt → `ReverseCharge=true` |
| T-12 | Booking-Suggestion-Prompt | Prompt ist ausformuliert und in `AiPromptsSeed.cs` integriert |

### 8.4 Sprint-3-Akzeptanzkriterien (Buchungslogik + Erlösverteilung)

| ID | Kriterium | Erwartung |
|---|---|---|
| T-13 | Buchungsvorschlag Soll-Konto | Immer 1400 (Forderungen aus L+L) |
| T-14 | Buchungsvorschlag Haben-Konten | 4110 (Lizenzen) oder 4400 (allgemein), je nach Produktkategorie |
| T-15 | USt bei Inland 19% | 3806 (Umsatzsteuer 19%) im Haben |
| T-16 | Reverse Charge | Keine USt-Buchung bei EU-Kunden mit RC |
| T-17 | PRA bei 12-Monats-Lizenz | 1. Monat Sofort-Erlös, 11 Entries mit Status `planned` |
| T-18 | Tagesgenaue Berechnung | Anbruchmonat proportional berechnet (Tage/Gesamttage × Betrag) |
| T-19 | Validierungsregeln V-01 bis V-10 | Jede Regel mit positivem und negativem Testfall geprüft |

### 8.5 Sprint-4-Akzeptanzkriterien (Integration + E2E)

| ID | Kriterium | Erwartung |
|---|---|---|
| T-20 | End-to-End: Alle 74 PDFs hochladen | Alle durchlaufen Pipeline ohne Fehler |
| T-21 | Klassifikation E2E | 74/74 als OUTGOING_INVOICE (Score ≥ 80) |
| T-22 | Extraktion Stichprobe | 10 zufällige Rechnungen: alle Felder korrekt extrahiert |
| T-23 | Buchungsvorschlag Stichprobe | 5 Rechnungen: korrekte Konten (1400/4110 oder 4400/3806) |
| T-24 | Erlösabgrenzung E2E | 1 Rechnung mit 12-Monats-Lizenz durchbuchen → 12 Entries, tagesgenaue Beträge |
| T-25 | RecurringPattern E2E | Nach Freigabe: Pattern erstellt. 2. Upload → Pattern-Match. 3. Upload → Auto-Booking. |
| T-26 | Debitor-Wiedererkennung | Gleicher Kunde bei 2. Rechnung automatisch erkannt (kein Duplikat) |
| T-27 | Frontend-Anzeige | Richtungs-Badge, Erlösverteilungs-Tabelle, Review-Gründe korrekt dargestellt |
| T-28 | Lokalisierung | Alle neuen Keys in de, en, ru vorhanden und korrekt |

### 8.6 Nicht-funktionale Akzeptanzkriterien

| ID | Kriterium | Erwartung |
|---|---|---|
| T-29 | Keine Regression | Alle bestehenden Eingangsrechnungen funktionieren unverändert |
| T-30 | Migration rückwärtskompatibel | Defaults auf allen neuen Spalten (`incoming`, `false`, `NULL`) |
| T-31 | Performance | Klassifikation < 500ms pro Dokument (ohne AI-Call) |
| T-32 | Frontend Build | `npm run build` ohne Fehler nach jeder Sprint-Lieferung |

---

## 9. Zusammenfassung der Entscheidungen

| Nr. | Entscheidung | Begründung |
|---|---|---|
| E1 | Gewichteter Scoring-Klassifikator (6 Regeln) mit Schwellwerten (≥80 Auto, 50–79 Review, <50 Eingang) statt einfachem deterministischem Match | Differenziertere Erkennung, weniger False Positives, Review-Stufe für Grenzfälle (CFO-Vorgabe) |
| E2 | Formales `DocumentType`-Enum statt String-basierter `DocumentDirection` | Typsicherheit, erweiterbar um Gutschriften, eindeutiger als Strings (CFO-Vorgabe) |
| E3 | Erste Monatsrate sofort als Erlös buchen (nicht alles auf PRA) | Periodengerecht korrekt: Erlös für den laufenden Monat wird sofort realisiert (CFO-Vorgabe) |
| E4 | Tagesgenaue Abgrenzung bereits im MVP | Fachlich korrekt, vermeidet Nacharbeit bei Anbruchmonaten (CFO-Vorgabe) |
| E5 | Explizite `CashflowEntry`-Entity für Zahlungstracking | Ermöglicht Fälligkeitsüberwachung, Mahnwesen-Trigger, Cashflow-Prognose (CFO-Vorgabe) |
| E6 | Konfigurierbare Produkt-Kategorie-Mapping-Tabelle | Mandantenübergreifend nutzbar, keine Hardcoded-Produktnamen (CFO-Vorgabe) |
| E7 | 10 strukturierte Validierungsregeln (V-01 bis V-10) | Nachvollziehbare, dokumentierte Prüflogik mit Severity-Stufen (CFO-Vorgabe) |
| E8 | USt wird zum Rechnungszeitpunkt vollständig fällig | Deutsches UStG: Steuerschuld entsteht mit Rechnungsstellung |
| E9 | Positionen mit Betrag 0,00 € werden nicht verbucht | Keine buchhalterische Relevanz |
| E10 | Fremdwährungsrechnungen gehen im MVP in Review | Wechselkurslogik erst in Phase 3 |
| E11 | Gutschriften erst in Phase 2 | Kein Gutschrift-Material in den 74 Testdokumenten |
| E12 | 4-Sprint-MVP mit inkrementeller Auslieferung | Klassifikation → Extraktion → Buchung → Integration (CFO-Vorgabe) |

---

*Dieses Konzept wurde auf Basis der Analyse von 74 realen Ausgangsrechnungen der aqua cloud GmbH sowie des CFO-Konzepts v1.0 vom 16.03.2026 erstellt. Es liefert eine umsetzungsnahe Grundlage für die Erweiterung des Dokumentenprozesses und dient als Entscheidungsvorlage für die technische und fachliche Abstimmung.*
