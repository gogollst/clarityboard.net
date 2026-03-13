using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
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

public record ApproveBookingRequest
{
    public Guid? HrEmployeeId { get; init; }
}

public record RejectBookingRequest
{
    public string? Reason { get; init; }
}

public record ModifyBookingRequest
{
    public Guid DebitAccountId { get; init; }
    public Guid CreditAccountId { get; init; }
    public decimal Amount { get; init; }
    public string? VatCode { get; init; }
    public decimal? VatAmount { get; init; }
    public string? Description { get; init; }
    public Guid? HrEmployeeId { get; init; }
}

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IMessagePublisher _messagePublisher;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
    };

    public DocumentController(ISender mediator, IMessagePublisher messagePublisher)
    {
        _mediator = mediator;
        _messagePublisher = messagePublisher;
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
        [FromQuery] string? search,
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
            Search = search,
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
        Guid id, [FromBody] ApproveBookingRequest? request,
        [FromQuery] Guid entityId, CancellationToken ct = default)
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
                HrEmployeeId = request?.HrEmployeeId,
            }, ct);

            return Ok(journalEntryId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reject a booking suggestion.
    /// </summary>
    [HttpPost("{id:guid}/reject-booking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RejectBooking(
        Guid id, [FromBody] RejectBookingRequest? request,
        [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new RejectBookingSuggestionCommand
            {
                EntityId = entityId,
                DocumentId = id,
                UserId = userId.Value,
                Reason = request?.Reason,
            }, ct);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Modify a booking suggestion and create a journal entry with the modified values.
    /// </summary>
    [HttpPost("{id:guid}/modify-booking")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> ModifyBooking(
        Guid id, [FromBody] ModifyBookingRequest request,
        [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var journalEntryId = await _mediator.Send(new ModifyBookingSuggestionCommand
            {
                EntityId = entityId,
                DocumentId = id,
                UserId = userId.Value,
                DebitAccountId = request.DebitAccountId,
                CreditAccountId = request.CreditAccountId,
                Amount = request.Amount,
                VatCode = request.VatCode,
                VatAmount = request.VatAmount,
                Description = request.Description,
                HrEmployeeId = request.HrEmployeeId,
            }, ct);

            return Ok(journalEntryId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Re-trigger document processing for a document stuck in uploaded/failed/processing status.
    /// </summary>
    [HttpPost("{id:guid}/reprocess")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reprocess(
        Guid id, [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var document = await _mediator.Send(new GetDocumentDetailQuery(entityId, id), ct);
        if (document is null)
            return NotFound();

        if (document.Status is not ("uploaded" or "failed" or "processing"))
            return BadRequest($"Cannot reprocess document in status '{document.Status}'.");

        await _messagePublisher.PublishAsync(new ProcessDocument(id, entityId), ct);
        return Accepted();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
