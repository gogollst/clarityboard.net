const fs = require('fs');
const { Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
        Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
        ShadingType, PageNumber, PageBreak, LevelFormat } = require('docx');

const border = { style: BorderStyle.SINGLE, size: 1, color: "CCCCCC" };
const borders = { top: border, bottom: border, left: border, right: border };
const noBorder = { style: BorderStyle.NONE, size: 0 };
const noBorders = { top: noBorder, bottom: noBorder, left: noBorder, right: noBorder };
const cellMargins = { top: 60, bottom: 60, left: 100, right: 100 };

const headerFill = "1B4F72";
const headerText = "FFFFFF";
const altRowFill = "F2F7FB";
const criticalFill = "FADBD8";
const warningFill = "FDEBD0";
const goodFill = "D5F5E3";

function headerCell(text, width) {
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA },
    shading: { fill: headerFill, type: ShadingType.CLEAR },
    margins: cellMargins,
    verticalAlign: "center",
    children: [new Paragraph({ children: [new TextRun({ text, bold: true, color: headerText, font: "Arial", size: 20 })] })]
  });
}

function cell(text, width, fill) {
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA },
    shading: fill ? { fill, type: ShadingType.CLEAR } : undefined,
    margins: cellMargins,
    children: [new Paragraph({ children: [new TextRun({ text, font: "Arial", size: 20 })] })]
  });
}

function boldCell(text, width, fill) {
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA },
    shading: fill ? { fill, type: ShadingType.CLEAR } : undefined,
    margins: cellMargins,
    children: [new Paragraph({ children: [new TextRun({ text, bold: true, font: "Arial", size: 20 })] })]
  });
}

const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 22 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 32, bold: true, font: "Arial", color: "1B4F72" },
        paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 26, bold: true, font: "Arial", color: "2E86C1" },
        paragraph: { spacing: { before: 240, after: 160 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 22, bold: true, font: "Arial", color: "2E86C1" },
        paragraph: { spacing: { before: 200, after: 120 }, outlineLevel: 2 } },
    ]
  },
  numbering: {
    config: [
      { reference: "bullets",
        levels: [{ level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ]
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 },
        margin: { top: 1440, right: 1200, bottom: 1440, left: 1200 }
      }
    },
    headers: {
      default: new Header({
        children: [new Paragraph({
          border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: "1B4F72", space: 1 } },
          children: [
            new TextRun({ text: "CLARITY BOARD", bold: true, font: "Arial", size: 18, color: "1B4F72" }),
            new TextRun({ text: "  |  Fachkonzept Review Summary  |  VERTRAULICH", font: "Arial", size: 16, color: "999999" }),
          ]
        })]
      })
    },
    footers: {
      default: new Footer({
        children: [new Paragraph({
          alignment: AlignmentType.CENTER,
          children: [
            new TextRun({ text: "Seite ", font: "Arial", size: 16, color: "999999" }),
            new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 16, color: "999999" }),
          ]
        })]
      })
    },
    children: [
      // TITLE PAGE
      new Paragraph({ spacing: { before: 2400 }, children: [] }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 200 },
        children: [new TextRun({ text: "CLARITY BOARD", bold: true, font: "Arial", size: 56, color: "1B4F72" })]
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 600 },
        children: [new TextRun({ text: "Fachkonzept Review Summary", font: "Arial", size: 32, color: "2E86C1" })]
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        border: { top: { style: BorderStyle.SINGLE, size: 2, color: "1B4F72", space: 8 }, bottom: { style: BorderStyle.SINGLE, size: 2, color: "1B4F72", space: 8 } },
        spacing: { before: 400, after: 400 },
        children: [new TextRun({ text: "Version 1.1  |  27. Februar 2026  |  DRAFT", font: "Arial", size: 24, color: "555555" })]
      }),
      new Paragraph({ spacing: { before: 600 }, children: [] }),
      new Table({
        width: { size: 5000, type: WidthType.DXA },
        columnWidths: [2000, 3000],
        alignment: AlignmentType.CENTER,
        rows: [
          new TableRow({ children: [
            new TableCell({ borders: noBorders, width: { size: 2000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "Erstellt von:", bold: true, font: "Arial", size: 20, color: "555555" })] })] }),
            new TableCell({ borders: noBorders, width: { size: 3000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "Claude (AI Review)", font: "Arial", size: 20 })] })] }),
          ]}),
          new TableRow({ children: [
            new TableCell({ borders: noBorders, width: { size: 2000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "Review fuer:", bold: true, font: "Arial", size: 20, color: "555555" })] })] }),
            new TableCell({ borders: noBorders, width: { size: 3000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "Maciej (CFO)", font: "Arial", size: 20 })] })] }),
          ]}),
          new TableRow({ children: [
            new TableCell({ borders: noBorders, width: { size: 2000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "Umfang:", bold: true, font: "Arial", size: 20, color: "555555" })] })] }),
            new TableCell({ borders: noBorders, width: { size: 3000, type: WidthType.DXA }, children: [new Paragraph({ children: [new TextRun({ text: "26 Dokumente (22 + 4 neu)", font: "Arial", size: 20 })] })] }),
          ]}),
        ]
      }),

      // PAGE BREAK
      new Paragraph({ children: [new PageBreak()] }),

      // SECTION 1: EXECUTIVE SUMMARY
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("1. Executive Summary")] }),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun("Das Fachkonzept fuer Clarity Board umfasst nach diesem Review 26 Dokumente und deckt KPI-Management, HGB-Buchhaltung, DATEV-Export, Cash-Flow-Management, Szenarien-Engine, Belegerfassung, Budgetplanung, Sicherheit und UI/UX ab. "),
      ]}),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun("Die urspruenglichen 22 Dokumente waren bereits umfassend. Der Review hat jedoch "),
        new TextRun({ text: "4 kritische Luecken", bold: true }),
        new TextRun(" identifiziert und durch neue Dokumente geschlossen, sowie "),
        new TextRun({ text: "1 inhaltlichen Fehler", bold: true }),
        new TextRun(" korrigiert."),
      ]}),

      // STATUS TABLE
      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("Gesamtstatus")] }),
      new Table({
        width: { size: 9506, type: WidthType.DXA },
        columnWidths: [3500, 2003, 2003, 2000],
        rows: [
          new TableRow({ children: [
            headerCell("Kategorie", 3500), headerCell("Anzahl", 2003), headerCell("Status", 2003), headerCell("Bewertung", 2000),
          ]}),
          new TableRow({ children: [
            cell("Bestehende Dokumente", 3500), cell("22", 2003), cell("Reviewed & korrigiert", 2003), cell("Gut", 2000, goodFill),
          ]}),
          new TableRow({ children: [
            cell("Neue Dokumente (Luecken)", 3500, altRowFill), cell("4", 2003, altRowFill), cell("Neu erstellt", 2003, altRowFill), cell("Kritisch noetig", 2000, criticalFill),
          ]}),
          new TableRow({ children: [
            cell("Korrigierte Fehler", 3500), cell("1", 2003), cell("Behoben", 2003), cell("Behoben", 2000, goodFill),
          ]}),
          new TableRow({ children: [
            boldCell("GESAMT", 3500), boldCell("26 Dokumente", 2003), boldCell("Review-Ready", 2003), boldCell("CFO-Review", 2000),
          ]}),
        ]
      }),

      new Paragraph({ children: [new PageBreak()] }),

      // SECTION 2: KORRIGIERTE FEHLER
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("2. Korrigierte Fehler")] }),

      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("2.1 FCF-Namenskonflikt (Dok. 11 - Cash Flow Management)")] }),
      new Paragraph({ spacing: { after: 120 }, children: [
        new TextRun({ text: "Problem: ", bold: true }),
        new TextRun("\"Financing Cash Flow\" wurde als \"FCF\" abgekuerzt. FCF steht aber branchenweit fuer \"Free Cash Flow\" (= OCF - CapEx). Das fuehrt zu Verwechslungsgefahr bei jeder Verwendung im Dashboard und in Reports."),
      ]}),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun({ text: "Loesung: ", bold: true }),
        new TextRun("Financing Cash Flow wird jetzt als \"FinCF\" abgekuerzt. Zusaetzlich wurde die FCF-Definition (Free Cash Flow = OCF - CapEx) explizit als eigenstaendige KPI ergaenzt."),
      ]}),

      new Paragraph({ children: [new PageBreak()] }),

      // SECTION 3: NEUE DOKUMENTE
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("3. Neu erstellte Dokumente")] }),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun("Folgende 4 Dokumente fehlten im urspruenglichen Konzept und wurden vollstaendig neu erstellt:"),
      ]}),

      // Doc 22
      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("3.1 Dok. 22: GetMOSS Integration")] }),
      new Paragraph({ spacing: { after: 120 }, children: [
        new TextRun({ text: "Warum kritisch: ", bold: true }),
        new TextRun("GetMOSS war explizit in der Projektspezifikation als Datenquelle genannt (\"Integration mit GetMOSS.com fuer Import von virtuellen Kreditkarten-Daten und Belegen\"), aber in keinem der 22 Dokumente spezifiziert. Ohne dieses Dokument wuerde ein zentraler Datenfluss im System undokumentiert bleiben."),
      ]}),
      new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: "Inhalt:", bold: true })] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("MOSS-API-Integration: Webhook-Events (transaction.created, receipt.uploaded, etc.)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Kategorie-Mapping: MOSS-Kategorien auf SKR03-Konten (inkl. Bewirtungskosten-Sonderlogik 70/30)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Settlement-basierte Buchungslogik (Autorisierung vs. Settlement vs. Monatsabrechnung)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("GoBD-konforme Belegpflicht mit Eskalationsstufen (3/7/14/30 Tage)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, spacing: { after: 200 }, children: [new TextRun("GWG-Pruefungslogik und steuerliche Besonderheiten (Vorsteuerabzug, Kleinbetraege)")] }),

      // Doc 23
      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("3.2 Dok. 23: Anlagenverwaltung & Abschreibungen (AfA)")] }),
      new Paragraph({ spacing: { after: 120 }, children: [
        new TextRun({ text: "Warum kritisch: ", bold: true }),
        new TextRun("Das bestehende Konzept erwaehnte Abschreibungen nur in Buchungsvorlagen (Dok. 10), aber ohne Verwaltung des Anlagevermogens, Nutzungsdauern, GWG-Behandlung oder Anlagenspiegel. Fuer den Jahresabschluss und DATEV-Export ist das zwingend erforderlich."),
      ]}),
      new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: "Inhalt:", bold: true })] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Anlageklassen mit SKR03-Kontenbereichen und AfA-Tabellen-Nutzungsdauern")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Sonderregel digitale Wirtschaftsgueter (BMF 2021: 1-Jahr-Sofortabschreibung)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("GWG-Entscheidungsbaum (bis 250 EUR / 250-800 EUR / ueber 800 EUR)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Vollstaendiger Anlagenspiegel im DATEV-kompatiblen Format")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, spacing: { after: 200 }, children: [new TextRun("KPI-Integration (Investitionsquote, Anlagendeckungsgrad, Reinvestitionsrate)")] }),

      // Doc 24
      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("3.3 Dok. 24: Regulatorische Meldepflichten")] }),
      new Paragraph({ spacing: { after: 120 }, children: [
        new TextRun({ text: "Warum kritisch: ", bold: true }),
        new TextRun("DATEV-Export allein reicht nicht. Es fehlte die komplette Spezifikation fuer E-Bilanz (seit 2013 Pflicht!), Jahresabschluss-Generierung nach HGB, Offenlegungspflichten im Bundesanzeiger, Steuerkalender und Konzernberichterstattung."),
      ]}),
      new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: "Inhalt:", bold: true })] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Jahresabschluss: Bilanz (266 HGB) und GuV (275 HGB) mit automatischer Generierung")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Groessenklassen-Automatik (267 HGB) mit Schwellenwert-Warnung")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("E-Bilanz: XBRL-Export mit Taxonomie-Mapping und Ueberleitungsrechnung HB/StB")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Steuerkalender mit automatischen Frist-Erinnerungen (USt-VA, LSt, KSt/GewSt-Vorauszahlungen)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Offenlegungspflichten mit Ordnungsgeld-Warnung")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Verrechnungspreisdokumentation (Transfer Pricing) fuer IC-Beziehungen")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, spacing: { after: 200 }, children: [new TextRun("Betriebspruefungs-Readiness (GDPdU, Z3-Zugriff, Verfahrensdokumentation)")] }),

      // Doc 25
      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("3.4 Dok. 25: Onboarding & Ersteinrichtung")] }),
      new Paragraph({ spacing: { after: 120 }, children: [
        new TextRun({ text: "Warum kritisch: ", bold: true }),
        new TextRun("Kein einziges Dokument beschrieb, wie ein neues Unternehmen in Clarity Board eingerichtet wird. Ohne Onboarding-Prozess ist das System nicht einsetzbar. Besonders der Eroeffnungsbilanz-Import und die Go-Live-Checkliste sind essenziell."),
      ]}),
      new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: "Inhalt:", bold: true })] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("8-Phasen-Onboarding (Registrierung bis Go-Live in 5 Werktagen)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Eroeffnungsbilanz-Import: 3 Formate (DATEV, CSV/Excel, manuell) mit Validierung")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Historischer Daten-Import mit Mapping-Assistent")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Webhook-Konfiguration Schritt fuer Schritt (am Beispiel Stripe)")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, children: [new TextRun("Unterstuetzte Quellsysteme: Stripe, Chargebee, sevDesk, lexoffice, Personio, HubSpot, Salesforce, MOSS, Banking")] }),
      new Paragraph({ numbering: { reference: "bullets", level: 0 }, spacing: { after: 200 }, children: [new TextRun("Go-Live-Checkliste und Post-Go-Live-Plan (erste 30 Tage)")] }),

      new Paragraph({ children: [new PageBreak()] }),

      // SECTION 4: VERBLEIBENDE OFFENE PUNKTE
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("4. Verbleibende offene Punkte")] }),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun("Folgende Punkte sind nicht kritisch fuer das Fachkonzept-Review, sollten aber vor der technischen Umsetzung geklaert werden:"),
      ]}),

      new Table({
        width: { size: 9506, type: WidthType.DXA },
        columnWidths: [500, 2500, 3506, 1500, 1500],
        rows: [
          new TableRow({ children: [
            headerCell("#", 500), headerCell("Thema", 2500), headerCell("Beschreibung", 3506), headerCell("Prioritaet", 1500), headerCell("Phase", 1500),
          ]}),
          new TableRow({ children: [
            cell("1", 500), boldCell("API-Spezifikation", 2500), cell("REST-API-Endpunkte, Versionierung, OpenAPI-Spec fehlen noch komplett", 3506), cell("Hoch", 1500, warningFill), cell("Technik", 1500),
          ]}),
          new TableRow({ children: [
            cell("2", 500, altRowFill), boldCell("DB-Schema", 2500, altRowFill), cell("Postgres-Schema-Design fuer alle Module steht aus", 3506, altRowFill), cell("Hoch", 1500, warningFill), cell("Technik", 1500, altRowFill),
          ]}),
          new TableRow({ children: [
            cell("3", 500), boldCell("Mobile App", 2500), cell("PWA vs. Native nicht entschieden. UI/UX-Dok beschreibt responsive Breakpoints, aber keine Offline-Faehigkeit", 3506), cell("Mittel", 1500), cell("Produkt", 1500),
          ]}),
          new TableRow({ children: [
            cell("4", 500, altRowFill), boldCell("Notification-System", 2500, altRowFill), cell("Alert-Konfiguration ist dokumentiert, aber Benachrichtigungspraeferenzen pro Benutzer fehlen", 3506, altRowFill), cell("Mittel", 1500, altRowFill), cell("Produkt", 1500, altRowFill),
          ]}),
          new TableRow({ children: [
            cell("5", 500), boldCell("DR-Testing", 2500), cell("Disaster Recovery ist beschrieben, aber Testfrequenz und -prozedur fehlen", 3506), cell("Mittel", 1500), cell("Betrieb", 1500),
          ]}),
          new TableRow({ children: [
            cell("6", 500, altRowFill), boldCell("Mandantenfaehigkeit", 2500, altRowFill), cell("Multi-Entity ist dokumentiert, aber echte Mandantentrennung (verschiedene Kunden) vs. Multi-Entity (ein Kunde, mehrere Firmen) nicht explizit differenziert", 3506, altRowFill), cell("Hoch", 1500, warningFill), cell("Architektur", 1500, altRowFill),
          ]}),
          new TableRow({ children: [
            cell("7", 500), boldCell("Pricing/Licensing", 2500), cell("Kein Pricing-Modell dokumentiert. Relevant fuer Architekturentscheidungen (Tenant-Isolation)", 3506), cell("Mittel", 1500), cell("Business", 1500),
          ]}),
          new TableRow({ children: [
            cell("8", 500, altRowFill), boldCell("Daten-Migration", 2500, altRowFill), cell("Onboarding deckt Ersteinrichtung ab, aber Migration von Bestandssystemen (z.B. Excel-basiertes Controlling) nicht im Detail", 3506, altRowFill), cell("Niedrig", 1500, altRowFill), cell("Betrieb", 1500, altRowFill),
          ]}),
        ]
      }),

      new Paragraph({ children: [new PageBreak()] }),

      // SECTION 5: EMPFEHLUNG
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("5. Empfehlung")] }),
      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun("Das Fachkonzept ist nach den Erweiterungen in einem soliden Zustand fuer den CFO-Review. Die 26 Dokumente decken alle wesentlichen funktionalen Anforderungen ab:"),
      ]}),

      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [
        new TextRun({ text: "Finanzen: ", bold: true }),
        new TextRun("KPIs, HGB-Buchhaltung, DATEV-Export, E-Bilanz, Anlagenverwaltung, Steuerkalender"),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [
        new TextRun({ text: "Cash Flow: ", bold: true }),
        new TextRun("13-Wochen-Forecast, Working Capital, Multi-Waehrung, Hedging"),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [
        new TextRun({ text: "Planung: ", bold: true }),
        new TextRun("Szenarien-Engine, Monte Carlo, Budgetplanung, Sensitivitaetsanalysen"),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [
        new TextRun({ text: "Operativ: ", bold: true }),
        new TextRun("Belegerfassung, MOSS-Integration, KI-Buchungsvorschlaege, Onboarding"),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [
        new TextRun({ text: "Compliance: ", bold: true }),
        new TextRun("GoBD, DSGVO, OWASP, Audit-Logging, Betriebspruefungs-Readiness"),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 200 }, children: [
        new TextRun({ text: "Multi-Entity: ", bold: true }),
        new TextRun("Holding, Konsolidierung, Organschaft, Gewinnabfuehrung, Transfer Pricing"),
      ]}),

      new Paragraph({ spacing: { after: 200 }, children: [
        new TextRun({ text: "Naechste Schritte nach CFO-Freigabe:", bold: true }),
      ]}),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [new TextRun("Offene Punkte aus Abschnitt 4 priorisieren und klaeren")] }),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [new TextRun("Technische Architektur-Dokumente erstellen (DB-Schema, API-Spec, Deployment)")] }),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [new TextRun("MVP-Scope definieren (welche Module in Phase 1?)")] }),
      new Paragraph({ numbering: { reference: "numbers", level: 0 }, children: [new TextRun("Entwicklungsteam-Planung und Sprint-Planung starten")] }),

      new Paragraph({ spacing: { before: 600 }, children: [] }),
      new Paragraph({
        border: { top: { style: BorderStyle.SINGLE, size: 2, color: "1B4F72", space: 8 } },
        spacing: { before: 200 },
        children: [
          new TextRun({ text: "Dokument erstellt am 27.02.2026 | Clarity Board Fachkonzept v1.1", font: "Arial", size: 18, color: "999999", italics: true }),
        ]
      }),
    ]
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync("/sessions/peaceful-relaxed-pasteur/mnt/clarityboard.net/docs/Clarity-Board-Review-Summary-CFO.docx", buffer);
  console.log("DOCX created successfully");
});
