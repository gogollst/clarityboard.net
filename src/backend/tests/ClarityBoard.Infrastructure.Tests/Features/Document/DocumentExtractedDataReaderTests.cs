using ClarityBoard.Application.Features.Document;

namespace ClarityBoard.Infrastructure.Tests.Features.Document;

public class DocumentExtractedDataReaderTests
{
    [Fact]
    public void ReadReviewReasons_ReturnsReviewReasonsFromExtractedData()
    {
        const string extractedData = """
            {"VendorName":"Vendor GmbH","ReviewReasons":["empty_text","low_booking_confidence"]}
            """;

        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons(extractedData);

        Assert.Equal(2, reviewReasons.Count);
        Assert.Equal("empty_text", reviewReasons[0]);
        Assert.Equal("low_booking_confidence", reviewReasons[1]);
    }

    [Fact]
    public void ReadReviewReasons_WhenExtractedDataIsInvalid_ReturnsEmptyArray()
    {
        var reviewReasons = DocumentExtractedDataReader.ReadReviewReasons("{not-json}");

        Assert.Empty(reviewReasons);
    }
}