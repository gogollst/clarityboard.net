using System.Security.Cryptography;
using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAppDbContext dbContext, ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogAsync(Guid? entityId, string action, string tableName, string? recordId,
        string? oldValues, string? newValues, Guid? userId, string? ipAddress, string? userAgent,
        CancellationToken ct = default)
    {
        // Get the previous hash for chain integrity (GoBD requirement)
        var previousHash = await GetLastHashAsync(entityId, ct);

        var auditLog = AuditLog.Create(
            entityId: entityId,
            action: action,
            tableName: tableName,
            recordId: recordId,
            oldValues: oldValues,
            newValues: newValues,
            userId: userId,
            ipAddress: ipAddress,
            userAgent: userAgent);

        // Build the data to hash (includes CreatedAt from the entity)
        var hashInput = $"{auditLog.EntityId}|{auditLog.Action}|{auditLog.TableName}|{auditLog.RecordId}|{auditLog.OldValues}|{auditLog.NewValues}|{auditLog.UserId}|{auditLog.CreatedAt:O}|{previousHash}";
        var hash = ComputeSha256(hashInput);

        auditLog.SetHash(hash, previousHash);

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> VerifyChainIntegrityAsync(Guid? entityId, CancellationToken ct = default)
    {
        var logs = await _dbContext.AuditLogs
            .Where(a => a.EntityId == entityId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        if (logs.Count == 0)
            return true;

        string? expectedPreviousHash = null;

        foreach (var log in logs)
        {
            if (log.PreviousHash != expectedPreviousHash)
            {
                _logger.LogError("Audit chain broken at log {LogId}. Expected previous hash {Expected}, got {Actual}",
                    log.Id, expectedPreviousHash, log.PreviousHash);
                return false;
            }

            // Recompute hash to verify content integrity
            var hashInput = $"{log.EntityId}|{log.Action}|{log.TableName}|{log.RecordId}|{log.OldValues}|{log.NewValues}|{log.UserId}|{log.CreatedAt:O}|{log.PreviousHash}";
            var expectedHash = ComputeSha256(hashInput);

            if (log.Hash != expectedHash)
            {
                _logger.LogError("Audit hash mismatch at log {LogId}. Expected {Expected}, got {Actual}",
                    log.Id, expectedHash, log.Hash);
                return false;
            }

            expectedPreviousHash = log.Hash;
        }

        return true;
    }

    private async Task<string?> GetLastHashAsync(Guid? entityId, CancellationToken ct)
    {
        return await _dbContext.AuditLogs
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.Hash)
            .FirstOrDefaultAsync(ct);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
