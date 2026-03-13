using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;

namespace InvoiceClassification;

/// <summary>
/// Service zur automatischen Klassifizierung und Verbuchung von Belegen
/// via Anthropic Claude API. Nutzt SKR 04 Kontenrahmen.
/// </summary>
public class InvoiceClassificationService
{
    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions;

    public InvoiceClassificationService(string? apiKey = null, string model = "claude-sonnet-4-6")
    {
        _client = apiKey != null 
            ? new AnthropicClient() { /* API key via env ANTHROPIC_API_KEY or constructor */ } 
            : new AnthropicClient();
        _model = model;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };
    }

    /// <summary>
    /// Klassifiziert einen per OCR extrahierten Beleg und gibt einen strukturierten
    /// Buchungsvorschlag zurück.
    /// </summary>
    public async Task<BookingProposal> ClassifyInvoiceAsync(
        OcrInvoiceData ocrData,
        CompanyContext? companyContext = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(companyContext);
        var userPrompt = BuildUserPrompt(ocrData);

        var parameters = new MessageCreateParams
        {
            Model = _model,
            MaxTokens = 2048,
            System = systemPrompt,
            Messages =
            [
                new() { Role = Role.User, Content = userPrompt }
            ],
            Temperature = 0.1 // Niedrige Temperatur für konsistente Buchungsvorschläge
        };

        var response = await _client.Messages.Create(parameters);

        var responseText = response.Content
            .Where(c => c.Text != null)
            .Select(c => c.Text)
            .FirstOrDefault() ?? throw new InvalidOperationException("Keine Antwort von Claude erhalten.");

        // JSON aus der Antwort extrahieren (Claude antwortet innerhalb von JSON-Tags)
        var jsonContent = ExtractJson(responseText);
        
        var proposal = JsonSerializer.Deserialize<BookingProposal>(jsonContent, _jsonOptions)
            ?? throw new InvalidOperationException("Buchungsvorschlag konnte nicht deserialisiert werden.");

        // Validierung der vorgeschlagenen Konten
        ValidateBookingProposal(proposal);

        return proposal;
    }

    /// <summary>
    /// Batch-Verarbeitung mehrerer Belege.
    /// Nutzt Task.WhenAll für parallele Verarbeitung mit optionalem Throttling.
    /// </summary>
    public async Task<List<BookingResult>> ClassifyBatchAsync(
        IEnumerable<OcrInvoiceData> invoices,
        CompanyContext? companyContext = null,
        int maxParallelism = 3,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxParallelism);
        var tasks = invoices.Select(async invoice =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var proposal = await ClassifyInvoiceAsync(invoice, companyContext, cancellationToken);
                return new BookingResult
                {
                    InvoiceId = invoice.InvoiceId,
                    Proposal = proposal,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new BookingResult
                {
                    InvoiceId = invoice.InvoiceId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    #region Prompt Construction

    private static string BuildSystemPrompt(CompanyContext? context)
    {
        var holding = context?.HoldingName ?? "Andagon Holding GmbH";
        var entities = context?.Entities != null
            ? string.Join("\n", context.Entities.Select(e => $"  - {e.Name} ({e.Type}): {e.Description}"))
            : @"  - Andagon Holding GmbH (Holding)
  - aqua Cloud GmbH (SaaS / IT)
  - Andagon People GmbH (IT-Personaldienstleistung)
  - Andagon Vermögensverwaltung GmbH (Immobilien)
  - MaidMyDay (SaaS-Plattform, in Entwicklung)";

        return $"""
Du bist ein erfahrener deutscher Finanzbuchhalter und Steuerexperte, spezialisiert auf:
- Kontenrahmen SKR 04 (DATEV)
- Deutsches Steuerrecht (UStG, EStG, KStG, HGB)
- Holding-Strukturen und konzerninternes Accounting

UNTERNEHMENSKONTEXT:
Holding: {holding}
Gesellschaften:
{entities}

DEINE AUFGABE:
Analysiere den folgenden OCR-extrahierten Beleg und erstelle einen vollständigen Buchungsvorschlag.

REGELN FÜR DIE KONTENZUORDNUNG (SKR 04):

=== ERLÖSE (Klasse 4) ===
4400  Erlöse 19% USt
4300  Erlöse 7% USt
4120  Steuerfreie Umsätze (§4 UStG)
4125  Steuerfreie innergemeinschaftliche Lieferungen
4130  Erlöse Drittland (§4 Nr.1a UStG)
4336  Erlöse aus im Inland steuerpflichtigen EU-Lieferungen 19%

=== MATERIALAUFWAND (Klasse 5) ===
5000  Aufwendungen für Roh-, Hilfs- und Betriebsstoffe
5100  Aufwendungen für bezogene Leistungen (Fremdleistungen)
5300  Aufwendungen für Waren
5900  Aufwendungen für Fremdleistungen (allgemein)
5905  Fremdleistungen betriebsfremd

=== PERSONALAUFWAND (Klasse 6) ===
6000  Löhne und Gehälter
6010  Löhne
6020  Gehälter
6080  Vermögenswirksame Leistungen
6100  Soziale Abgaben und Aufwendungen
6110  Gesetzliche Sozialaufwendungen
6120  Beiträge zur Berufsgenossenschaft
6130  Freiwillige Sozialaufwendungen
6170  Sonstige Personalaufwendungen

=== SONSTIGE BETRIEBLICHE AUFWENDUNGEN (Klasse 6) ===
6300  Sonstige betriebliche Aufwendungen
6310  Miete/Pacht für Geschäftsräume
6315  Nebenkosten Geschäftsräume
6320  Grundstücksaufwendungen
6325  Reinigung
6330  Abgaben (kommunal)
6335  Versicherungen
6400  Bürobedarf
6420  Zeitschriften, Bücher
6430  Fortbildungskosten
6450  Bewirtungskosten (70% abziehbar)
6455  Nicht abziehbare Bewirtungskosten (30%)
6460  Geschenke abziehbar (bis 50€)
6465  Geschenke nicht abziehbar (über 50€)
6470  Aufmerksamkeiten
6490  Sonstiger Betriebsbedarf
6500  Fahrzeugkosten
6510  Kfz-Steuern
6520  Kfz-Versicherungen
6530  Laufende Kfz-Kosten
6540  Kfz-Reparaturen
6570  Reisekosten Unternehmer
6580  Reisekosten Arbeitnehmer
6590  Telefonkosten
6600  Porto
6610  Rechts- und Beratungskosten (allgemein)
6615  Abschluss- und Prüfungskosten
6617  Buchführungskosten
6620  Steuerberatungskosten
6625  Rechtsberatungskosten (Anwalt)
6630  Inkassokosten
6640  Bankgebühren
6700  Kosten des Geldverkehrs
6790  Sonstige Aufwendungen unregelmäßig
6800  Abschreibungen auf Sachanlagen
6805  Abschreibungen auf GWG
6810  Abschreibungen auf immaterielle Vermögensgegenstände
6815  Außerplanmäßige Abschreibungen Sachanlagen
6820  Abschreibungen auf Finanzanlagen
6825  Abschreibungen auf Forderungen
6830  Einstellungen in Rückstellungen
6850  Verluste aus Anlagenabgang

=== IT- UND SOFTWARE-SPEZIFISCHE KONTEN ===
6560  EDV-Kosten / IT-Kosten
6565  IT-Wartung und Support
6568  Softwarelizenzen (laufend, < 1 Jahr)
6569  Cloud-Services / SaaS-Gebühren (Azure, AWS, etc.)
6572  Hosting und Serverkosten
6574  Domain- und Zertifikatskosten

=== ANLAGEVERMÖGEN (Klasse 0) - für Aktivierung ===
0027  EDV-Software (> 800€ netto, > 1 Jahr Nutzung)
0400  Technische Anlagen und Maschinen
0410  Maschinen
0420  Betriebs- und Geschäftsausstattung
0430  EDV-Hardware
0480  GWG (250,01€ - 800€ netto)
0485  GWG Sammelposten (250,01€ - 1.000€ netto)

=== VORSTEUER- UND UST-KONTEN ===
1400  Forderungen aus Lieferungen und Leistungen
1406  Vorsteuer 19%
1401  Vorsteuer 7%
1407  Abziehbare Vorsteuer §13b UStG
1577  Abziehbare Vorsteuer aus innergemeinschaftlichem Erwerb 19%
3400  Verbindlichkeiten aus Lieferungen und Leistungen
3806  Umsatzsteuer 19%
3801  Umsatzsteuer 7%
3837  USt aus innergemeinschaftlichem Erwerb 19%
3830  Umsatzsteuer nach §13b UStG

=== STEUERLICHE SONDERREGELN ===

§13b UStG (Reverse Charge):
- Gilt für: Leistungen ausländischer Unternehmer, Bauleistungen, bestimmte Dienstleistungen
- Leistungsempfänger schuldet die USt
- Buchung: Aufwand an Verbindlichkeiten OHNE USt, dann USt 3830 an Vorsteuer 1407
- Prüfe: Sitz des Leistenden, Art der Leistung, B2B-Kontext

Innergemeinschaftlicher Erwerb:
- Bei Waren aus EU-Mitgliedstaaten
- USt-IdNr. des Lieferanten prüfen
- Buchung: Erwerb + USt 3837 an Vorsteuer 1577

Bewirtungskosten:
- 70% abziehbar (6450), 30% nicht abziehbar (6455)
- Nur bei geschäftlichem Anlass mit externen Gästen
- Bewirtungsbeleg mit allen Pflichtangaben erforderlich

GWG-Regelung:
- Bis 250€ netto: Sofortaufwand (6805)
- 250,01€ - 800€ netto: GWG, sofort abschreiben (0480)
- 800,01€ - 1.000€ netto: Optional Sammelposten 5 Jahre (0485)
- Über 1.000€ netto: Aktivierung + planmäßige AfA

M&A / Transaktionskosten:
- Laufende strategische Beratung: 6610 (Rechts- und Beratungskosten)
- Kosten bei konkretem Unternehmenskauf: Aktivierung als AHK auf Beteiligung (0500-0570)
- Kosten bei konkretem Verkauf: Mindern Veräußerungserlös
- Due-Diligence-Kosten: Abhängig vom Stadium (Sondierungsphase = Aufwand, Transaktionsphase = Aktivierung)

ANTWORTFORMAT:
Antworte AUSSCHLIESSLICH mit folgendem JSON (kein anderer Text, keine Markdown-Backticks):

{{
  "confidence": 0.0-1.0,
  "invoiceType": "incoming_invoice | outgoing_invoice | credit_note | travel_expense | internal",
  "bookingDate": "YYYY-MM-DD",
  "invoiceDate": "YYYY-MM-DD",
  "dueDate": "YYYY-MM-DD oder null",
  "creditor": {{
    "name": "Firmenname",
    "vatId": "USt-IdNr. oder null",
    "country": "DE/AT/CH/...",
    "isEuMember": true/false
  }},
  "debtor": {{
    "name": "Empfänger-Firma",
    "entity": "zugehörige Gesellschaft aus dem Holding-Kontext"
  }},
  "lineItems": [
    {{
      "description": "Positionsbeschreibung",
      "netAmount": 0.00,
      "vatRate": 19.0,
      "vatAmount": 0.00,
      "grossAmount": 0.00,
      "accountNumber": "SKR04-Konto",
      "accountName": "Kontenbezeichnung",
      "costCenter": "Kostenstelle oder null"
    }}
  ],
  "totalNet": 0.00,
  "totalVat": 0.00,
  "totalGross": 0.00,
  "vatTreatment": {{
    "type": "standard_19 | standard_7 | reverse_charge_13b | intra_community | tax_free | third_country",
    "explanation": "Kurze steuerliche Erläuterung",
    "inputTaxAccount": "Vorsteuerkonto",
    "outputTaxAccount": "Umsatzsteuerkonto oder null (bei Reverse Charge)"
  }},
  "bookingEntries": [
    {{
      "debitAccount": "Soll-Konto",
      "debitAccountName": "Soll-Kontenbezeichnung",
      "creditAccount": "Haben-Konto",
      "creditAccountName": "Haben-Kontenbezeichnung",
      "amount": 0.00,
      "taxKey": "DATEV-Steuerschlüssel (z.B. 9 für 19% VSt)",
      "description": "Buchungstext"
    }}
  ],
  "flags": {{
    "needsManualReview": false,
    "reviewReasons": [],
    "isRecurring": false,
    "gwgRelevant": false,
    "activationRequired": false,
    "reverseCharge": false,
    "intraCommunity": false,
    "entertainmentExpense": false
  }},
  "notes": "Zusätzliche Hinweise für den Buchhalter"
}}
""";
    }

    private static string BuildUserPrompt(OcrInvoiceData ocrData)
    {
        var prompt = $"""
Bitte analysiere den folgenden Beleg und erstelle einen Buchungsvorschlag:

=== BELEG-DATEN (OCR-Extrakt) ===
Rechnungsnummer: {ocrData.InvoiceNumber ?? "nicht erkannt"}
Rechnungsdatum: {ocrData.InvoiceDate ?? "nicht erkannt"}
Fälligkeitsdatum: {ocrData.DueDate ?? "nicht erkannt"}

Absender/Lieferant:
{ocrData.CreditorName ?? "nicht erkannt"}
{ocrData.CreditorAddress ?? ""}
USt-IdNr.: {ocrData.CreditorVatId ?? "nicht erkannt"}
Steuernummer: {ocrData.CreditorTaxNumber ?? "nicht erkannt"}

Empfänger:
{ocrData.DebtorName ?? "nicht erkannt"}
{ocrData.DebtorAddress ?? ""}

Positionen:
{FormatLineItems(ocrData.RawLineItems)}

Beträge:
Netto: {ocrData.TotalNet?.ToString("F2") ?? "nicht erkannt"} EUR
USt: {ocrData.TotalVat?.ToString("F2") ?? "nicht erkannt"} EUR
Brutto: {ocrData.TotalGross?.ToString("F2") ?? "nicht erkannt"} EUR

USt-Satz: {ocrData.VatRate?.ToString("F1") ?? "nicht erkannt"}%

Zahlungsbedingungen: {ocrData.PaymentTerms ?? "nicht erkannt"}
IBAN: {ocrData.Iban ?? "nicht erkannt"}
BIC: {ocrData.Bic ?? "nicht erkannt"}

Vollständiger OCR-Text:
{ocrData.RawOcrText}
=== ENDE BELEG-DATEN ===
""";

        return prompt;
    }

    private static string FormatLineItems(List<RawLineItem>? items)
    {
        if (items == null || items.Count == 0)
            return "  (keine einzelnen Positionen erkannt)";

        return string.Join("\n", items.Select((item, i) =>
            $"  {i + 1}. {item.Description} | Menge: {item.Quantity} | " +
            $"Einzelpreis: {item.UnitPrice?.ToString("F2") ?? "?"} EUR | " +
            $"Gesamt: {item.TotalPrice?.ToString("F2") ?? "?"} EUR"));
    }

    #endregion

    #region JSON Extraction & Validation

    private static string ExtractJson(string response)
    {
        // Claude sollte reines JSON liefern, aber sicherheitshalber
        // Markdown-Backticks und umgebenden Text entfernen
        var trimmed = response.Trim();

        // Markdown-Codeblöcke entfernen
        if (trimmed.StartsWith("```json"))
            trimmed = trimmed[7..];
        else if (trimmed.StartsWith("```"))
            trimmed = trimmed[3..];

        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3];

        trimmed = trimmed.Trim();

        // Erstes { bis letztes } extrahieren
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        throw new InvalidOperationException(
            $"Konnte kein gültiges JSON aus der API-Antwort extrahieren. Antwort: {response[..Math.Min(200, response.Length)]}...");
    }

    private static void ValidateBookingProposal(BookingProposal proposal)
    {
        var errors = new List<string>();

        // Prüfe ob Confidence hoch genug ist
        if (proposal.Confidence < 0.5)
        {
            proposal.Flags.NeedsManualReview = true;
            proposal.Flags.ReviewReasons.Add("Confidence unter 50% - manuelle Prüfung empfohlen");
        }

        // Prüfe ob alle Buchungssätze balanciert sind
        var totalDebit = proposal.BookingEntries.Sum(e => e.Amount);
        // Bei doppelter Buchführung sollte Soll = Haben sein (hier vereinfacht)

        // Prüfe ob Kontonummern im gültigen SKR04-Bereich liegen
        foreach (var entry in proposal.BookingEntries)
        {
            if (!IsValidSkr04Account(entry.DebitAccount))
                errors.Add($"Ungültiges Soll-Konto: {entry.DebitAccount}");
            if (!IsValidSkr04Account(entry.CreditAccount))
                errors.Add($"Ungültiges Haben-Konto: {entry.CreditAccount}");
        }

        // Prüfe Reverse Charge Konsistenz
        if (proposal.VatTreatment.Type == "reverse_charge_13b" && !proposal.Flags.ReverseCharge)
        {
            proposal.Flags.ReverseCharge = true;
        }

        if (errors.Count > 0)
        {
            proposal.Flags.NeedsManualReview = true;
            proposal.Flags.ReviewReasons.AddRange(errors);
        }
    }

    private static bool IsValidSkr04Account(string account)
    {
        if (!int.TryParse(account, out var num))
            return false;

        // SKR 04 Kontenrahmen: 0000-9999
        // Klasse 0: Anlagevermögen
        // Klasse 1: Umlaufvermögen / Finanzen
        // Klasse 2: Eigenkapital / Rückstellungen
        // Klasse 3: Verbindlichkeiten
        // Klasse 4: Erlöse
        // Klasse 5: Material / Wareneinsatz
        // Klasse 6: Betriebliche Aufwendungen
        // Klasse 7: Weitere Erträge / Aufwendungen
        // Klasse 8: (reserviert)
        // Klasse 9: Vortrags- und statistische Konten
        return num is >= 0 and <= 9999;
    }

    #endregion
}
