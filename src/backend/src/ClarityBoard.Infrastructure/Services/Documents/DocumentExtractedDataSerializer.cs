using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.Infrastructure.Services.Documents;

public static class DocumentExtractedDataSerializer
{
    public static string Serialize(
        DocumentExtractionResult extraction,
        IReadOnlyCollection<ReviewReason> reviewReasons,
        DocumentTextAcquisitionResult? textAcquisition = null)
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
                .Where(r => !string.IsNullOrWhiteSpace(r.Key))
                .GroupBy(r => r.Key, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(r => r.Key, StringComparer.Ordinal)
                .Select(r => new { r.Key, r.Detail })
                .ToArray(),
            OcrMetadata = textAcquisition is null
                ? null
                : new
                {
                    textAcquisition.Source,
                    textAcquisition.Confidence,
                    Warnings = textAcquisition.Warnings
                        .Where(warning => !string.IsNullOrWhiteSpace(warning))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(warning => warning, StringComparer.Ordinal)
                        .ToArray(),
                    textAcquisition.UsedVision,
                    textAcquisition.UsedProvider,
                    textAcquisition.NativeTextLength,
                    textAcquisition.VisionTextLength,
                },
        };

        return JsonSerializer.Serialize(payload);
    }
}