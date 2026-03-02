using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.UserProfile.Commands;
using ClarityBoard.Application.Features.UserProfile.DTOs;
using ClarityBoard.Application.Features.UserProfile.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class UserProfileController : ControllerBase
{
    private readonly ISender _mediator;

    public UserProfileController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Serves the avatar image for a given user. AllowAnonymous because the browser
    /// loads this via an &lt;img&gt; tag which cannot attach Authorization headers.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/api/avatars/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetAvatar(
        Guid userId,
        [FromServices] IAppDbContext db,
        [FromServices] IDocumentStorage documentStorage,
        CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user?.AvatarPath is null)
            return NotFound();

        // Find which entity bucket this user's files are stored in
        var entityId = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.EntityId)
            .FirstOrDefaultAsync(ct);

        if (entityId == Guid.Empty)
            return NotFound();

        var stream = await documentStorage.DownloadAsync(entityId, user.AvatarPath, ct);
        var contentType = user.AvatarPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";

        return File(stream, contentType);
    }

    [HttpPost("avatar")]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvatarUploadResponse>> UploadAvatar(
        IFormFile file,
        [FromServices] ICurrentUser currentUser,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UploadAvatarCommand
        {
            FileName = file.FileName,
            ContentStream = stream,
            ContentType = file.ContentType,
        }, ct);
        return Ok(new AvatarUploadResponse { AvatarUrl = $"/api/avatars/{currentUser.UserId}" });
    }

    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        await _mediator.Send(new DeleteAvatarCommand(), ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("2fa/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable2FA(Disable2FACommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }
}

public record AvatarUploadResponse
{
    public string AvatarUrl { get; init; } = default!;
}
