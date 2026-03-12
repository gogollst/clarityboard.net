using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record DeactivateBusinessPartnerCommand(Guid Id) : IRequest;

public class DeactivateBusinessPartnerCommandHandler : IRequestHandler<DeactivateBusinessPartnerCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeactivateBusinessPartnerCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateBusinessPartnerCommand request, CancellationToken ct)
    {
        var partner = await _db.BusinessPartners
            .FirstOrDefaultAsync(bp => bp.Id == request.Id && bp.EntityId == _currentUser.EntityId, ct)
            ?? throw new InvalidOperationException("Business partner not found.");

        partner.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}
