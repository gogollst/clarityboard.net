using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Admin.Commands;
using ClarityBoard.Application.Features.Admin.DTOs;
using ClarityBoard.Application.Features.Admin.Queries;
using AuthConfigResponse = ClarityBoard.Application.Features.Admin.Queries.AuthConfigResponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ── User Management ───────────────────────────────────────────────

    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            IsActive = isActive,
        }, ct);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> GetUserDetail(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserDetailQuery { UserId = userId }, ct);
        return Ok(result);
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(CreateUserCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetUserDetail), new { userId = result.UserId }, result);
    }

    [HttpPut("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid userId, UpdateUserRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateUserCommand
        {
            UserId = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
        }, ct);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateUserCommand { UserId = userId }, ct);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUser(Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new ReactivateUserCommand { UserId = userId }, ct);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand { UserId = userId }, ct);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/resend-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendInvitation(Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new ResendInvitationCommand { UserId = userId }, ct);
        return NoContent();
    }

    // ── Role Management ───────────────────────────────────────────────

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRolesQuery(), ct);
        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid userId, AssignRoleRequest request, CancellationToken ct)
    {
        await _mediator.Send(new AssignRoleCommand
        {
            UserId = userId,
            RoleId = request.RoleId,
            EntityId = request.EntityId,
        }, ct);
        return NoContent();
    }

    [HttpDelete("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid userId, [FromQuery] Guid roleId, [FromQuery] Guid entityId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveRoleCommand
        {
            UserId = userId,
            RoleId = roleId,
            EntityId = entityId,
        }, ct);
        return NoContent();
    }

    // ── Entity Access ─────────────────────────────────────────────────

    [HttpPost("users/{userId:guid}/entities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignEntity(Guid userId, AssignEntityRequest request, CancellationToken ct)
    {
        await _mediator.Send(new AssignEntityCommand
        {
            UserId = userId,
            EntityId = request.EntityId,
            RoleId = request.RoleId,
        }, ct);
        return NoContent();
    }

    // ── Auth Config ───────────────────────────────────────────────────

    [HttpGet("auth-config")]
    [ProducesResponseType(typeof(AuthConfigResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthConfigResponse>> GetAuthConfig(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAuthConfigQuery(), ct);
        return Ok(result);
    }

    [HttpPut("auth-config")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertAuthConfig([FromBody] UpsertAuthConfigCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // ── Audit Logs ────────────────────────────────────────────────────

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search,
        }, ct);
        return Ok(result);
    }

    [HttpGet("audit-logs/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogsCsv(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var csv = await _mediator.Send(new ExportAuditLogsCsvQuery
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search,
        }, ct);

        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(csv, "text/csv; charset=utf-8", fileName);
    }

    // ── Product Category Mappings ────────────────────────────────────────

    [HttpGet("product-mappings")]
    public async Task<ActionResult<IReadOnlyList<ProductMappingDto>>> GetProductMappings(
        [FromQuery] Guid entityId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductMappingsQuery(entityId), ct);
        return Ok(result);
    }

    [HttpPut("product-mappings")]
    public async Task<ActionResult<Guid>> UpsertProductMapping(
        [FromBody] UpsertProductMappingCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Ok(id);
    }

    [HttpDelete("product-mappings/{mappingId:guid}")]
    public async Task<IActionResult> DeleteProductMapping(
        Guid mappingId, [FromQuery] Guid entityId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProductMappingCommand(entityId, mappingId), ct);
        return NoContent();
    }
}

// ── Request Models ────────────────────────────────────────────────────

public record UpdateUserRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}

public record AssignRoleRequest
{
    public required Guid RoleId { get; init; }
    public required Guid EntityId { get; init; }
}

public record AssignEntityRequest
{
    public required Guid EntityId { get; init; }
    public Guid? RoleId { get; init; }
}
