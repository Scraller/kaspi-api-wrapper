using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Services;

/// <summary>
/// Client for accessing Kaspi's product filtering APIs
/// Based on discovered API: yml/product-view/pl/filters
/// </summary>
public class KaspiProductFilterApiClient
{
    private readonly KaspiHttpClient _httpClient;
    private readonly ILogger<KaspiProductFilterApiClient> _logger;
    private readonly Random _random = new();

    // Discovered API endpoints
    private const string FILTER_API_URL = "https://kaspi.kz/yml/product-view/pl/filters";
    private const string RESULTS_API_URL = "https://kaspi.kz/yml/product-view/pl/results";

    // Common parameters
    private const string DEFAULT_CITY = "750000000"; // Almaty
    private const string DEFAULT_UI = "d"; // Desktop
    private const string DEFAULT_ZONE = "Magnum_ZONE1";

    public KaspiProductFilterApiClient(KaspiHttpClient httpClient, ILogger<KaspiProductFilterApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets available filters for a category using the discovered filter API
    /// </summary>
    /// <param name="categorySlug">Category slug (e.g., "Smartphones")</param>
    /// <param name="manufacturerName">Optional manufacturer filter</param>
    /// <param name="searchText">Optional search text</param>
    /// <param name="cityId">City ID (default: Almaty)</param>
    /// <returns>Available filters and their options</returns>
    public async Task<CategoryFilterInfo> GetCategoryFiltersAsync(
        string categorySlug,
        string? manufacturerName = null,
        string? searchText = null,
        string cityId = DEFAULT_CITY)
    {
        try
        {
            _logger.LogInformation("Fetching filters for category: {Category}", categorySlug);

            var strategy = CreateFilterApiStrategy();
            var filterQuery = BuildFilterQuery(categorySlug, manufacturerName);
            var url = BuildFilterApiUrl(filterQuery, searchText, cityId);

            var customHeaders = new Dictionary<string, string>
            {
                ["Referer"] = $"https://kaspi.kz/shop/c/{Uri.EscapeDataString(categorySlug)}/"
            };

            var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

            if (!success || string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Failed to fetch filters for category {Category}", categorySlug);
                return new CategoryFilterInfo { CategorySlug = categorySlug };
            }

            return ParseFilterResponse(content, categorySlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching filters for category {Category}", categorySlug);
            return new CategoryFilterInfo { CategorySlug = categorySlug };
        }
    }

    /// <summary>
    /// Searches for products using text query and gets available filters
    /// </summary>
    /// <param name="searchText">Search query text</param>
    /// <param name="categorySlug">Optional category filter</param>
    /// <param name="cityId">City ID</param>
    /// <returns>Search filters and metadata</returns>
    public async Task<CategoryFilterInfo> GetSearchFiltersAsync(
        string searchText,
        string? categorySlug = null,
        string cityId = DEFAULT_CITY)
    {
        try
        {
            _logger.LogInformation("Fetching search filters for query: {Query}", searchText);

            var strategy = CreateFilterApiStrategy();
            var filterQuery = BuildSearchFilterQuery(categorySlug);
            var url = BuildFilterApiUrl(filterQuery, searchText, cityId, page: 0, searchMode: true);

            var customHeaders = new Dictionary<string, string>
            {
                ["Referer"] = $"https://kaspi.kz/shop/search/?text={Uri.EscapeDataString(searchText)}"
            };

            var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

            if (!success || string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Failed to fetch search filters for query {Query}", searchText);
                return new CategoryFilterInfo { SearchText = searchText };
            }

            return ParseFilterResponse(content, categorySlug, searchText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching search filters for query {Query}", searchText);
            return new CategoryFilterInfo { SearchText = searchText };
        }
    }

    /// <summary>
    /// Gets product results with applied filters using the discovered results API
    /// </summary>
    /// <param name="filterQuery">Filter query string</param>
    /// <param name="searchText">Search text</param>
    /// <param name="sortOrder">Sort order</param>
    /// <param name="page">Page number</param>
    /// <param name="cityId">City ID</param>
    /// <returns>Product results</returns>
    public virtual async Task<ProductSearchResults> GetProductResultsAsync(
        string filterQuery,
        string? searchText = null,
        string sortOrder = "relevance",
        int page = 0,
        string cityId = DEFAULT_CITY)
    {
        try
        {
            _logger.LogInformation("Fetching product results for filter: {Filter}, page: {Page}", filterQuery, page);

            var strategy = CreateResultsApiStrategy();
            var url = BuildResultsApiUrl(filterQuery, searchText, sortOrder, page, cityId);

            var customHeaders = new Dictionary<string, string>
            {
                ["Referer"] = string.IsNullOrEmpty(searchText)
                    ? "https://kaspi.kz/shop/"
                    : $"https://kaspi.kz/shop/search/?text={Uri.EscapeDataString(searchText)}"
            };

            var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

            if (!success || string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Failed to fetch product results for filter {Filter}", filterQuery);
                return new ProductSearchResults { FilterQuery = filterQuery, Page = page };
            }

            return ParseResultsResponse(content, filterQuery, page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product results for filter {Filter}", filterQuery);
            return new ProductSearchResults { FilterQuery = filterQuery, Page = page };
        }
    }

    /// <summary>
    /// Builds the filter query string in the discovered colon-separated format
    /// </summary>
    private string BuildFilterQuery(string categorySlug, string? manufacturerName = null)
    {
        var filters = new List<string>
        {
            $"category:{categorySlug}",
            $"availableInZones:{DEFAULT_ZONE}"
        };

        if (!string.IsNullOrEmpty(manufacturerName))
        {
            filters.Add($"manufacturerName:{manufacturerName}");
        }

        return ":" + string.Join(":", filters);
    }

    /// <summary>
    /// Builds filter query for search mode
    /// </summary>
    private string BuildSearchFilterQuery(string? categorySlug = null)
    {
        var filters = new List<string>
        {
            $"availableInZones:{DEFAULT_ZONE}"
        };

        if (!string.IsNullOrEmpty(categorySlug))
        {
            filters.Add($"category:{categorySlug}");
        }

        return ":" + string.Join(":", filters);
    }

    /// <summary>
    /// Builds the filter API URL with discovered parameters
    /// </summary>
    private string BuildFilterApiUrl(
        string filterQuery,
        string? searchText = null,
        string cityId = DEFAULT_CITY,
        int page = 0,
        bool searchMode = false)
    {
        var parameters = new Dictionary<string, string>
        {
            ["q"] = filterQuery,
            ["text"] = searchText ?? "",
            ["all"] = "false",
            ["sort"] = "relevance",
            ["ui"] = DEFAULT_UI,
            ["i"] = "-1",
            ["c"] = cityId
        };

        if (searchMode)
        {
            parameters["page"] = page.ToString();
            parameters["fl"] = "true";
        }

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{FILTER_API_URL}?{queryString}";
    }

    /// <summary>
    /// Builds the results API URL with discovered parameters
    /// </summary>
    private string BuildResultsApiUrl(
        string filterQuery,
        string? searchText,
        string sortOrder,
        int page,
        string cityId)
    {
        var parameters = new Dictionary<string, string>
        {
            ["page"] = page.ToString(),
            ["q"] = filterQuery,
            ["text"] = searchText ?? "",
            ["sort"] = sortOrder,
            ["qs"] = "",
            ["requestId"] = GenerateRequestId(),
            ["ui"] = DEFAULT_UI,
            ["i"] = "-1",
            ["c"] = cityId
        };

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{RESULTS_API_URL}?{queryString}";
    }

    /// <summary>
    /// Generates a unique request ID for session consistency
    /// </summary>
    private string GenerateRequestId()
    {
        var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var result = new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
        return result;
    }

    /// <summary>
    /// Parses the filter API response
    /// </summary>
    private CategoryFilterInfo ParseFilterResponse(string jsonContent, string? categorySlug = null, string? searchText = null)
    {
        var filterInfo = new CategoryFilterInfo
        {
            CategorySlug = categorySlug,
            SearchText = searchText,
            Filters = new List<FilterOption>()
        };

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Parse manufacturers
            if (root.TryGetProperty("manufacturers", out var manufacturersElement))
            {
                filterInfo.Filters.Add(ParseManufacturerFilter(manufacturersElement));
            }

            // Parse other filters (price, rating, etc.)
            if (root.TryGetProperty("filters", out var filtersElement))
            {
                filterInfo.Filters.AddRange(ParseGenericFilters(filtersElement));
            }

            // Parse available categories
            if (root.TryGetProperty("categories", out var categoriesElement))
            {
                filterInfo.AvailableCategories = ParseCategoryOptions(categoriesElement);
            }

            // Parse total count
            if (root.TryGetProperty("totalCount", out var totalCountElement))
            {
                filterInfo.TotalProductCount = totalCountElement.GetInt32();
            }

            _logger.LogInformation("Parsed {FilterCount} filter options for category {Category}",
                filterInfo.Filters.Count, categorySlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing filter response for category {Category}", categorySlug);
        }

        return filterInfo;
    }

    /// <summary>
    /// Parses manufacturer filter from response
    /// </summary>
    private FilterOption ParseManufacturerFilter(JsonElement manufacturersElement)
    {
        var manufacturerFilter = new FilterOption
        {
            Name = "manufacturerName",
            DisplayName = "Manufacturer",
            Type = "multi-select",
            Options = new List<FilterValue>()
        };

        if (manufacturersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var manufacturer in manufacturersElement.EnumerateArray())
            {
                if (manufacturer.TryGetProperty("name", out var nameElement) &&
                    manufacturer.TryGetProperty("count", out var countElement))
                {
                    manufacturerFilter.Options.Add(new FilterValue
                    {
                        Value = nameElement.GetString() ?? "",
                        DisplayName = nameElement.GetString() ?? "",
                        Count = countElement.GetInt32()
                    });
                }
            }
        }

        return manufacturerFilter;
    }

    /// <summary>
    /// Parses generic filters from response
    /// </summary>
    private List<FilterOption> ParseGenericFilters(JsonElement filtersElement)
    {
        var filters = new List<FilterOption>();

        if (filtersElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in filtersElement.EnumerateObject())
            {
                var filter = new FilterOption
                {
                    Name = property.Name,
                    DisplayName = property.Name,
                    Type = "multi-select",
                    Options = new List<FilterValue>()
                };

                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in property.Value.EnumerateArray())
                    {
                        if (option.TryGetProperty("value", out var valueElement) &&
                            option.TryGetProperty("count", out var countElement))
                        {
                            filter.Options.Add(new FilterValue
                            {
                                Value = valueElement.GetString() ?? "",
                                DisplayName = valueElement.GetString() ?? "",
                                Count = countElement.GetInt32()
                            });
                        }
                    }
                }

                if (filter.Options.Any())
                {
                    filters.Add(filter);
                }
            }
        }

        return filters;
    }

    /// <summary>
    /// Parses category options from response
    /// </summary>
    private List<string> ParseCategoryOptions(JsonElement categoriesElement)
    {
        var categories = new List<string>();

        if (categoriesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var category in categoriesElement.EnumerateArray())
            {
                if (category.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        categories.Add(name);
                    }
                }
            }
        }

        return categories;
    }

    /// <summary>
    /// Parses the results API response
    /// </summary>
    private ProductSearchResults ParseResultsResponse(string jsonContent, string filterQuery, int page)
    {
        var results = new ProductSearchResults
        {
            FilterQuery = filterQuery,
            Page = page,
            Products = new List<ProductSummary>()
        };

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Parse products array - Kaspi API uses "data" field
            if (root.TryGetProperty("data", out var productsElement) &&
                productsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var productElement in productsElement.EnumerateArray())
                {
                    var product = ParseProductSummary(productElement);
                    if (product != null)
                    {
                        results.Products.Add(product);
                    }
                }
            }

            // Parse pagination info
            if (root.TryGetProperty("totalPages", out var totalPagesElement))
            {
                results.TotalPages = totalPagesElement.GetInt32();
            }

            if (root.TryGetProperty("totalCount", out var totalCountElement))
            {
                results.TotalCount = totalCountElement.GetInt32();
            }

            _logger.LogInformation("Parsed {ProductCount} products from results API", results.Products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing results response");
        }

        return results;
    }

    /// <summary>
    /// Parses individual product from results
    /// </summary>
    private ProductSummary? ParseProductSummary(JsonElement productElement)
    {
        try
        {
            if (!productElement.TryGetProperty("id", out var idElement))
                return null;

            return new ProductSummary
            {
                Id = idElement.GetString() ?? "",
                Name = GetJsonString(productElement, "title", "name"),
                Brand = GetJsonString(productElement, "brand"),
                Price = GetJsonDecimal(productElement, "unitPrice", "price"),
                ImageUrl = GetProductImageUrl(productElement),
                Url = GetJsonString(productElement, "shopLink", "url"),
                Rating = GetJsonDouble(productElement, "rating"),
                ReviewCount = GetJsonInt(productElement, "reviewsQuantity", "reviewCount", "reviews"),
                Category = GetProductCategory(productElement),
                InStock = GetJsonBool(productElement, "stock", true) // Default to true if not specified
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing product summary");
            return null;
        }
    }

    private string GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? "";
            }
        }
        return "";
    }

    private decimal GetJsonDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDecimal();
            }
        }
        return 0;
    }

    private double GetJsonDouble(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDouble();
            }
        }
        return 0;
    }

    private int GetJsonInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
        }
        return 0;
    }

    private bool GetJsonBool(JsonElement element, string propertyName, bool defaultValue = false)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True) return true;
            if (property.ValueKind == JsonValueKind.False) return false;
            if (property.ValueKind == JsonValueKind.Number) return property.GetInt32() > 0;
        }
        return defaultValue;
    }

    private string GetProductImageUrl(JsonElement productElement)
    {
        // Look for previewImages array and get the first small image
        if (productElement.TryGetProperty("previewImages", out var imagesElement) &&
            imagesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in imagesElement.EnumerateArray())
            {
                if (image.TryGetProperty("small", out var smallImage) &&
                    smallImage.ValueKind == JsonValueKind.String)
                {
                    return smallImage.GetString() ?? "";
                }
            }
        }
        return "";
    }

    private string GetProductCategory(JsonElement productElement)
    {
        // Look for category array and join with " > " or use categoryRu
        if (productElement.TryGetProperty("categoryRu", out var categoryRuElement) &&
            categoryRuElement.ValueKind == JsonValueKind.Array)
        {
            var categories = new List<string>();
            foreach (var cat in categoryRuElement.EnumerateArray())
            {
                if (cat.ValueKind == JsonValueKind.String)
                {
                    var catString = cat.GetString();
                    if (!string.IsNullOrEmpty(catString))
                        categories.Add(catString);
                }
            }
            return string.Join(" > ", categories);
        }

        // Fallback to single category field
        return GetJsonString(productElement, "category");
    }

    /// <summary>
    /// Creates anti-detection strategy for filter API
    /// </summary>
    private AntiDetectionStrategy CreateFilterApiStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            SimulateBrowserFingerprint = true,
            AddReferrerPath = true,
            AddSessionCookies = true,
            SimulateHumanTiming = true,
            BaseDelayMs = 200,
            JitterPercentage = 25
        };
    }

    /// <summary>
    /// Creates anti-detection strategy for results API
    /// </summary>
    private AntiDetectionStrategy CreateResultsApiStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            SimulateBrowserFingerprint = true,
            AddReferrerPath = true,
            AddSessionCookies = true,
            SimulateHumanTiming = true,
            BaseDelayMs = 300,
            JitterPercentage = 30
        };
    }
}
