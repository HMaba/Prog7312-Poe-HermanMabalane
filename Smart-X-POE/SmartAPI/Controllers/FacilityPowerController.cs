using Microsoft.AspNetCore.Mvc;
using SmartAPI.Services;

namespace SmartAPI.Controllers;

[ApiController]
[Route("api/facilitypower")]
public class FacilityPowerController : ControllerBase
{
    private readonly FarmStore _store;

    public FacilityPowerController(FarmStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_store.Power);
    }
}
