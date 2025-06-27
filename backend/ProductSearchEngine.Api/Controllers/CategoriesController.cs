using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Services;
using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Controller for managing product categories and subcategories using Kaspi's Navigation API.
/// This controller provides access to 1,970+ product categories with hierarchical structure,
/// smart caching, and rate limiting to ensure reliable performance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Category Management")]
public class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly KaspiNavigationApiClient _navigationApiClient;

    // Static caching to prevent frequent API calls and improve performance
    private static List<HierarchicalCategoryInfo>? _cachedCategories = null;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly object _cacheLock = new object();
    private static readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(30); // 30 minute cache for API data
    private static readonly TimeSpan MinimumRequestInterval = TimeSpan.FromSeconds(10); // 10 seconds minimum between API requests (development-friendly)

    /// <summary>
    /// Initializes a new instance of the CategoriesController
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="navigationApiClient">Kaspi Navigation API client for fetching categories</param>
    public CategoriesController(
        ILogger<CategoriesController> logger,
        KaspiNavigationApiClient navigationApiClient)
    {
        _logger = logger;
        _navigationApiClient = navigationApiClient;
    }

    /// <summary>
    /// Get all available categories from Kaspi marketplace with hierarchical structure using Navigation API.
    /// This endpoint fetches 1,970+ categories directly from Kaspi's official navigation APIs, providing
    /// complete hierarchical relationships and fast response times.
    /// Features intelligent caching (30-minute TTL) and rate limiting to ensure reliable performance.
    /// </summary>
    /// <param name="includeSubcategories">Whether to include subcategories in the response (default: true)</param>
    /// <returns>Hierarchical list of categories and subcategories</returns>
    /// <response code="200">Categories retrieved successfully from Navigation API</response>
    /// <response code="429">Rate limited - too many requests</response>
    /// <response code="500">Error retrieving categories from Navigation API</response>
    /// <remarks>
    /// Sample response includes categories like:
    /// - Electronics → Smartphones (25,847 products)
    /// - Fashion → Men's Clothing (15,623 products)
    /// - Home and Garden → Furniture (8,934 products)
    /// 
    /// Rate limiting: Minimum 10-second interval between fresh API calls.
    /// Cache duration: 30 minutes for optimal performance.
    /// </remarks>
    [HttpGet("kaspi")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 429)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> GetKaspiCategories(
        [FromQuery] bool includeSubcategories = true)
    {
        try
        {
            List<HierarchicalCategoryInfo> categories;

            // Check cache first
            lock (_cacheLock)
            {
                if (_cachedCategories != null && DateTime.UtcNow < _cacheExpiry)
                {
                    _logger.LogInformation("Returning cached categories ({CategoryCount} items)", _cachedCategories.Count);
                    categories = _cachedCategories;
                }
                else
                {
                    categories = []; // Will trigger fresh fetch
                }
            }

            // If no cached data or cache expired, fetch fresh data
            if (!categories.Any())
            {
                // Check if we need to wait to avoid triggering anti-bot protection
                lock (_cacheLock)
                {
                    var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                    if (timeSinceLastRequest < MinimumRequestInterval)
                    {
                        var waitTime = MinimumRequestInterval - timeSinceLastRequest;
                        _logger.LogWarning("Rate limiting: Last request was {TimeSinceLastRequest} ago, waiting {WaitTime} more",
                            timeSinceLastRequest, waitTime);

                        // Return stale cache if available to avoid blocking the request
                        if (_cachedCategories != null && _cachedCategories.Any())
                        {
                            _logger.LogInformation("Returning stale cached categories ({CategoryCount} items) due to rate limiting",
                                _cachedCategories.Count);
                            categories = _cachedCategories;
                        }
                        else
                        {
                            // Return rate limit response instead of blocking
                            return StatusCode(429, new ApiResponse<List<HierarchicalCategoryInfo>>
                            {
                                Success = false,
                                Message = $"Rate limited. Please wait {waitTime.TotalSeconds:F1} seconds before next request.",
                                Errors = ["TOO_MANY_REQUESTS"],
                                Data = [],
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }

                    _lastRequestTime = DateTime.UtcNow;
                }

                // Fetch fresh data if we still don't have any
                if (!categories.Any())
                {
                    _logger.LogInformation("Fetching fresh categories from Kaspi Navigation API...");

                    categories = await _navigationApiClient.GetCategoryHierarchyAsync();

                    _logger.LogInformation("GetCategoriesAsync returned {CategoryCount} categories", categories?.Count ?? 0);

                    // Cache successful results
                    if (categories?.Any() == true)
                    {
                        lock (_cacheLock)
                        {
                            _cachedCategories = categories;
                            _cacheExpiry = DateTime.UtcNow.Add(CacheTimeout);
                            _logger.LogInformation("Cached {CategoryCount} categories for {CacheMinutes} minutes",
                                categories.Count, CacheTimeout.TotalMinutes);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Empty result detected - possible anti-bot protection triggered");

                        // Return stale cache if available
                        lock (_cacheLock)
                        {
                            if (_cachedCategories != null && _cachedCategories.Any())
                            {
                                _logger.LogInformation("Returning stale cached categories ({CategoryCount} items) due to empty response",
                                    _cachedCategories.Count);
                                categories = _cachedCategories;
                            }
                            else
                            {
                                categories = [];
                            }
                        }
                    }
                }
            }

            if (categories?.Any() == true)
            {
                _logger.LogInformation("Categories found: {Categories}",
                    string.Join(", ", categories.Take(5).Select(c => c.Name)));

                // Debug parent-child relationships
                var parentCategories = categories.Where(c => c.ParentSlug != null).Take(10);
                foreach (var cat in parentCategories)
                {
                    _logger.LogInformation("Category '{CategoryName}' (slug: '{CategorySlug}') has parent slug: '{ParentSlug}'",
                        cat.Name, cat.Slug, cat.ParentSlug);
                }

                var categoriesWithSubcategories = categories.Where(c => c.Subcategories.Any()).Take(5);
                foreach (var cat in categoriesWithSubcategories)
                {
                    _logger.LogInformation("Category '{CategoryName}' has {SubcategoryCount} subcategories: {Subcategories}",
                        cat.Name, cat.Subcategories.Count, string.Join(", ", cat.Subcategories.Select(s => s.Name)));
                }

                if (!categoriesWithSubcategories.Any())
                {
                    _logger.LogWarning("No categories have subcategories populated!");
                }
            }
            else
            {
                _logger.LogWarning("No categories returned from scanner");
            }

            if (categories?.Any() == true)
            {
                if (includeSubcategories)
                {
                    // Flatten the hierarchical structure to include all subcategories as separate items
                    categories = FlattenCategoryHierarchy(categories);
                    _logger.LogInformation("Flattened hierarchy: {TotalCount} categories including subcategories", categories.Count);
                }
                else
                {
                    // Remove subcategories from the structure
                    categories = [.. categories.Select(c => c with { Subcategories = [] })];
                    _logger.LogInformation("Removed subcategories: {TotalCount} top-level categories only", categories.Count);
                }
            }

            var response = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Retrieved {categories?.Count ?? 0} categories successfully",
                Data = categories ?? [],
                Metadata = new ResponseMetadata
                {
                    TotalCount = categories?.Count ?? 0,
                    Page = 1,
                    PageSize = categories?.Count ?? 0
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Kaspi categories: {ErrorMessage}", ex.Message);

            var errorResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Failed to retrieve categories",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get top-level categories only (no subcategories)
    /// </summary>
    /// <returns>List of top-level categories</returns>
    /// <response code="200">Top-level categories retrieved successfully</response>
    /// <response code="500">Error retrieving categories</response>
    [HttpGet("kaspi/top-level")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> GetTopLevelCategories()
    {
        try
        {
            _logger.LogInformation("Retrieving top-level categories from Kaspi");

            // Get categories using the same caching logic
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            // Filter to top-level categories only (parentSlug is null) and remove subcategories
            var topLevelCategories = allCategories
                .Where(c => c.ParentSlug == null)
                .Select(c => c with { Subcategories = [] }) // Clear subcategories for top-level endpoint
                .ToList();

            var response = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Retrieved {topLevelCategories.Count} top-level categories",
                Data = topLevelCategories,
                Metadata = new ResponseMetadata
                {
                    TotalCount = topLevelCategories.Count,
                    Page = 1,
                    PageSize = topLevelCategories.Count
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top-level categories");

            var errorResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Failed to retrieve top-level categories",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get subcategories for a specific parent category
    /// </summary>
    /// <param name="parentSlug">The slug of the parent category</param>
    /// <returns>List of subcategories for the specified parent</returns>
    /// <response code="200">Subcategories retrieved successfully</response>
    /// <response code="404">Parent category not found</response>
    /// <response code="500">Error retrieving subcategories</response>
    [HttpGet("kaspi/{parentSlug}/subcategories")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> GetSubcategories(
        [Required] string parentSlug)
    {
        try
        {
            // ASP.NET Core automatically URL-decodes the parameter, but our data contains URL-encoded slugs
            // So we need to re-encode it to match our data format
            var encodedParentSlug = Uri.EscapeDataString(parentSlug);
            _logger.LogInformation("Retrieving subcategories for parent category: {ParentSlug} (encoded: {EncodedParentSlug})", parentSlug, encodedParentSlug);

            // Get categories using the same caching logic
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            // Find the parent category using the encoded slug to match our data
            var parentCategory = allCategories.FirstOrDefault(c => c.Slug.Equals(encodedParentSlug, StringComparison.OrdinalIgnoreCase));

            if (parentCategory == null)
            {
                var notFoundResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
                {
                    Success = false,
                    Message = $"Parent category with slug '{parentSlug}' not found",
                    Errors = ["Parent category not found"]
                };

                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Retrieved {parentCategory.Subcategories.Count} subcategories for '{parentCategory.Name}'",
                Data = parentCategory.Subcategories,
                Metadata = new ResponseMetadata
                {
                    TotalCount = parentCategory.Subcategories.Count,
                    Page = 1,
                    PageSize = parentCategory.Subcategories.Count
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategories for parent: {ParentSlug}", parentSlug);

            var errorResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Failed to retrieve subcategories",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Search categories by name or slug using the Navigation API data.
    /// Performs case-insensitive search across category names and URL slugs
    /// from the cached Navigation API dataset.
    /// </summary>
    /// <param name="query">Search term to match against category names or slugs (minimum 2 characters)</param>
    /// <param name="includeSubcategories">Whether to include subcategories in results (default: false)</param>
    /// <returns>List of categories matching the search criteria</returns>
    /// <response code="200">Categories found successfully</response>
    /// <response code="400">Invalid search query (less than 2 characters)</response>
    /// <response code="500">Error searching categories</response>
    /// <remarks>
    /// Examples:
    /// - query="phone" → returns "Smartphones", "Phone accessories", etc.
    /// - query="smartfony" → returns categories with slug containing "smartfony"
    /// - query="electr" → returns "Electronics" and related categories
    /// </remarks>
    [HttpGet("kaspi/search")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> SearchCategories(
        [FromQuery][Required] string query,
        [FromQuery] bool includeSubcategories = false)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            var badRequestResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Search query must be at least 2 characters long",
                Errors = ["Invalid query parameter"]
            };

            return BadRequest(badRequestResponse);
        }

        try
        {
            _logger.LogInformation("Searching categories with query: {Query}", query);

            // Get categories using the same caching logic
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            _logger.LogInformation("Total categories available for search: {Count}", allCategories.Count);
            
            // Log some sample category names and slugs for debugging
            var sampleCategories = allCategories.Take(10).Select(c => $"{c.Name} (slug: {c.Slug})").ToList();
            _logger.LogInformation("Sample categories: {SampleCategories}", string.Join(", ", sampleCategories));

            // Search in both name and slug
            var matchingCategories = allCategories.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Slug.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            _logger.LogInformation("Found {MatchCount} categories matching query '{Query}'", matchingCategories.Count, query);
            
            // Log the matching categories for debugging
            if (matchingCategories.Any())
            {
                var matchedNames = matchingCategories.Take(5).Select(c => $"{c.Name} (slug: {c.Slug})").ToList();
                _logger.LogInformation("Matched categories: {MatchedCategories}", string.Join(", ", matchedNames));
            }

            if (includeSubcategories)
            {
                // For search results, we need to ensure subcategories are properly populated
                // because the hierarchy might not be complete for non-root categories
                var enrichedCategories = new List<HierarchicalCategoryInfo>();
                
                foreach (var category in matchingCategories)
                {
                    // Find all direct children of this category
                    var directChildren = allCategories.Where(c => 
                        string.Equals(c.ParentSlug, category.Slug, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    // Create a copy with properly populated subcategories
                    var enrichedCategory = category with { Subcategories = directChildren };
                    enrichedCategories.Add(enrichedCategory);
                }
                
                matchingCategories = enrichedCategories;
            }
            else
            {
                // Remove subcategories from results
                matchingCategories = [.. matchingCategories.Select(c => c with { Subcategories = [] })];
            }

            var response = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Found {matchingCategories.Count} categories matching '{query}'",
                Data = matchingCategories,
                Metadata = new ResponseMetadata
                {
                    TotalCount = matchingCategories.Count,
                    Page = 1,
                    PageSize = matchingCategories.Count
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching categories with query: {Query}", query);

            var errorResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Failed to search categories",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Scan a specific category page to extract its subcategories and sub-subcategories
    /// </summary>
    /// <param name="parentSlug">The slug of the parent category to scan for subcategories</param>
    /// <param name="maxDepth">Maximum depth to scan (default: 3, max: 5)</param>
    /// <param name="recursive">Whether to recursively scan subcategories of subcategories</param>
    /// <returns>List of subcategories found on the specific category page</returns>
    /// <response code="200">Subcategories retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="404">Parent category not found</response>
    /// <response code="500">Error scanning subcategories</response>
    [HttpGet("kaspi/{parentSlug}/scan-subcategories")]
    [ProducesResponseType(typeof(ApiResponse<List<HierarchicalCategoryInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<HierarchicalCategoryInfo>>>> ScanCategorySubcategories(
        [Required] string parentSlug,
        [FromQuery] int maxDepth = 3,
        [FromQuery] bool recursive = false)
    {
        try
        {
            // ASP.NET Core automatically URL-decodes the parameter, but our data contains URL-encoded slugs
            // So we need to re-encode it to match our data format
            var encodedParentSlug = Uri.EscapeDataString(parentSlug);
            
            // Validate maxDepth parameter
            if (maxDepth < 1 || maxDepth > 5)
            {
                var badRequestResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
                {
                    Success = false,
                    Message = "MaxDepth must be between 1 and 5",
                    Errors = ["Invalid maxDepth parameter"]
                };
                return BadRequest(badRequestResponse);
            }

            _logger.LogInformation("Scanning subcategories for category: {ParentSlug} (encoded: {EncodedParentSlug}) (maxDepth: {MaxDepth}, recursive: {Recursive})",
                parentSlug, encodedParentSlug, maxDepth, recursive);

            // First, get all categories to find the parent category URL
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            _logger.LogInformation("Total categories available: {Count}", allCategories.Count);
            
            // Log some category slugs for debugging
            var sampleSlugs = allCategories.Take(10).Select(c => c.Slug).ToList();
            _logger.LogInformation("Sample category slugs: {SampleSlugs}", string.Join(", ", sampleSlugs));

            // Find the parent category using the encoded slug to match our data
            var parentCategory = allCategories.FirstOrDefault(c => c.Slug.Equals(encodedParentSlug, StringComparison.OrdinalIgnoreCase));

            // Also try without encoding in case the data is stored differently
            if (parentCategory == null)
            {
                parentCategory = allCategories.FirstOrDefault(c => c.Slug.Equals(parentSlug, StringComparison.OrdinalIgnoreCase));
                _logger.LogInformation("Trying unencoded slug '{UnEncodedSlug}' - found: {Found}", parentSlug, parentCategory != null);
            }

            _logger.LogInformation("Parent category search - encoded '{EncodedSlug}' found: {Found}", encodedParentSlug, parentCategory != null);

            if (parentCategory == null)
            {
                var notFoundResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
                {
                    Success = false,
                    Message = $"Parent category with slug '{parentSlug}' not found",
                    Errors = ["Parent category not found"]
                };
                return NotFound(notFoundResponse);
            }

            // Extract subcategories from the hierarchical Subcategories property
            var subcategories = new List<HierarchicalCategoryInfo>();
            
            if (recursive)
            {
                // Get all descendants up to maxDepth using hierarchical traversal
                var toProcess = new Queue<(HierarchicalCategoryInfo category, int depth)>();
                toProcess.Enqueue((parentCategory, 0));
                
                while (toProcess.Count > 0)
                {
                    var (currentCategory, currentDepth) = toProcess.Dequeue();
                    
                    if (currentDepth >= maxDepth) continue;
                    
                    // Add direct subcategories
                    var children = currentCategory.Subcategories ?? [];
                    subcategories.AddRange(children);
                    
                    // Queue children for processing at next depth level
                    foreach (var child in children)
                    {
                        toProcess.Enqueue((child, currentDepth + 1));
                    }
                }
            }
            else
            {
                // Get only direct children from the hierarchical Subcategories property
                subcategories = [.. (parentCategory.Subcategories ?? [])];
            }

            _logger.LogInformation("Found {SubcategoryCount} subcategories for {ParentSlug}", 
                subcategories.Count, parentSlug);

            var response = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = true,
                Message = $"Retrieved {subcategories.Count} subcategories for '{parentCategory.Name}' (depth: {maxDepth}, recursive: {recursive})",
                Data = subcategories,
                Metadata = new ResponseMetadata
                {
                    TotalCount = subcategories.Count,
                    Page = 1,
                    PageSize = subcategories.Count,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "parentCategoryName", parentCategory.Name },
                        { "parentCategorySlug", parentCategory.Slug },
                        { "maxDepthScanned", maxDepth },
                        { "scanDepth", maxDepth },
                        { "recursiveMode", recursive },
                        { "level1Count", subcategories.Count(c => c.Level == 1) },
                        { "level2PlusCount", subcategories.Count(c => c.Level > 1) }
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning subcategories for {ParentSlug}: {ErrorMessage}", parentSlug, ex.Message);

            var errorResponse = new ApiResponse<List<HierarchicalCategoryInfo>>
            {
                Success = false,
                Message = "Failed to scan subcategories",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Scan multiple categories for their subcategories in bulk
    /// </summary>
    /// <param name="parentSlugs">List of parent category slugs to scan</param>
    /// <param name="maxDepth">Maximum depth to scan for each category</param>
    /// <param name="recursive">Whether to use recursive scanning</param>
    /// <returns>Dictionary mapping parent slugs to their subcategories</returns>
    /// <response code="200">Bulk subcategory scan completed</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="500">Error during bulk scanning</response>
    [HttpGet("kaspi/bulk-scan-subcategories")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>>> BulkScanSubcategories(
        [FromQuery] string? parentSlugs = null,
        [FromQuery] int maxDepth = 2,
        [FromQuery] bool recursive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(parentSlugs))
            {
                var badRequestResponse = new ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>
                {
                    Success = false,
                    Message = "At least one parent slug is required",
                    Errors = ["At least one parent slug is required"]
                };
                return BadRequest(badRequestResponse);
            }

            // Parse comma-separated slugs and re-encode them since ASP.NET Core URL-decodes them but our data contains URL-encoded slugs
            var parentSlugsList = parentSlugs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Uri.EscapeDataString(s.Trim()))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (!parentSlugsList.Any())
            {
                var badRequestResponse = new ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>
                {
                    Success = false,
                    Message = "At least one parent slug is required",
                    Errors = ["At least one parent slug is required"]
                };
                return BadRequest(badRequestResponse);
            }

            if (parentSlugsList.Count > 10)
            {
                var badRequestResponse = new ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>
                {
                    Success = false,
                    Message = "Maximum 10 categories can be scanned in bulk",
                    Errors = ["Too many parent slugs"]
                };
                return BadRequest(badRequestResponse);
            }

            _logger.LogInformation("Starting bulk subcategory scan for {Count} categories", parentSlugsList.Count);

            var results = new Dictionary<string, List<HierarchicalCategoryInfo>>();
            var errors = new List<string>();

            // Get all categories once to find parent URLs
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            // Use API-based approach for bulk scanning
            foreach (var parentSlug in parentSlugsList)
            {
                try
                {
                    var parentCategory = allCategories.FirstOrDefault(c => c.Slug.Equals(parentSlug, StringComparison.OrdinalIgnoreCase));

                    if (parentCategory == null)
                    {
                        _logger.LogWarning("Parent category '{ParentSlug}' not found in category list", parentSlug);
                        errors.Add($"Parent category '{parentSlug}' not found");
                        results[parentSlug] = []; // Use original slug as key
                        continue;
                    }

                    // Extract subcategories from existing API data
                    var subcategories = new List<HierarchicalCategoryInfo>();
                    
                    if (recursive)
                    {
                        // For recursive mode, we need to flatten all descendants
                        // Start with the parent's direct subcategories
                        var allDescendants = new List<HierarchicalCategoryInfo>();
                        var toProcess = new Queue<HierarchicalCategoryInfo>();
                        
                        // Add all direct subcategories to the queue
                        foreach (var subcat in parentCategory.Subcategories)
                        {
                            toProcess.Enqueue(subcat);
                            allDescendants.Add(subcat);
                        }
                        
                        // Process each level up to maxDepth
                        int currentDepth = 1;
                        while (toProcess.Count > 0 && currentDepth < maxDepth)
                        {
                            var currentLevelCount = toProcess.Count;
                            for (int i = 0; i < currentLevelCount; i++)
                            {
                                var current = toProcess.Dequeue();
                                foreach (var subcat in current.Subcategories)
                                {
                                    toProcess.Enqueue(subcat);
                                    allDescendants.Add(subcat);
                                }
                            }
                            currentDepth++;
                        }
                        
                        subcategories = allDescendants;
                    }
                    else
                    {
                        // Get only direct children from the parent's Subcategories property
                        subcategories = parentCategory.Subcategories.ToList();
                    }
                    results[parentSlug] = subcategories; // Use original slug as key
                    _logger.LogInformation("Scanned {Count} subcategories for {ParentSlug}", subcategories.Count, parentSlug);

                    // Add delay between requests to be respectful
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scanning subcategories for {ParentSlug}", parentSlug);
                    errors.Add($"Error scanning '{parentSlug}': {ex.Message}");
                    results[parentSlug] = []; // Use original slug as key
                }
            }

            var response = new ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>
            {
                Success = results.Any(r => r.Value.Any()) || errors.Count == 0, // Success if we found any subcategories OR no errors
                Message = $"Bulk scan completed for {parentSlugsList.Count} categories. {results.Values.Sum(v => v.Count)} total subcategories found.",
                Data = results,
                Errors = errors,
                Metadata = new ResponseMetadata
                {
                    TotalCount = results.Values.Sum(v => v.Count),
                    Page = 1,
                    PageSize = results.Count,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "categoriesScanned", parentSlugsList.Count },
                        { "successfulScans", results.Count(r => r.Value.Any()) },
                        { "failedScans", errors.Count },
                        { "totalSubcategories", results.Values.Sum(v => v.Count) }
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk subcategory scanning: {ErrorMessage}", ex.Message);

            var errorResponse = new ApiResponse<Dictionary<string, List<HierarchicalCategoryInfo>>>
            {
                Success = false,
                Message = "Failed to perform bulk subcategory scanning",
                Errors = [ex.Message]
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Debug endpoint to show available category slugs and names for troubleshooting
    /// </summary>
    /// <returns>List of all category slugs and names</returns>
    [HttpGet("kaspi/debug/slugs")]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), 200)]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetCategorySlugsDebug()
    {
        try
        {
            // Get categories using the same caching logic
            var categoriesResult = await GetKaspiCategories(true);

            List<HierarchicalCategoryInfo> allCategories;
            if (categoriesResult.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<List<HierarchicalCategoryInfo>> apiResponse)
            {
                allCategories = apiResponse.Data ?? [];
            }
            else
            {
                allCategories = [];
            }

            // Create debug info showing slug, name, and level
            var debugInfo = allCategories
                .Select(c => new
                {
                    slug = c.Slug,
                    name = c.Name,
                    level = c.Level,
                    parentSlug = c.ParentSlug,
                    url = c.Url,
                    subcategoryCount = c.Subcategories.Count
                })
                .OrderBy(c => c.level)
                .ThenBy(c => c.name)
                .Take(50) // Limit to first 50 for debugging
                .ToList<object>();

            var response = new ApiResponse<List<object>>
            {
                Success = true,
                Message = $"Debug info for first 50 categories (total: {allCategories.Count})",
                Data = debugInfo,
                Metadata = new ResponseMetadata
                {
                    TotalCount = allCategories.Count,
                    Page = 1,
                    PageSize = debugInfo.Count,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["totalCategoriesLoaded"] = allCategories.Count,
                        ["topLevelCategories"] = allCategories.Count(c => c.Level == 0),
                        ["categoriesWithSubcategories"] = allCategories.Count(c => c.Subcategories.Any())
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug category slugs");
            return StatusCode(500, new ApiResponse<List<object>>
            {
                Success = false,
                Message = "Failed to get debug category slugs",
                Errors = [ex.Message]
            });
        }
    }

    /// <summary>
    /// Flattens a hierarchical category structure into a flat list of all categories and subcategories
    /// </summary>
    /// <param name="hierarchicalCategories">List of hierarchical categories</param>
    /// <returns>Flattened list containing all categories and subcategories</returns>
    private static List<HierarchicalCategoryInfo> FlattenCategoryHierarchy(List<HierarchicalCategoryInfo> hierarchicalCategories)
    {
        var flatList = new List<HierarchicalCategoryInfo>();
        
        foreach (var category in hierarchicalCategories)
        {
            // Add the category itself
            flatList.Add(category);
            
            // Recursively add all subcategories
            if (category.Subcategories.Any())
            {
                var flattenedSubcategories = FlattenCategoryHierarchy(category.Subcategories);
                flatList.AddRange(flattenedSubcategories);
            }
        }
        
        return flatList;
    }
}
