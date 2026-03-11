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
}