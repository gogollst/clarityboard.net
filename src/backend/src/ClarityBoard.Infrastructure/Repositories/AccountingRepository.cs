using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Repositories;

public class AccountingRepository : IAccountingRepository
{
    private readonly ClarityBoardContext _context;

    public AccountingRepository(ClarityBoardContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetAccountAsync(Guid entityId, string accountNumber, CancellationToken ct = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId && a.AccountNumber == accountNumber, ct);
    }

    public async Task<List<Account>> GetChartOfAccountsAsync(Guid entityId, CancellationToken ct = default)
    {
        return await _context.Accounts
            .Where(a => a.EntityId == entityId && a.IsActive)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(ct);
    }

    public async Task<JournalEntry?> GetJournalEntryAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync(Guid entityId, int year, int month, CancellationToken ct = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.EntityId == entityId && j.EntryDate.Year == year && j.EntryDate.Month == month)
            .OrderBy(j => j.EntryNumber)
            .ToListAsync(ct);
    }

    public async Task<long> GetNextEntryNumberAsync(Guid entityId, CancellationToken ct = default)
    {
        var max = await _context.JournalEntries
            .Where(j => j.EntityId == entityId)
            .MaxAsync(j => (long?)j.EntryNumber, ct);
        return (max ?? 0) + 1;
    }

    public async Task AddJournalEntryAsync(JournalEntry entry, CancellationToken ct = default)
    {
        await _context.JournalEntries.AddAsync(entry, ct);
    }

    public async Task AddAccountAsync(Account account, CancellationToken ct = default)
    {
        await _context.Accounts.AddAsync(account, ct);
    }
}
