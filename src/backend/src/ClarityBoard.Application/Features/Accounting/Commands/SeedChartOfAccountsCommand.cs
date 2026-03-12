using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.plan")]
public record SeedChartOfAccountsCommand : IRequest<int>;

public class SeedChartOfAccountsCommandHandler : IRequestHandler<SeedChartOfAccountsCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IChartOfAccountsSeeder _seeder;

    public SeedChartOfAccountsCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IChartOfAccountsSeeder seeder)
    {
        _db = db;
        _currentUser = currentUser;
        _seeder = seeder;
    }

    public async Task<int> Handle(SeedChartOfAccountsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LegalEntities
            .FirstOrDefaultAsync(e => e.Id == _currentUser.EntityId, cancellationToken)
            ?? throw new InvalidOperationException("Entity not found.");

        var existingCount = await _db.Accounts
            .CountAsync(a => a.EntityId == _currentUser.EntityId, cancellationToken);

        if (existingCount > 0)
            throw new InvalidOperationException(
                $"Entity already has {existingCount} accounts. Seeding is only allowed for entities with no accounts.");

        await _seeder.SeedAsync(_currentUser.EntityId, entity.ChartOfAccounts, cancellationToken);

        return await _db.Accounts.CountAsync(a => a.EntityId == _currentUser.EntityId, cancellationToken);
    }
}
