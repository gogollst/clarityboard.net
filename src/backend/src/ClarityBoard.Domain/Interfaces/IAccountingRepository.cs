using ClarityBoard.Domain.Entities.Accounting;

namespace ClarityBoard.Domain.Interfaces;

public interface IAccountingRepository
{
    Task<Account?> GetAccountAsync(Guid entityId, string accountNumber, CancellationToken ct = default);
    Task<List<Account>> GetChartOfAccountsAsync(Guid entityId, CancellationToken ct = default);
    Task<JournalEntry?> GetJournalEntryAsync(Guid id, CancellationToken ct = default);
    Task<List<JournalEntry>> GetJournalEntriesAsync(Guid entityId, int year, int month, CancellationToken ct = default);
    Task<long> GetNextEntryNumberAsync(Guid entityId, CancellationToken ct = default);
    Task AddJournalEntryAsync(JournalEntry entry, CancellationToken ct = default);
    Task AddAccountAsync(Account account, CancellationToken ct = default);
}
