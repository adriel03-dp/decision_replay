using Microsoft.AspNetCore.Mvc;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "DecisionReplay API"
        });
    }
}
