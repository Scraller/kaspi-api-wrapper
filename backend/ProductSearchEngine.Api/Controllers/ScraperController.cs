using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Scraper;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Api.Models;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Controller for scraping products from various marketplaces using enhanced API-based methods.
/// Now integrated with the new Navigation API system for better performance and reliability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Product Scraping")]
public class ScraperController : ControllerBase
{
    private readonly KaspiApiScraperEnhanced _scraper;
    private readonly ILogger<ScraperController> _logger;

    /// <summary>
    /// Initializes a new instance of the ScraperController
    /// </summary>
    /// <param name="scraper">Kaspi scraper instance</param>
    /// <param name="logger">Logger instance</param>
    public ScraperController(
        KaspiApiScraperEnhanced scraper,
        ILogger<ScraperController> logger)
    {
        _scraper = scraper;
        _logger = logger;
    }

    /// <summary>
    /// Test scraper functionality for a specific marketplace and category
    /// </summary>
    /// <param name="marketplace">The marketplace to test (e.g., kaspi)</param>
    /// <param name="category">The category to test (e.g., smartphones)</param>
    /// <returns>Diagnostic information about the scraper test</returns>
    /// <response code="200">Scraper test completed successfully</response>
    /// <response code="404">Scraper for the specified marketplace not found</response>
    /// <response code="500">Internal server error during testing</response>
    [HttpGet("diagnostics/test/{marketplace}/{category}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult> TestScraper(string marketplace, string category)
    {
        try
        {
            Console.WriteLine($"[DIAGNOSTICS] Testing scraper for marketplace: {marketplace}, category: {category}");

            if (marketplace.ToLower() != "kaspi")
            {
                return NotFound($"Scraper for marketplace '{marketplace}' not found. Only 'kaspi' is supported.");
            }

            // Try to scrape a few products
            try
            {
                Console.WriteLine($"[DIAGNOSTICS] Attempting to scrape products for category: {category}");
                var products = await _scraper.ScrapeProductsAsync(category, 5);

                if (products != null && products.Any())
                {
                    Console.WriteLine($"[DIAGNOSTICS] Successfully scraped {products.Count()} products");
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            success = true,
                            count = products.Count(),
                            products = products.Select(p => new
                            {
                                name = p.Name,
                                brand = p.Brand,
                                price = p.CurrentPrice,
                                marketplace = p.Marketplace,
                                url = p.ProductUrl
                            }).Take(5)
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[DIAGNOSTICS] No products found for category: {category}");
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Data = new
                        {
                            success = false,
                            message = $"No products found for category '{category}'",
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIAGNOSTICS][ERROR] Scraping failed: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Scraping failed: {ex.Message}"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DIAGNOSTICS][ERROR] Unexpected error: {ex.Message}");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Scrape products from Kaspi marketplace
    /// </summary>
    /// <param name="marketplace">The marketplace to scrape from (must be 'kaspi')</param>
    /// <param name="request">The scrape request containing query and options</param>
    /// <returns>Scraped product data</returns>
    /// <response code="200">Products scraped successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Scraper for the specified marketplace not found</response>
    /// <response code="500">Internal server error during scraping</response>
    [HttpPost("{marketplace}")]
    [ProducesResponseType(typeof(ApiResponse<List<Product>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult> ScrapeMarketplace(
        string marketplace,
        [FromBody] ScrapeRequest request)
    {
        var requestStopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short request ID for tracking

        try
        {
            _logger.LogInformation("[REQUEST:{RequestId}] Starting scrape request for {Marketplace}, query: {Query}, maxPages: {MaxPages}",
                requestId, marketplace, request.Query, request.MaxPages);
            Console.WriteLine($"[SCRAPER][API][{requestId}] Starting scrape request for {marketplace}");

            if (marketplace.ToLower() != "kaspi")
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Scraper for marketplace '{marketplace}' not found. Only 'kaspi' is supported."
                });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Query is required"
                });
            }

            _logger.LogInformation("[REQUEST:{RequestId}] Starting scraping for {Marketplace}, query: {Query}",
                requestId, marketplace, request.Query);

            try
            {
                _logger.LogInformation("[REQUEST:{RequestId}] Calling ScrapeProductsAsync for {Marketplace}, query: {Query}, maxPages: {MaxPages}",
                    requestId, marketplace, request.Query, request.MaxPages);

                var scrapingStopwatch = Stopwatch.StartNew();
                var maxProducts = request.MaxPages * 12; // Kaspi has ~12 products per page
                var products = await _scraper.ScrapeProductsAsync(request.Query, maxProducts);
                scrapingStopwatch.Stop();

                Console.WriteLine($"[SCRAPER][API][{requestId}] Scraping completed in {scrapingStopwatch.ElapsedMilliseconds}ms");
                _logger.LogInformation("[REQUEST:{RequestId}] ScrapeProductsAsync completed. Returned {Count} products", requestId, products?.Count() ?? 0);

                // Clean data - remove any products with missing critical information
                var validProducts = products?.Where(p =>
                    !string.IsNullOrEmpty(p.Name) &&
                    p.CurrentPrice > 0).ToList() ?? new List<Product>();

                _logger.LogInformation("[REQUEST:{RequestId}] Scraped {TotalCount} products, {ValidCount} valid products from {Marketplace}",
                    requestId, products?.Count() ?? 0, validProducts.Count, marketplace);
                Console.WriteLine($"[SCRAPER][API][{requestId}] Scraped {products?.Count() ?? 0} products, {validProducts.Count} valid products from {marketplace}");

                requestStopwatch.Stop();
                _logger.LogInformation("[REQUEST:{RequestId}] API request completed in {TotalTimeMs}ms", requestId, requestStopwatch.ElapsedMilliseconds);
                Console.WriteLine($"[SCRAPER][API][{requestId}] ===== API RESPONSE PERFORMANCE =====");
                Console.WriteLine($"[SCRAPER][API][{requestId}] Total API response time: {requestStopwatch.ElapsedMilliseconds:N0}ms ({requestStopwatch.ElapsedMilliseconds / 1000.0:F2}s)");

                return Ok(new ApiResponse<List<Product>>
                {
                    Success = true,
                    Data = validProducts,
                    Message = $"Successfully scraped {validProducts.Count} products"
                });
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Error during scraping for {Marketplace}: {ErrorMessage}",
                    marketplace, innerEx.Message);

                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Scraping failed: {innerEx.Message}"
                });
            }
        }
        catch (Exception ex)
        {
            requestStopwatch.Stop();
            _logger.LogError(ex, "[REQUEST:{RequestId}] Failed to start scraping for {Marketplace} after {TotalTimeMs}ms: {ErrorMessage}",
                requestId, marketplace, requestStopwatch.ElapsedMilliseconds, ex.Message);
            Console.WriteLine($"[SCRAPER][API][{requestId}] Failed after {requestStopwatch.ElapsedMilliseconds:N0}ms");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while starting the scraping process."
            });
        }
    }

    /// <summary>
    /// Get performance statistics and monitoring information
    /// </summary>
    /// <returns>Performance monitoring features and status</returns>
    /// <response code="200">Performance statistics retrieved successfully</response>
    /// <response code="500">Error retrieving performance statistics</response>
    [HttpGet("performance/stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public ActionResult GetPerformanceStats()
    {
        try
        {
            var stats = new
            {
                message = "Performance monitoring is active",
                features = new[]
                {
                    "Real-time scraping performance measurement",
                    "HTTP request timing",
                    "Content parsing metrics",
                    "Browser automation timing",
                    "Request correlation tracking",
                    "Phase-by-phase breakdown"
                },
                metrics_tracked = new[]
                {
                    "Total scraping time",
                    "URL building time",
                    "HTTP request latency",
                    "Content reading time",
                    "HTML parsing time",
                    "Browser automation overhead",
                    "Products per second"
                },
                logging_prefixes = new
                {
                    kaspi_scraper = "[SCRAPER][KASPI][PERFORMANCE]",
                    api_controller = "[SCRAPER][API][{requestId}]",
                    request_tracking = "[REQUEST:{RequestId}]"
                },
                note = "Check application logs for detailed performance metrics during scraping operations"
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance stats");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving performance statistics"
            });
        }
    }

    /// <summary>
    /// Get detailed performance metrics for scraping operations
    /// </summary>
    /// <returns>Detailed performance metrics and system information</returns>
    /// <response code="200">Performance metrics retrieved successfully</response>
    /// <response code="500">Error retrieving performance metrics</response>
    [HttpGet("performance/metrics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public ActionResult GetPerformanceMetrics()
    {
        try
        {
            var metrics = new
            {
                status = "monitoring_active",
                timestamp = DateTime.UtcNow,
                system_info = new
                {
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    machine_name = Environment.MachineName,
                    processor_count = Environment.ProcessorCount,
                    working_set = GC.GetTotalMemory(false)
                },
                scraper_performance = new
                {
                    kaspi_scraper = new
                    {
                        metrics_tracked = new[]
                        {
                            "url_build_ms",
                            "http_request_ms",
                            "content_read_ms",
                            "parsing_ms",
                            "browser_automation_ms",
                            "browser_parsing_ms"
                        },
                        optimization_targets = new[]
                        {
                            "Browser automation pooling (69.6% of time)",
                            "HTTP request optimization (8.3% of time)",
                            "Parallel processing implementation"
                        }
                    }
                },
                api_performance = new
                {
                    request_tracking = "unique_request_ids",
                    background_processing = "async_task_timing"
                },
                recommendations = new[]
                {
                    "Monitor browser automation time - primary bottleneck",
                    "Check products per second rate for throughput",
                    "Use request IDs for end-to-end correlation"
                }
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = metrics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving performance metrics"
            });
        }
    }
}