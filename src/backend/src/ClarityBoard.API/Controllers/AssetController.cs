using ClarityBoard.Application.Features.Asset.Commands;
using ClarityBoard.Application.Features.Asset.DTOs;
using ClarityBoard.Application.Features.Asset.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AssetController : ControllerBase
{
    private readonly ISender _mediator;

    public AssetController(ISender mediator) => _mediator = mediator;

    // ── List Assets ──

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssetListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetListDto>>> GetAssets(
        [FromQuery] Guid entityId,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAssetsQuery
        {
            EntityId = entityId,
            Category = category,
            Status = status,
        }, ct);
        return Ok(result);
    }

    // ── Get Asset Detail ──

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDetailDto>> GetAssetDetail(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAssetDetailQuery { AssetId = id }, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    // ── Register Asset ──

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> RegisterAsset(
        [FromBody] RegisterAssetCommand command, CancellationToken ct)
    {
        var assetId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAssetDetail), new { id = assetId }, assetId);
    }

    // ── Dispose Asset ──

    [HttpPost("{id:guid}/dispose")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> DisposeAsset(
        Guid id, [FromBody] DisposeAssetCommand command, CancellationToken ct)
    {
        var cmd = command with { AssetId = id };
        var disposalId = await _mediator.Send(cmd, ct);
        return Ok(disposalId);
    }

    // ── Anlagenspiegel (Asset Movement Report) ──

    [HttpGet("anlagenspiegel")]
    [ProducesResponseType(typeof(AnlagenspiegelDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnlagenspiegelDto>> GetAnlagenspiegel(
        [FromQuery] Guid entityId,
        [FromQuery] short fiscalYear,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAnlagenspiegelQuery
        {
            EntityId = entityId,
            FiscalYear = fiscalYear,
        }, ct);
        return Ok(result);
    }
}
