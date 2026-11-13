using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly FarmStore _store;

    public AlertsController(FarmStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_store.Alerts);
    }
}
