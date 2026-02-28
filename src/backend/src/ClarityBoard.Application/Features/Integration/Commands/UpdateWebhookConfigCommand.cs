using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Commands;

public record UpdateWebhookConfigCommand : IRequest
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Secret { get; init; }
    public string? HeaderSignatureKey { get; init; }
    public string? EventFilter { get; init; }
    public bool IsActive { get; init; } = true;
}

public class UpdateWebhookConfigCommandValidator : AbstractValidator<UpdateWebhookConfigCommand>
{
    public UpdateWebhookConfigCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateWebhookConfigCommandHandler : IRequestHandler<UpdateWebhookConfigCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateWebhookConfigCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateWebhookConfigCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var config = await _db.WebhookConfigs
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Webhook config '{request.Id}' not found.");

        config.Update(
            request.Name,
            request.Secret,
            request.HeaderSignatureKey,
            request.EventFilter,
            request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
