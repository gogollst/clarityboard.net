using ClarityBoard.Application.Features.Entity.Commands;
using ClarityBoard.Application.Features.Entity.DTOs;
using ClarityBoard.Application.Features.Entity.Queries;
using ClarityBoard.Application.Features.Hr.Commands;
using ClarityBoard.Application.Features.Hr.Queries;
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

    [HttpGet("{entityId:guid}/departments")]
    public async Task<IActionResult> GetDepartmentTree(Guid entityId, CancellationToken ct)
    {
        var tree = await _mediator.Send(new GetDepartmentTreeQuery { EntityId = entityId }, ct);
        return Ok(tree);
    }

    [HttpPost("{entityId:guid}/departments")]
    public async Task<IActionResult> CreateDepartment(Guid entityId, [FromBody] CreateDepartmentBodyRequest body, CancellationToken ct)
    {
        await _mediator.Send(new CreateDepartmentCommand
        {
            EntityId = entityId,
            Name = body.Name,
            Code = body.Code,
            Description = body.Description,
            ParentDepartmentId = body.ParentDepartmentId,
            ManagerId = body.ManagerId,
        }, ct);
        return Ok();
    }

    [HttpPut("{entityId:guid}/departments/{departmentId:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid entityId, Guid departmentId, [FromBody] UpdateDepartmentBodyRequest body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            EntityId = entityId,
            Name = body.Name,
            Code = body.Code,
            Description = body.Description,
            ParentDepartmentId = body.ParentDepartmentId,
            ManagerId = body.ManagerId,
        }, ct);
        return Ok();
    }

    [HttpDelete("{entityId:guid}/departments/{departmentId:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid entityId, Guid departmentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDepartmentCommand { DepartmentId = departmentId, EntityId = entityId }, ct);
        return Ok();
    }

}

public record SetEntityActiveRequest(bool IsActive);
public record CreateDepartmentBodyRequest(
    string Name, string Code, string? Description,
    Guid? ParentDepartmentId, Guid? ManagerId);
public record UpdateDepartmentBodyRequest(
    string Name, string Code, string? Description,
    Guid? ParentDepartmentId, Guid? ManagerId);
