using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;
using SmartAPI.Models;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly FarmStore _store;
    private readonly DataHub _hub;

    public DevicesController(FarmStore store, DataHub hub)
    {
        _store = store;
        _hub = hub;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_hub.GetDevices());
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? category, [FromQuery] string? alertState)
    {
        return Ok(_hub.SearchDevices(category, alertState));
    }

    [Authorize]
    [HttpPost]
    public IActionResult Post(Device device)
    {
        if (!string.IsNullOrWhiteSpace(device.MacAddress) && _hub.HasDevice(device.MacAddress))
            return Conflict(new { message = "A device with that MAC address is already registered." });

        _store.AddDevice(device);
        _hub.RegisterDevice(device);
        return Ok(device);
    }
}
