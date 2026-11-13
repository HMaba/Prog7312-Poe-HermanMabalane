using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;
using SmartAPI.Models;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly DataHub _hub;

    public CommandsController(DataHub hub)
    {
        _hub = hub;
    }

    [Authorize]
    [HttpPost]
    public IActionResult Post(CommandRequest request)
    {
        var result = _hub.ApplyCommand(request.DeviceId, request.Action);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("undo")]
    public IActionResult Undo()
    {
        return Ok(_hub.Undo());
    }

    [HttpGet("history")]
    public IActionResult History([FromQuery] int count = 50)
    {
        return Ok(_hub.GetHistoryEvents(count));
    }
}
