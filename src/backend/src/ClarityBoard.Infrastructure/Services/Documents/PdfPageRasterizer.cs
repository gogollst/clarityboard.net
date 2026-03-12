using System.IO.Compression;
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

            // Docnet returns raw BGRA pixels — encode to PNG for API compatibility
            var pngBytes = EncodeRawBgraToPng(rawBytes, width, height);

            pages.Add(new RasterizedPage(
                PageNumber: i + 1,
                ImageBytes: pngBytes,
                MimeType: "image/png",
                Width: width,
                Height: height));

            _logger.LogDebug(
                "Rasterized page {PageNumber}: {Width}x{Height}, {Size} bytes",
                i + 1, width, height, pngBytes.Length);
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
    /// Encodes raw BGRA pixel data to a PNG byte array (no System.Drawing dependency).
    /// </summary>
    private static byte[] EncodeRawBgraToPng(byte[] bgraPixels, int width, int height)
    {
        using var output = new MemoryStream();

        // PNG Signature
        output.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk: width, height, bit depth 8, color type 6 (RGBA)
        WriteChunk(output, "IHDR"u8, writer =>
        {
            WriteBE32(writer, width);
            WriteBE32(writer, height);
            writer.WriteByte(8);  // bit depth
            writer.WriteByte(6);  // color type: RGBA
            writer.WriteByte(0);  // compression
            writer.WriteByte(0);  // filter
            writer.WriteByte(0);  // interlace
        });

        // IDAT chunk: zlib-compressed filtered row data
        // Convert BGRA → RGBA and prepend filter byte (0 = None) per row
        var rowStride = width * 4;
        var rawImageData = new byte[height * (1 + rowStride)];
        for (var y = 0; y < height; y++)
        {
            var rawOffset = y * (1 + rowStride);
            rawImageData[rawOffset] = 0; // filter: None
            for (var x = 0; x < width; x++)
            {
                var srcIdx = (y * rowStride) + (x * 4);
                var dstIdx = rawOffset + 1 + (x * 4);
                rawImageData[dstIdx] = bgraPixels[srcIdx + 2];     // R (was B)
                rawImageData[dstIdx + 1] = bgraPixels[srcIdx + 1]; // G
                rawImageData[dstIdx + 2] = bgraPixels[srcIdx];     // B (was R)
                rawImageData[dstIdx + 3] = bgraPixels[srcIdx + 3]; // A
            }
        }

        using var compressedStream = new MemoryStream();
        using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Fastest, leaveOpen: true))
            zlib.Write(rawImageData);

        WriteChunk(output, "IDAT"u8, writer => writer.Write(compressedStream.ToArray()));

        // IEND chunk
        WriteChunk(output, "IEND"u8, _ => { });

        return output.ToArray();
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, Action<MemoryStream> writeData)
    {
        using var data = new MemoryStream();
        writeData(data);
        var dataBytes = data.ToArray();

        // Length (4 bytes BE)
        var lengthBytes = new byte[4];
        WriteBE32Bytes(lengthBytes, 0, dataBytes.Length);
        output.Write(lengthBytes);

        // Type (4 bytes)
        output.Write(type);

        // Data
        if (dataBytes.Length > 0)
            output.Write(dataBytes);

        // CRC32 over type + data
        var crcInput = new byte[4 + dataBytes.Length];
        type.CopyTo(crcInput);
        Buffer.BlockCopy(dataBytes, 0, crcInput, 4, dataBytes.Length);
        var crc = ComputeCrc32(crcInput);
        var crcBytes = new byte[4];
        WriteBE32Bytes(crcBytes, 0, (int)crc);
        output.Write(crcBytes);
    }

    private static void WriteBE32(MemoryStream ms, int value)
    {
        ms.WriteByte((byte)((value >> 24) & 0xFF));
        ms.WriteByte((byte)((value >> 16) & 0xFF));
        ms.WriteByte((byte)((value >> 8) & 0xFF));
        ms.WriteByte((byte)(value & 0xFF));
    }

    private static void WriteBE32Bytes(byte[] buf, int offset, int value)
    {
        buf[offset] = (byte)((value >> 24) & 0xFF);
        buf[offset + 1] = (byte)((value >> 16) & 0xFF);
        buf[offset + 2] = (byte)((value >> 8) & 0xFF);
        buf[offset + 3] = (byte)(value & 0xFF);
    }

    private static uint ComputeCrc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
