using InvoiceClassification;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceClassification;

// ============================================================
// DI-REGISTRIERUNG
// ============================================================

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert den InvoiceClassificationService in der DI.
    /// API-Key kommt aus Environment-Variable ANTHROPIC_API_KEY.
    /// </summary>
    public static IServiceCollection AddInvoiceClassification(
        this IServiceCollection services,
        string model = "claude-sonnet-4-6")
    {
        services.AddSingleton(new InvoiceClassificationService(model: model));
        return services;
    }
}

// ============================================================
// USAGE EXAMPLES
// ============================================================

/// <summary>
/// Beispiele für die Nutzung des InvoiceClassificationService.
/// </summary>
public static class UsageExamples
{
    /// <summary>
    /// Beispiel 1: Einfache Eingangsrechnung (z.B. M&A-Beratung)
    /// </summary>
    public static async Task Example_MABeratungsrechnung()
    {
        var service = new InvoiceClassificationService();

        // Diese Daten kommen von eurem OCR-System
        var ocrData = new OcrInvoiceData
        {
            InvoiceNumber = "2026-0042",
            InvoiceDate = "2026-03-10",
            DueDate = "2026-04-10",
            CreditorName = "Strategy Partners GmbH",
            CreditorAddress = "Königsallee 92, 40212 Düsseldorf",
            CreditorVatId = "DE123456789",
            DebtorName = "Andagon Holding GmbH",
            DebtorAddress = "Köln",
            TotalNet = 12500.00m,
            TotalVat = 2375.00m,
            TotalGross = 14875.00m,
            VatRate = 19.0m,
            RawLineItems = new List<RawLineItem>
            {
                new()
                {
                    Description = "M&A-Beratung: Strategische Optionsanalyse Andagon People GmbH",
                    Quantity = 1,
                    UnitPrice = 12500.00m,
                    TotalPrice = 12500.00m
                }
            },
            RawOcrText = """
                Strategy Partners GmbH
                Königsallee 92, 40212 Düsseldorf
                USt-IdNr.: DE123456789
                
                Rechnung Nr. 2026-0042
                Datum: 10.03.2026
                
                An: Andagon Holding GmbH, Köln
                
                Pos. 1: M&A-Beratung - Strategische Optionsanalyse 
                         Andagon People GmbH
                         Honorar März 2026
                         12.500,00 EUR
                
                Nettobetrag:    12.500,00 EUR
                USt 19%:         2.375,00 EUR
                Bruttobetrag:   14.875,00 EUR
                
                Zahlbar bis: 10.04.2026
                IBAN: DE89 3704 0044 0532 0130 00
                """
        };

        // Unternehmenskontext mitgeben für korrekte Zuordnung
        var context = new CompanyContext
        {
            HoldingName = "Andagon Holding GmbH",
            Entities = new List<EntityInfo>
            {
                new() { Name = "Andagon Holding GmbH", Type = "Holding", Description = "Dachgesellschaft" },
                new() { Name = "aqua Cloud GmbH", Type = "SaaS/IT", Description = "AI-powered Test Management" },
                new() { Name = "Andagon People GmbH", Type = "Personaldienstleistung", Description = "IT-Staffing, möglicher Verkauf geplant" },
                new() { Name = "Andagon Vermögensverwaltung GmbH", Type = "Immobilien", Description = "Immobilienverwaltung" },
            }
        };

        var proposal = await service.ClassifyInvoiceAsync(ocrData, context);

        // Ergebnis ausgeben
        Console.WriteLine($"=== Buchungsvorschlag (Konfidenz: {proposal.Confidence:P0}) ===");
        Console.WriteLine($"Belegart: {proposal.InvoiceType}");
        Console.WriteLine($"Zugeordnet zu: {proposal.Debtor.Entity}");
        Console.WriteLine($"USt-Behandlung: {proposal.VatTreatment.Type}");
        Console.WriteLine($"  → {proposal.VatTreatment.Explanation}");
        Console.WriteLine();

        Console.WriteLine("Buchungssätze:");
        foreach (var entry in proposal.BookingEntries)
        {
            Console.WriteLine($"  Soll {entry.DebitAccount} ({entry.DebitAccountName})");
            Console.WriteLine($"  Haben {entry.CreditAccount} ({entry.CreditAccountName})");
            Console.WriteLine($"  Betrag: {entry.Amount:N2} EUR | BU-Schlüssel: {entry.TaxKey}");
            Console.WriteLine($"  Text: {entry.Description}");
            Console.WriteLine();
        }

        if (proposal.Flags.NeedsManualReview)
        {
            Console.WriteLine("⚠️ MANUELLE PRÜFUNG ERFORDERLICH:");
            foreach (var reason in proposal.Flags.ReviewReasons)
                Console.WriteLine($"  - {reason}");
        }

        if (!string.IsNullOrEmpty(proposal.Notes))
            Console.WriteLine($"\nHinweise: {proposal.Notes}");

        // Erwartetes Ergebnis für M&A-Beratung:
        // Soll 6610 (Rechts- und Beratungskosten) / Haben 3400 (Verbindlichkeiten LuL)
        // + Vorsteuer 1406
        // Flag: NeedsManualReview = true, weil M&A-Kosten bei konkreter Transaktion
        //       ggf. aktivierungspflichtig sind
    }

    /// <summary>
    /// Beispiel 2: Cloud-Hosting-Rechnung (Reverse Charge aus EU)
    /// </summary>
    public static async Task Example_CloudHostingEU()
    {
        var service = new InvoiceClassificationService();

        var ocrData = new OcrInvoiceData
        {
            InvoiceNumber = "INV-2026-9871",
            InvoiceDate = "2026-03-01",
            CreditorName = "CloudHost B.V.",
            CreditorAddress = "Herengracht 100, 1015 Amsterdam, Netherlands",
            CreditorVatId = "NL123456789B01",
            DebtorName = "aqua Cloud GmbH",
            TotalNet = 890.00m,
            TotalVat = 0.00m, // Kein USt ausgewiesen - Reverse Charge
            TotalGross = 890.00m,
            RawLineItems = new List<RawLineItem>
            {
                new() { Description = "Dedicated Server Hosting - März 2026", Quantity = 1, UnitPrice = 690m, TotalPrice = 690m },
                new() { Description = "CDN Traffic 2.5TB", Quantity = 1, UnitPrice = 200m, TotalPrice = 200m }
            },
            RawOcrText = "CloudHost B.V. ... Reverse Charge - VAT to be accounted for by the recipient ..."
        };

        var proposal = await service.ClassifyInvoiceAsync(ocrData);

        // Erwartetes Ergebnis:
        // §13b UStG Reverse Charge
        // Soll 6572 (Hosting) / Haben 3400 (Verbindlichkeiten) = 890,00 EUR netto
        // Soll 1407 (VSt §13b) / Haben 3830 (USt §13b) = 169,10 EUR (19% auf 890)
    }

    /// <summary>
    /// Beispiel 3: Batch-Verarbeitung mehrerer Belege
    /// </summary>
    public static async Task Example_BatchProcessing()
    {
        var service = new InvoiceClassificationService();

        var invoices = new List<OcrInvoiceData>
        {
            // ... mehrere OCR-Datensätze
        };

        var results = await service.ClassifyBatchAsync(
            invoices,
            maxParallelism: 3 // Max 3 parallele API-Calls
        );

        var successful = results.Count(r => r.Success);
        var needsReview = results.Count(r => r.Success && r.Proposal!.Flags.NeedsManualReview);
        var failed = results.Count(r => !r.Success);

        Console.WriteLine($"Verarbeitet: {results.Count}");
        Console.WriteLine($"  Erfolgreich: {successful}");
        Console.WriteLine($"  Manuelle Prüfung: {needsReview}");
        Console.WriteLine($"  Fehler: {failed}");
    }
}
