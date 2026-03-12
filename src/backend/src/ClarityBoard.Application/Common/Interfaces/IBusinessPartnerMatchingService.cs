using ClarityBoard.Domain.Entities.Accounting;

namespace ClarityBoard.Application.Common.Interfaces;

public enum PartnerMatchType { Exact, Fuzzy, None }

public record BusinessPartnerMatchResult(
    PartnerMatchType MatchType,
    BusinessPartner? MatchedPartner,
    List<BusinessPartner> SuggestedPartners);

public interface IBusinessPartnerMatchingService
{
    Task<BusinessPartnerMatchResult> MatchPartnerAsync(
        Guid entityId, string vendorName, string? taxId, string? iban, CancellationToken ct);
}
