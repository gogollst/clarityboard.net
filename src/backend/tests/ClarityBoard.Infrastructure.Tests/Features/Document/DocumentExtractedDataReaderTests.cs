using ClarityBoard.Application.Features.Document;

namespace ClarityBoard.Infrastructure.Tests.Features.Document;

public class DocumentExtractedDataReaderTests
{
    [Fact]
    public void ReadReviewReasons_WithOldStringFormat_ReturnsReviewReasons()
    {
        const string extractedData = """
            {"VendorName":"Vendor GmbH","ReviewReasons":["empty_text","low_booking_confidence"]}
            """;

        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons(extractedData);

        Assert.Equal(2, reviewReasons.Count);
        Assert.Equal("empty_text", reviewReasons[0].Key);
        Assert.Null(reviewReasons[0].Detail);
        Assert.Equal("low_booking_confidence", reviewReasons[1].Key);
        Assert.Null(reviewReasons[1].Detail);
    }

    [Fact]
    public void ReadReviewReasons_WithNewObjectFormat_ReturnsReviewReasons()
    {
        const string extractedData = """
            {"ReviewReasons":[{"Key":"cost_center_split_required","Detail":"Aufteilung auf Kostenstellen 100 und 200"},{"Key":"low_booking_confidence","Detail":""}]}
            """;

        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons(extractedData);

        Assert.Equal(2, reviewReasons.Count);
        Assert.Equal("cost_center_split_required", reviewReasons[0].Key);
        Assert.Equal("Aufteilung auf Kostenstellen 100 und 200", reviewReasons[0].Detail);
        Assert.Equal("low_booking_confidence", reviewReasons[1].Key);
        Assert.Null(reviewReasons[1].Detail); // empty string → null
    }

    [Fact]
    public void ReadReviewReasons_WithMixedFormat_ReturnsAllReasons()
    {
        const string extractedData = """
            {"ReviewReasons":["legacy_string",{"Key":"new_format","Detail":"some detail"}]}
            """;

        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons(extractedData);

        Assert.Equal(2, reviewReasons.Count);
        Assert.Equal("legacy_string", reviewReasons[0].Key);
        Assert.Null(reviewReasons[0].Detail);
        Assert.Equal("new_format", reviewReasons[1].Key);
        Assert.Equal("some detail", reviewReasons[1].Detail);
    }

    [Fact]
    public void ReadReviewReasons_WhenExtractedDataIsInvalid_ReturnsEmptyArray()
    {
        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons("{not-json}");

        Assert.Empty(reviewReasons);
    }
}
