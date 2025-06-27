using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Health check and test endpoints for the Kaspi.kz API wrapper
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Health Check")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    /// <summary>
    /// Initializes a new instance of the TestController
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint to verify the API wrapper is running
    /// </summary>
    /// <returns>API health status</returns>
    /// <response code="200">API is healthy and running</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> GetHealth()
    {
        _logger.LogInformation("Health check requested");
        
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Kaspi.kz API Wrapper is healthy and running",
            Data = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Service = "Kaspi.kz API Wrapper"
            },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Simple ping endpoint for basic connectivity test
    /// </summary>
    /// <returns>Pong response</returns>
    /// <response code="200">Ping successful</response>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public ActionResult<ApiResponse<string>> Ping()
    {
        _logger.LogInformation("Ping requested");
        
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Pong",
            Data = "API is responding",
            Timestamp = DateTime.UtcNow
        });
    }
}
