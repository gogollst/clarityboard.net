using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record AssignDocumentPartnerCommand(Guid DocumentId, Guid? BusinessPartnerId) : IRequest;

public class AssignDocumentPartnerCommandHandler : IRequestHandler<AssignDocumentPartnerCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AssignDocumentPartnerCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(AssignDocumentPartnerCommand request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == _currentUser.EntityId, ct)
            ?? throw new InvalidOperationException("Document not found.");

        if (request.BusinessPartnerId.HasValue)
        {
            var partnerExists = await _db.BusinessPartners
                .AnyAsync(bp => bp.Id == request.BusinessPartnerId.Value && bp.EntityId == _currentUser.EntityId, ct);

            if (!partnerExists)
                throw new InvalidOperationException("Business partner not found.");
        }

        document.AssignBusinessPartner(request.BusinessPartnerId);
        await _db.SaveChangesAsync(ct);
    }
}
