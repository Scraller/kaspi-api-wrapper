using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Handles API calls to Kaspi's offer endpoint to get merchant/pricing data
/// </summary>
public class KaspiOfferApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly KaspiHttpClient _httpClient;
    private readonly Random _random = new();

    private const string OFFER_API_URL = "https://kaspi.kz/yml/offer-view/offers/{0}";

    public KaspiOfferApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        KaspiHttpClient httpClient)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Fetches all offers for a specific product ID
    /// </summary>
    public virtual async Task<IEnumerable<Offer>> GetOffersForProductAsync(
        string productId,
        string cityId,
        AntiDetectionStrategy strategy)
    {
        try
        {
            _logger.LogInformation("Fetching offers for product {ProductId} in city {CityId}", productId, cityId);

            var apiUrl = string.Format(OFFER_API_URL, productId);
            var requestBody = new
            {
                cityId = cityId,
                id = productId
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Apply anti-detection measures
            await Task.Delay(_random.Next(200, 800)); // Human-like delay

            var (success, responseContent) = await _httpClient.SendPostRequestAsync(apiUrl, content, strategy, cityId);

            if (!success || string.IsNullOrEmpty(responseContent))
            {
                _logger.LogWarning("Failed to fetch offers for product {ProductId}", productId);
                return Enumerable.Empty<Offer>();
            }

            return ParseOffersFromResponse(responseContent, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching offers for product {ProductId}", productId);
            return Enumerable.Empty<Offer>();
        }
    }

    /// <summary>
    /// Parses the JSON response from Kaspi offer API
    /// </summary>
    private IEnumerable<Offer> ParseOffersFromResponse(string jsonContent, string productId)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (!root.TryGetProperty("offers", out var offersElement) ||
                offersElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("No offers array found in response for product {ProductId}", productId);
                return Enumerable.Empty<Offer>();
            }

            var offers = new List<Offer>();

            foreach (var offerElement in offersElement.EnumerateArray())
            {
                var offer = new Offer
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = productId,

                    // Basic offer info
                    Merchant = GetJsonPropertyString(offerElement, "merchantName"),
                    Price = GetJsonPropertyDecimal(offerElement, "price"),
                    Currency = "KZT",
                    InStock = GetJsonPropertyInt(offerElement, "preorder") == 0,

                    // Enhanced merchant info
                    MerchantId = GetJsonPropertyString(offerElement, "merchantId"),
                    MasterSku = GetJsonPropertyString(offerElement, "masterSku"),
                    MerchantSku = GetJsonPropertyString(offerElement, "merchantSku"),
                    MerchantReviewsQuantity = GetJsonPropertyInt(offerElement, "merchantReviewsQuantity"),
                    MerchantRating = GetJsonPropertyDouble(offerElement, "merchantRating"),

                    // Delivery information
                    DeliveryDate = GetJsonPropertyDateTime(offerElement, "delivery"),
                    PickupDate = GetJsonPropertyDateTime(offerElement, "kdPickupDate"),
                    DeliveryType = GetJsonPropertyString(offerElement, "deliveryType"),
                    DeliveryDuration = GetJsonPropertyString(offerElement, "deliveryDuration"),
                    KaspiDelivery = GetJsonPropertyBool(offerElement, "kaspiDelivery"),
                    InterCity = GetDeliveryOptionBool(offerElement, "interCity"),
                    DeliveryCost = GetDeliveryOptionDecimal(offerElement, "deliveryCost"),
                    DeliveryThreshold = GetDeliveryOptionDecimal(offerElement, "deliveryThreshold"),

                    // Availability
                    AvailabilityDate = GetJsonPropertyDateTime(offerElement, "availabilityDate"),
                    Preorder = GetJsonPropertyInt(offerElement, "preorder"),
                    LocatedInPoint = GetJsonPropertyString(offerElement, "locatedInPoint"),
                    KdPoints = GetJsonPropertyStringArray(offerElement, "kdPoints"),

                    Timestamp = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                // Generate offer URL if we have merchant info
                if (!string.IsNullOrEmpty(offer.MerchantId))
                {
                    offer.OfferUrl = $"https://kaspi.kz/shop/p/{productId}/?offerMerchantId={offer.MerchantId}";
                }

                offers.Add(offer);
            }

            _logger.LogInformation("Parsed {OfferCount} offers for product {ProductId}", offers.Count, productId);
            return offers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing offers response for product {ProductId}", productId);
            return Enumerable.Empty<Offer>();
        }
    }

    /// <summary>
    /// Public method for testing JSON parsing functionality
    /// </summary>
    public IEnumerable<Offer> ParseOffersFromJson(string jsonContent)
    {
        return ParseOffersFromResponse(jsonContent, "test-product-id");
    }

    #region Helper Methods

    private string GetJsonPropertyString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private decimal GetJsonPropertyDecimal(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.TryGetDecimal(out decimal value))
                return value;
            if (property.TryGetDouble(out double doubleValue))
                return (decimal)doubleValue;
        }
        return 0;
    }

    private double GetJsonPropertyDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.TryGetDouble(out double value))
        {
            return value;
        }
        return 0.0;
    }

    private int GetJsonPropertyInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.TryGetInt32(out int value))
        {
            return value;
        }
        return 0;
    }

    private bool GetJsonPropertyBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.True)
        {
            return true;
        }
        if (element.TryGetProperty(propertyName, out property) &&
            property.ValueKind == JsonValueKind.False)
        {
            return false;
        }
        return false;
    }

    private DateTime? GetJsonPropertyDateTime(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.TryGetDateTime(out DateTime value))
        {
            return value;
        }
        return null;
    }

    private List<string>? GetJsonPropertyStringArray(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Array)
        {
            return property.EnumerateArray()
                         .Select(item => item.GetString() ?? string.Empty)
                         .Where(s => !string.IsNullOrEmpty(s))
                         .ToList();
        }
        return null;
    }

    private decimal GetDeliveryOptionDecimal(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty("deliveryOptions", out var deliveryOptions) &&
            deliveryOptions.TryGetProperty("TO_DOOR", out var toDoor) &&
            toDoor.TryGetProperty(propertyName, out var property))
        {
            if (property.TryGetDecimal(out decimal value))
                return value;
            if (property.TryGetDouble(out double doubleValue))
                return (decimal)doubleValue;
        }
        return 0;
    }

    private bool GetDeliveryOptionBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty("deliveryOptions", out var deliveryOptions) &&
            deliveryOptions.TryGetProperty("TO_DOOR", out var toDoor) &&
            toDoor.TryGetProperty(propertyName, out var property))
        {
            return property.ValueKind == JsonValueKind.True;
        }
        return false;
    }

    #endregion
}
