using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/insights")]
public class InsightsController : ControllerBase
{
    private readonly DataHub _hub;

    public InsightsController(DataHub hub)
    {
        _hub = hub;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_hub.GetInsights());
    }
}
