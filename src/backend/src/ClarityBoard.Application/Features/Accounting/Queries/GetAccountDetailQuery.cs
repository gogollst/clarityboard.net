using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Accounting.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetAccountDetailQuery(Guid Id) : IRequest<AccountDetailDto>;

public class GetAccountDetailQueryHandler : IRequestHandler<GetAccountDetailQuery, AccountDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAccountDetailQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AccountDetailDto> Handle(
        GetAccountDetailQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .Where(a => a.Id == request.Id && a.EntityId == _currentUser.EntityId)
            .Select(a => new AccountDetailDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                Name = a.Name,
                AccountType = a.AccountType,
                AccountClass = a.AccountClass,
                IsActive = a.IsActive,
                VatDefault = a.VatDefault,
                DatevAuto = a.DatevAuto,
                CostCenterDefault = a.CostCenterDefault,
                BwaLine = a.BwaLine,
                IsAutoPosting = a.IsAutoPosting,
                IsSystemAccount = a.IsSystemAccount,
                ParentId = a.ParentId,
                NameDe = a.NameDe,
                NameEn = a.NameEn,
                NameRu = a.NameRu,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        account.JournalEntryCount = await _db.JournalEntryLines
            .Where(l => l.AccountId == request.Id)
            .Join(_db.JournalEntries.Where(e => e.EntityId == _currentUser.EntityId),
                l => l.JournalEntryId, e => e.Id, (l, e) => l)
            .CountAsync(cancellationToken);

        account.LastBookingDate = await _db.JournalEntryLines
            .Where(l => l.AccountId == request.Id)
            .Join(_db.JournalEntries.Where(e => e.EntityId == _currentUser.EntityId),
                l => l.JournalEntryId, e => e.Id, (l, e) => e.EntryDate)
            .OrderByDescending(d => d)
            .Select(d => (DateOnly?)d)
            .FirstOrDefaultAsync(cancellationToken);

        return account;
    }
}
