using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check probe endpoint.
    /// </summary>
    /// <returns>Service status and diagnostic metadata.</returns>
    [HttpGet]
    [Route("/health")]
    [Route("/api/health")]
    public IActionResult GetHealth()
    {
        var response = new
        {
            status = "Healthy",
            service = "DocuMind.WebApi",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            framework = ".NET 10.0",
            timestamp = DateTime.UtcNow
        };

        _logger.LogDebug("Health check requested and responding Healthy at {Timestamp}", response.timestamp);
        return Ok(response);
    }
}
