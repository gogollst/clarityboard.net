using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record BusinessPartnerListItemDto(
    Guid Id,
    string PartnerNumber,
    string Name,
    bool IsCreditor,
    bool IsDebtor,
    bool IsActive,
    string? City,
    int OpenDocumentCount);

public record GetBusinessPartnersQuery(
    Guid EntityId,
    bool? IsCreditor = null,
    bool? IsDebtor = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<BusinessPartnerListItemDto>>, IEntityScoped;

public class GetBusinessPartnersQueryHandler : IRequestHandler<GetBusinessPartnersQuery, PagedResult<BusinessPartnerListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetBusinessPartnersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<BusinessPartnerListItemDto>> Handle(GetBusinessPartnersQuery request, CancellationToken ct)
    {
        var query = _db.BusinessPartners
            .Where(bp => bp.EntityId == request.EntityId);

        if (request.IsCreditor.HasValue)
            query = query.Where(bp => bp.IsCreditor == request.IsCreditor.Value);
        if (request.IsDebtor.HasValue)
            query = query.Where(bp => bp.IsDebtor == request.IsDebtor.Value);
        if (request.IsActive.HasValue)
            query = query.Where(bp => bp.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(bp =>
                bp.Name.ToLower().Contains(search) ||
                bp.PartnerNumber.ToLower().Contains(search) ||
                (bp.TaxId != null && bp.TaxId.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(bp => bp.PartnerNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(bp => new BusinessPartnerListItemDto(
                bp.Id,
                bp.PartnerNumber,
                bp.Name,
                bp.IsCreditor,
                bp.IsDebtor,
                bp.IsActive,
                bp.City,
                _db.Documents.Count(d => d.BusinessPartnerId == bp.Id && d.Status != "booked" && d.Status != "archived")))
            .ToListAsync(ct);

        return new PagedResult<BusinessPartnerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
