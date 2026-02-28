using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IAppDbContext dbContext, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only wrap commands in transactions, not queries
        if (!IsCommand())
            return await next(cancellationToken);

        var dbContext = (DbContext)_dbContext;

        if (dbContext.Database.CurrentTransaction is not null)
            return await next(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Begin transaction for {RequestType}", typeof(TRequest).Name);

            var response = await next(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Committed transaction for {RequestType}", typeof(TRequest).Name);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning("Rolled back transaction for {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
            || typeof(ICommand).IsAssignableFrom(typeof(TRequest));
    }
}
