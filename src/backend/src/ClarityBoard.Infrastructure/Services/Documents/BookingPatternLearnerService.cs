using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Document;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services.Documents;

public class BookingPatternLearnerService : IBookingPatternLearner
{
    private readonly IAppDbContext _db;

    public BookingPatternLearnerService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task LearnFromDecisionAsync(Guid entityId, string? vendorName, Guid? businessPartnerId,
        Guid debitAccountId, Guid creditAccountId, string? vatCode, Guid? hrEmployeeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorName))
            return;

        var pattern = await _db.RecurringPatterns
            .FirstOrDefaultAsync(p => p.EntityId == entityId
                && p.IsActive
                && p.VendorName.ToLower() == vendorName.ToLower(), ct);

        if (pattern is not null)
        {
            if (pattern.DebitAccountId != debitAccountId || pattern.CreditAccountId != creditAccountId || pattern.VatCode != vatCode)
                pattern.UpdateAccounts(debitAccountId, creditAccountId, vatCode);

            pattern.IncrementMatch();
            pattern.SetEmployee(hrEmployeeId);

            if (businessPartnerId.HasValue)
                pattern.AssignBusinessPartner(businessPartnerId);
        }
        else
        {
            var newPattern = RecurringPattern.Create(
                entityId: entityId,
                vendorName: vendorName,
                debitAccountId: debitAccountId,
                creditAccountId: creditAccountId,
                vatCode: vatCode,
                costCenter: null,
                confidence: 0.60m);

            newPattern.SetEmployee(hrEmployeeId);

            if (businessPartnerId.HasValue)
                newPattern.AssignBusinessPartner(businessPartnerId);

            _db.RecurringPatterns.Add(newPattern);
        }
    }
}
