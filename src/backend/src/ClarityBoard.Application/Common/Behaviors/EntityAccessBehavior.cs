using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Application.Common.Behaviors;

public class EntityAccessBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EntityAccessBehavior<TRequest, TResponse>> _logger;

    public EntityAccessBehavior(ICurrentUser currentUser, ILogger<EntityAccessBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IEntityScoped entityScoped)
        {
            if (entityScoped.EntityId != _currentUser.EntityId)
            {
                _logger.LogWarning(
                    "Entity access denied. User {UserId} attempted to access entity {RequestedEntityId} but is scoped to {UserEntityId}",
                    _currentUser.UserId, entityScoped.EntityId, _currentUser.EntityId);

                throw new ForbiddenException(
                    $"You do not have access to entity '{entityScoped.EntityId}'.");
            }
        }

        return await next(cancellationToken);
    }
}
