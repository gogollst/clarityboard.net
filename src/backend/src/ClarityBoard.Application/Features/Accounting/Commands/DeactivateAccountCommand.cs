using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.manage")]
public record DeactivateAccountCommand(Guid Id) : IRequest;

public class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeactivateAccountCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id
                && a.EntityId == _currentUser.EntityId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        if (account.IsSystemAccount)
            throw new InvalidOperationException("System accounts cannot be deactivated.");

        account.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
