using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Asset;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Asset.Commands;

public record DisposeAssetCommand : IRequest<Guid>
{
    public required Guid AssetId { get; init; }
    public required DateOnly DisposalDate { get; init; }
    public decimal ProceedsAmount { get; init; }
    public string DisposalType { get; init; } = "sale";
    public string? Notes { get; init; }
}

public class DisposeAssetCommandValidator : AbstractValidator<DisposeAssetCommand>
{
    private static readonly string[] ValidDisposalTypes =
        ["sale", "scrap", "transfer", "write_off"];

    public DisposeAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.DisposalDate).NotEmpty();
        RuleFor(x => x.ProceedsAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DisposalType)
            .Must(t => ValidDisposalTypes.Contains(t))
            .WithMessage($"DisposalType must be one of: {string.Join(", ", ValidDisposalTypes)}.");
    }
}

public class DisposeAssetCommandHandler : IRequestHandler<DisposeAssetCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DisposeAssetCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        DisposeAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _db.FixedAssets
            .FirstOrDefaultAsync(
                a => a.Id == request.AssetId && a.EntityId == _currentUser.EntityId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Asset '{request.AssetId}' not found.");

        if (asset.Status == "disposed")
            throw new InvalidOperationException("Asset has already been disposed.");

        var disposal = AssetDisposal.Create(
            asset.Id,
            asset.EntityId,
            request.DisposalDate,
            request.DisposalType,
            request.ProceedsAmount,
            asset.BookValue,
            _currentUser.UserId,
            request.Notes);

        _db.AssetDisposals.Add(disposal);
        await _db.SaveChangesAsync(cancellationToken);

        return disposal.Id;
    }
}
