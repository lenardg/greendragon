using GreenDragon.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenDragon.Web.Controllers;

/// <summary>Reports application availability without contacting Sonos.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Returns availability, optionally including the Core assembly version.</summary>
    /// <param name="version">Whether to include the library version.</param>
    /// <returns>HTTP 200 with an optional version string.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] bool version = false)
    {
        return version ? new JsonResult(typeof(BusinessRules).Assembly.GetName().Version?.ToString()) : Ok();
    }
}
