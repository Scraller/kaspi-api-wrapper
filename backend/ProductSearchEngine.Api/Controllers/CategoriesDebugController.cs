using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Services;
using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Enhanced Categories controller with better anti-bot protection for browser requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Categories (Debug)")]
public class CategoriesDebugController : ControllerBase
{
    private readonly KaspiNavigationApiClient _navigationApiClient;
    private readonly ILogger<CategoriesDebugController> _logger;
    
    // Enhanced rate limiting for browser requests
    private static readonly Dictionary<string, DateTime> _userRequestTimes = [];
    private static readonly object _requestLock = new();
    private static readonly TimeSpan BrowserRequestCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Initializes a new instance of the CategoriesDebugController
    /// </summary>
    /// <param name="navigationApiClient">Kaspi Navigation API client</param>
    /// <param name="logger">Logger instance</param>
    public CategoriesDebugController(
        KaspiNavigationApiClient navigationApiClient,
        ILogger<CategoriesDebugController> logger)
    {
        _navigationApiClient = navigationApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Get categories with enhanced debugging and anti-bot protection
    /// </summary>
    /// <param name="includeSubcategories">Include subcategories in response</param>
    /// <param name="cityId">City ID for categories</param>
    /// <param name="depth">Hierarchy depth</param>
    /// <returns>Categories with debugging info</returns>
    [HttpGet("kaspi-debug")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> GetKaspiCategoriesDebug(
        [FromQuery] bool includeSubcategories = true,
        [FromQuery] string cityId = "750000000",
        [FromQuery] int depth = 3)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var userAgent = Request.Headers.UserAgent.ToString();
        var referer = Request.Headers.Referer.ToString();
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        _logger.LogInformation("[{RequestId}] Categories request from {IP} | UA: {UserAgent} | Referer: {Referer}",
            requestId, remoteIp, userAgent, referer);

        try
        {
            // Enhanced rate limiting based on IP + User Agent
            var userKey = $"{remoteIp}_{userAgent?.GetHashCode()}";
            lock (_requestLock)
            {
                if (_userRequestTimes.TryGetValue(userKey, out var lastRequestTime))
                {
                    var timeSinceLastRequest = DateTime.UtcNow - lastRequestTime;
                    if (timeSinceLastRequest < BrowserRequestCooldown)
                    {
                        var waitTime = BrowserRequestCooldown - timeSinceLastRequest;
                        _logger.LogWarning("[{RequestId}] Rate limiting user {UserKey}, must wait {WaitTime}",
                            requestId, userKey, waitTime);
                        
                        return StatusCode(429, new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Rate limited. Please wait {waitTime.TotalSeconds:F1} seconds before next request.",
                            Errors = ["TOO_MANY_REQUESTS"],
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                _userRequestTimes[userKey] = DateTime.UtcNow;
            }

            _logger.LogInformation("[{RequestId}] Making Kaspi API call: cityId={CityId}, depth={Depth}, includeSubcategories={IncludeSubcategories}",
                requestId, cityId, depth, includeSubcategories);

            var startTime = DateTime.UtcNow;
            var categories = await _navigationApiClient.GetCategoryHierarchyAsync(cityId, depth);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("[{RequestId}] Kaspi API responded in {Duration}ms with {CategoryCount} categories",
                requestId, duration.TotalMilliseconds, categories?.Count ?? 0);

            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("[{RequestId}] Empty response from Kaspi API - possible anti-bot protection triggered", requestId);
                
                return Ok(new ApiResponse<List<HierarchicalCategoryInfo>>
                {
                    Success = true,
                    Message = "No categories found. This might indicate anti-bot protection was triggered.",
                    Data = [],
                    Metadata = new ResponseMetadata
                    {
                        TotalCount = 0,
                        AdditionalInfo = new Dictionary<string, object>
                        {
                            ["requestId"] = requestId,
                            ["possibleAntiBot"] = true,
                            ["userAgent"] = userAgent ?? "unknown",
                            ["responseTimeMs"] = duration.TotalMilliseconds
                        }
                    },
                    Timestamp = DateTime.UtcNow
                });
            }

            // Filter subcategories if requested
            if (!includeSubcategories)
            {
                categories = categories.Where(c => c.Level == 0).ToList();
            }

            return Ok(new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Retrieved {categories.Count} categories successfully",
                Data = categories,
                Metadata = new ResponseMetadata
                {
                    TotalCount = categories.Count,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["responseTimeMs"] = duration.TotalMilliseconds,
                        ["userAgent"] = userAgent ?? "unknown",
                        ["cityId"] = cityId,
                        ["depth"] = depth
                    }
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Error fetching categories", requestId);
            
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to fetch categories",
                Errors = [ex.Message],
                Metadata = new ResponseMetadata
                {
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["errorType"] = ex.GetType().Name
                    }
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Simple ping endpoint to test connectivity without hitting Kaspi APIs
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> Ping()
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var userAgent = Request.Headers.UserAgent.ToString();
        var referer = Request.Headers.Referer.ToString();
        
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Categories controller is responding",
            Data = new
            {
                RequestId = requestId,
                UserAgent = userAgent,
                Referer = referer,
                Timestamp = DateTime.UtcNow,
                Server = "Kaspi.kz API Wrapper"
            },
            Timestamp = DateTime.UtcNow
        });
    }
}
