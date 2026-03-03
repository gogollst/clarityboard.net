using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;

namespace ClarityBoard.Infrastructure.Services.Hr;

public class DataAccessLogger : IDataAccessLogger
{
    private readonly IAppDbContext _db;

    public DataAccessLogger(IAppDbContext db) => _db = db;

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

        _db.DataAccessLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
