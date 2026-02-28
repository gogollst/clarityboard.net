using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Commands;

public record RetryWebhookEventCommand : IRequest
{
    public Guid EventId { get; init; }
}

public class RetryWebhookEventCommandValidator : AbstractValidator<RetryWebhookEventCommand>
{
    public RetryWebhookEventCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
    }
}

public class RetryWebhookEventCommandHandler : IRequestHandler<RetryWebhookEventCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMessagePublisher _messagePublisher;

    public RetryWebhookEventCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IMessagePublisher messagePublisher)
    {
        _db = db;
        _currentUser = currentUser;
        _messagePublisher = messagePublisher;
    }

    public async Task Handle(
        RetryWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var webhookEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(e =>
                e.Id == request.EventId
                && e.EntityId == entityId
                && (e.Status == "failed" || e.Status == "dead_letter"),
                cancellationToken)
            ?? throw new InvalidOperationException($"Webhook event '{request.EventId}' not found or not in a retryable state.");

        webhookEvent.ResetForRetry();
        await _db.SaveChangesAsync(cancellationToken);

        await _messagePublisher.PublishAsync(new ProcessWebhookEvent(
            webhookEvent.Id,
            entityId,
            webhookEvent.SourceType,
            webhookEvent.EventType), cancellationToken);
    }
}
