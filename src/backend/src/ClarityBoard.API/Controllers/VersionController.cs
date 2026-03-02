using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClarityBoard.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private static readonly string VersionFilePath =
        Path.Combine(AppContext.BaseDirectory, "version.json");

    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        if (!System.IO.File.Exists(VersionFilePath))
            return Ok(new VersionResponse("0.0.0", DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")));

        var json = System.IO.File.ReadAllText(VersionFilePath);
        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetString() ?? "0.0.0";
        var buildDate = doc.RootElement.GetProperty("buildDate").GetString()
                        ?? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        return Ok(new VersionResponse(version, buildDate));
    }

    private record VersionResponse(string Version, string BuildDate);
}
