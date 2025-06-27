using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Parses JSON responses from Kaspi API into domain objects
/// </summary>
public class KaspiJsonParser
{
    private readonly ILogger _logger;
    private readonly string _marketplaceName;

    public KaspiJsonParser(ILogger logger, string marketplaceName)
    {
        _logger = logger;
        _marketplaceName = marketplaceName;
    }

    /// <summary>
    /// Parses products from the Kaspi API search response
    /// </summary>
    public IEnumerable<Product> ParseProductsFromApiResponse(string jsonContent, string category)
    {
        try
        {
            // Check if the content is valid JSON
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON content received");
                return Enumerable.Empty<Product>();
            }

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                var products = new List<Product>();

                foreach (var item in dataElement.EnumerateArray())
                {
                    var product = new Product
                    {
                        Id = GetJsonPropertyString(item, "id"),
                        Name = GetJsonPropertyString(item, "title"),
                        CurrentPrice = GetJsonPropertyInt(item, "unitSalePrice"),
                        Currency = "KZT",
                        Marketplace = _marketplaceName,
                        Category = category,
                        ProductUrl = $"https://kaspi.kz/shop/p/{GetJsonPropertyString(item, "id")}",
                        ImageUrl = GetFirstImageUrl(item),
                        Rating = (int?)GetJsonPropertyDouble(item, "rating"),
                        ReviewCount = GetJsonPropertyInt(item, "reviewsCount"),
                        InStock = true,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };

                    if (!string.IsNullOrEmpty(product.Id) && !string.IsNullOrEmpty(product.Name))
                    {
                        products.Add(product);
                    }
                }

                return products;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing API response");
        }

        return Enumerable.Empty<Product>();
    }

    /// <summary>
    /// Parses a product detail from the Kaspi API detail response
    /// </summary>
    public Product? ParseProductDetails(string jsonContent, string productId)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var dataElement))
            {
                var product = new Product
                {
                    Id = productId,
                    Name = GetJsonPropertyString(dataElement, "title"),
                    CurrentPrice = GetJsonPropertyInt(dataElement, "unitSalePrice"),
                    Currency = "KZT",
                    Marketplace = _marketplaceName,
                    ProductUrl = $"https://kaspi.kz/shop/p/{productId}",
                    ImageUrl = GetFirstImageUrl(dataElement),
                    Brand = GetJsonPropertyString(dataElement, "brand"),
                    Model = GetJsonPropertyString(dataElement, "model"),
                    Rating = (int?)GetJsonPropertyDouble(dataElement, "rating"),
                    ReviewCount = GetJsonPropertyInt(dataElement, "reviewsCount"),
                    InStock = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                // Extract description if available
                if (dataElement.TryGetProperty("description", out var descElement))
                {
                    product.Description = descElement.GetString();
                }

                // Extract specifications if available
                if (dataElement.TryGetProperty("specifications", out var specsElement) &&
                    specsElement.ValueKind == JsonValueKind.Array)
                {
                    var specs = new Dictionary<string, string>();
                    foreach (var spec in specsElement.EnumerateArray())
                    {
                        var name = GetJsonPropertyString(spec, "name");
                        var value = GetJsonPropertyString(spec, "value");
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            specs[name] = value;
                        }
                    }
                    product.Specifications = specs.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                }

                return product;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing product details JSON for ID: {ProductId}", productId);
        }

        return null;
    }

    /// <summary>
    /// Extracts product ID from a Kaspi URL
    /// </summary>
    public string ExtractProductIdFromUrl(string url)
    {
        // Extract product ID from URL formats like https://kaspi.kz/shop/p/product-name-123456789
        var parts = url.Split('/');
        if (parts.Length > 0)
        {
            var lastPart = parts[parts.Length - 1];
            // Extract numeric part from the end
            var numericPart = new String(lastPart.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(numericPart))
            {
                return numericPart;
            }
        }
        return string.Empty;
    }

    #region Helper Methods
    private string GetFirstImageUrl(JsonElement element)
    {
        if (element.TryGetProperty("images", out var imagesElement) &&
            imagesElement.ValueKind == JsonValueKind.Array &&
            imagesElement.GetArrayLength() > 0)
        {
            var firstImage = imagesElement[0];
            return firstImage.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private string GetJsonPropertyString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString() ?? string.Empty;
        }
        return string.Empty;
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

    private double GetJsonPropertyDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.TryGetDouble(out double value))
        {
            return value;
        }
        return 0.0;
    }
    #endregion
}