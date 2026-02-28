# GetMOSS Integration

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Overview

GetMOSS (getmoss.com) ist eine Plattform fuer virtuelle Kreditkarten und Ausgabenmanagement. Clarity Board integriert MOSS als Datenquelle fuer:

- Import virtueller Kreditkarten-Transaktionen
- Automatischer Belegabgleich (Receipt Matching)
- Echtzeit-Ausgabenueberwachung pro Mitarbeiter / Abteilung
- Automatisierte Buchungsvorschlaege aus Kartentransaktionen

---

## 2. Data Import via MOSS API

### Synchronisierte Daten

| Datentyp | Beschreibung | Frequenz |
|----------|-------------|----------|
| **Transaktionen** | Alle Kartenumsaetze (Betrag, Haendler, Datum, Kategorie) | Echtzeit via Webhook |
| **Belege** | Zugeordnete Belege/Fotos zu Transaktionen | Echtzeit via Webhook |
| **Karten** | Virtuelle Karten (Inhaber, Limit, Abteilung, Ablaufdatum) | Taegliche Synchronisation |
| **Budgets** | MOSS-interne Budgetzuweisungen pro Karte/Team | Taegliche Synchronisation |
| **Mitarbeiter** | Karteninhaber mit Abteilungszuordnung | Taegliche Synchronisation |

### Webhook-Events von MOSS

| Event | Trigger | Clarity Board Aktion |
|-------|---------|---------------------|
| `transaction.created` | Neue Kartentransaktion | Transaktions-Record anlegen, Buchungsvorschlag generieren |
| `transaction.updated` | Status-Aenderung (z.B. Autorisierung → Settlement) | Record aktualisieren, finale Buchung auslosen |
| `transaction.declined` | Karte abgelehnt | Info-Alert an Finance |
| `receipt.uploaded` | Beleg zu Transaktion zugeordnet | Beleg in Document Capture Pipeline einschleusen |
| `receipt.missing` | Beleg ueberfaellig (>5 Tage nach Transaktion) | Erinnerung an Karteninhaber, Warning an Finance |
| `card.created` | Neue virtuelle Karte erstellt | Karten-Registry aktualisieren |
| `card.limit_changed` | Kartenlimit geaendert | Budget-Tracking aktualisieren |
| `budget.exceeded` | MOSS-Budget ueberschritten | Alert an Finance + Abteilungsleiter |

### Daten-Mapping

```
MOSS Transaktion → Clarity Board Buchung:

MOSS Felder:                    Clarity Board Mapping:
─────────────────────────────────────────────────────────
merchant_name                →  Vendor (automatisches Matching)
amount                       →  Buchungsbetrag
currency                     →  Originalwaehrung + EUR-Umrechnung
category (MOSS)              →  HGB-Konto (via Mapping-Tabelle)
card_holder                  →  Kostenstelle (via Mitarbeiter → Abteilung)
transaction_date             →  Buchungsdatum
settlement_date              →  Zahlungsdatum (Cash-Flow-relevant)
vat_amount (wenn erkannt)    →  USt-Betrag + BU-Schluessel
receipt_url                  →  Beleg-Referenz (GoBD-konform gespeichert)
```

### Kategorie-Mapping (MOSS → HGB-Konto)

| MOSS Kategorie | HGB Konto (SKR03) | Beschreibung |
|---------------|-------------------|-------------|
| Software & SaaS | 4964 | Cloud-Infrastruktur / IT-Kosten |
| Travel & Transport | 4660 | Reisekosten |
| Hotels & Accommodation | 4666 | Uebernachtungskosten |
| Office Supplies | 4930 | Buerokosten |
| Marketing & Advertising | 4600 | Werbekosten |
| Food & Beverages | 4654 | Bewirtungskosten (70% absetzbar) |
| Education & Training | 4945 | Fortbildungskosten |
| Subscriptions | 4964 | IT-Kosten / Abonnements |
| Hardware | 0650 | IT-Equipment (>800 EUR netto: Aktivierung) |
| Other | 4900 | Sonstige Betriebsausgaben |

**Bewirtungskosten-Sonderlogik:**
```
Wenn Kategorie = "Food & Beverages" UND Betrag > 50 EUR:
  → 70% auf 4654 (Bewirtungskosten, steuerlich absetzbar)
  → 30% auf 4655 (Nicht abzugsfaehige Bewirtung)
  → Pflichtfeld: Bewirtungsanlass + Teilnehmer
  → Warnung wenn Anlass/Teilnehmer fehlen
```

---

## 3. Automatische Buchungslogik

### Standard-Buchung (Kartentransaktion mit 19% USt)

```
Beispiel: AWS-Rechnung 119 EUR brutto via MOSS-Karte

Debit  4964 (Cloud Infrastructure)      100.00 EUR
Debit  1576 (Vorsteuer 19%)              19.00 EUR
Credit 1200 (Bank / Kreditkartenkonto)  119.00 EUR
```

### Settlement-basierte Buchung

MOSS arbeitet mit Autorisierung und Settlement:

```
Phase 1: Autorisierung (Transaktion erstellt)
  → Vormerkung in Clarity Board (kein Journal Entry)
  → Betrag reserviert in Cash-Flow-Forecast

Phase 2: Settlement (Transaktion abgerechnet)
  → Finale Buchung erstellen
  → Cash-Flow-Impact verbuchen
  → KPIs aktualisieren

Phase 3: MOSS-Monatsabrechnung (Kreditkarten-Settlement)
  → Sammelzahlung an MOSS
  → Abstimmung Einzeltransaktionen vs. Gesamtbetrag
  → Differenz-Behandlung (Gebuehren, FX)
```

### Schwellenwert-Regeln

| Betrag | Regel | Aktion |
|--------|-------|--------|
| < 50 EUR | Kleinbeleg | Vereinfachte Buchung, kein Pflichtbeleg |
| 50-250 EUR | Standard | Normaler Buchungsvorschlag, Beleg erwartet |
| 250-1.000 EUR | Erweitert | Buchungsvorschlag + Kostenstellen-Pflicht |
| > 1.000 EUR | Genehmigung | Finance-Freigabe vor finaler Buchung |
| > 800 EUR netto (Hardware) | Aktivierung | Pruefe Aktivierungspflicht (GWG vs. Anlagegut) |

---

## 4. Receipt Matching & Compliance

### Belegabgleich-Prozess

```
Belegabgleich-Workflow:

1. MOSS meldet Transaktion (Webhook)
2. Clarity Board erstellt Vormerkung
3. Beleg-Status tracken:
   - Beleg vorhanden → Automatischer Abgleich
   - Beleg fehlt → Erinnerung nach 3 Tagen
   - Beleg fehlt → Eskalation nach 7 Tagen
   - Beleg fehlt → Finance-Alert nach 14 Tagen

4. Belegpruefung:
   - Betrag auf Beleg = Transaktionsbetrag (±5% Toleranz fuer Trinkgeld/Rundung)
   - Datum plausibel (±3 Tage)
   - USt ausgewiesen (fuer Vorsteuerabzug)
   - Empfaenger/Haendler stimmt ueberein

5. Wenn Beleg fehlt nach 30 Tagen:
   - Eigenbeleg-Erstellung anbieten
   - Warnung: Kein Vorsteuerabzug ohne ordnungsgemaessen Beleg
```

### GoBD-Anforderungen fuer MOSS-Belege

| Anforderung | Umsetzung |
|-------------|-----------|
| Unveraenderbarkeit | Belege werden bei Import SHA-256-gehasht und unveraenderbar gespeichert |
| Vollstaendigkeit | Jede MOSS-Transaktion erhaelt einen Beleg-Status (vorhanden/fehlend) |
| Nachvollziehbarkeit | Transaktion → Beleg → Buchung vollstaendig verlinkt |
| Aufbewahrung | 10 Jahre ab Ende des Geschaeftsjahres |
| Maschinelle Lesbarkeit | OCR-Extraktion + strukturierte Speicherung parallel zum Original |

---

## 5. Budget-Integration

### MOSS-Budgets → Clarity Board Budgets

```
Synchronisation:

MOSS Budget:
  Team "Engineering": 10.000 EUR/Monat
  Karte "Max M.": 3.000 EUR/Monat
  Karte "Anna K.": 2.000 EUR/Monat

Clarity Board Mapping:
  MOSS Team "Engineering" → Department "Engineering"
  MOSS Budget 10.000 → Unterbudget "Kreditkarten-Ausgaben" im Dept-Budget

  Plan vs. Ist:
    Budget:  10.000 EUR
    MOSS-Ist: 7.800 EUR (78% ausgeschoepft)
    Gesamt-Dept-Budget inkl. anderer Kosten: separat getrackt
```

### Reporting

| Report | Inhalt | Frequenz |
|--------|--------|----------|
| MOSS Transaction Summary | Alle Transaktionen nach Kategorie, Abteilung, Mitarbeiter | Taeglich |
| Fehlende Belege | Offene Transaktionen ohne Beleg | Taeglich |
| Budget-Auslastung | MOSS-Budget vs. Ist pro Team/Karte | Wochentlich |
| Top-Ausgaben | Groesste Einzeltransaktionen + Haendler-Ranking | Monatlich |
| Compliance-Status | GoBD-Konformitaet der Belegdokumentation | Monatlich |

---

## 6. Steuerliche Besonderheiten

### Vorsteuerabzug

- Vorsteuerabzug nur moeglich bei **ordnungsgemaeassem Beleg** (§ 15 UStG)
- MOSS-Transaktionen ohne Beleg: USt wird auf nicht-abzugsfaehig gebucht
- Auslaendische Transaktionen: Reverse-Charge pruefen (§ 13b UStG)
- Kleinbetraege < 250 EUR brutto: Vereinfachte Rechnung ausreichend (§ 33 UStDV)

### GWG-Pruefung (Geringwertige Wirtschaftsgueter)

```
Wenn MOSS-Kategorie = "Hardware" oder "Equipment":
  Nettobetrag < 250 EUR:     → Sofort-Aufwand (4930)
  Nettobetrag 250-800 EUR:   → GWG, Sofortabschreibung moeglich (0480)
  Nettobetrag > 800 EUR:     → Aktivierungspflicht (0650), lineare AfA
```

---

## Document Navigation

- Previous: [Glossary & Appendices](./21-glossary-appendices.md)
- Next: [Fixed Asset & Depreciation Management](./23-fixed-asset-management.md)
- [Back to Index](./README.md)
