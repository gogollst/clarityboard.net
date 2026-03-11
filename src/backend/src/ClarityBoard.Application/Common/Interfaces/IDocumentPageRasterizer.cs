namespace ClarityBoard.Application.Common.Interfaces;

public interface IDocumentPageRasterizer
{
    Task<IReadOnlyList<RasterizedPage>> RasterizePdfAsync(
        Stream pdfStream,
        int maxPages = 10,
        int maxLongestSidePx = 1920,
        CancellationToken ct = default);
}

public record RasterizedPage(
    int PageNumber,
    byte[] ImageBytes,
    string MimeType,
    int Width,
    int Height);
