# Onboarding & Initial Setup

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Overview

Dieses Dokument beschreibt den vollstaendigen Onboarding-Prozess fuer neue Unternehmen in Clarity Board: von der Erstregistrierung ueber die Konfiguration bis zur ersten produktiven Nutzung. Ziel: Produktivstart innerhalb von 5 Werktagen.

---

## 2. Onboarding-Phasen

```
Phase 1: REGISTRIERUNG (Tag 1)
  ├── Account-Erstellung (Admin-User)
  ├── Unternehmensbasisdaten erfassen
  ├── Rechtsform waehlen
  └── Kontenrahmen waehlen (SKR03/SKR04)

Phase 2: ENTITAETS-SETUP (Tag 1-2)
  ├── Entitaet(en) anlegen
  ├── Holding-Struktur definieren (falls Multi-Entity)
  ├── Steuerliche Einheiten konfigurieren
  ├── Geschaeftsjahr/Perioden definieren
  └── Waehrung(en) festlegen

Phase 3: KONTEN & STEUER (Tag 2)
  ├── Kontenrahmen pruefen/anpassen
  ├── Individuelle Konten anlegen (falls noetig)
  ├── Steuersaetze konfigurieren
  ├── DATEV-Berater-Nummer hinterlegen
  └── Kostenstellen definieren

Phase 4: EROEFFNUNGSBILANZ (Tag 2-3)
  ├── Eroeffnungsbilanz importieren
  │   ├── Option A: Manuell eingeben
  │   ├── Option B: CSV/Excel-Import
  │   └── Option C: DATEV-Import (Kontensalden)
  ├── Offene Posten (AR/AP) importieren
  ├── Anlagenbestand importieren
  └── Saldenpruefung (Aktiva = Passiva)

Phase 5: INTEGRATIONEN (Tag 3-4)
  ├── Datenquellen-Webhooks einrichten
  │   ├── Billing/Invoicing → Revenue, AR
  │   ├── Banking (API oder CSV) → Cash, Zahlungen
  │   ├── MOSS → Kreditkarten-Transaktionen
  │   ├── HR-System → Personalkosten, Headcount
  │   └── CRM → Pipeline, Conversion
  ├── Webhook-Tests durchfuehren
  ├── Historische Daten importieren (falls vorhanden)
  └── Erste Daten-Synchronisation pruefen

Phase 6: BENUTZER & ROLLEN (Tag 4)
  ├── Benutzer anlegen
  ├── Rollen zuweisen (Finance, Sales, Marketing, HR, Executive)
  ├── Entity-Zugriffe konfigurieren
  ├── 2FA fuer Finance/Admin erzwingen
  └── Erste Benutzer einladen

Phase 7: KONFIGURATION (Tag 4-5)
  ├── KPI-Targets definieren
  ├── Alert-Schwellenwerte setzen
  ├── Wiederkehrende Einnahmen/Kosten konfigurieren
  ├── Budget-Struktur anlegen (falls vorhanden)
  ├── DATEV-Export testen (Probe-Export)
  └── Dashboard-Layout pro Rolle pruefen

Phase 8: GO-LIVE (Tag 5)
  ├── Checkliste durchgehen
  ├── Erster produktiver Monat starten
  ├── Support-Kontakt bestaetigen
  └── Feedback-Zyklus definieren
```

---

## 3. Eroeffnungsbilanz-Import

### Import-Formate

| Format | Beschreibung | Empfohlen fuer |
|--------|-------------|---------------|
| **DATEV-Kontensalden** | Export aus DATEV mit Kontonummer + Saldo | Unternehmen mit bestehendem DATEV-Setup |
| **CSV/Excel** | Freiformat mit Mapping-Assistent | Alle |
| **Manuell** | Einzeleingabe pro Konto | Kleine Unternehmen / Neugruendungen |

### CSV-Import-Format

```csv
Konto;Bezeichnung;Soll;Haben
0650;IT-Equipment;180000.00;
0090;Kum. AfA IT-Equipment;;90000.00
1200;Bank;500000.00;
1400;Forderungen LuL;350000.00;
1500;Geleistete Anzahlungen;12000.00;
1600;Verbindlichkeiten LuL;;188000.00
2000;Rueckstellungen;;45000.00
2100;Bankdarlehen;;200000.00
0800;Gezeichnetes Kapital;;25000.00
0855;Kapitalruecklage;;100000.00
0860;Gewinnvortrag;;394000.00
```

### Validierung

```
Eroeffnungsbilanz-Pruefung:

1. Summe Soll = Summe Haben
   → Hard Block wenn unbalanciert

2. Alle Konten existieren im gewaehlten Kontenrahmen
   → Warning fuer unbekannte Konten

3. Plausibilitaetspruefung:
   → Bank-Saldo nicht negativ (Warning)
   → Eigenkapital vorhanden (Warning)
   → Forderungen und Verbindlichkeiten plausibel

4. Offene-Posten-Abgleich:
   → Summe OP AR = Saldo 1400 (Hard Block)
   → Summe OP AP = Saldo 1600 (Hard Block)

5. Anlagenabgleich:
   → Summe Anlagenbuchwerte = AK - Kum. AfA = Saldo Anlagen-Konten
```

---

## 4. Historischer Daten-Import

### Importierbare historische Daten

| Datentyp | Zweck | Format |
|----------|-------|--------|
| **Monats-GuV** (letzte 12-24 Monate) | KPI-Trends, Vergleichsdaten | CSV/Excel |
| **Monatliche Kontensalden** | Historische Bilanz-Entwicklung | CSV/DATEV |
| **Offene Posten** | AR/AP Aging, DSO-Berechnung | CSV |
| **Anlagenverzeichnis** | Bestehende Abschreibungen fortfuehren | CSV/Excel |
| **Budget Vorjahr** | Plan-vs-Ist Vergleiche | CSV/Excel |
| **KPI-Werte** | Historische Dashboards | CSV (Datum + Wert) |

### Import-Assistent

```
Schritt 1: Datei hochladen (CSV, Excel, DATEV-Export)
Schritt 2: Spalten-Mapping (Drag & Drop)
  - System schlaegt Mapping vor basierend auf Spaltennamen
  - Benutzer bestaetigt oder korrigiert
Schritt 3: Vorschau mit Validierung
  - Zeige erste 20 Zeilen mit erkannten Werten
  - Markiere Fehler/Warnungen
Schritt 4: Import ausfuehren
  - Fortschrittsbalken
  - Fehler-Log nach Abschluss
Schritt 5: Verifizierung
  - Probe-Dashboard mit importierten Daten anzeigen
  - Benutzer bestaetigt Korrektheit
```

---

## 5. Integrations-Setup

### Webhook-Konfiguration (Schritt fuer Schritt)

```
Webhook-Setup fuer Billing-System (Beispiel: Stripe):

1. In Clarity Board:
   → Settings > Integrations > Add Source
   → Type: Billing
   → Name: "Stripe Production"
   → Webhook URL wird generiert: https://api.clarityboard.net/webhook/billing/ent_001/abc123

2. In Stripe Dashboard:
   → Developers > Webhooks > Add Endpoint
   → URL: [oben generierte URL]
   → Events: invoice.created, invoice.paid, invoice.voided,
             charge.succeeded, charge.refunded, subscription.created,
             subscription.updated, subscription.deleted

3. Zurueck in Clarity Board:
   → Signing Secret von Stripe eintragen
   → Mapping konfigurieren:
     - Stripe amount → Clarity Board revenue_amount
     - Stripe customer → Clarity Board customer_id
     - Stripe currency → Clarity Board currency

4. Test:
   → "Send Test Event" in Stripe klicken
   → Clarity Board zeigt empfangenes Event mit Mapping-Vorschau
   → Bestaetigen oder Mapping anpassen

5. Go Live:
   → Webhook aktivieren
   → Erste echte Events werden verarbeitet
```

### Unterstuetzte Quellsysteme (Out-of-Box)

| System | Typ | Events |
|--------|-----|--------|
| **Stripe** | Billing | Invoices, Payments, Subscriptions |
| **Chargebee** | Billing | Invoices, Subscriptions, Credits |
| **sevDesk** | Buchhaltung | Rechnungen, Belege, Zahlungen |
| **lexoffice** | Buchhaltung | Rechnungen, Kontakte |
| **Personio** | HR | Mitarbeiter, Gehaelter, Abwesenheiten |
| **HubSpot** | CRM | Deals, Contacts, Pipeline |
| **Salesforce** | CRM | Opportunities, Accounts |
| **MOSS** | Kreditkarten | Transaktionen, Belege, Budgets |
| **Banking (FinTS/PSD2)** | Banking | Kontostaende, Umsaetze |
| **Custom** | Beliebig | Via generisches Webhook-Schema |

---

## 6. Go-Live Checkliste

```
Go-Live Checkliste:

Entitaet & Struktur:
  [ ] Alle Entitaeten angelegt
  [ ] Holding-Struktur korrekt (falls zutreffend)
  [ ] Organschaft konfiguriert (falls zutreffend)

Kontenrahmen & Steuer:
  [ ] Kontenrahmen geprueft und angepasst
  [ ] Steuersaetze korrekt (19%, 7%, Reverse Charge etc.)
  [ ] DATEV-Beraternummer und Mandantennummer hinterlegt

Eroeffnungsbilanz:
  [ ] Eroeffnungssalden importiert
  [ ] Aktiva = Passiva (Bilanz ausgeglichen)
  [ ] Offene Posten (AR/AP) importiert und abgeglichen
  [ ] Anlagenbestand importiert

Integrationen:
  [ ] Billing-Webhook aktiv und getestet
  [ ] Banking-Integration aktiv (oder CSV-Import-Prozess definiert)
  [ ] HR-Integration aktiv (oder manueller Prozess)
  [ ] CRM-Integration aktiv (falls relevant)
  [ ] MOSS-Integration aktiv (falls relevant)

Benutzer:
  [ ] Alle Benutzer angelegt und eingeladen
  [ ] Rollen korrekt zugewiesen
  [ ] Entity-Zugriffe geprueft
  [ ] 2FA fuer Finance/Admin aktiviert

Konfiguration:
  [ ] KPI-Targets definiert (mindestens fuer Kern-KPIs)
  [ ] Alert-Schwellenwerte gesetzt
  [ ] Wiederkehrende Posten konfiguriert
  [ ] Kostenstellen angelegt

Validierung:
  [ ] Probe-DATEV-Export erfolgreich und durch Steuerberater geprueft
  [ ] Dashboard zeigt korrekte Daten
  [ ] Budget importiert (falls vorhanden)
  [ ] Mindestens ein Testbeleg verarbeitet
```

---

## 7. Post-Go-Live

### Erste 30 Tage

| Woche | Fokus | Aktivitaeten |
|-------|-------|-------------|
| Woche 1 | Datenqualitaet | Taegliche Pruefung der Webhook-Events, Buchungsvorschlaege reviewen |
| Woche 2 | Prozess-Feintuning | Mapping-Korrekturen, Kostenstellen-Zuordnungen optimieren |
| Woche 3 | Erster Monatsabschluss | Gemeinsamer erster DATEV-Export mit Steuerberater |
| Woche 4 | Rollout | Weitere Benutzer einladen, Dashboard-Feedback sammeln |

### Support-Eskalation

| Thema | Kanal | SLA |
|-------|-------|-----|
| Technischer Fehler (Webhook down, Dateninkonsistenz) | Ticket + Email | 4 Stunden |
| Buchungsfragen | Email / Chat | 24 Stunden |
| Konfigurationsaenderung | Email | 48 Stunden |
| Feature-Wunsch | Feedback-Portal | Monatliches Review |

---

## Document Navigation

- Previous: [Regulatory Reporting](./24-regulatory-reporting.md)
- [Back to Index](./README.md)
