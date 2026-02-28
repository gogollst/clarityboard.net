using System.Security.Cryptography;
using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Integration;
using ClarityBoard.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.API.Controllers;

/// <summary>
/// Public webhook endpoint for receiving events from external systems.
/// No [Authorize] attribute -- webhooks authenticate via HMAC signature.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IAppDbContext db,
        IPublishEndpoint publishEndpoint,
        ILogger<WebhookController> logger)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Receives a webhook event from an external source.
    /// Validates HMAC signature, checks idempotency, stores the event, and publishes for async processing.
    /// </summary>
    [HttpPost("{sourceType}/events")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReceiveEvent(
        string sourceType, CancellationToken ct)
    {
        // 1. Read raw body for HMAC verification
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawPayload = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawPayload))
            return BadRequest(new { error = "Empty request body." });

        // 2. Extract entityId from header or query param
        var entityIdStr = Request.Headers["X-Entity-Id"].FirstOrDefault()
                       ?? Request.Query["entityId"].FirstOrDefault();

        if (!Guid.TryParse(entityIdStr, out var entityId))
            return BadRequest(new { error = "Missing or invalid X-Entity-Id header or entityId query parameter." });

        // 3. Look up WebhookConfig by sourceType + entityId
        var config = await _db.WebhookConfigs
            .FirstOrDefaultAsync(c =>
                c.SourceType == sourceType
                && c.EntityId == entityId
                && c.IsActive, ct);

        if (config is null)
            return BadRequest(new { error = $"No active webhook configuration found for source '{sourceType}' and entity '{entityId}'." });

        // 4. HMAC-SHA256 verification
        if (!string.IsNullOrWhiteSpace(config.SecretKey))
        {
            var signatureHeaderName = config.HeaderSignatureKey ?? "X-Webhook-Signature";
            var signature = Request.Headers[signatureHeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning(
                    "Webhook from {SourceType} for entity {EntityId} missing signature header {Header}",
                    sourceType, entityId, signatureHeaderName);
                return Unauthorized(new { error = "Missing webhook signature." });
            }

            if (!WebhookSignatureValidator.ValidateHmacSha256(rawPayload, config.SecretKey, signature))
            {
                _logger.LogWarning(
                    "Webhook from {SourceType} for entity {EntityId} failed HMAC verification",
                    sourceType, entityId);
                return Unauthorized(new { error = "Invalid webhook signature." });
            }
        }

        // 5. Determine idempotency key
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Fallback: use SHA256 hash of the payload
            var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
            var hashBytes = SHA256.HashData(payloadBytes);
            idempotencyKey = Convert.ToHexStringLower(hashBytes);
        }

        // 6. Idempotency check
        var existingEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(e =>
                e.SourceType == sourceType
                && e.SourceId == entityId.ToString()
                && e.IdempotencyKey == idempotencyKey, ct);

        if (existingEvent is not null)
        {
            _logger.LogInformation(
                "Duplicate webhook event detected: {IdempotencyKey} for {SourceType}",
                idempotencyKey, sourceType);
            return Conflict(new { error = "Duplicate event.", eventId = existingEvent.Id });
        }

        // 7. Extract event type from common headers or default
        var eventType = Request.Headers["X-Event-Type"].FirstOrDefault() ?? "unknown";

        // 8. Store WebhookEvent
        var webhookEvent = WebhookEvent.Create(
            sourceType: sourceType,
            sourceId: entityId.ToString(),
            eventType: eventType,
            idempotencyKey: idempotencyKey,
            payload: rawPayload);
        webhookEvent.SetEntityId(entityId);

        _db.WebhookEvents.Add(webhookEvent);
        config.RecordReceived();
        await _db.SaveChangesAsync(ct);

        // 9. Publish for async processing
        await _publishEndpoint.Publish(new ProcessWebhookEvent(
            webhookEvent.Id, entityId, sourceType, eventType), ct);

        _logger.LogInformation(
            "Webhook event {EventId} received and queued from {SourceType} for entity {EntityId}",
            webhookEvent.Id, sourceType, entityId);

        return Accepted(new { eventId = webhookEvent.Id });
    }
}
