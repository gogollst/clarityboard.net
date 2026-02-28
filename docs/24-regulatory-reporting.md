# Regulatory Reporting & Compliance Exports

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Overview

Clarity Board unterstuetzt die Erstellung und den Export aller regulatorisch geforderten Meldungen und Abschluesse. Dies geht ueber den monatlichen DATEV-Export hinaus und umfasst Jahresabschluss, E-Bilanz, steuerliche Meldungen und gesellschaftsrechtliche Pflichten.

---

## 2. Jahresabschluss (Annual Financial Statements)

### Pflichtbestandteile nach HGB

| Bestandteil | Rechtsgrundlage | Clarity Board Modul |
|-------------|----------------|---------------------|
| **Bilanz** | § 242 Abs. 1 HGB | Auto-generiert aus Kontenstaenden |
| **GuV** (Gewinn- und Verlustrechnung) | § 242 Abs. 2 HGB | Auto-generiert aus Erfolgskonten |
| **Anhang** (nur Kapitalgesellschaften) | § 264 Abs. 1 HGB | Template mit automatisch befuellten Daten |
| **Lagebericht** (mittelgrosse/grosse KapGes) | § 264 Abs. 1 HGB | KI-gestuetzter Entwurf basierend auf KPIs |
| **Anlagenspiegel** | § 268 Abs. 2 HGB | Auto-generiert (siehe Dok. 23) |

### Groessenklassen nach § 267 HGB

```
Klarstellung: Zwei von drei Kriterien muessen in zwei aufeinanderfolgenden Jahren erfuellt sein.

                          Klein           Mittel          Gross
Bilanzsumme:              ≤ 6 Mio EUR    ≤ 20 Mio EUR    > 20 Mio EUR
Umsatzerloese:            ≤ 12 Mio EUR   ≤ 40 Mio EUR    > 40 Mio EUR
Mitarbeiter (Durchschn.): ≤ 50           ≤ 250           > 250

Clarity Board:
  - Berechnet automatisch die Groessenklasse pro Entitaet
  - Passt Offenlegungspflichten und Abschlussumfang an
  - Warnt bei Schwellenwert-Naehe (z.B. "Bilanzsumme bei 5,8 Mio EUR")
```

### Bilanz-Gliederung (§ 266 HGB)

```
AKTIVA                                         PASSIVA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A. Anlagevermoegen                             A. Eigenkapital
   I.   Immaterielle VG                           I.   Gezeichnetes Kapital
   II.  Sachanlagen                               II.  Kapitalruecklage
   III. Finanzanlagen                             III. Gewinnruecklagen
                                                  IV.  Gewinnvortrag/Verlustvortrag
B. Umlaufvermoegen                                V.   Jahresueberschuss/-fehlbetrag
   I.   Vorraete
   II.  Forderungen                            B. Rueckstellungen
   III. Wertpapiere                               1. Steuerrueckstellungen
   IV.  Kasse, Bank                               2. Sonstige Rueckstellungen

C. Rechnungsabgrenzungsposten                  C. Verbindlichkeiten
                                                  1. Verb. ggue. Kreditinstituten
D. Aktive latente Steuern                         2. Verb. aus LuL
                                                  3. Sonstige Verbindlichkeiten

                                               D. Rechnungsabgrenzungsposten
                                               E. Passive latente Steuern
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Automatisch generiert aus:
  - Kontenstaenden zum Stichtag (31.12.)
  - Mapping: SKR03-Konto → HGB-Bilanzposition
  - Vorjahresvergleich automatisch beigefuegt
```

### GuV-Gliederung (§ 275 HGB - Gesamtkostenverfahren)

```
1.  Umsatzerloese
2.  Erhoehung/Verminderung des Bestands
3.  Andere aktivierte Eigenleistungen
4.  Sonstige betriebliche Ertraege
5.  Materialaufwand
    a) Aufwendungen fuer Roh-, Hilfs- und Betriebsstoffe
    b) Aufwendungen fuer bezogene Leistungen
6.  Personalaufwand
    a) Loehne und Gehaelter
    b) Soziale Abgaben und Aufwendungen fuer Altersversorgung
7.  Abschreibungen
    a) auf immaterielle VG und Sachanlagen
    b) auf Umlaufvermoegen (soweit unueblich)
8.  Sonstige betriebliche Aufwendungen
9.  Ertraege aus Beteiligungen
10. Ertraege aus Wertpapieren/Ausleihungen des AV
11. Sonstige Zinsen und aehnliche Ertraege
12. Abschreibungen auf Finanzanlagen
13. Zinsen und aehnliche Aufwendungen
14. Steuern vom Einkommen und Ertrag
15. Ergebnis nach Steuern
16. Sonstige Steuern
17. Jahresueberschuss / Jahresfehlbetrag
```

---

## 3. E-Bilanz (§ 5b EStG)

### Pflicht

Seit 2013 muessen alle bilanzierenden Steuerpflichtigen den Jahresabschluss elektronisch an das Finanzamt uebermitteln (E-Bilanz via XBRL-Format).

### Clarity Board Umsetzung

```
E-Bilanz-Erstellung:

1. Datengrundlage: Jahresabschluss (Handelsrecht)
2. Steuerliche Korrekturen (Ueberleitungsrechnung):
   - Unterschiede HGB ↔ Steuerrecht identifizieren
   - z.B. unterschiedliche Bewertung, nicht absetzbare Aufwendungen
   - § 60 Abs. 2 EStDV: Steuerliche Anpassungen dokumentieren

3. Taxonomie-Mapping:
   - HGB-Bilanzpositionen → XBRL-Taxonomie (Steuer-Taxonomie 6.x)
   - Jedes SKR03-Konto hat eine Taxonomie-Position zugeordnet
   - Mindestumfang: Pflicht-Positionen der Taxonomie

4. Export:
   - XBRL-Datei generieren (konform mit ERiC-Schnittstelle)
   - Validierung gegen Taxonomie-Schema
   - Optional: Direktuebermittlung via ELSTER (zukuenftig)
   - Standard: Export fuer Upload durch Steuerberater

5. Ueberleitungsrechnung (Pflicht wenn HB ≠ StB):
   HGB-Bilanz → Steuerliche Korrekturen → Steuer-Bilanz
```

### Mapping-Tabelle (Auszug)

| SKR03 Konto | HGB-Position | E-Bilanz Taxonomie-Position |
|-------------|-------------|---------------------------|
| 1200 | Kasse, Bank | us-gaap:CashAndCashEquivalents (bzw. HGB-Taxonomie) |
| 1400 | Forderungen aus LuL | de-gaap-ci:TradeReceivables |
| 0200-0699 | Sachanlagen | de-gaap-ci:PropertyPlantAndEquipment |
| 8400 | Umsatzerloese 19% | de-gaap-ci:Revenue |

---

## 4. Steuerliche Meldungen

### Umsatzsteuer

| Meldung | Frequenz | Frist | Clarity Board |
|---------|----------|-------|---------------|
| **USt-Voranmeldung** | Monatlich (bei USt > 7.500 EUR/Jahr) oder quartalsmaeig | 10. des Folgemonats | Auto-generiert aus USt-Konten |
| **USt-Jahreserklaerung** | Jaehrlich | 31.07. (mit Berater: 28.02. Folgejahr+1) | Zusammenfassung der VA-Daten |
| **ZM (Zusammenfassende Meldung)** | Monatlich/quartalsmaeig | 25. des Folgemonats | Innergemeinschaftliche Lieferungen |

### Koerperschaftsteuer / Gewerbesteuer

| Meldung | Zustaendig | Clarity Board |
|---------|-----------|---------------|
| **KSt-Erklaerung** | Steuerberater | Datengrundlage liefern (E-Bilanz + GuV) |
| **GewSt-Erklaerung** | Steuerberater | Hinzurechnungen/Kuerzungen vorbereiten |
| **Steuerliche Organschaft** | Steuerberater | Konsolidierte Steuerbasis berechnen |
| **Verrechnungspreisdokumentation** | Steuerberater + Intern | Intercompany-Transaktionen dokumentieren |

### Steuerkalender (automatische Erinnerungen)

```
Steuerkalender 2026:

Jan 10: USt-VA Dezember 2025
Feb 10: USt-VA Januar 2026
Feb 10: LSt-Anmeldung Januar 2026
Mär 10: USt-VA Februar + LSt-Anmeldung Februar
Mär 10: KSt/GewSt-Vorauszahlung Q1
...
Jun 10: KSt/GewSt-Vorauszahlung Q2
Jul 31: Jahressteuererklaerungen Vorjahr (ohne Berater)
Sep 10: KSt/GewSt-Vorauszahlung Q3
Dez 10: KSt/GewSt-Vorauszahlung Q4

Automatische Alerts:
  - 7 Tage vor Frist: Info-Alert an Finance
  - 3 Tage vor Frist: Warning an Finance + CFO
  - Am Fristtag: Critical Alert wenn nicht erledigt
```

---

## 5. Offenlegungspflichten (§ 325 HGB)

### Pflichten nach Groessenklasse

| Groesse | Offenlegung | Frist | Clarity Board |
|---------|-------------|-------|---------------|
| Klein | Verkuerzte Bilanz, kein GuV, kein Lagebericht | 12 Monate nach Abschluss | PDF-Export verkuerzte Bilanz |
| Mittel | Verkuerzte Bilanz + GuV + Anhang (gekuerzt) | 12 Monate | PDF-Export + XBRL |
| Gross | Vollstaendiger Abschluss + Lagebericht | 12 Monate (4 Monate fuer Aufstellung) | PDF + XBRL + Lagebericht-Draft |

### Elektronische Offenlegung

```
Offenlegung via Bundesanzeiger:

1. Clarity Board generiert Abschlussunterlagen
2. Export im XBRL-Format (kompatibel mit Bundesanzeiger-Portal)
3. Alternativ: PDF-Export fuer manuellen Upload
4. Status-Tracking: "Offengelegt" / "Ausstehend" / "Frist laeuft"
5. Alert bei Fristversaeumnis (Ordnungsgeld-Risiko: ab 2.500 EUR)
```

---

## 6. Konzern-Berichterstattung

### Konsolidierter Abschluss

Fuer Holding-Strukturen mit Konsolidierungspflicht (§ 290 HGB):

| Bestandteil | Beschreibung | Automatisierung |
|-------------|-------------|----------------|
| Konzern-Bilanz | Summen-Bilanz nach IC-Eliminierung | Vollautomatisch |
| Konzern-GuV | Summen-GuV nach IC-Eliminierung | Vollautomatisch |
| Konzern-Kapitalflussrechnung | Konsolidierter Cash Flow | Vollautomatisch |
| Konzern-Anhang | Konsolidierungskreis, Methoden | Template + Auto-Daten |
| Segmentberichterstattung | Nach Geschaeftsbereichen/Regionen | Konfigurierbar |

### Konsolidierungsschritte (automatisiert)

```
Konsolidierung:

1. Summenbilanz/-GuV erstellen (alle Entitaeten addieren)
2. Kapitalkonsolidierung (§ 301 HGB)
   → Beteiligungsbuchwert gegen anteiliges EK aufrechnen
3. Schuldenkonsolidierung (§ 303 HGB)
   → IC-Forderungen gegen IC-Verbindlichkeiten eliminieren
4. Aufwands-/Ertragskonsolidierung (§ 305 HGB)
   → IC-Umsaetze und IC-Aufwendungen eliminieren
5. Zwischenergebniseliminierung (§ 304 HGB)
   → Gewinne aus IC-Lieferungen eliminieren
6. Latente Steuern auf Konsolidierungsmassnahmen
7. Konzern-Ergebnis berechnen
```

---

## 7. Verrechnungspreisdokumentation (Transfer Pricing)

### OECD-konforme Dokumentation

```
Transfer-Pricing-Dokumentation:

Fuer jede Intercompany-Beziehung:
  1. Funktionsanalyse: Welche Funktionen, Risiken, Vermoegenswerte
  2. Vergleichbarkeitsanalyse: Fremdvergleich
  3. Methode: CUP, TNMM, Cost-Plus, Resale-Minus, Profit-Split
  4. Preis/Marge: Dokumentiert und begruendet
  5. Monitoring: Tatsaechliche vs. vereinbarte Verrechnung

Clarity Board liefert:
  - Automatische Aggregation aller IC-Transaktionen
  - Margen-Berechnung pro IC-Beziehung
  - Vergleich mit vereinbarten Verrechnungspreisen
  - Alert bei Abweichung > Toleranz (konfigurierbar, Standard 5%)
  - Export fuer Steuerberater / Betriebspruefung
```

---

## 8. Audit-Support

### Betriebspruefungs-Readiness

| Anforderung | Clarity Board Umsetzung |
|-------------|------------------------|
| GDPdU/GoBD-konforme Datentraegerueberlassung | Export aller Buchungen im IDEA-kompatiblen Format |
| Z3-Zugriff (unmittelbarer Datenzugriff) | Read-Only Auditor-Rolle mit voller Datenansicht |
| Lückenlose Belegnummerierung | System-generierte Belegnummern, lueckenlos |
| Verfahrensdokumentation | Automatisch generierte Prozessdokumentation |
| Kontennachweise | Einzelaufstellung pro Konto exportierbar |
| Summen- und Saldenliste | Monatlich und jaehrlich generiert |

---

## Document Navigation

- Previous: [Fixed Asset & Depreciation Management](./23-fixed-asset-management.md)
- Next: [Onboarding & Initial Setup](./25-onboarding-setup.md)
- [Back to Index](./README.md)
