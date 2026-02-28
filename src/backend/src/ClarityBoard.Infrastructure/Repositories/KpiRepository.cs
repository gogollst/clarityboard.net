using ClarityBoard.Domain.Entities.KPI;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Repositories;

public class KpiRepository : IKpiRepository
{
    private readonly ClarityBoardContext _context;

    public KpiRepository(ClarityBoardContext context)
    {
        _context = context;
    }

    public async Task<List<KpiDefinition>> GetDefinitionsAsync(string? domain = null, CancellationToken ct = default)
    {
        var query = _context.KpiDefinitions.Where(k => k.IsActive);

        if (!string.IsNullOrEmpty(domain))
            query = query.Where(k => k.Domain == domain);

        return await query.OrderBy(k => k.DisplayOrder).ToListAsync(ct);
    }

    public async Task<KpiSnapshot?> GetLatestSnapshotAsync(Guid entityId, string kpiId, CancellationToken ct = default)
    {
        return await _context.KpiSnapshots
            .Where(s => s.EntityId == entityId && s.KpiId == kpiId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<KpiSnapshot>> GetSnapshotsAsync(Guid entityId, string kpiId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _context.KpiSnapshots
            .Where(s => s.EntityId == entityId && s.KpiId == kpiId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);
    }

    public async Task<List<KpiSnapshot>> GetDashboardSnapshotsAsync(Guid entityId, string role, CancellationToken ct = default)
    {
        // Get latest snapshot date
        var latestDate = await _context.KpiSnapshots
            .Where(s => s.EntityId == entityId)
            .MaxAsync(s => (DateOnly?)s.SnapshotDate, ct);

        if (latestDate is null)
            return [];

        // Get domain filter based on role
        var domains = role.ToLowerInvariant() switch
        {
            "admin" or "executive" => new[] { "financial", "sales", "marketing", "hr", "general" },
            "finance" => new[] { "financial", "general" },
            "sales" => new[] { "sales", "general" },
            "hr" => new[] { "hr", "general" },
            _ => new[] { "financial", "general" },
        };

        return await _context.KpiSnapshots
            .Join(_context.KpiDefinitions,
                s => s.KpiId,
                d => d.Id,
                (s, d) => new { Snapshot = s, Definition = d })
            .Where(x => x.Snapshot.EntityId == entityId
                && x.Snapshot.SnapshotDate == latestDate.Value
                && domains.Contains(x.Definition.Domain))
            .Select(x => x.Snapshot)
            .ToListAsync(ct);
    }

    public async Task AddSnapshotAsync(KpiSnapshot snapshot, CancellationToken ct = default)
    {
        await _context.KpiSnapshots.AddAsync(snapshot, ct);
    }

    public async Task AddSnapshotsAsync(IEnumerable<KpiSnapshot> snapshots, CancellationToken ct = default)
    {
        await _context.KpiSnapshots.AddRangeAsync(snapshots, ct);
    }
}
