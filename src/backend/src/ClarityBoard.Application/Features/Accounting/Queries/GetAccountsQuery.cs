using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Accounting.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record GetAccountsQuery : IRequest<IReadOnlyList<AccountDto>>
{
    public string? AccountType { get; init; }
    public int? AccountClass { get; init; }
    public string? Search { get; init; }
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

        if (request.AccountClass.HasValue)
            query = query.Where(a => a.AccountClass == request.AccountClass.Value);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(search) ||
                a.AccountNumber.Contains(search));
        }

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
