using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record BusinessPartnerSearchResultDto(
    Guid Id,
    string PartnerNumber,
    string Name,
    string? TaxId,
    bool IsCreditor,
    bool IsDebtor);

public record SearchBusinessPartnersQuery(Guid EntityId, string Query) : IRequest<List<BusinessPartnerSearchResultDto>>, IEntityScoped;

public class SearchBusinessPartnersQueryHandler : IRequestHandler<SearchBusinessPartnersQuery, List<BusinessPartnerSearchResultDto>>
{
    private readonly IAppDbContext _db;

    public SearchBusinessPartnersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<BusinessPartnerSearchResultDto>> Handle(SearchBusinessPartnersQuery request, CancellationToken ct)
    {
        var search = request.Query.ToLower();

        return await _db.BusinessPartners
            .Where(bp => bp.EntityId == request.EntityId && bp.IsActive)
            .Where(bp =>
                bp.Name.ToLower().Contains(search) ||
                bp.PartnerNumber.ToLower().Contains(search) ||
                (bp.TaxId != null && bp.TaxId.ToLower().Contains(search)) ||
                (bp.Iban != null && bp.Iban.ToLower().Contains(search)))
            .OrderBy(bp => bp.Name)
            .Take(20)
            .Select(bp => new BusinessPartnerSearchResultDto(
                bp.Id,
                bp.PartnerNumber,
                bp.Name,
                bp.TaxId,
                bp.IsCreditor,
                bp.IsDebtor))
            .ToListAsync(ct);
    }
}
