namespace ClarityBoard.Application.Common.Interfaces;

public interface IChartOfAccountsSeeder
{
    Task SeedAsync(Guid entityId, string chartOfAccounts, CancellationToken ct);
}
