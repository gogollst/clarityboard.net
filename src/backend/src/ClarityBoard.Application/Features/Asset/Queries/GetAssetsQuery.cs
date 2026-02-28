using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Asset.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Asset.Queries;

public record GetAssetsQuery : IRequest<IReadOnlyList<AssetListDto>>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public string? Category { get; init; }
    public string? Status { get; init; }
}

public class GetAssetsQueryHandler
    : IRequestHandler<GetAssetsQuery, IReadOnlyList<AssetListDto>>
{
    private readonly IAppDbContext _db;

    public GetAssetsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssetListDto>> Handle(
        GetAssetsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FixedAssets
            .Where(a => a.EntityId == request.EntityId);

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(a => a.AssetCategory == request.Category);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(a => a.Status == request.Status);

        return await query
            .OrderBy(a => a.AssetNumber)
            .Select(a => new AssetListDto
            {
                Id = a.Id,
                EntityId = a.EntityId,
                AssetNumber = a.AssetNumber,
                Name = a.Name,
                AssetCategory = a.AssetCategory,
                AcquisitionCost = a.AcquisitionCost,
                AcquisitionDate = a.AcquisitionDate,
                DepreciationMethod = a.DepreciationMethod,
                BookValue = a.BookValue,
                Status = a.Status,
                Location = a.Location,
            })
            .ToListAsync(cancellationToken);
    }
}
