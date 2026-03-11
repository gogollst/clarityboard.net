using System.Text.Json;

namespace ClarityBoard.Application.Features.Document;

public static class DocumentExtractedDataReader
{
    public static IReadOnlyList<string> ReadReviewReasons(string? extractedData)
    {
        if (string.IsNullOrWhiteSpace(extractedData))
            return [];

        try
        {
            using var document = JsonDocument.Parse(extractedData);

            if (!document.RootElement.TryGetProperty("ReviewReasons", out var reviewReasonsElement)
                || reviewReasonsElement.ValueKind != JsonValueKind.Array)
                return [];

            return reviewReasonsElement
                .EnumerateArray()
                .Select(element => element.GetString())
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Select(reason => reason!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}