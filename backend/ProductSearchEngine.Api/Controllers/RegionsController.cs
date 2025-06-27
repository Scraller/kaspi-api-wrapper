using Microsoft.AspNetCore.Mvc;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Api.Services;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;
using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Controllers;

/// <summary>
/// Regional data and location-based services for Kaspi.kz
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Regions")]
public class RegionsController : ControllerBase
{
    private readonly KaspiLocationService _locationService;
    private readonly KaspiOfferApiClient _offerApiClient;
    private readonly ILogger<RegionsController> _logger;

    /// <summary>
    /// Initialize the RegionsController with location service, offer API client, and logger
    /// </summary>
    /// <param name="locationService">Service for Kaspi location operations</param>
    /// <param name="offerApiClient">Kaspi Offer API client for checking product availability</param>
    /// <param name="logger">Logger for the controller</param>
    public RegionsController(
        KaspiLocationService locationService, 
        KaspiOfferApiClient offerApiClient,
        ILogger<RegionsController> logger)
    {
        _locationService = locationService;
        _offerApiClient = offerApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all supported cities from Kaspi.kz
    /// </summary>
    /// <param name="majorCitiesOnly">If true, returns only major regional centers</param>
    /// <returns>List of all supported cities</returns>
    /// <response code="200">Returns the list of cities</response>
    [HttpGet("cities")]
    [ProducesResponseType<ApiResponse<List<CityResponse>>>(200)]
    public ActionResult<ApiResponse<List<CityResponse>>> GetCities([FromQuery] bool majorCitiesOnly = false)
    {
        try
        {
            _logger.LogInformation("Getting cities list. Major cities only: {MajorCitiesOnly}", majorCitiesOnly);

            var cities = majorCitiesOnly 
                ? _locationService.GetMajorCities() 
                : _locationService.GetAllCities();

            return Ok(ApiResponse<List<CityResponse>>.SuccessResult(
                cities,
                $"Retrieved {cities.Count} cities from Kaspi.kz",
                new ResponseMetadata
                {
                    TotalCount = cities.Count,
                    Page = 1,
                    PageSize = cities.Count
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cities list");
            return StatusCode(500, ApiResponse<List<CityResponse>>.ErrorResult(
                "Failed to retrieve cities", 
                new[] { ex.Message }.ToList()));
        }
    }

    /// <summary>
    /// Search cities by name (supports partial matches)
    /// </summary>
    /// <param name="q">Search query for city name</param>
    /// <param name="majorOnly">If true, searches only major cities</param>
    /// <param name="limit">Maximum number of results (1-100)</param>
    /// <returns>List of matching cities</returns>
    /// <response code="200">Returns matching cities</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("cities/search")]
    [ProducesResponseType<ApiResponse<List<CityResponse>>>(200)]
    [ProducesResponseType<ApiResponse<object>>(400)]
    public ActionResult<ApiResponse<List<CityResponse>>> SearchCities(
        [FromQuery] string? q = null,
        [FromQuery] bool majorOnly = false,
        [FromQuery] [Range(1, 100)] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Searching cities with query: '{Query}', Major only: {MajorOnly}, Limit: {Limit}", 
                q, majorOnly, limit);

            var cities = _locationService.SearchCities(q ?? string.Empty);

            if (majorOnly)
            {
                cities = cities.Where(c => c.IsMajorCity).ToList();
            }

            var limitedResults = cities.Take(limit).ToList();

            return Ok(ApiResponse<List<CityResponse>>.SuccessResult(
                limitedResults,
                $"Found {limitedResults.Count} cities matching '{q}'",
                new ResponseMetadata
                {
                    TotalCount = cities.Count, // Total found before limiting
                    Page = 1,
                    PageSize = limitedResults.Count
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cities with query: {Query}", q);
            return StatusCode(500, ApiResponse<List<CityResponse>>.ErrorResult(
                "Failed to search cities",
                new[] { ex.Message }.ToList()));
        }
    }

    /// <summary>
    /// Get detailed information about a specific city
    /// </summary>
    /// <param name="cityId">Kaspi city ID (e.g., 750000000 for Almaty)</param>
    /// <returns>Detailed city information</returns>
    /// <response code="200">Returns city details</response>
    /// <response code="404">City not found</response>
    [HttpGet("cities/{cityId}")]
    [ProducesResponseType<ApiResponse<CityResponse>>(200)]
    [ProducesResponseType<ApiResponse<object>>(404)]
    public ActionResult<ApiResponse<CityResponse>> GetCity([FromRoute] string cityId)
    {
        try
        {
            _logger.LogInformation("Getting city details for ID: {CityId}", cityId);

            var city = _locationService.GetCityById(cityId);
            if (city == null)
            {
                _logger.LogWarning("City not found: {CityId}", cityId);
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"City with ID '{cityId}' not found",
                    new[] { $"The city ID '{cityId}' is not supported by Kaspi.kz" }.ToList()));
            }

            return Ok(ApiResponse<CityResponse>.SuccessResult(
                city,
                $"Retrieved details for city: {city.Name}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving city details for ID: {CityId}", cityId);
            return StatusCode(500, ApiResponse<CityResponse>.ErrorResult(
                "Failed to retrieve city details",
                new[] { ex.Message }.ToList()));
        }
    }

    /// <summary>
    /// Check regional availability and service options for a specific city
    /// </summary>
    /// <param name="cityId">Kaspi city ID to check availability for</param>
    /// <param name="productId">Optional product ID to check specific product availability</param>
    /// <returns>Regional availability information including delivery options and payment methods</returns>
    /// <response code="200">Returns availability information</response>
    /// <response code="404">City not found</response>
    /// <response code="400">Invalid product ID</response>
    [HttpGet("availability/{cityId}")]
    [ProducesResponseType<ApiResponse<RegionalAvailabilityResponse>>(200)]
    [ProducesResponseType<ApiResponse<object>>(404)]
    [ProducesResponseType<ApiResponse<object>>(400)]
    public async Task<ActionResult<ApiResponse<RegionalAvailabilityResponse>>> GetRegionalAvailability(
        [FromRoute] string cityId,
        [FromQuery] string? productId = null)
    {
        try
        {
            _logger.LogInformation("Checking regional availability for city: {CityId}, Product: {ProductId}", 
                cityId, productId);

            if (!_locationService.IsCitySupported(cityId))
            {
                _logger.LogWarning("City not supported: {CityId}", cityId);
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"City with ID '{cityId}' is not supported",
                    new[] { $"The city ID '{cityId}' is not available in Kaspi.kz coverage area" }.ToList()));
            }

            var city = _locationService.GetCityById(cityId);
            if (city == null)
            {
                _logger.LogWarning("City not found: {CityId}", cityId);
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"City with ID '{cityId}' not found",
                    new[] { $"The city ID '{cityId}' is not supported by Kaspi.kz" }.ToList()));
            }

            RegionalAvailabilityResponse availability;

            if (!string.IsNullOrWhiteSpace(productId))
            {
                // Check availability for specific product
                _logger.LogInformation("Checking product {ProductId} availability in city {CityId}", productId, cityId);
                
                try
                {
                    var offers = await _offerApiClient.GetOffersForProductAsync(productId, cityId, AntiDetectionStrategy.CreateBasicStrategy());
                    
                    var isAvailable = offers != null && offers.Any();
                    var inStockOffers = offers?.Where(o => o.InStock).ToList() ?? new List<Offer>();
                    var totalOffers = offers?.Count() ?? 0;

                    availability = new RegionalAvailabilityResponse
                    {
                        CityId = cityId,
                        CityName = city.Name,
                        CityNameEn = city.NameEn,
                        IsAvailable = isAvailable,
                        ProductId = productId,
                        OfferCount = totalOffers,
                        InStockOfferCount = inStockOffers.Count,
                        MinPrice = offers?.Any() == true ? offers.Min(o => o.Price) : null,
                        MaxPrice = offers?.Any() == true ? offers.Max(o => o.Price) : null,
                        Currency = "KZT",
                        DeliveryOptions = isAvailable ? new List<string> { "Standard delivery", "Express delivery", "Pickup" } : new List<string>(),
                        PaymentMethods = isAvailable ? new List<string> { "Credit card", "Kaspi RED", "Installments", "Cash on delivery" } : new List<string>(),
                        SpecialOffers = isAvailable ? new List<string> { "Free delivery from 15000 KZT", "Cashback up to 10%" } : new List<string>(),
                        LastChecked = DateTime.UtcNow
                    };

                    _logger.LogInformation("Product {ProductId} availability in {CityName}: {IsAvailable} ({InStockCount}/{TotalCount} offers in stock)", 
                        productId, city.Name, isAvailable, inStockOffers.Count, totalOffers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking product {ProductId} availability in city {CityId}", productId, cityId);
                    return BadRequest(ApiResponse<object>.ErrorResult(
                        "Failed to check product availability",
                        new[] { $"Could not check availability for product '{productId}': {ex.Message}" }.ToList()));
                }
            }
            else
            {
                // General city availability without specific product
                availability = new RegionalAvailabilityResponse
                {
                    CityId = cityId,
                    CityName = city.Name,
                    CityNameEn = city.NameEn,
                    IsAvailable = true, // City is supported if we got here
                    ProductId = null,
                    OfferCount = null,
                    InStockOfferCount = null,
                    MinPrice = null,
                    MaxPrice = null,
                    Currency = "KZT",
                    DeliveryOptions = new List<string> { "Standard delivery", "Express delivery", "Pickup" },
                    PaymentMethods = new List<string> { "Credit card", "Kaspi RED", "Installments", "Cash on delivery" },
                    SpecialOffers = new List<string> { "Free delivery from 15000 KZT", "Cashback up to 10%" },
                    LastChecked = DateTime.UtcNow
                };

                _logger.LogInformation("General availability for {CityName}: Available", city.Name);
            }

            return Ok(ApiResponse<RegionalAvailabilityResponse>.SuccessResult(
                availability,
                $"Retrieved availability information for {availability.CityName}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking regional availability for city: {CityId}", cityId);
            return StatusCode(500, ApiResponse<RegionalAvailabilityResponse>.ErrorResult(
                "Failed to check regional availability",
                new[] { ex.Message }.ToList()));
        }
    }
}
