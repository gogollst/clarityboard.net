using ClarityBoard.Application.Features.Entity.Commands;
using ClarityBoard.Application.Features.Entity.DTOs;
using ClarityBoard.Application.Features.Entity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EntityController : ControllerBase
{
    private readonly ISender _mediator;

    public EntityController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LegalEntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LegalEntityDto>>> GetEntities(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEntitiesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LegalEntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LegalEntityDto>> CreateEntity(CreateEntityCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetEntities), result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LegalEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegalEntityDto>> UpdateEntity(Guid id, UpdateEntityCommand command, CancellationToken ct)
    {
        var commandWithId = command with { Id = id };
        var result = await _mediator.Send(commandWithId, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetEntityActive(Guid id, SetEntityActiveRequest body, CancellationToken ct)
    {
        await _mediator.Send(new SetEntityActiveCommand { Id = id, IsActive = body.IsActive }, ct);
        return NoContent();
    }
}

public record SetEntityActiveRequest(bool IsActive);
