namespace ClarityBoard.Application.Common.Interfaces;

public interface IDataAccessLogger
{
    Task LogAsync(
        Guid accessedEmployeeId,
        Guid accessedByUserId,
        string accessType,
        string resourceType,
        Guid? resourceId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);
}
