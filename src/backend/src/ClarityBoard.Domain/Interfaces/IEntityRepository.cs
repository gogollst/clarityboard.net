using ClarityBoard.Domain.Entities.Entity;

namespace ClarityBoard.Domain.Interfaces;

public interface IEntityRepository
{
    Task<LegalEntity?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<LegalEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<LegalEntity>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(LegalEntity entity, CancellationToken ct = default);
    Task UpdateAsync(LegalEntity entity, CancellationToken ct = default);
}
