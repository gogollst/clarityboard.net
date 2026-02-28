using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Asset.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Asset.Queries;

public record GetAssetDetailQuery : IRequest<AssetDetailDto?>
{
    public Guid AssetId { get; init; }
}

public class GetAssetDetailQueryHandler
    : IRequestHandler<GetAssetDetailQuery, AssetDetailDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAssetDetailQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssetDetailDto?> Handle(
        GetAssetDetailQuery request, CancellationToken cancellationToken)
    {
        var asset = await _db.FixedAssets
            .Include(a => a.Schedules)
            .FirstOrDefaultAsync(
                a => a.Id == request.AssetId && a.EntityId == _currentUser.EntityId,
                cancellationToken);

        if (asset is null)
            return null;

        var disposal = await _db.AssetDisposals
            .FirstOrDefaultAsync(d => d.AssetId == asset.Id, cancellationToken);

        return new AssetDetailDto
        {
            Id = asset.Id,
            EntityId = asset.EntityId,
            AssetNumber = asset.AssetNumber,
            Name = asset.Name,
            Description = asset.Description,
            AssetCategory = asset.AssetCategory,
            AcquisitionCost = asset.AcquisitionCost,
            AcquisitionDate = asset.AcquisitionDate,
            InServiceDate = asset.InServiceDate,
            DepreciationMethod = asset.DepreciationMethod,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            ResidualValue = asset.ResidualValue,
            AccumulatedDepreciation = asset.AccumulatedDepreciation,
            BookValue = asset.BookValue,
            Status = asset.Status,
            Location = asset.Location,
            SerialNumber = asset.SerialNumber,
            AfaCode = asset.AfaCode,
            CreatedAt = asset.CreatedAt,
            Schedules = asset.Schedules
                .OrderBy(s => s.PeriodDate)
                .Select(s => new DepreciationScheduleDto
                {
                    Id = s.Id,
                    PeriodDate = s.PeriodDate,
                    DepreciationAmount = s.DepreciationAmount,
                    AccumulatedAmount = s.AccumulatedAmount,
                    BookValueAfter = s.BookValueAfter,
                    IsPosted = s.IsPosted,
                    PostedAt = s.PostedAt,
                }).ToList(),
            Disposal = disposal is null ? null : new AssetDisposalDto
            {
                Id = disposal.Id,
                DisposalDate = disposal.DisposalDate,
                DisposalType = disposal.DisposalType,
                DisposalProceeds = disposal.DisposalProceeds,
                BookValueAtDisposal = disposal.BookValueAtDisposal,
                GainLoss = disposal.GainLoss,
                Notes = disposal.Notes,
                CreatedAt = disposal.CreatedAt,
            },
        };
    }
}
