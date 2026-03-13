using System.Text.Json.Serialization;

namespace InvoiceClassification;

// ============================================================
// INPUT MODELS (von OCR-Extraktion)
// ============================================================

/// <summary>
/// Daten, die vom OCR-System extrahiert wurden.
/// Felder sind nullable, da OCR nicht immer alles erkennt.
/// </summary>
public class OcrInvoiceData
{
    /// <summary>Interne ID des Belegs in eurer App</summary>
    public string InvoiceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Rechnungsnummer</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Rechnungsdatum (ISO-Format bevorzugt)</summary>
    public string? InvoiceDate { get; set; }

    /// <summary>Fälligkeitsdatum</summary>
    public string? DueDate { get; set; }

    // --- Kreditor (Absender) ---
    public string? CreditorName { get; set; }
    public string? CreditorAddress { get; set; }
    public string? CreditorVatId { get; set; }
    public string? CreditorTaxNumber { get; set; }

    // --- Debitor (Empfänger, also ihr) ---
    public string? DebtorName { get; set; }
    public string? DebtorAddress { get; set; }

    // --- Positionen ---
    public List<RawLineItem>? RawLineItems { get; set; }

    // --- Beträge ---
    public decimal? TotalNet { get; set; }
    public decimal? TotalVat { get; set; }
    public decimal? TotalGross { get; set; }
    public decimal? VatRate { get; set; }

    // --- Zahlungsinfos ---
    public string? PaymentTerms { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }

    /// <summary>Der vollständige OCR-Rohtext als Fallback</summary>
    public string RawOcrText { get; set; } = string.Empty;
}

public class RawLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
}

// ============================================================
// OUTPUT MODELS (Buchungsvorschlag von Claude)
// ============================================================

/// <summary>
/// Vollständiger Buchungsvorschlag für einen Beleg.
/// </summary>
public class BookingProposal
{
    /// <summary>Konfidenz des Vorschlags (0.0-1.0)</summary>
    public double Confidence { get; set; }

    /// <summary>Art des Belegs</summary>
    public string InvoiceType { get; set; } = string.Empty;

    public string? BookingDate { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }

    /// <summary>Lieferant / Rechnungssteller</summary>
    public CreditorInfo Creditor { get; set; } = new();

    /// <summary>Empfänger / eure Gesellschaft</summary>
    public DebtorInfo Debtor { get; set; } = new();

    /// <summary>Einzelne Rechnungspositionen mit Kontenzuordnung</summary>
    public List<ClassifiedLineItem> LineItems { get; set; } = new();

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    /// <summary>Steuerliche Behandlung</summary>
    public VatTreatmentInfo VatTreatment { get; set; } = new();

    /// <summary>Konkrete Buchungssätze für DATEV</summary>
    public List<BookingEntry> BookingEntries { get; set; } = new();

    /// <summary>Flags für Sonderfälle und manuelle Prüfung</summary>
    public BookingFlags Flags { get; set; } = new();

    /// <summary>Zusätzliche Hinweise</summary>
    public string? Notes { get; set; }
}

public class CreditorInfo
{
    public string Name { get; set; } = string.Empty;
    public string? VatId { get; set; }
    public string Country { get; set; } = "DE";
    public bool IsEuMember { get; set; } = true;
}

public class DebtorInfo
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Zuordnung zur konkreten Holding-Gesellschaft</summary>
    public string Entity { get; set; } = string.Empty;
}

public class ClassifiedLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }

    /// <summary>SKR 04 Kontonummer</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Name des Kontos</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Kostenstelle (optional)</summary>
    public string? CostCenter { get; set; }
}

public class VatTreatmentInfo
{
    /// <summary>
    /// standard_19 | standard_7 | reverse_charge_13b | 
    /// intra_community | tax_free | third_country
    /// </summary>
    public string Type { get; set; } = "standard_19";

    /// <summary>Kurze steuerliche Erläuterung</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>Vorsteuerkonto (z.B. 1406)</summary>
    public string InputTaxAccount { get; set; } = string.Empty;

    /// <summary>Umsatzsteuerkonto bei Reverse Charge (z.B. 3830)</summary>
    public string? OutputTaxAccount { get; set; }
}

/// <summary>
/// Ein einzelner Buchungssatz (Soll/Haben) für den DATEV-Export.
/// </summary>
public class BookingEntry
{
    public string DebitAccount { get; set; } = string.Empty;
    public string DebitAccountName { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public string CreditAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>
    /// DATEV-Steuerschlüssel:
    /// 0 = keine Steuer, 2 = 7% USt, 3 = 19% USt,
    /// 5 = 7% VSt, 9 = 19% VSt, 19 = 19% VSt §13b,
    /// 94 = 19% innergemeinschaftlicher Erwerb
    /// </summary>
    public string TaxKey { get; set; } = "0";

    public string Description { get; set; } = string.Empty;
}

public class BookingFlags
{
    /// <summary>Muss manuell geprüft werden?</summary>
    public bool NeedsManualReview { get; set; }

    /// <summary>Gründe für manuelle Prüfung</summary>
    public List<string> ReviewReasons { get; set; } = new();

    /// <summary>Wiederkehrender Beleg (z.B. monatliche Lizenz)?</summary>
    public bool IsRecurring { get; set; }

    /// <summary>GWG-Prüfung relevant?</summary>
    public bool GwgRelevant { get; set; }

    /// <summary>Muss als Anlagevermögen aktiviert werden?</summary>
    public bool ActivationRequired { get; set; }

    /// <summary>§13b UStG Reverse Charge?</summary>
    public bool ReverseCharge { get; set; }

    /// <summary>Innergemeinschaftlicher Erwerb?</summary>
    public bool IntraCommunity { get; set; }

    /// <summary>Bewirtungskosten (70/30-Aufteilung)?</summary>
    public bool EntertainmentExpense { get; set; }
}

// ============================================================
// CONTEXT & BATCH MODELS
// ============================================================

/// <summary>
/// Unternehmenskontext für die Holding-Zuordnung.
/// </summary>
public class CompanyContext
{
    public string HoldingName { get; set; } = "Andagon Holding GmbH";
    public List<EntityInfo> Entities { get; set; } = new();
}

public class EntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Ergebnis einer Batch-Verarbeitung.
/// </summary>
public class BookingResult
{
    public string InvoiceId { get; set; } = string.Empty;
    public BookingProposal? Proposal { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
