using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Document.Commands;
using ClarityBoard.Application.Features.Document.DTOs;
using ClarityBoard.Application.Features.Document.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

public class DocumentUploadFormData
{
    public IFormFile File { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string? DocumentType { get; set; }
}

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly ISender _mediator;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
    };

    public DocumentController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Upload a document (PDF, JPEG, PNG, TIFF) for AI processing.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentUploadResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<IActionResult> Upload(
        [FromForm] DocumentUploadFormData formData,
        CancellationToken ct)
    {
        var file = formData.File;

        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest($"Content type '{file.ContentType}' is not allowed. Allowed: PDF, JPEG, PNG, TIFF.");

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        using var stream = file.OpenReadStream();

        var result = await _mediator.Send(new UploadDocumentCommand
        {
            EntityId = formData.EntityId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileStream = stream,
            FileSize = file.Length,
            DocumentType = formData.DocumentType ?? "invoice",
            CreatedBy = userId.Value,
        }, ct);

        return AcceptedAtAction(nameof(GetDetail), new { id = result.DocumentId }, result);
    }

    /// <summary>
    /// List documents for an entity (paginated, filterable).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetList(
        [FromQuery] Guid entityId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? vendorName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDocumentsQuery
        {
            EntityId = entityId,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            VendorName = vendorName,
            Page = page,
            PageSize = pageSize,
        }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Get document detail with fields and booking suggestion.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> GetDetail(
        Guid id, [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDocumentDetailQuery(entityId, id), ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get a presigned download URL for a document (15-minute expiry).
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(PresignedDownloadUrl), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresignedDownloadUrl>> GetDownloadUrl(
        Guid id, [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDocumentDownloadUrlQuery(entityId, id), ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Approve a booking suggestion and create a journal entry.
    /// </summary>
    [HttpPost("{id:guid}/approve-booking")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> ApproveBooking(
        Guid id, [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var journalEntryId = await _mediator.Send(new ApproveBookingCommand
            {
                EntityId = entityId,
                DocumentId = id,
                UserId = userId.Value,
            }, ct);

            return Ok(journalEntryId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
