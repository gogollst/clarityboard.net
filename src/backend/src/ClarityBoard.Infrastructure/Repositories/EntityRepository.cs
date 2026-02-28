using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Repositories;

public class EntityRepository : IEntityRepository
{
    private readonly ClarityBoardContext _context;

    public EntityRepository(ClarityBoardContext context)
    {
        _context = context;
    }

    public async Task<LegalEntity?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.LegalEntities
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<List<LegalEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.LegalEntities
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public async Task<List<LegalEntity>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entityIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.EntityId)
            .Distinct()
            .ToListAsync(ct);

        return await _context.LegalEntities
            .Where(e => entityIds.Contains(e.Id) && e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(LegalEntity entity, CancellationToken ct = default)
    {
        await _context.LegalEntities.AddAsync(entity, ct);
    }

    public async Task UpdateAsync(LegalEntity entity, CancellationToken ct = default)
    {
        _context.LegalEntities.Update(entity);
        await Task.CompletedTask;
    }
}
