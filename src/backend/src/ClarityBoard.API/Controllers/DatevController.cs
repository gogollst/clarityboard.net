using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Services.Datev;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

/// <summary>
/// Controller for DATEV export operations.
/// Provides endpoints to generate DATEV EXTF format files
/// for import into DATEV accounting software.
/// </summary>
[ApiController]
[Route("api/datev")]
[Authorize]
public class DatevController : ControllerBase
{
    private readonly DatevExportService _datevService;
    private readonly ICurrentUser _currentUser;

    public DatevController(DatevExportService datevService, ICurrentUser currentUser)
    {
        _datevService = datevService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Exports a DATEV EXTF Buchungsstapel (booking batch) file for the specified period.
    /// The file uses Windows-1252 encoding and semicolon-separated fields per DATEV specification.
    /// </summary>
    /// <param name="year">Fiscal year (e.g. 2025).</param>
    /// <param name="month">Fiscal month (1-12).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>CSV file in DATEV EXTF format with SHA-256 checksum in response headers.</returns>
    /// <response code="200">Returns the DATEV EXTF file as a downloadable CSV.</response>
    /// <response code="400">If the entity is missing required DATEV configuration.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost("export/buchungsstapel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportBuchungsstapel(
        [FromQuery] short year, [FromQuery] short month, CancellationToken ct)
    {
        if (month < 1 || month > 12)
            return BadRequest("Month must be between 1 and 12.");

        if (year < 2000 || year > 2099)
            return BadRequest("Year must be between 2000 and 2099.");

        try
        {
            var result = await _datevService.ExportBuchungsstapelAsync(
                _currentUser.EntityId, year, month, ct);

            // Include checksum and entry count in response headers for client verification
            Response.Headers.Append("X-Datev-Checksum", result.Checksum);
            Response.Headers.Append("X-Datev-EntryCount", result.EntryCount.ToString());
            Response.Headers.Append("X-Datev-ExportedAt", result.ExportedAt.ToString("O"));

            return File(result.FileContent, "text/csv; charset=windows-1252", result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
