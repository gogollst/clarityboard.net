namespace ClarityBoard.Application.Common.Interfaces;

public interface IBookingPatternLearner
{
    Task LearnFromDecisionAsync(Guid entityId, string? vendorName, Guid? businessPartnerId,
        Guid debitAccountId, Guid creditAccountId, string? vatCode, Guid? hrEmployeeId, CancellationToken ct);
}
