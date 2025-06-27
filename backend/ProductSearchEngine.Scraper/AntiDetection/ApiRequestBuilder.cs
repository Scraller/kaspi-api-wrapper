using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProductSearchEngine.Scraper.AntiDetection;

public class ApiRequestBuilder
{
    private readonly Random _random = new();
    private readonly string _baseUrl = "https://kaspi.kz/yml/product-view/pl/results";

    private readonly string[] _sortOptions = { "relevance", "price-asc", "price-desc", "rating" };

    public string BuildApiUrl(string category, string cityId, AntiDetectionStrategy strategy, int page = 1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["q"] = $":category:{category}:availableInZones:{cityId}",
            ["c"] = cityId,
            ["sort"] = strategy.SortOption ?? _sortOptions[_random.Next(_sortOptions.Length)],
            ["page"] = page.ToString()
        };

        // Add request ID for session consistency
        parameters["requestId"] = GenerateRequestId();

        // Vary query parameters if strategy requires it
        if (strategy.VaryQueryParams)
        {
            AddVariedQueryParams(parameters);
        }

        // Add cache busting if strategy requires it
        if (strategy.CacheBusting)
        {
            AddCacheBustingParams(parameters);
        }

        // Randomize parameter order
        if (strategy.RandomizeParamOrder)
        {
            parameters = parameters.OrderBy(x => _random.Next()).ToDictionary(x => x.Key, x => x.Value);
        }

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{_baseUrl}?{queryString}";
    }

    public string MapCategoryToApiFormat(string category)
    {
        // Map user-friendly category names to Kaspi API format
        return category.ToLowerInvariant().Trim() switch
        {
            "smartphones" or "phone" or "phones" => "Smartphones",
            "notebooks" or "laptop" or "laptops" => "Notebooks",
            "televisions" or "tv" or "tvs" => "Televisions",
            "tablets" or "tablet" => "Tablets",
            "smartwatches" or "smartwatch" or "watches" => "Smart watches",
            "headphones" or "earphones" => "Headphones",
            "gaming" or "consoles" => "Gaming consoles",
            "cameras" or "camera" => "Cameras",
            "accessories" => "Mobile accessories",
            _ => category
        };
    }

    private string GenerateRequestId()
    {
        // Generate a session-consistent request ID
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = _random.Next(1000, 9999);
        return $"req-{timestamp}-{random}";
    }

    private void AddVariedQueryParams(Dictionary<string, string> parameters)
    {
        // Randomize results limit (20% chance)
        if (_random.NextDouble() < 0.2)
        {
            var limits = new[] { 20, 40, 60, 80, 100 };
            parameters["limit"] = limits[_random.Next(limits.Length)].ToString();
        }

        // Add random filter parameters (15% chance)
        if (_random.NextDouble() < 0.15)
        {
            var filters = new[] { "HasPhoto", "InStock", "OnlyDiscounted", "HasReviews" };
            var filter = filters[_random.Next(filters.Length)];
            parameters[filter] = "true";
        }

        // Add ordering parameters (10% chance)
        if (_random.NextDouble() < 0.1)
        {
            var orderOptions = new[] { "price", "popular", "rating", "newest" };
            parameters["orderBy"] = orderOptions[_random.Next(orderOptions.Length)];
        }
    }

    private void AddCacheBustingParams(Dictionary<string, string> parameters)
    {
        // Add timestamp
        parameters["_ts"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Add random nonce
        parameters["_r"] = _random.Next(1000, 9999).ToString();

        // Add random view parameter
        var viewOptions = new[] { "default", "full", "compact", "standard", "grid", "list" };
        parameters["view"] = viewOptions[_random.Next(viewOptions.Length)];
    }
}
