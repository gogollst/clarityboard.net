using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

/// <summary>
/// Orchestrates text acquisition from documents:
/// 1. Try native text extraction (PDF text / plain text)
/// 2. Evaluate text quality
/// 3. If insufficient, rasterize (PDF) or pass through (image) and call Vision OCR
/// 4. Return consolidated result with metadata
/// </summary>
public sealed class DocumentTextAcquisitionService : IDocumentTextAcquisitionService
{
    private const int MinNativeTextLength = 50;
    private const decimal MinNativeTextQuality = 0.3m;
    private const int MaxVisionPages = 10;
    private const int MaxLongestSidePx = 1920;

    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/bmp", "image/tiff", "image/heic",
    };

    private readonly DocumentTextExtractor _nativeExtractor;
    private readonly IDocumentPageRasterizer _rasterizer;
    private readonly IDocumentVisionService _visionService;
    private readonly ILogger<DocumentTextAcquisitionService> _logger;

    public DocumentTextAcquisitionService(
        DocumentTextExtractor nativeExtractor,
        IDocumentPageRasterizer rasterizer,
        IDocumentVisionService visionService,
        ILogger<DocumentTextAcquisitionService> logger)
    {
        _nativeExtractor = nativeExtractor;
        _rasterizer = rasterizer;
        _visionService = visionService;
        _logger = logger;
    }

    public async Task<DocumentTextAcquisitionResult> AcquireTextAsync(
        Stream fileStream,
        string contentType,
        Guid documentId,
        Guid entityId,
        string? fileName = null,
        CancellationToken ct = default)
    {
        var reviewReasons = new List<string>();
        var warnings = new List<string>();

        // ── Step 1: Determine if this is an image (skip native extraction) ──
        if (IsImageType(contentType))
        {
            _logger.LogInformation(
                "Document {DocumentId} is an image ({ContentType}), skipping native extraction",
                documentId, contentType);

            return await RunVisionOnImageAsync(
                fileStream, contentType, documentId, entityId, fileName, reviewReasons, ct);
        }

        // ── Step 2: Try native text extraction ──────────────────────────────
        // We need the stream twice potentially (native + rasterize), so buffer it
        using var bufferedStream = new MemoryStream();
        await fileStream.CopyToAsync(bufferedStream, ct);
        bufferedStream.Position = 0;

        var nativeText = await _nativeExtractor.ExtractTextAsync(bufferedStream, contentType, ct);
        var nativeLength = nativeText?.Trim().Length ?? 0;

        _logger.LogInformation(
            "Document {DocumentId} native extraction: {Length} chars",
            documentId, nativeLength);

        // ── Step 3: Evaluate text quality ───────────────────────────────────
        var quality = EvaluateTextQuality(nativeText, contentType);

        if (quality.IsAcceptable)
        {
            _logger.LogInformation(
                "Document {DocumentId} native text quality acceptable (score: {Score:F2})",
                documentId, quality.Score);

            return new DocumentTextAcquisitionResult
            {
                Text = nativeText!.Trim(),
                Source = "native",
                Confidence = quality.Score,
                Warnings = warnings,
                UsedVision = false,
                NativeTextLength = nativeLength,
                ReviewReasons = reviewReasons,
            };
        }

        // ── Step 4: Native text insufficient — run Vision OCR ───────────────
        if (nativeLength == 0)
            reviewReasons.Add("native_text_empty");
        else
            reviewReasons.Add("native_text_low_quality");

        _logger.LogInformation(
            "Document {DocumentId} native text insufficient (score: {Score:F2}, length: {Length}), activating Vision OCR",
            documentId, quality.Score, nativeLength);

        // For PDFs: rasterize pages, then send to vision
        if (contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await RunVisionOnPdfAsync(
                bufferedStream, contentType, documentId, entityId, fileName,
                nativeText, nativeLength, reviewReasons, ct);
        }

        // For other types with poor text: return what we have with low confidence
        _logger.LogWarning(
            "Document {DocumentId} has unsupported type {ContentType} for Vision OCR",
            documentId, contentType);

        return new DocumentTextAcquisitionResult
        {
            Text = nativeText?.Trim() ?? string.Empty,
            Source = "native",
            Confidence = quality.Score,
            Warnings = ["unsupported_content_type_for_vision"],
            UsedVision = false,
            NativeTextLength = nativeLength,
            ReviewReasons = reviewReasons,
        };
    }

    // ── Vision: Image ───────────────────────────────────────────────────────

    private async Task<DocumentTextAcquisitionResult> RunVisionOnImageAsync(
        Stream imageStream,
        string contentType,
        Guid documentId,
        Guid entityId,
        string? fileName,
        List<string> reviewReasons,
        CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var imageBytes = ms.ToArray();

        var request = new DocumentVisionRequest
        {
            DocumentId = documentId,
            EntityId = entityId,
            ContentType = contentType,
            FileName = fileName,
            PageImages = [new VisionPageInput(1, imageBytes, contentType)],
        };

        try
        {
            var ocrResult = await _visionService.ExtractTextAsync(request, ct);
            return BuildVisionResult(ocrResult, nativeText: null, nativeLength: 0, reviewReasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vision OCR failed for image document {DocumentId}", documentId);
            reviewReasons.Add("vision_ocr_failed");

            return new DocumentTextAcquisitionResult
            {
                Text = string.Empty,
                Source = "vision",
                Confidence = 0m,
                Warnings = ["vision_ocr_failed"],
                UsedVision = true,
                NativeTextLength = 0,
                ReviewReasons = reviewReasons,
            };
        }
    }

    // ── Vision: PDF ─────────────────────────────────────────────────────────

    private async Task<DocumentTextAcquisitionResult> RunVisionOnPdfAsync(
        MemoryStream pdfStream,
        string contentType,
        Guid documentId,
        Guid entityId,
        string? fileName,
        string? nativeText,
        int nativeLength,
        List<string> reviewReasons,
        CancellationToken ct)
    {
        // Rasterize PDF pages
        IReadOnlyList<RasterizedPage> pages;
        try
        {
            pdfStream.Position = 0;
            pages = await _rasterizer.RasterizePdfAsync(pdfStream, MaxVisionPages, MaxLongestSidePx, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF rasterization failed for document {DocumentId}", documentId);
            reviewReasons.Add("pdf_rasterization_failed");

            // Fall back to native text even if poor
            return new DocumentTextAcquisitionResult
            {
                Text = nativeText?.Trim() ?? string.Empty,
                Source = "native",
                Confidence = 0.2m,
                Warnings = ["pdf_rasterization_failed"],
                UsedVision = false,
                NativeTextLength = nativeLength,
                ReviewReasons = reviewReasons,
            };
        }

        if (pages.Count == 0)
        {
            _logger.LogWarning("PDF rasterization returned 0 pages for document {DocumentId}", documentId);
            reviewReasons.Add("pdf_rasterization_empty");

            return new DocumentTextAcquisitionResult
            {
                Text = nativeText?.Trim() ?? string.Empty,
                Source = "native",
                Confidence = 0.2m,
                Warnings = ["pdf_rasterization_empty"],
                UsedVision = false,
                NativeTextLength = nativeLength,
                ReviewReasons = reviewReasons,
            };
        }

        _logger.LogInformation(
            "Rasterized {PageCount} pages for document {DocumentId}, sending to Vision OCR",
            pages.Count, documentId);

        // Send rasterized pages to Vision
        var request = new DocumentVisionRequest
        {
            DocumentId = documentId,
            EntityId = entityId,
            ContentType = contentType,
            FileName = fileName,
            PageImages = pages.Select(p => new VisionPageInput(p.PageNumber, p.ImageBytes, p.MimeType)).ToList(),
        };

        try
        {
            var ocrResult = await _visionService.ExtractTextAsync(request, ct);
            return BuildVisionResult(ocrResult, nativeText, nativeLength, reviewReasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vision OCR failed for PDF document {DocumentId}", documentId);
            reviewReasons.Add("vision_ocr_failed");

            // Fall back to native text
            return new DocumentTextAcquisitionResult
            {
                Text = nativeText?.Trim() ?? string.Empty,
                Source = "native",
                Confidence = 0.2m,
                Warnings = ["vision_ocr_failed"],
                UsedVision = true,
                NativeTextLength = nativeLength,
                ReviewReasons = reviewReasons,
            };
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DocumentTextAcquisitionResult BuildVisionResult(
        DocumentOcrResult ocrResult,
        string? nativeText,
        int nativeLength,
        List<string> reviewReasons)
    {
        var warnings = new List<string>(ocrResult.Warnings);
        var visionLength = ocrResult.FullText.Length;

        // Merge text: prefer vision text, but note if native text was also available
        var finalText = ocrResult.FullText;

        if (string.IsNullOrWhiteSpace(finalText))
        {
            reviewReasons.Add("vision_ocr_empty");
            finalText = nativeText?.Trim() ?? string.Empty;
        }

        if (ocrResult.Confidence < 0.5m)
            reviewReasons.Add("vision_ocr_low_confidence");

        // Check for partial page recognition
        var totalPages = ocrResult.Pages.Count;
        var lowConfPages = ocrResult.Pages.Count(p => p.Confidence < 0.5m);
        if (totalPages > 0 && lowConfPages > 0)
            reviewReasons.Add("vision_ocr_partial_pages");

        if (ocrResult.UsedFallback)
            reviewReasons.Add("vision_provider_fallback_used");

        return new DocumentTextAcquisitionResult
        {
            Text = finalText,
            Source = ocrResult.Source,
            Confidence = ocrResult.Confidence,
            Warnings = warnings,
            UsedVision = true,
            UsedProvider = ocrResult.UsedProvider,
            NativeTextLength = nativeLength,
            VisionTextLength = visionLength,
            ReviewReasons = reviewReasons,
        };
    }

    private static bool IsImageType(string contentType)
        => ImageMimeTypes.Contains(contentType);

    // ── Text Quality Evaluation ─────────────────────────────────────────────

    private record TextQuality(decimal Score, bool IsAcceptable);

    private static TextQuality EvaluateTextQuality(string? text, string contentType)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TextQuality(0m, false);

        var trimmed = text.Trim();

        // Placeholder text from DocumentTextExtractor for images
        if (trimmed.StartsWith("[Image document", StringComparison.OrdinalIgnoreCase))
            return new TextQuality(0m, false);

        if (trimmed.StartsWith("[No text could be extracted", StringComparison.OrdinalIgnoreCase))
            return new TextQuality(0m, false);

        var length = trimmed.Length;
        if (length < MinNativeTextLength)
            return new TextQuality(0.1m, false);

        // Calculate quality score based on content characteristics
        var alphaNumCount = trimmed.Count(char.IsLetterOrDigit);
        var alphaNumRatio = (decimal)alphaNumCount / length;

        // Penalize if mostly non-printable or garbage characters
        var controlCount = trimmed.Count(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t');
        var controlRatio = (decimal)controlCount / length;

        // Look for indicators of real document content
        var hasWords = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Count(w => w.Length >= 3 && w.Any(char.IsLetter)) > 3;

        var score = alphaNumRatio * 0.6m
                  + (hasWords ? 0.3m : 0m)
                  + (length >= 200 ? 0.1m : (decimal)length / 2000m)
                  - controlRatio * 0.5m;

        score = Math.Clamp(score, 0m, 1m);

        return new TextQuality(score, score >= MinNativeTextQuality);
    }
}
