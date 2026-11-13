using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly FarmStore _store;
    private readonly DataHub _hub;

    public TelemetryController(FarmStore store, DataHub hub)
    {
        _store = store;
        _hub = hub;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_store.SnapshotTelemetry());
    }

    [HttpGet("timeline")]
    public IActionResult Timeline([FromQuery] int count = 50)
    {
        return Ok(_hub.GetTimeline(count));
    }
}
