# Fixed Asset & Depreciation Management (Anlagenbuchhaltung)

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Overview

Die Anlagenbuchhaltung in Clarity Board verwaltet das gesamte Anlagevermoegen aller Entitaeten: Erfassung, Bewertung, planmaessige Abschreibung (AfA), Sonderabschreibungen und Abgaenge. Alle Buchungen erfolgen HGB-konform mit automatischer DATEV-kompatibeler AfA-Spiegelgenerierung.

---

## 2. Anlageklassen

| Klasse | SKR03 Kontenbereich | Nutzungsdauer (AfA-Tabelle) | AfA-Methode |
|--------|--------------------|-----------------------------|-------------|
| Immaterielle Vermoegensggegenstaende | 0010-0099 | 3-5 Jahre | Linear |
| Grundstuecke & Gebaeude | 0100-0199 | 33 Jahre (Gebaeude) | Linear |
| Technische Anlagen & Maschinen | 0200-0299 | 5-15 Jahre | Linear |
| Betriebs- & Geschaeftsausstattung | 0400-0499 | 3-13 Jahre | Linear |
| IT-Equipment (Computer, Server) | 0650-0699 | 3 Jahre (seit BMF 2021: 1 Jahr fuer Digital) | Linear / Sofort |
| Fahrzeuge | 0320-0399 | 6 Jahre | Linear |
| GWG-Sammelposten | 0485 | 5 Jahre (Pool) | Linear Pool |
| Finanzanlagen | 0500-0599 | Keine planmaessige AfA | Nur ausserplanmaessig |

### Sonderregel: Digitale Wirtschaftsgueter (BMF-Schreiben 2021)

```
Seit 2021 koennen digitale Wirtschaftsgueter mit Nutzungsdauer 1 Jahr abgeschrieben werden:
  - Computer-Hardware (inkl. Peripherie)
  - Software (Betriebs- und Anwendungssoftware)
  - Datenverarbeitungsgeraete

Clarity Board bietet Wahlrecht:
  Option A: Sofortabschreibung im Zugangsjahr (1 Jahr ND)
  Option B: Regulaere AfA ueber 3 Jahre (konservativ)

Konfigurierbar pro Entitaet in den Steuer-Einstellungen.
```

---

## 3. Anlage-Lebenszyklus

### Erfassung

```
Anlagezugang:

1. Automatisch (aus MOSS/Belegerfassung):
   - Beleg mit Nettobetrag > 800 EUR + Hardware/Equipment-Kategorie
   - System schlaegt Aktivierung vor
   - Felder: Bezeichnung, Anschaffungskosten, Anschaffungsdatum,
             Nutzungsdauer, AfA-Methode, Standort, Kostenstelle

2. Manuell:
   - Finance erfasst Anlage direkt
   - Pflichtfelder + optionale Inventarnummer

Buchung bei Zugang:
  Debit  0650 (IT Equipment)                2.500,00 EUR
  Debit  1576 (Vorsteuer 19%)                 475,00 EUR
  Credit 1200 (Bank) oder 1600 (Verb. LuL)  2.975,00 EUR
```

### GWG-Entscheidungsbaum

```
Nettobetrag des Wirtschaftsguts:

  ≤ 250 EUR:     → Sofort-Aufwand (kein Anlagegut)
                    Buchung: Debit 4930 / Credit 1200

  250 - 800 EUR: → GWG (§ 6 Abs. 2 EStG)
                    Option 1: Sofortabschreibung im Zugangsjahr
                    Option 2: Sammelposten (§ 6 Abs. 2a EStG, 5 Jahre linear)
                    Buchung: Debit 0480 / Credit 1200
                    AfA sofort: Debit 4855 / Credit 0480

  > 800 EUR:     → Regulaeres Anlagegut
                    Aktivierung + planmaessige AfA
                    Buchung: Debit 0650 / Credit 1200
                    AfA monatlich: Debit 4830 / Credit 0090
```

### Monatliche Abschreibung

```
Monatliche AfA-Berechnung (automatisiert):

Fuer jede aktive Anlage:
  AfA pro Monat = Anschaffungskosten / (Nutzungsdauer_Monate)

  Zeitanteilig im Zugangsjahr:
    Zugang im Maerz → AfA ab Maerz (10/12 im ersten Jahr)

  Buchung (generiert am 1. des Folgemonats):
    Debit  4830 (Abschreibungen Sachanlagen)     [AfA-Betrag]
    Credit 0090 (Kumulierte AfA Equipment)        [AfA-Betrag]

Sonderfall: Ausserplanmaessige Abschreibung
  Bei dauerhafter Wertminderung (§ 253 Abs. 3 HGB):
    Debit  4840 (Ausserplanmaessige Abschreibung) [Wertminderung]
    Credit 0090 (Kumulierte AfA)                   [Wertminderung]
    → Begruendung + Genehmigung durch Finance erforderlich
```

### Anlagenabgang

```
Abgangsarten:

1. Verkauf:
   Restbuchwert = Anschaffungskosten - Kumulierte AfA
   Erloes > Restbuchwert → Buchgewinn (Ertrag)
   Erloes < Restbuchwert → Buchverlust (Aufwand)

   Beispiel: IT-Equipment, AK 3.000 EUR, kum. AfA 2.000 EUR, Verkauf 500 EUR
   Debit  1200 (Bank)                   500,00 EUR
   Debit  0090 (Kumulierte AfA)       2.000,00 EUR
   Debit  2310 (Verlust Anlagenabgang)  500,00 EUR
   Credit 0650 (IT Equipment)         3.000,00 EUR

2. Verschrottung:
   Debit  0090 (Kumulierte AfA)       [kum. AfA]
   Debit  2310 (Verlust Anlagenabgang) [Restbuchwert]
   Credit 0650 (IT Equipment)          [AK]

3. Diebstahl/Verlust:
   Wie Verschrottung + ggf. Versicherungserstattung als separater Ertrag
```

---

## 4. Anlagenspiegel (DATEV-kompatibel)

### Generierung

Der Anlagenspiegel wird automatisch generiert und ist Pflichtbestandteil des Jahresabschlusses (§ 268 Abs. 2 HGB):

```
Anlagenspiegel per 31.12.2026:

                          AK/HK      Zugaenge   Abgaenge   Umbuch.    AK/HK
                          01.01.                                       31.12.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Immaterielle VG           50.000      10.000          0        0       60.000
Techn. Anlagen           120.000      25.000    -15.000        0      130.000
BGA                       85.000      18.000     -5.000        0       98.000
IT-Equipment             180.000      45.000    -12.000        0      213.000
Fahrzeuge                 60.000           0          0        0       60.000
GWG-Sammelposten          12.000       8.000     -3.000        0       17.000
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summe                    507.000     106.000    -35.000        0      578.000

                          Kum. AfA   Zugaenge   Abgaenge   Auserpl.   Kum. AfA   Buchwert
                          01.01.     (plan.)                           31.12.     31.12.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Immaterielle VG           20.000      12.000          0        0       32.000     28.000
Techn. Anlagen            48.000      13.000     -8.000        0       53.000     77.000
BGA                       34.000       9.800     -3.000        0       40.800     57.200
IT-Equipment              90.000      45.000     -9.000        0      126.000     87.000
Fahrzeuge                 20.000      10.000          0        0       30.000     30.000
GWG-Sammelposten           4.800       3.400     -3.000        0        5.200     11.800
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summe                    216.800      93.200    -23.000        0      287.000    291.000
```

### DATEV-Export des Anlagenspiegels

```
Export-Paket enthaelt zusaetzlich:
  EXTF_Anlagenbuchungen.csv    → Alle AfA-Buchungen der Periode
  Anlagenspiegel.pdf           → Formatierter Anlagenspiegel
  Anlagenverzeichnis.csv       → Einzelaufstellung aller Anlagen
```

---

## 5. KPI-Integration

### Relevante KPIs

| KPI | Berechnung | Auswirkung |
|-----|-----------|-----------|
| Investitionsquote | CapEx / Revenue | Wie viel vom Umsatz investiert wird |
| Abschreibungsquote | AfA / Durchschn. Anlagevermoegen | Alterung des Anlagevermogens |
| Anlagenintensitaet | Anlagevermoegen / Gesamtvermoegen | Kapitalbindung im AV |
| Anlagendeckungsgrad I | Eigenkapital / Anlagevermoegen | Goldene Bilanzregel |
| Anlagendeckungsgrad II | (EK + langfr. FK) / AV | Silberne Bilanzregel |
| Reinvestitionsrate | CapEx / AfA | >1 = Wachstum, <1 = Substanzverzehr |

### Cash-Flow-Impact

```
AfA-Impact auf Cash Flow:
  P&L: AfA mindert Gewinn (nicht zahlungswirksam)
  Cash Flow: AfA wird im OCF zurueckgerechnet (Add-back)
  Steuer: AfA mindert steuerlichen Gewinn → Steuerersparnis

  Steuerersparnis durch AfA = AfA-Betrag * Effektiver Steuersatz
  Beispiel: AfA 93.200 EUR * 30% ≈ 27.960 EUR Steuerersparnis
```

---

## 6. Automatisierung

### Monatliche Jobs

| Job | Zeitpunkt | Beschreibung |
|-----|----------|-------------|
| AfA-Berechnung | 1. des Monats, 03:00 UTC | Berechne monatliche AfA fuer alle aktiven Anlagen |
| AfA-Buchungen | 1. des Monats, 03:30 UTC | Generiere Journal Entries fuer alle AfA-Betraege |
| GWG-Pool-AfA | 1. des Monats, 03:30 UTC | 1/60 des Sammelpostens abschreiben |
| Vollabschreibungs-Check | 1. des Monats, 04:00 UTC | Anlagen mit Restbuchwert 0 markieren |

### Jaehrliche Jobs

| Job | Zeitpunkt | Beschreibung |
|-----|----------|-------------|
| Anlagenspiegel | Nach Jahresabschluss | Generiere Anlagenspiegel fuer alle Entitaeten |
| Inventur-Reminder | November | Erinnerung an physische Inventur (falls relevant) |
| Nutzungsdauer-Review | Dezember | Pruefen, ob Nutzungsdauern noch angemessen |

---

## Document Navigation

- Previous: [GetMOSS Integration](./22-getmoss-integration.md)
- Next: [Regulatory Reporting](./24-regulatory-reporting.md)
- [Back to Index](./README.md)
