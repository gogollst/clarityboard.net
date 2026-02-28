namespace ClarityBoard.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? entityId, string action, string tableName, string? recordId,
        string? oldValues, string? newValues, Guid? userId, string? ipAddress, string? userAgent,
        CancellationToken ct = default);

    Task<bool> VerifyChainIntegrityAsync(Guid? entityId, CancellationToken ct = default);
}
