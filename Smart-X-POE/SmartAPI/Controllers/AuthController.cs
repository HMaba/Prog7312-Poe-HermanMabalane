using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Sign-in itself happens against Firebase; this returns the identity carried by the Firebase ID token.
    [HttpGet("me")]
    public ActionResult<object> Me() => Ok(new
    {
        UserId = User.FindFirstValue("user_id") ?? User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        Email = User.FindFirstValue("email") ?? User.FindFirstValue(ClaimTypes.Email),
        Name = User.FindFirstValue("name"),
        Provider = User.FindFirstValue("firebase")
    });
}
