using ClarityBoard.Domain.Entities.Document;

namespace ClarityBoard.Domain.Tests.Entities.Document;

public class DocumentStatusTests
{
    [Fact]
    public void MarkReview_AfterExtraction_SetsStatusToReview()
    {
        var document = CreateDocument();
        document.SetExtraction("text", "{}", 0.42m, vendorName: "Vendor");

        document.MarkReview();

        Assert.Equal("review", document.Status);
        Assert.Equal("Vendor", document.VendorName);
        Assert.NotNull(document.ProcessedAt);
    }

    [Fact]
    public void MarkReview_AfterBooked_OverridesStatusToReview()
    {
        var document = CreateDocument();
        document.MarkBooked(Guid.NewGuid());

        document.MarkReview();

        Assert.Equal("review", document.Status);
    }

    [Fact]
    public void UpdateExtractedData_AfterExtraction_ReplacesExtractedDataWithoutChangingStatus()
    {
        var document = CreateDocument();
        document.SetExtraction("text", "{\"reviewReasons\":[]}", 0.42m);

        document.UpdateExtractedData("{\"reviewReasons\":[\"empty_text\"]}");

        Assert.Equal("extracted", document.Status);
        Assert.Equal("{\"reviewReasons\":[\"empty_text\"]}", document.ExtractedData);
    }

    private static Document CreateDocument() => Document.Create(
        entityId: Guid.NewGuid(),
        fileName: "invoice.pdf",
        contentType: "application/pdf",
        fileSize: 1024,
        storagePath: "docs/invoice.pdf",
        documentType: "invoice",
        createdBy: Guid.NewGuid());
}