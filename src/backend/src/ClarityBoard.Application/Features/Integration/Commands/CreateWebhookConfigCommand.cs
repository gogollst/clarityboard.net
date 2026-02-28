using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Integration;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Integration.Commands;

public record CreateWebhookConfigCommand : IRequest<Guid>
{
    public required string SourceType { get; init; }
    public required string Name { get; init; }
    public string? Secret { get; init; }
    public string? HeaderSignatureKey { get; init; }
    public string? EventFilter { get; init; }
    public bool IsActive { get; init; } = true;
}

public class CreateWebhookConfigCommandValidator : AbstractValidator<CreateWebhookConfigCommand>
{
    public CreateWebhookConfigCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreateWebhookConfigCommandHandler : IRequestHandler<CreateWebhookConfigCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateWebhookConfigCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateWebhookConfigCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;
        var endpointPath = $"/api/webhooks/{request.SourceType}/events";

        var config = WebhookConfig.Create(
            entityId,
            request.SourceType,
            request.Name,
            endpointPath,
            request.Secret,
            request.HeaderSignatureKey);

        if (!request.IsActive)
            config.Deactivate();

        _db.WebhookConfigs.Add(config);
        await _db.SaveChangesAsync(cancellationToken);

        return config.Id;
    }
}
