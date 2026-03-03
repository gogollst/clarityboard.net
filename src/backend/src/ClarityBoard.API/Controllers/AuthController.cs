using ClarityBoard.Application.Features.Auth.Commands;
using ClarityBoard.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthController(ISender mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var enrichedCommand = command with { IpAddress = ipAddress, UserAgent = userAgent };
        var result = await _mediator.Send(enrichedCommand, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenCommand command, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var enrichedCommand = command with { IpAddress = ipAddress, UserAgent = userAgent };
        var result = await _mediator.Send(enrichedCommand, ct);
        return Ok(result);
    }

    // ── Password Reset ────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent(); // Always 204 – prevents email enumeration
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViaTokenCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // ── Invitation Acceptance ─────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("accept-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // ── Two-Factor Authentication ─────────────────────────────────────

    [Authorize]
    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(Setup2FAResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Setup2FAResponse>> Setup2FA(CancellationToken ct)
    {
        var result = await _mediator.Send(new Setup2FACommand(), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("2fa/verify")]
    [ProducesResponseType(typeof(Verify2FAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Verify2FAResponse>> Verify2FA(Verify2FACommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
