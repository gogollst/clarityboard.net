using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Asset.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Asset.Queries;

public record GetAnlagenspiegelQuery : IRequest<AnlagenspiegelDto>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public short FiscalYear { get; init; }
}

public class GetAnlagenspiegelQueryHandler
    : IRequestHandler<GetAnlagenspiegelQuery, AnlagenspiegelDto>
{
    private readonly IAppDbContext _db;

    public GetAnlagenspiegelQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AnlagenspiegelDto> Handle(
        GetAnlagenspiegelQuery request, CancellationToken cancellationToken)
    {
        var yearStart = new DateOnly(request.FiscalYear, 1, 1);
        var yearEnd = new DateOnly(request.FiscalYear, 12, 31);

        // Get all assets for this entity (including disposed ones for reporting)
        var assets = await _db.FixedAssets
            .Where(a => a.EntityId == request.EntityId)
            .Include(a => a.Schedules)
            .ToListAsync(cancellationToken);

        // Get disposals for the year
        var disposals = await _db.AssetDisposals
            .Where(d => d.EntityId == request.EntityId
                && d.DisposalDate >= yearStart
                && d.DisposalDate <= yearEnd)
            .ToListAsync(cancellationToken);

        // Group by category
        var categories = assets
            .GroupBy(a => a.AssetCategory)
            .Select(group =>
            {
                var categoryAssets = group.ToList();

                // Assets acquired before the fiscal year
                var existingAssets = categoryAssets
                    .Where(a => a.AcquisitionDate < yearStart)
                    .ToList();

                // New acquisitions during the year
                var additions = categoryAssets
                    .Where(a => a.AcquisitionDate >= yearStart && a.AcquisitionDate <= yearEnd)
                    .ToList();

                // Disposals in this category during the year
                var categoryDisposals = disposals
                    .Where(d => categoryAssets.Any(a => a.Id == d.AssetId))
                    .ToList();

                // Opening cost: existing assets' acquisition cost
                var openingCost = existingAssets.Sum(a => a.AcquisitionCost);

                // Additions: new acquisitions
                var additionsCost = additions.Sum(a => a.AcquisitionCost);

                // Disposals: book value at disposal
                var disposalsCost = categoryDisposals.Sum(d => d.BookValueAtDisposal);

                // Depreciation charge during the year
                var depreciationCharge = categoryAssets
                    .SelectMany(a => a.Schedules)
                    .Where(s => s.PeriodDate >= yearStart && s.PeriodDate <= yearEnd)
                    .Sum(s => s.DepreciationAmount);

                // Accumulated depreciation (total)
                var accumulatedDepreciation = categoryAssets
                    .Sum(a => a.AccumulatedDepreciation);

                // Closing book value
                var closingBookValue = categoryAssets
                    .Where(a => a.Status != "disposed")
                    .Sum(a => a.BookValue);

                return new AnlagenspiegelCategoryDto
                {
                    Category = group.Key,
                    OpeningCost = openingCost,
                    Additions = additionsCost,
                    Disposals = disposalsCost,
                    DepreciationCharge = depreciationCharge,
                    AccumulatedDepreciation = accumulatedDepreciation,
                    ClosingBookValue = closingBookValue,
                    AssetCount = categoryAssets.Count,
                };
            })
            .OrderBy(c => c.Category)
            .ToList();

        return new AnlagenspiegelDto
        {
            EntityId = request.EntityId,
            FiscalYear = request.FiscalYear,
            Categories = categories,
            TotalOpeningCost = categories.Sum(c => c.OpeningCost),
            TotalAdditions = categories.Sum(c => c.Additions),
            TotalDisposals = categories.Sum(c => c.Disposals),
            TotalDepreciation = categories.Sum(c => c.DepreciationCharge),
            TotalClosingBookValue = categories.Sum(c => c.ClosingBookValue),
        };
    }
}
