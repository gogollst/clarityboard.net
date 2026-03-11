using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.Infrastructure.Services.Documents;

public static class DocumentExtractedDataSerializer
{
    public static string Serialize(DocumentExtractionResult extraction, IReadOnlyCollection<string> reviewReasons)
    {
        var payload = new
        {
            extraction.VendorName,
            extraction.InvoiceNumber,
            extraction.InvoiceDate,
            extraction.TotalAmount,
            extraction.Currency,
            extraction.TaxRate,
            LineItems = extraction.LineItems,
            RawFields = extraction.RawFields,
            extraction.Confidence,
            ReviewReasons = reviewReasons
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray(),
        };

        return JsonSerializer.Serialize(payload);
    }
}