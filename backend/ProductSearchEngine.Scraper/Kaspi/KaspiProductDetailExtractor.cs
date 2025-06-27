using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Extracts detailed product information including specifications and descriptions from Kaspi.kz product pages.
/// Based on Product Specifications Discovery Session findings from window.BACKEND.components.item
/// </summary>
public class KaspiProductDetailExtractor
{
    private readonly KaspiHttpClient _kaspiHttpClient;
    private readonly ILogger<KaspiProductDetailExtractor> _logger;

    /// <summary>
    /// Regex pattern to extract BACKEND.components.item object from HTML
    /// </summary>
    private static readonly Regex BackendComponentsRegex = new(
        @"BACKEND\.components\.item\s*=\s*(\{.*\});", 
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// JSON serializer options for Kaspi data deserialization
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public KaspiProductDetailExtractor(KaspiHttpClient kaspiHttpClient, ILogger<KaspiProductDetailExtractor> logger)
    {
        _kaspiHttpClient = kaspiHttpClient ?? throw new ArgumentNullException(nameof(kaspiHttpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extract detailed product information from Kaspi.kz product page
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cityCode">City code for regional data (default: 750000000 - Almaty)</param>
    /// <param name="antiDetectionStrategy">Anti-detection strategy to use</param>
    /// <returns>Complete product details including specifications and description</returns>
    /// <exception cref="InvalidOperationException">When product data cannot be extracted</exception>
    public virtual async Task<KaspiProductDetailData?> ExtractProductDetailAsync(
        string productId, 
        string cityCode = "750000000", 
        AntiDetectionStrategy? antiDetectionStrategy = null)
    {
        try
        {
            _logger.LogInformation("Extracting product details for ID: {ProductId} in city: {CityCode}", productId, cityCode);

            // Construct product page URL with city parameter
            var productUrl = $"https://kaspi.kz/shop/p/product-{productId}/?c={cityCode}";
            _logger.LogDebug("Constructed product URL: {ProductUrl}", productUrl);
            
            // Apply anti-detection strategy
            if (antiDetectionStrategy?.SimulateHumanTiming == true)
            {
                var delay = Random.Shared.Next(200, 800); 
                await Task.Delay(delay);
            }

            // Fetch HTML content using KaspiHttpClient with proper headers
            var (success, html) = await _kaspiHttpClient.SendRequestAsync(
                productUrl, 
                antiDetectionStrategy ?? AntiDetectionStrategy.CreateBasicStrategy(), 
                cityCode, 
                useSessionCookies: true);
            
            if (!success)
            {
                _logger.LogWarning("Failed to fetch HTML for product ID: {ProductId}", productId);
                return null;
            }
            
            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Empty HTML response for product ID: {ProductId}", productId);
                return null;
            }

            // Extract BACKEND.components.item data
            var jsonData = ExtractBackendComponentsItem(html);
            if (string.IsNullOrEmpty(jsonData))
            {
                _logger.LogWarning("Could not find BACKEND.components.item in HTML for product ID: {ProductId}", productId);
                return null;
            }
            
            try
            {
                var itemComponent = JsonSerializer.Deserialize<KaspiItemComponent>(jsonData, JsonOptions);
                
                if (itemComponent == null)
                {
                    _logger.LogWarning("Failed to deserialize BACKEND.components.item for product ID: {ProductId}", productId);
                    return null;
                }

                _logger.LogInformation("Successfully extracted product details for ID: {ProductId}. " +
                    "Specifications groups: {SpecGroupCount}, Description length: {DescLength}",
                    productId, itemComponent.Specifications?.Count ?? 0, itemComponent.Description?.Length ?? 0);

                return MapToProductDetailData(itemComponent, productId, cityCode);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for product ID: {ProductId}", productId);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for product ID: {ProductId}", productId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error extracting product details for ID: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Extract BACKEND.components.item JSON from HTML using proper brace matching
    /// </summary>
    private static string? ExtractBackendComponentsItem(string html)
    {
        const string pattern = "BACKEND.components.item = {";
        var startIndex = html.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
            return null;

        // Move to the start of the JSON object
        var jsonStart = startIndex + pattern.Length - 1; // -1 to include the opening brace
        var braceCount = 0;
        var inString = false;
        var escaped = false;

        for (int i = jsonStart; i < html.Length; i++)
        {
            char c = html[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{')
                braceCount++;
            else if (c == '}')
                braceCount--;

            // When we reach brace count of 0, we've found the end of the JSON object
            if (braceCount == 0)
            {
                var jsonLength = i - jsonStart + 1;
                return html.Substring(jsonStart, jsonLength);
            }
        }

        return null;
    }

    /// <summary>
    /// Map Kaspi raw data to structured product detail data
    /// </summary>
    private static KaspiProductDetailData MapToProductDetailData(KaspiItemComponent itemComponent, string productId, string cityCode)
    {
        return new KaspiProductDetailData
        {
            Id = productId,
            Title = itemComponent.Card?.Title ?? string.Empty,
            Description = itemComponent.Description,
            Price = itemComponent.Card?.Price ?? 0,
            Currency = itemComponent.Card?.Currency ?? "KZT",
            CityCode = cityCode,
            Specifications = itemComponent.Specifications?.Select(MapSpecificationGroup).ToList() ?? [],
            GalleryImages = itemComponent.GalleryImages?.Select(img => new KaspiProductImage
            {
                Small = img.Small ?? string.Empty,
                Medium = img.Medium ?? string.Empty,
                Large = img.Large ?? string.Empty,
                Location = img.Location ?? string.Empty
            }).ToList() ?? [],
            ShopLink = itemComponent.Card?.ShopLink,
            Endpoint = itemComponent.Endpoint
        };
    }

    /// <summary>
    /// Map Kaspi specification group to structured model
    /// </summary>
    private static KaspiSpecificationGroup MapSpecificationGroup(KaspiRawSpecificationGroup rawGroup)
    {
        return new KaspiSpecificationGroup
        {
            Code = rawGroup.Code ?? string.Empty,
            Name = rawGroup.Name ?? string.Empty,
            Position = rawGroup.Position,
            Features = rawGroup.Features?.Select(MapSpecificationFeature).ToList() ?? []
        };
    }

    /// <summary>
    /// Map Kaspi specification feature to structured model
    /// </summary>
    private static KaspiSpecificationFeature MapSpecificationFeature(KaspiRawFeature rawFeature)
    {
        return new KaspiSpecificationFeature
        {
            Code = rawFeature.Code,
            Name = rawFeature.Name ?? string.Empty,
            Type = rawFeature.Type,
            Values = rawFeature.FeatureValues?.Select(v => v.Value).Where(v => !string.IsNullOrEmpty(v)).ToList() ?? [],
            Position = rawFeature.Position,
            Visible = rawFeature.Visible,
            MultiValued = rawFeature.MultiValued
        };
    }
}

#region Raw Kaspi Data Models for Deserialization

/// <summary>
/// Raw model for deserializing window.BACKEND.components.item
/// </summary>
internal class KaspiItemComponent
{
    [JsonPropertyName("card")]
    public KaspiRawCard? Card { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("specifications")]
    public List<KaspiRawSpecificationGroup>? Specifications { get; set; }

    [JsonPropertyName("galleryImages")]
    public List<KaspiRawGalleryImage>? GalleryImages { get; set; }

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }
}

/// <summary>
/// Raw model for product card data
/// </summary>
internal class KaspiRawCard
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("shopLink")]
    public string? ShopLink { get; set; }
}

/// <summary>
/// Raw model for specification groups
/// </summary>
internal class KaspiRawSpecificationGroup
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("features")]
    public List<KaspiRawFeature>? Features { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}

/// <summary>
/// Raw model for individual specifications
/// </summary>
internal class KaspiRawFeature
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("featureValues")]
    public List<KaspiRawFeatureValue>? FeatureValues { get; set; }

    [JsonPropertyName("multiValued")]
    public bool MultiValued { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("position")]
    public int Position { get; set; }
}

/// <summary>
/// Raw model for feature values
/// </summary>
internal class KaspiRawFeatureValue
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Raw model for gallery images
/// </summary>
internal class KaspiRawGalleryImage
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("large")]
    public string? Large { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

#endregion

#region Structured Output Models

/// <summary>
/// Structured product detail data extracted from Kaspi
/// </summary>
public class KaspiProductDetailData
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "KZT";
    public string? CityCode { get; set; }
    public List<KaspiSpecificationGroup> Specifications { get; set; } = [];
    public List<KaspiProductImage> GalleryImages { get; set; } = [];
    public string? ShopLink { get; set; }
    public string? Endpoint { get; set; }
}

/// <summary>
/// Structured specification group
/// </summary>
public class KaspiSpecificationGroup
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public List<KaspiSpecificationFeature> Features { get; set; } = [];
}

/// <summary>
/// Structured specification feature
/// </summary>
public class KaspiSpecificationFeature
{
    public string? Code { get; set; }
    public required string Name { get; set; }
    public string? Type { get; set; }
    public List<string> Values { get; set; } = [];
    public int Position { get; set; }
    public bool Visible { get; set; } = true;
    public bool MultiValued { get; set; }
}

/// <summary>
/// Product image data
/// </summary>
public class KaspiProductImage
{
    public required string Small { get; set; }
    public required string Medium { get; set; }
    public required string Large { get; set; }
    public required string Location { get; set; }
}

#endregion
