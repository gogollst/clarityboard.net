using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Accounting.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetAccountsQuery : IRequest<IReadOnlyList<AccountDto>>
{
    public string? AccountType { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAccountsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AccountDto>> Handle(
        GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Accounts.Where(a => a.EntityId == _currentUser.EntityId);

        if (request.ActiveOnly)
            query = query.Where(a => a.IsActive);

        if (!string.IsNullOrEmpty(request.AccountType))
            query = query.Where(a => a.AccountType == request.AccountType);

        return await query
            .OrderBy(a => a.AccountNumber)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                Name = a.Name,
                AccountType = a.AccountType,
                AccountClass = a.AccountClass,
                IsActive = a.IsActive,
                VatDefault = a.VatDefault,
            })
            .ToListAsync(cancellationToken);
    }
}
