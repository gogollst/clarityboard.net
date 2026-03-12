using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Persistence.Seed;

public class ChartOfAccountsSeeder : IChartOfAccountsSeeder
{
    private readonly ClarityBoardContext _context;
    private readonly ILogger<ChartOfAccountsSeeder> _logger;

    public ChartOfAccountsSeeder(ClarityBoardContext context, ILogger<ChartOfAccountsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(Guid entityId, string chartOfAccounts, CancellationToken ct)
    {
        if (chartOfAccounts != "SKR03")
        {
            _logger.LogWarning("Chart of accounts '{ChartOfAccounts}' is not supported for seeding", chartOfAccounts);
            return;
        }

        var existingCount = await _context.Accounts
            .CountAsync(a => a.EntityId == entityId, ct);

        if (existingCount > 0)
        {
            _logger.LogInformation(
                "Entity {EntityId} already has {Count} accounts, skipping seed",
                entityId, existingCount);
            return;
        }

        var accounts = Skr03Accounts.All.Select(a =>
            Account.Create(
                entityId,
                a.Number,
                a.Name,
                a.AccountType,
                a.AccountClass,
                vatDefault: a.VatDefault)).ToList();

        _context.Accounts.AddRange(accounts);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Seeded {Count} SKR03 accounts for entity {EntityId}",
            accounts.Count, entityId);
    }
}
