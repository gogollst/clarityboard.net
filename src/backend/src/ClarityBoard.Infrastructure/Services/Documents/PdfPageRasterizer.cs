using ClarityBoard.Application.Common.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

public sealed class PdfPageRasterizer : IDocumentPageRasterizer
{
    private readonly ILogger<PdfPageRasterizer> _logger;

    public PdfPageRasterizer(ILogger<PdfPageRasterizer> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<RasterizedPage>> RasterizePdfAsync(
        Stream pdfStream,
        int maxPages = 10,
        int maxLongestSidePx = 1920,
        CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct);
        var pdfBytes = ms.ToArray();

        var pages = new List<RasterizedPage>();

        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfBytes, new PageDimensions(maxLongestSidePx, maxLongestSidePx));

        var pageCount = docReader.GetPageCount();
        var pagesToProcess = Math.Min(pageCount, maxPages);

        _logger.LogInformation(
            "Rasterizing PDF: {PageCount} total pages, processing {PagesToProcess}",
            pageCount, pagesToProcess);

        for (var i = 0; i < pagesToProcess; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var pageReader = docReader.GetPageReader(i);
            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0)
            {
                _logger.LogWarning("Page {PageNumber} returned empty image, skipping", i + 1);
                continue;
            }

            // Docnet returns raw BGRA pixels — encode to BMP for broad API compatibility
            var bmpBytes = EncodeRawBgraToBmp(rawBytes, width, height);

            pages.Add(new RasterizedPage(
                PageNumber: i + 1,
                ImageBytes: bmpBytes,
                MimeType: "image/bmp",
                Width: width,
                Height: height));

            _logger.LogDebug(
                "Rasterized page {PageNumber}: {Width}x{Height}, {Size} bytes",
                i + 1, width, height, bmpBytes.Length);
        }

        if (pageCount > maxPages)
        {
            _logger.LogWarning(
                "PDF has {PageCount} pages, only {MaxPages} were rasterized",
                pageCount, maxPages);
        }

        return pages;
    }

    /// <summary>
    /// Encodes raw BGRA pixel data to a BMP byte array using a System.Drawing-free approach.
    /// </summary>
    private static byte[] EncodeRawBgraToBmp(byte[] bgraPixels, int width, int height)
    {
        // Build a minimal BMP in memory from BGRA data.
        // We intentionally avoid System.Drawing-based transcoding so the rasterizer
        // stays portable in containerized Linux environments.

        var rowStride = width * 4; // BGRA = 4 bytes per pixel
        var imageSize = rowStride * height;
        var headerSize = 54; // BMP header
        var fileSize = headerSize + imageSize;

        var bmp = new byte[fileSize];

        // BMP File Header (14 bytes)
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteInt32LE(bmp, 2, fileSize);
        WriteInt32LE(bmp, 10, headerSize);

        // DIB Header (BITMAPINFOHEADER, 40 bytes)
        WriteInt32LE(bmp, 14, 40); // header size
        WriteInt32LE(bmp, 18, width);
        WriteInt32LE(bmp, 22, height); // positive = bottom-up
        WriteInt16LE(bmp, 26, 1); // color planes
        WriteInt16LE(bmp, 28, 32); // bits per pixel (BGRA)
        WriteInt32LE(bmp, 30, 0); // compression (none)
        WriteInt32LE(bmp, 34, imageSize);

        // Docnet produces top-down BGRA, BMP expects bottom-up.
        // Flip rows while copying.
        for (var y = 0; y < height; y++)
        {
            var srcOffset = y * rowStride;
            var dstOffset = headerSize + (height - 1 - y) * rowStride;
            Buffer.BlockCopy(bgraPixels, srcOffset, bmp, dstOffset, rowStride);
        }

        return bmp;
    }

    private static void WriteInt32LE(byte[] buf, int offset, int value)
    {
        buf[offset] = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)((value >> 8) & 0xFF);
        buf[offset + 2] = (byte)((value >> 16) & 0xFF);
        buf[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteInt16LE(byte[] buf, int offset, short value)
    {
        buf[offset] = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
