using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

/// <summary>
/// Confirms a fuzzy partner match: assigns the confirmed BusinessPartner to the Document
/// and updates the RecurringPattern so future documents with the same vendor are matched automatically.
/// </summary>
public record ConfirmPartnerMatchCommand(Guid DocumentId, Guid BusinessPartnerId) : IRequest;

public class ConfirmPartnerMatchCommandHandler : IRequestHandler<ConfirmPartnerMatchCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ConfirmPartnerMatchCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ConfirmPartnerMatchCommand request, CancellationToken ct)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == _currentUser.EntityId, ct)
            ?? throw new InvalidOperationException("Document not found.");

        var partnerExists = await _db.BusinessPartners
            .AnyAsync(bp => bp.Id == request.BusinessPartnerId && bp.EntityId == _currentUser.EntityId, ct);

        if (!partnerExists)
            throw new InvalidOperationException("Business partner not found.");

        // Assign the confirmed partner
        document.AssignBusinessPartner(request.BusinessPartnerId);

        // Update RecurringPattern for future automatic matching
        if (!string.IsNullOrEmpty(document.VendorName))
        {
            var pattern = await _db.RecurringPatterns
                .FirstOrDefaultAsync(rp => rp.EntityId == _currentUser.EntityId
                    && rp.VendorName == document.VendorName, ct);

            if (pattern is not null)
            {
                pattern.AssignBusinessPartner(request.BusinessPartnerId);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
