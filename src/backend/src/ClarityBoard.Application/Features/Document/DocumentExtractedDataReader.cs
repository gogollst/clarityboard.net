using System.Text.Json;
using ClarityBoard.Application.Features.Document.DTOs;

namespace ClarityBoard.Application.Features.Document;

public static class DocumentExtractedDataReader
{
    public static IReadOnlyList<ReviewReasonDto> ReadReviewReasons(string? extractedData)
    {
        if (string.IsNullOrWhiteSpace(extractedData))
            return [];

        try
        {
            using var document = JsonDocument.Parse(extractedData);

            if (!document.RootElement.TryGetProperty("ReviewReasons", out var reviewReasonsElement)
                || reviewReasonsElement.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<ReviewReasonDto>();
            foreach (var element in reviewReasonsElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    // Old format: plain string
                    var value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        results.Add(new ReviewReasonDto { Key = value, Detail = null });
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    // New format: { "Key": "...", "Detail": "..." }
                    var key = element.TryGetProperty("Key", out var k) ? k.GetString() : null;
                    var detail = element.TryGetProperty("Detail", out var d) ? d.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(key))
                        results.Add(new ReviewReasonDto { Key = key!, Detail = string.IsNullOrWhiteSpace(detail) ? null : detail });
                }
            }

            return results;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static OcrMetadataDto? ReadOcrMetadata(string? extractedData)
    {
        if (string.IsNullOrWhiteSpace(extractedData))
            return null;

        try
        {
            using var document = JsonDocument.Parse(extractedData);

            if (!document.RootElement.TryGetProperty("OcrMetadata", out var ocrElement)
                || ocrElement.ValueKind != JsonValueKind.Object)
                return null;

            return new OcrMetadataDto
            {
                Source = ocrElement.TryGetProperty("Source", out var s) && s.ValueKind == JsonValueKind.String
                    ? s.GetString() : null,
                Confidence = ocrElement.TryGetProperty("Confidence", out var c) && c.ValueKind == JsonValueKind.Number
                    ? c.GetDecimal() : null,
                UsedVision = ocrElement.TryGetProperty("UsedVision", out var v)
                    && v.ValueKind == JsonValueKind.True,
                UsedProvider = ocrElement.TryGetProperty("UsedProvider", out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString() : null,
                Warnings = ocrElement.TryGetProperty("Warnings", out var w) && w.ValueKind == JsonValueKind.Array
                    ? w.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(x => x is not null)
                        .Select(x => x!)
                        .ToArray()
                    : null,
                NativeTextLength = ocrElement.TryGetProperty("NativeTextLength", out var ntl) && ntl.ValueKind == JsonValueKind.Number
                    ? ntl.GetInt32() : null,
                VisionTextLength = ocrElement.TryGetProperty("VisionTextLength", out var vtl) && vtl.ValueKind == JsonValueKind.Number
                    ? vtl.GetInt32() : null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}