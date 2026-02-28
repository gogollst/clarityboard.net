using System.Reflection;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;

namespace ClarityBoard.Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attributes = request.GetType()
            .GetCustomAttributes<RequirePermissionAttribute>(true)
            .ToList();

        if (attributes.Count == 0)
            return await next(cancellationToken);

        foreach (var attr in attributes)
        {
            if (!_currentUser.HasPermission(attr.Permission))
            {
                throw new ForbiddenException(
                    $"Missing required permission: '{attr.Permission}'.");
            }
        }

        return await next(cancellationToken);
    }
}
