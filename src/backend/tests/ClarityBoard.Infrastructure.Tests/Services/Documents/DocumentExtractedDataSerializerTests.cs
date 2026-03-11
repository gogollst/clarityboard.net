using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Services.Documents;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class DocumentExtractedDataSerializerTests
{
    [Fact]
    public void Serialize_IncludesExtractionFieldsAndSortedDistinctReviewReasons()
    {
        var extraction = new DocumentExtractionResult
        {
            VendorName = "Vendor GmbH",
            InvoiceNumber = "INV-42",
            Confidence = 0.42m,
            RawFields = new Dictionary<string, string> { ["invoiceNumber"] = "INV-42" },
        };

        var json = DocumentExtractedDataSerializer.Serialize(extraction, ["low_booking_confidence", "empty_text", "empty_text"]);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Vendor GmbH", document.RootElement.GetProperty("VendorName").GetString());
        Assert.Equal("INV-42", document.RootElement.GetProperty("InvoiceNumber").GetString());
        var reviewReasons = document.RootElement.GetProperty("ReviewReasons")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToArray();

        Assert.Equal(2, reviewReasons.Length);
        Assert.Equal("empty_text", reviewReasons[0]);
        Assert.Equal("low_booking_confidence", reviewReasons[1]);
    }

    [Fact]
    public void Serialize_WithTextAcquisitionMetadata_IncludesOcrMetadata()
    {
        var extraction = new DocumentExtractionResult
        {
            VendorName = "Scan Supplier",
            Confidence = 0.91m,
        };
        var acquisition = new DocumentTextAcquisitionResult
        {
            Source = "native_fallback_after_vision",
            Confidence = 0.63m,
            Warnings = ["vision_ocr_empty", "vision_provider_fallback_used", "vision_ocr_empty"],
            UsedVision = true,
            UsedProvider = "Gemini",
            NativeTextLength = 18,
            VisionTextLength = 0,
        };

        var json = DocumentExtractedDataSerializer.Serialize(extraction, ["vision_ocr_empty"], acquisition);

        using var document = JsonDocument.Parse(json);
        var metadata = document.RootElement.GetProperty("OcrMetadata");

        Assert.Equal("native_fallback_after_vision", metadata.GetProperty("Source").GetString());
        Assert.Equal(0.63m, metadata.GetProperty("Confidence").GetDecimal());
        Assert.True(metadata.GetProperty("UsedVision").GetBoolean());
        Assert.Equal("Gemini", metadata.GetProperty("UsedProvider").GetString());
        Assert.Equal(18, metadata.GetProperty("NativeTextLength").GetInt32());
        Assert.Equal(0, metadata.GetProperty("VisionTextLength").GetInt32());

        var warnings = metadata.GetProperty("Warnings")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToArray();

        Assert.Equal(2, warnings.Length);
        Assert.Equal("vision_ocr_empty", warnings[0]);
        Assert.Equal("vision_provider_fallback_used", warnings[1]);
    }
}
