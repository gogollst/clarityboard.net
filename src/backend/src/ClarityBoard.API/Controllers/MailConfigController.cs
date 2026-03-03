using ClarityBoard.Application.Features.Admin.Mail.Commands;
using ClarityBoard.Application.Features.Admin.Mail.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/mail")]
public class MailConfigController : ControllerBase
{
    private readonly ISender _mediator;

    public MailConfigController(ISender mediator) => _mediator = mediator;

    /// <summary>Gets the current SMTP configuration. Password is never returned.</summary>
    [HttpGet("config")]
    public async Task<ActionResult<MailConfigResponse?>> GetConfig(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMailConfigQuery(), ct);
        return Ok(result);
    }

    /// <summary>Creates or updates the SMTP configuration.</summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpsertConfig([FromBody] UpsertMailConfigCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Sends a test email using the provided SMTP config (not saved to DB).</summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(SendTestEmailResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SendTestEmailResult>> TestMailConfig(
        [FromBody] SendTestEmailCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
