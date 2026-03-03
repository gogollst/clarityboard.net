using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services.Hr;

public class DataAccessLogger : IDataAccessLogger
{
    private readonly IDbContextFactory<ClarityBoardContext> _contextFactory;

    public DataAccessLogger(IDbContextFactory<ClarityBoardContext> contextFactory) => _contextFactory = contextFactory;

    public async Task LogAsync(
        Guid subjectId,
        Guid accessedByUserId,
        string accessType,
        string resourceType,
        Guid? resourceId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var log = DataAccessLog.Create(
            accessedEmployeeId: subjectId,
            accessedBy:         accessedByUserId,
            accessType:         accessType,
            resourceType:       resourceType,
            resourceId:         resourceId,
            ipAddress:          ipAddress,
            userAgent:          userAgent);

        await using var db = _contextFactory.CreateDbContext();
        db.DataAccessLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
