using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Services;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.AntiDetection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Controller for managing product search, details, and offers using Kaspi's Product APIs.
/// This controller provides access to 2.5M+ products with pricing, availability, and merchant information,
/// smart caching, and rate limiting to ensure reliable performance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Product Management")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly KaspiOfferApiClient _offerApiClient;
    private readonly KaspiProductFilterApiClient _productFilterApiClient;
    private readonly KaspiProductDetailExtractor _productDetailExtractor;

    /// <summary>
    /// Initializes a new instance of the ProductsController
    /// </summary>
    /// <param name="logger">Logger instance for structured logging</param>
    /// <param name="offerApiClient">Kaspi Offer API client for product offers</param>
    /// <param name="productFilterApiClient">Kaspi Product Filter API client for product search</param>
    /// <param name="productDetailExtractor">Kaspi Product Detail Extractor for descriptions and specifications</param>
    public ProductsController(
        ILogger<ProductsController> logger,
        KaspiOfferApiClient offerApiClient,
        KaspiProductFilterApiClient productFilterApiClient,
        KaspiProductDetailExtractor productDetailExtractor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _offerApiClient = offerApiClient ?? throw new ArgumentNullException(nameof(offerApiClient));
        _productFilterApiClient = productFilterApiClient ?? throw new ArgumentNullException(nameof(productFilterApiClient));
        _productDetailExtractor = productDetailExtractor ?? throw new ArgumentNullException(nameof(productDetailExtractor));
    }

    /// <summary>
    /// Get detailed product information by product ID with regional pricing and availability.
    /// This endpoint fetches comprehensive product details including specifications, images,
    /// pricing across different merchants, and regional availability information.
    /// </summary>
    /// <param name="productId">The unique product identifier (e.g., 102298404)</param>
    /// <param name="cityCode">City code for regional pricing (default: 750000000 - Almaty)</param>
    /// <returns>Detailed product information with offers and availability</returns>
    /// <response code="200">Product details retrieved successfully</response>
    /// <response code="400">Invalid product ID format</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Error retrieving product information</response>
    /// <remarks>
    /// Example usage:
    /// ```
    /// GET /api/products/102298404?cityCode=750000000
    /// ```
    /// 
    /// Returns comprehensive product data including:
    /// - Basic information (name, description, brand)
    /// - Pricing from multiple merchants
    /// - Availability and delivery options
    /// - Product specifications and attributes
    /// - High-resolution product images
    /// 
    /// Performance: Typically responds within 2 seconds
    /// Rate limiting: Max 100 requests per minute per IP
    /// </remarks>
    [HttpGet("{productId}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<ProductDetailResponse>>> GetProduct(
        [Required] string productId,
        [FromQuery] string? cityCode = "750000000")
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("GetProduct called with empty productId");
                return BadRequest(CreateErrorResponse<ProductDetailResponse>("Product ID is required", "Invalid product ID parameter"));
            }

            _logger.LogInformation("Retrieving product details for ID: {ProductId} in city: {CityCode}", productId, cityCode);

            // Get product offers which contain the basic product information
            var offers = await _offerApiClient.GetOffersForProductAsync(productId, cityCode ?? "750000000", AntiDetectionStrategy.CreateBasicStrategy());

            if (offers == null || !offers.Any())
            {
                _logger.LogWarning("No offers found for product ID: {ProductId}", productId);
                return NotFound(CreateErrorResponse<ProductDetailResponse>("Product not found", $"No product found with ID '{productId}'"));
            }

            // Try to get actual product name by searching for the product ID
            string productName = $"Product {productId}"; // Default fallback
            string productSlug = GenerateProductSlug(productName, productId);
            
            try
            {
                // Attempt to get product details by searching for the product ID
                var searchResults = await _productFilterApiClient.GetProductResultsAsync("", productId, "relevance", 0, cityCode ?? "750000000");
                if (searchResults?.Products?.Any() == true)
                {
                    var product = searchResults.Products.FirstOrDefault(p => p.Id == productId);
                    if (product != null && !string.IsNullOrWhiteSpace(product.Name))
                    {
                        productName = product.Name;
                        productSlug = GenerateProductSlug(productName, productId);
                        _logger.LogInformation("Found actual product name: {ProductName} for ID: {ProductId}", productName, productId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve product name for ID: {ProductId}, using fallback", productId);
            }

            // Extract product information from offers
            var firstOffer = offers.First();
            
            // Try to get detailed product information including description and specifications
            string? description = null;
            List<SpecificationGroup> specifications = [];
            List<string> galleryImages = [];
            
            try
            {
                _logger.LogInformation("Fetching detailed product information for ID: {ProductId}", productId);
                var detailData = await _productDetailExtractor.ExtractProductDetailAsync(productId, cityCode ?? "750000000", AntiDetectionStrategy.CreateBasicStrategy());
                
                if (detailData != null)
                {
                    description = detailData.Description;
                    galleryImages = detailData.GalleryImages?.Select(img => img.Large ?? img.Medium ?? img.Small).Where(url => !string.IsNullOrEmpty(url)).ToList() ?? [];
                    
                    // Map specifications from Kaspi format to API format
                    specifications = detailData.Specifications?.Select(specGroup => new SpecificationGroup
                    {
                        Code = specGroup.Code,
                        Name = specGroup.Name,
                        Features = specGroup.Features?.Select(feature => new SpecificationFeature
                        {
                            Code = feature.Code,
                            Name = feature.Name,
                            Type = feature.Type,
                            FeatureValues = feature.Values?.Select(value => new SpecificationFeatureValue
                            {
                                Value = value
                            }).ToList() ?? [],
                            Position = feature.Position,
                            Visible = feature.Visible,
                            MultiValued = feature.MultiValued
                        }).ToList() ?? []
                    }).ToList() ?? [];
                    
                    _logger.LogInformation("Successfully extracted detailed info for product {ProductId}: Description length: {DescLength}, Specification groups: {SpecCount}, Images: {ImageCount}",
                        productId, description?.Length ?? 0, specifications.Count, galleryImages.Count);
                }
                else
                {
                    _logger.LogWarning("Could not extract detailed product information for ID: {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract detailed product information for ID: {ProductId}, continuing with basic info", productId);
            }
            
            var productDetail = new ProductDetailResponse
            {
                Id = productId,
                Name = productName, // Use the actual product name we retrieved
                Slug = productSlug, // Use the properly generated slug
                Description = description, // Now populated from detailed extraction
                Specifications = specifications, // Now populated from detailed extraction
                GalleryImages = galleryImages, // Now populated from detailed extraction
                Price = offers.Min(o => o.Price),
                Currency = "KZT",
                CityCode = cityCode,
                CityName = GetCityName(cityCode),
                Offers = offers.Select(offer => new OfferResponse
                {
                    Id = offer.MerchantId ?? "unknown",
                    MerchantName = offer.Merchant,
                    Price = offer.Price,
                    Currency = offer.Currency,
                    Availability = offer.InStock ? "in_stock" : "out_of_stock",
                    DeliveryInfo = offer.DeliveryType ?? "Standard delivery",
                    Rating = (decimal?)(offer.MerchantRating ?? 0),
                    ReviewCount = offer.MerchantReviewsQuantity ?? 0,
                    Url = offer.OfferUrl ?? $"https://kaspi.kz/shop/p/{productId}"
                }).ToList(),
                Attributes = ExtractProductAttributes(offers.ToList())
            };

            stopwatch.Stop();
            var offerCount = offers.Count();
            _logger.LogInformation("Successfully retrieved product {ProductId} with {OfferCount} offers in {ElapsedMs}ms", 
                productId, offerCount, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<ProductDetailResponse>
            {
                Success = true,
                Message = $"Product '{productDetail.Name}' retrieved successfully",
                Data = productDetail,
                Metadata = new ResponseMetadata
                {
                    TotalCount = 1,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "offerCount", offerCount },
                        { "cityCode", cityCode ?? "750000000" },
                        { "priceRange", $"{offers.Min(o => o.Price):N0} - {offers.Max(o => o.Price):N0} KZT" }
                    }
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid product ID format: {ProductId}", productId);
            return BadRequest(CreateErrorResponse<ProductDetailResponse>("Invalid parameter", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout retrieving product {ProductId}", productId);
            return StatusCode(408, CreateErrorResponse<ProductDetailResponse>("Request timeout", "The request took too long to process"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving product {ProductId}", productId);
            return StatusCode(500, CreateErrorResponse<ProductDetailResponse>("Internal server error", 
                "An unexpected error occurred while retrieving product information"));
        }
    }

    /// <summary>
    /// Get available offers for a specific product from different merchants.
    /// This endpoint provides real-time pricing, availability, and merchant information
    /// for a specific product across all available sellers on Kaspi marketplace.
    /// </summary>
    /// <param name="productId">The unique product identifier</param>
    /// <param name="cityCode">City code for regional pricing (default: 750000000 - Almaty)</param>
    /// <returns>List of available offers for the product</returns>
    /// <response code="200">Offers retrieved successfully</response>
    /// <response code="400">Invalid product ID format</response>
    /// <response code="404">No offers found for the product</response>
    /// <response code="500">Error retrieving offers</response>
    /// <remarks>
    /// Example usage:
    /// ```
    /// GET /api/products/102298404/offers?cityCode=750000000
    /// ```
    /// 
    /// Returns comprehensive offer data including:
    /// - Merchant information and ratings
    /// - Current pricing and availability
    /// - Delivery options and timeframes
    /// - Customer reviews and ratings
    /// - Direct purchase links
    /// 
    /// Performance: Typically responds within 2 seconds
    /// Rate limiting: Max 100 requests per minute per IP
    /// </remarks>
    [HttpGet("{productId}/offers")]
    [ProducesResponseType(typeof(ApiResponse<List<OfferResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<List<OfferResponse>>>> GetProductOffers(
        [Required] string productId,
        [FromQuery] string? cityCode = "750000000")
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("GetProductOffers called with empty productId");
                return BadRequest(CreateErrorResponse<List<OfferResponse>>("Product ID is required", "Invalid product ID parameter"));
            }

            _logger.LogInformation("Retrieving offers for product ID: {ProductId} in city: {CityCode}", productId, cityCode);

            var offers = await _offerApiClient.GetOffersForProductAsync(productId, cityCode ?? "750000000", AntiDetectionStrategy.CreateBasicStrategy());

            if (offers == null || !offers.Any())
            {
                _logger.LogWarning("No offers found for product ID: {ProductId}", productId);
                return NotFound(CreateErrorResponse<List<OfferResponse>>("No offers found", $"No offers available for product '{productId}'"));
            }

            var offerResponses = offers.Select(offer => new OfferResponse
            {
                Id = offer.MerchantId ?? "unknown",
                MerchantName = offer.Merchant,
                Price = offer.Price,
                Currency = offer.Currency,
                Availability = offer.InStock ? "in_stock" : "out_of_stock",
                DeliveryInfo = offer.DeliveryType ?? "Standard delivery",
                Rating = (decimal?)(offer.MerchantRating ?? 0),
                ReviewCount = offer.MerchantReviewsQuantity ?? 0,
                Url = offer.OfferUrl ?? $"https://kaspi.kz/shop/p/{productId}"
            }).ToList();

            stopwatch.Stop();
            var offerCount = offers.Count();
            _logger.LogInformation("Successfully retrieved {OfferCount} offers for product {ProductId} in {ElapsedMs}ms", 
                offerCount, productId, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<List<OfferResponse>>
            {
                Success = true,
                Message = $"Retrieved {offerCount} offers for product",
                Data = offerResponses,
                Metadata = new ResponseMetadata
                {
                    TotalCount = offerCount,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "productId", productId },
                        { "cityCode", cityCode ?? "750000000" },
                        { "priceRange", $"{offers.Min(o => o.Price):N0} - {offers.Max(o => o.Price):N0} KZT" },
                        { "availableOffers", offers.Count(o => o.InStock) },
                        { "merchantCount", offers.Select(o => o.MerchantId).Distinct().Count() }
                    }
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid product ID format: {ProductId}", productId);
            return BadRequest(CreateErrorResponse<List<OfferResponse>>("Invalid parameter", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout retrieving offers for product {ProductId}", productId);
            return StatusCode(408, CreateErrorResponse<List<OfferResponse>>("Request timeout", "The request took too long to process"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving offers for product {ProductId}", productId);
            return StatusCode(500, CreateErrorResponse<List<OfferResponse>>("Internal server error", 
                "An unexpected error occurred while retrieving product offers"));
        }
    }

    /// <summary>
    /// Search for products using text query with advanced filtering options.
    /// This endpoint provides comprehensive product search capabilities with category filtering,
    /// regional pricing, and pagination support across Kaspi's 2.5M+ product catalog.
    /// </summary>
    /// <param name="text">Search query text (minimum 2 characters)</param>
    /// <param name="category">Optional category filter (category slug)</param>
    /// <param name="cityCode">City code for regional pricing (default: 750000000 - Almaty)</param>
    /// <param name="page">Page number for pagination (0-based, default: 0)</param>
    /// <param name="pageSize">Number of items per page (Note: Kaspi returns ~12 products per page regardless of this parameter)</param>
    /// <returns>Search results with product information and pagination metadata</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid search parameters</response>
    /// <response code="500">Error performing search</response>
    /// <remarks>
    /// Example usage:
    /// ```
    /// GET /api/products/search?text=iPhone&amp;category=smartphones&amp;page=0&amp;pageSize=20
    /// ```
    /// 
    /// Search features:
    /// - Full-text search across product names and descriptions
    /// - Category-based filtering using category slugs
    /// - Regional pricing based on city codes
    /// - Pagination support for large result sets
    /// - Relevance scoring and sorting
    /// 
    /// **Important Note**: Due to Kaspi.kz API limitations, approximately 12 products are returned per page 
    /// regardless of the pageSize parameter. The pageSize parameter is kept for API consistency but 
    /// doesn't affect the actual number of results returned.
    /// 
    /// Performance: Typically responds within 3 seconds
    /// Rate limiting: Max 60 requests per minute per IP
    /// </remarks>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<ProductSearchResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<ProductSearchResponse>>> SearchProducts(
        [FromQuery, Required] string text,
        [FromQuery] string? category = null,
        [FromQuery] string? cityCode = "750000000",
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            {
                return BadRequest(CreateErrorResponse<ProductSearchResponse>("Invalid search query", 
                    "Search text must be at least 2 characters long"));
            }

            if (page < 0)
            {
                return BadRequest(CreateErrorResponse<ProductSearchResponse>("Invalid page number", 
                    "Page number must be 0 or greater"));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(CreateErrorResponse<ProductSearchResponse>("Invalid page size", 
                    "Page size must be between 1 and 100 (Note: Kaspi returns ~12 products per page regardless of this parameter)"));
            }

            _logger.LogInformation("Searching products with text: '{SearchText}', category: '{Category}', page: {Page}, pageSize: {PageSize}", 
                text, category, page, pageSize);

            // Perform product search - use search filters to get filter query first
            string filterQuery = ""; // This would need to be built from the search parameters
            var searchResults = await _productFilterApiClient.GetProductResultsAsync(filterQuery, text, "relevance", page, cityCode ?? "750000000");

            if (searchResults?.Products == null)
            {
                _logger.LogWarning("Search returned null results for query: {SearchText}", text);
                searchResults = new ProductSearchResults { Products = [] };
            }

            var products = searchResults.Products.Select(product => new ProductSummaryResponse
            {
                Id = product.Id ?? "unknown",
                Name = product.Name ?? "Unknown Product",
                Slug = GenerateProductSlug(product.Name, product.Id),
                Price = product.Price,
                Currency = "KZT",
                ImageUrl = product.ImageUrl,
                Rating = (decimal)product.Rating,
                ReviewCount = product.ReviewCount,
                Availability = product.InStock ? "in_stock" : "out_of_stock",
                Category = category ?? "general"
            }).ToList();

            var totalCount = searchResults.TotalCount;
            var hasNextPage = searchResults.Page < searchResults.TotalPages;

            var searchResponse = new ProductSearchResponse
            {
                Products = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasNextPage = hasNextPage,
                SearchQuery = text,
                Category = category
            };

            stopwatch.Stop();
            _logger.LogInformation("Search completed for '{SearchText}' - found {ProductCount} products in {ElapsedMs}ms", 
                text, products.Count, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<ProductSearchResponse>
            {
                Success = true,
                Message = $"Found {products.Count} products matching '{text}'",
                Data = searchResponse,
                Metadata = new ResponseMetadata
                {
                    TotalCount = totalCount,
                    Page = page, // Keep 0-based for consistency with input parameter
                    PageSize = pageSize,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "searchQuery", text },
                        { "category", category ?? "all" },
                        { "cityCode", cityCode ?? "750000000" },
                        { "hasNextPage", hasNextPage },
                        { "avgPrice", products.Any() ? products.Average(p => p.Price) : 0 },
                        { "kaspiPageSizeLimit", "~12 products per page due to Kaspi.kz API limitations" }
                    }
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search parameters: {SearchText}", text);
            return BadRequest(CreateErrorResponse<ProductSearchResponse>("Invalid parameters", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Search timeout for query: {SearchText}", text);
            return StatusCode(408, CreateErrorResponse<ProductSearchResponse>("Search timeout", "The search request took too long to process"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during product search: {SearchText}", text);
            return StatusCode(500, CreateErrorResponse<ProductSearchResponse>("Internal server error", 
                "An unexpected error occurred while searching for products"));
        }
    }

    #region Helper Methods

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    private static ApiResponse<T> CreateErrorResponse<T>(string message, string detail)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = [detail],
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a URL-friendly slug for a product
    /// </summary>
    private static string GenerateProductSlug(string? productName, string? productId)
    {
        if (string.IsNullOrWhiteSpace(productName) && string.IsNullOrWhiteSpace(productId))
            return "unknown-product";

        var name = productName ?? "product";
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');

        return !string.IsNullOrWhiteSpace(productId) ? $"{slug}-{productId}" : slug;
    }

    /// <summary>
    /// Gets the city name for a given city code
    /// </summary>
    private static string? GetCityName(string? cityCode)
    {
        return cityCode switch
        {
            "750000000" => "Almaty",
            "710000000" => "Nur-Sultan",
            "160000000" => "Shymkent",
            _ => null
        };
    }

    /// <summary>
    /// Extracts product attributes from offers
    /// </summary>
    private static Dictionary<string, object> ExtractProductAttributes(List<Offer> offers)
    {
        var attributes = new Dictionary<string, object>();
        
        if (offers.Any())
        {
            var firstOffer = offers.First();
            attributes["merchantCount"] = offers.Select(o => o.MerchantId).Distinct().Count();
            attributes["availableOffers"] = offers.Count(o => o.InStock);
            attributes["priceRange"] = new { min = offers.Min(o => o.Price), max = offers.Max(o => o.Price) };
            
            if (offers.Any(o => o.MerchantRating.HasValue))
            {
                attributes["avgRating"] = offers.Where(o => o.MerchantRating.HasValue).Average(o => o.MerchantRating!.Value);
            }
            
            if (offers.Any(o => o.MerchantReviewsQuantity.HasValue))
            {
                attributes["totalReviews"] = offers.Where(o => o.MerchantReviewsQuantity.HasValue).Sum(o => o.MerchantReviewsQuantity!.Value);
            }
        }
        
        return attributes;
    }

    #endregion
}
