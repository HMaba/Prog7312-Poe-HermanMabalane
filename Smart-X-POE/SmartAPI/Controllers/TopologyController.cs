using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api")]
public class TopologyController : ControllerBase
{
    private readonly DataHub _hub;

    public TopologyController(DataHub hub)
    {
        _hub = hub;
    }

    [HttpGet("topology")]
    public IActionResult Get()
    {
        return Ok(_hub.GetTopology());
    }

    [HttpGet("topology/route/{mac}")]
    public IActionResult Route(string mac)
    {
        var route = _hub.GetRoute(mac);
        return route == null ? NotFound(new { message = "Unknown sensor MAC address." }) : Ok(route);
    }

    [HttpGet("telemetry/{mac}/history")]
    public IActionResult History(string mac, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!_hub.HasDevice(mac))
            return NotFound(new { message = "Unknown sensor MAC address." });

        var start = from ?? DateTime.MinValue;
        var end = to ?? DateTime.MaxValue;

        if (start > end)
            return BadRequest(new { message = "The from time must be earlier than the to time." });

        return Ok(_hub.GetHistory(mac, start, end));
    }

    [Authorize]
    [HttpPost("topology/nodes/{mac}/fail")]
    public IActionResult Fail(string mac)
    {
        return _hub.SetDisconnected(mac, true)
            ? Ok(new { message = "Node marked as disconnected." })
            : NotFound(new { message = "Unknown sensor MAC address." });
    }

    [Authorize]
    [HttpPost("topology/nodes/{mac}/restore")]
    public IActionResult Restore(string mac)
    {
        return _hub.SetDisconnected(mac, false)
            ? Ok(new { message = "Node restored to the mesh." })
            : NotFound(new { message = "Unknown sensor MAC address." });
    }
}
