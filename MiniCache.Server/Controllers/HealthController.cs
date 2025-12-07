using Microsoft.AspNetCore.Mvc;

namespace MiniCache.Server.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { Status = "Healthy", TimestampUtc = DateTimeOffset.UtcNow });
}
