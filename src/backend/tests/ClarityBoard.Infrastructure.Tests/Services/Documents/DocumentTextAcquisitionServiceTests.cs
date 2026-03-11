using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Services.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class DocumentTextAcquisitionServiceTests
{
    [Fact]
    public async Task AcquireTextAsync_TextDocument_UsesNativeTextWithoutVision()
    {
        var vision = new FakeDocumentVisionService();
        var sut = CreateSut(new FakeDocumentPageRasterizer(), vision);
        var text = "Dies ist ein ausreichend langer Beispieltext für die native Dokumentextraktion.";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var result = await sut.AcquireTextAsync(stream, "text/plain", Guid.NewGuid(), Guid.NewGuid(), ct: CancellationToken.None);

        Assert.Equal(text, result.Text);
        Assert.Equal("native", result.Source);
        Assert.False(result.UsedVision);
        Assert.Empty(result.ReviewReasons);
        Assert.Equal(text.Length, result.NativeTextLength);
        Assert.False(vision.WasCalled);
    }

    [Fact]
    public async Task AcquireTextAsync_ImageDocument_UsesVisionOcrResult()
    {
        var vision = new FakeDocumentVisionService
        {
            Result = new DocumentOcrResult
            {
                FullText = "Rechnung 4711\nGesamt 123,45 EUR",
                Confidence = 0.94m,
                UsedProvider = "Gemini",
                Pages = [new DocumentOcrPageResult(1, "Rechnung 4711", 0.94m, [])],
            },
        };
        var sut = CreateSut(new FakeDocumentPageRasterizer(), vision);
        await using var stream = new MemoryStream([1, 2, 3, 4]);

        var result = await sut.AcquireTextAsync(stream, "image/png", Guid.NewGuid(), Guid.NewGuid(), "receipt.png", CancellationToken.None);

        Assert.True(result.UsedVision);
        Assert.Equal("vision", result.Source);
        Assert.Equal("Gemini", result.UsedProvider);
        Assert.Equal("Rechnung 4711\nGesamt 123,45 EUR", result.Text);
        Assert.Equal(result.Text.Length, result.VisionTextLength);
        Assert.Empty(result.ReviewReasons);
    }

    [Fact]
    public async Task AcquireTextAsync_PdfWithPoorNativeText_UsesVisionAndMarksLowQualityNativeText()
    {
        var rasterizer = new FakeDocumentPageRasterizer
        {
            Pages = [new RasterizedPage(1, [10, 20, 30], "image/png", 1024, 768)],
        };
        var vision = new FakeDocumentVisionService
        {
            Result = new DocumentOcrResult
            {
                FullText = "Lieferant GmbH\nRechnung INV-77\nGesamt 199,00 EUR",
                Confidence = 0.89m,
                UsedProvider = "Gemini",
                Pages = [new DocumentOcrPageResult(1, "Lieferant GmbH", 0.89m, [])],
            },
        };
        var sut = CreateSut(rasterizer, vision);
        await using var stream = new MemoryStream(CreatePdf("BT\n(Hi) Tj\nET"));

        var result = await sut.AcquireTextAsync(stream, "application/pdf", Guid.NewGuid(), Guid.NewGuid(), "scan.pdf", CancellationToken.None);

        Assert.True(result.UsedVision);
        Assert.Equal("vision", result.Source);
        Assert.Contains("native_text_low_quality", result.ReviewReasons);
        Assert.Equal("Lieferant GmbH\nRechnung INV-77\nGesamt 199,00 EUR", result.Text);
        Assert.True(rasterizer.WasCalled);
        Assert.True(vision.WasCalled);
    }

    [Fact]
    public async Task AcquireTextAsync_PdfWhenVisionReturnsEmpty_FallsBackToNativeAndAddsReviewReason()
    {
        var rasterizer = new FakeDocumentPageRasterizer
        {
            Pages = [new RasterizedPage(1, [10, 20, 30], "image/png", 800, 600)],
        };
        var vision = new FakeDocumentVisionService
        {
            Result = new DocumentOcrResult
            {
                FullText = string.Empty,
                Confidence = 0.72m,
                UsedProvider = "Gemini",
                Warnings = ["vision_ocr_empty"],
                Pages = [new DocumentOcrPageResult(1, string.Empty, 0.72m, [])],
            },
        };
        var sut = CreateSut(rasterizer, vision);
        await using var stream = new MemoryStream(CreatePdf("BT\n(Kurzer nativer Text) Tj\nET"));

        var result = await sut.AcquireTextAsync(stream, "application/pdf", Guid.NewGuid(), Guid.NewGuid(), "scan.pdf", CancellationToken.None);

        Assert.True(result.UsedVision);
        Assert.Equal("native_fallback_after_vision", result.Source);
        Assert.Equal("Kurzer nativer Text", result.Text);
        Assert.Contains("native_text_low_quality", result.ReviewReasons);
        Assert.Contains("vision_ocr_empty", result.ReviewReasons);
    }

    [Fact]
    public async Task AcquireTextAsync_ImageWhenVisionTimesOut_ReturnsTimeoutReviewReason()
    {
        var vision = new FakeDocumentVisionService
        {
            Exception = new TimeoutException("vision timeout"),
        };
        var sut = CreateSut(new FakeDocumentPageRasterizer(), vision);
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await sut.AcquireTextAsync(stream, "image/jpeg", Guid.NewGuid(), Guid.NewGuid(), "phone.jpg", CancellationToken.None);

        Assert.True(result.UsedVision);
        Assert.Equal(string.Empty, result.Text);
        Assert.Contains("vision_ocr_timeout", result.ReviewReasons);
        Assert.Contains("vision_ocr_timeout", result.Warnings);
    }

    private static DocumentTextAcquisitionService CreateSut(
        IDocumentPageRasterizer rasterizer,
        IDocumentVisionService visionService)
        => new(new DocumentTextExtractor(), rasterizer, visionService, NullLogger<DocumentTextAcquisitionService>.Instance);

    private static byte[] CreatePdf(string contentStream)
    {
        var streamBytes = Encoding.UTF8.GetBytes(contentStream);
        var header = Encoding.ASCII.GetBytes($"%PDF-1.4\n1 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF");
        return [.. header, .. streamBytes, .. footer];
    }

    private sealed class FakeDocumentPageRasterizer : IDocumentPageRasterizer
    {
        public bool WasCalled { get; private set; }
        public IReadOnlyList<RasterizedPage> Pages { get; init; } = [];

        public Task<IReadOnlyList<RasterizedPage>> RasterizePdfAsync(Stream pdfStream, int maxPages = 10, int maxLongestSidePx = 1920, CancellationToken ct = default)
        {
            WasCalled = true;
            return Task.FromResult(Pages);
        }
    }

    private sealed class FakeDocumentVisionService : IDocumentVisionService
    {
        public bool WasCalled { get; private set; }
        public DocumentOcrResult Result { get; init; } = new();
        public Exception? Exception { get; init; }

        public Task<DocumentOcrResult> ExtractTextAsync(DocumentVisionRequest request, CancellationToken ct = default)
        {
            WasCalled = true;
            if (Exception is not null)
                throw Exception;
            return Task.FromResult(Result);
        }
    }
}