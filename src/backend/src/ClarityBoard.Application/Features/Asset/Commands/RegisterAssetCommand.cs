using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Asset;
using ClarityBoard.Domain.Services;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Asset.Commands;

public record RegisterAssetCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AssetCategory { get; init; }
    public required Guid AssetAccountId { get; init; }
    public required Guid DepreciationAccountId { get; init; }
    public required decimal AcquisitionCost { get; init; }
    public required DateOnly AcquisitionDate { get; init; }
    public int UsefulLifeYears { get; init; }
    public string DepreciationMethod { get; init; } = "straight_line";
    public decimal ResidualValue { get; init; }
    public string? Location { get; init; }
    public string? SerialNumber { get; init; }
    public string? AfaCode { get; init; }
}

public class RegisterAssetCommandValidator : AbstractValidator<RegisterAssetCommand>
{
    private static readonly string[] ValidCategories =
        ["land", "buildings", "equipment", "vehicles", "intangible", "financial"];

    private static readonly string[] ValidMethods =
        ["straight_line", "declining_balance", "units_of_production"];

    public RegisterAssetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetCategory)
            .NotEmpty()
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"AssetCategory must be one of: {string.Join(", ", ValidCategories)}.");
        RuleFor(x => x.AssetAccountId).NotEmpty();
        RuleFor(x => x.DepreciationAccountId).NotEmpty();
        RuleFor(x => x.AcquisitionCost).GreaterThan(0);
        RuleFor(x => x.AcquisitionDate).NotEmpty();
        RuleFor(x => x.UsefulLifeYears).InclusiveBetween(1, 100);
        RuleFor(x => x.DepreciationMethod)
            .Must(m => ValidMethods.Contains(m))
            .WithMessage($"DepreciationMethod must be one of: {string.Join(", ", ValidMethods)}.");
        RuleFor(x => x.ResidualValue).GreaterThanOrEqualTo(0);
    }
}

public class RegisterAssetCommandHandler : IRequestHandler<RegisterAssetCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDepreciationService _depreciationService;

    public RegisterAssetCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IDepreciationService depreciationService)
    {
        _db = db;
        _currentUser = currentUser;
        _depreciationService = depreciationService;
    }

    public async Task<Guid> Handle(
        RegisterAssetCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        // Generate asset number
        var assetCount = _db.FixedAssets.Count(a => a.EntityId == entityId);
        var assetNumber = $"FA-{assetCount + 1:D6}";

        var asset = FixedAsset.Create(
            entityId,
            assetNumber,
            request.Name,
            request.AssetCategory,
            request.AssetAccountId,
            request.DepreciationAccountId,
            request.AcquisitionCost,
            request.AcquisitionDate,
            request.UsefulLifeYears * 12,
            _currentUser.UserId,
            request.DepreciationMethod,
            request.ResidualValue,
            request.Description);

        // Pre-calculate the full depreciation schedule
        var schedules = _depreciationService.CalculateSchedule(asset);
        foreach (var schedule in schedules)
        {
            asset.AddSchedule(schedule);
        }

        _db.FixedAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
