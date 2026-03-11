using System.IO.Compression;
using System.Text;
using ClarityBoard.Infrastructure.Services.Documents;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractTextAsync_UncompressedPdf_ReturnsStructuredText()
    {
        await using var stream = new MemoryStream(CreatePdf("BT\n(Invoice 4711) Tj\n(Total 123.45 EUR) Tj\nET"));

        var result = await _extractor.ExtractTextAsync(stream, "application/pdf", CancellationToken.None);

        Assert.Contains("Invoice 4711", result);
        Assert.Contains("Total 123.45 EUR", result);
        Assert.DoesNotContain("%PDF", result);
    }

    [Fact]
    public async Task ExtractTextAsync_FlateCompressedPdf_InflatesAndReturnsText()
    {
        await using var stream = new MemoryStream(CreatePdf("BT\n[(Recurring) 120 (Invoice)] TJ\nET", compress: true));

        var result = await _extractor.ExtractTextAsync(stream, "application/pdf", CancellationToken.None);

        Assert.Contains("Recurring", result);
        Assert.Contains("Invoice", result);
    }

    [Fact]
    public async Task ExtractTextAsync_EmptyPdf_ReturnsEmptyString()
    {
        await using var stream = new MemoryStream(CreatePdf(string.Empty));

        var result = await _extractor.ExtractTextAsync(stream, "application/pdf", CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ExtractTextAsync_TextFallback_ReturnsPlainText()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello from txt"));

        var result = await _extractor.ExtractTextAsync(stream, "text/plain", CancellationToken.None);

        Assert.Equal("hello from txt", result);
    }

    private static byte[] CreatePdf(string contentStream, bool compress = false)
    {
        var streamBytes = Encoding.UTF8.GetBytes(contentStream);
        var filter = string.Empty;
        if (compress)
        {
            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
                zlib.Write(streamBytes, 0, streamBytes.Length);
            streamBytes = compressed.ToArray();
            filter = "/Filter /FlateDecode ";
        }

        var header = Encoding.ASCII.GetBytes($"%PDF-1.4\n1 0 obj\n<< {filter}/Length {streamBytes.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF");
        return [.. header, .. streamBytes, .. footer];
    }
}