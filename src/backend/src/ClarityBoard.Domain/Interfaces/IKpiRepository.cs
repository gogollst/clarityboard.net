using ClarityBoard.Domain.Entities.KPI;

namespace ClarityBoard.Domain.Interfaces;

public interface IKpiRepository
{
    Task<List<KpiDefinition>> GetDefinitionsAsync(string? domain = null, CancellationToken ct = default);
    Task<KpiSnapshot?> GetLatestSnapshotAsync(Guid entityId, string kpiId, CancellationToken ct = default);
    Task<List<KpiSnapshot>> GetSnapshotsAsync(Guid entityId, string kpiId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<List<KpiSnapshot>> GetDashboardSnapshotsAsync(Guid entityId, string role, CancellationToken ct = default);
    Task AddSnapshotAsync(KpiSnapshot snapshot, CancellationToken ct = default);
    Task AddSnapshotsAsync(IEnumerable<KpiSnapshot> snapshots, CancellationToken ct = default);
}
