using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Services;

/// <summary>
/// Client for accessing Kaspi's navigation APIs to get category hierarchy data
/// Based on discovered API endpoints: main-navigation/desktop-topbar, desktop-menu, desktop-footer
/// </summary>
public class KaspiNavigationApiClient
{
    private readonly KaspiHttpClient _httpClient;
    private readonly ILogger<KaspiNavigationApiClient> _logger;
    private readonly Random _random = new();

    // Discovered API endpoints
    private const string BASE_URL = "https://kaspi.kz/yml/main-navigation/n/n";
    private const string DESKTOP_TOPBAR_API = BASE_URL + "/desktop-topbar";
    private const string DESKTOP_MENU_API = BASE_URL + "/desktop-menu";
    private const string DESKTOP_FOOTER_API = BASE_URL + "/desktop-footer";

    // Common parameters discovered
    private const string DEFAULT_CITY = "750000000"; // Almaty city code
    private const string ROOT_TYPE = "desktop";

    public KaspiNavigationApiClient(KaspiHttpClient httpClient, ILogger<KaspiNavigationApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches the complete category hierarchy from Kaspi's navigation APIs
    /// </summary>
    /// <param name="cityId">City ID (defaults to Almaty: 750000000)</param>
    /// <param name="depth">Navigation depth (1-3)</param>
    /// <returns>Hierarchical category structure</returns>
    public async Task<List<HierarchicalCategoryInfo>> GetCategoryHierarchyAsync(
        string cityId = DEFAULT_CITY,
        int depth = 3)
    {
        var allCategories = new List<HierarchicalCategoryInfo>();

        try
        {
            _logger.LogInformation("Fetching category hierarchy from Kaspi navigation APIs");

            // Create anti-detection strategy for navigation APIs
            var strategy = CreateNavigationApiStrategy();

            // Fetch from multiple navigation endpoints for comprehensive data
            var topbarCategories = await FetchFromTopbarApiAsync(cityId, depth, strategy);
            var menuCategories = await FetchFromMenuApiAsync(cityId, depth, strategy);
            var footerCategories = await FetchFromFooterApiAsync(cityId, depth, strategy);

            // Merge and deduplicate categories
            var flatCategories = MergeAndDeduplicateCategories(topbarCategories, menuCategories, footerCategories);
            
            // Build hierarchical relationships
            allCategories = BuildHierarchy(flatCategories);

            _logger.LogInformation("Successfully fetched {CategoryCount} categories from navigation APIs", allCategories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category hierarchy from navigation APIs");
            throw;
        }

        return allCategories;
    }

    /// <summary>
    /// Fetches categories from the desktop topbar API (primary navigation)
    /// </summary>
    private async Task<List<HierarchicalCategoryInfo>> FetchFromTopbarApiAsync(
        string cityId,
        int depth,
        AntiDetectionStrategy strategy)
    {
        var url = BuildNavigationApiUrl(DESKTOP_TOPBAR_API, cityId, depth);
        _logger.LogInformation("Fetching from topbar API: {Url}", url);

        var customHeaders = new Dictionary<string, string>
        {
            ["Referer"] = "https://kaspi.kz/"
        };

        // Add slight delay between API calls
        await Task.Delay(_random.Next(500, 1500));

        var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

        _logger.LogInformation("Topbar API call - Success: {Success}, Content length: {Length}",
            success, content?.Length ?? 0);

        if (!success || string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Failed to fetch data from topbar API - Success: {Success}, HasContent: {HasContent}",
                success, !string.IsNullOrEmpty(content));
            return new List<HierarchicalCategoryInfo>();
        }

        // Log first 200 characters of response for debugging
        _logger.LogDebug("Topbar API response preview: {Preview}",
            content.Length > 200 ? content.Substring(0, 200) + "..." : content);

        return ParseNavigationApiResponse(content, "topbar");
    }

    /// <summary>
    /// Fetches categories from the desktop menu API (main navigation)
    /// </summary>
    private async Task<List<HierarchicalCategoryInfo>> FetchFromMenuApiAsync(
        string cityId,
        int depth,
        AntiDetectionStrategy strategy)
    {
        var url = BuildNavigationApiUrl(DESKTOP_MENU_API, cityId, depth, code: "");
        _logger.LogInformation("Fetching from menu API: {Url}", url);

        var customHeaders = new Dictionary<string, string>
        {
            ["Referer"] = "https://kaspi.kz/"
        };

        // Add slight delay between API calls
        await Task.Delay(_random.Next(500, 1500));

        var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

        _logger.LogInformation("Menu API call - Success: {Success}, Content length: {Length}",
            success, content?.Length ?? 0);

        if (!success || string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Failed to fetch data from menu API - Success: {Success}, HasContent: {HasContent}",
                success, !string.IsNullOrEmpty(content));
            return new List<HierarchicalCategoryInfo>();
        }

        // Log first 200 characters of response for debugging
        _logger.LogDebug("Menu API response preview: {Preview}",
            content.Length > 200 ? content.Substring(0, 200) + "..." : content);

        return ParseNavigationApiResponse(content, "menu");
    }

    /// <summary>
    /// Fetches categories from the desktop footer API (supplementary data)
    /// </summary>
    private async Task<List<HierarchicalCategoryInfo>> FetchFromFooterApiAsync(
        string cityId,
        int depth,
        AntiDetectionStrategy strategy)
    {
        var url = BuildNavigationApiUrl(DESKTOP_FOOTER_API, cityId, Math.Min(depth, 2)); // Footer typically has max depth 2
        _logger.LogInformation("Fetching from footer API: {Url}", url);

        var customHeaders = new Dictionary<string, string>
        {
            ["Referer"] = "https://kaspi.kz/"
        };

        // Add slight delay between API calls
        await Task.Delay(_random.Next(500, 1500));

        var (success, content) = await _httpClient.SendRequestAsync(url, strategy, cityId, true, customHeaders);

        _logger.LogInformation("Footer API call - Success: {Success}, Content length: {Length}",
            success, content?.Length ?? 0);

        if (!success || string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Failed to fetch data from footer API - Success: {Success}, HasContent: {HasContent}",
                success, !string.IsNullOrEmpty(content));
            return new List<HierarchicalCategoryInfo>();
        }

        // Log first 200 characters of response for debugging
        _logger.LogDebug("Footer API response preview: {Preview}",
            content.Length > 200 ? content.Substring(0, 200) + "..." : content);

        return ParseNavigationApiResponse(content, "footer");
    }

    /// <summary>
    /// Builds the navigation API URL with proper parameters
    /// </summary>
    private string BuildNavigationApiUrl(string baseUrl, string cityId, int depth, string? code = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["depth"] = depth.ToString(),
            ["city"] = cityId,
            ["rootType"] = ROOT_TYPE
        };

        if (!string.IsNullOrEmpty(code))
        {
            parameters["code"] = code;
        }

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{baseUrl}?{queryString}";
    }

    /// <summary>
    /// Parses response from navigation API (supports both JSON and XML/YML formats)
    /// </summary>
    private List<HierarchicalCategoryInfo> ParseNavigationApiResponse(string content, string source)
    {
        var categories = new List<HierarchicalCategoryInfo>();

        try
        {
            _logger.LogDebug("Parsing {Source} API response, content starts with: {Start}",
                source, content.Length > 50 ? content.Substring(0, 50) : content);

            // First try to parse as JSON
            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                _logger.LogDebug("Attempting to parse as JSON for {Source}", source);
                categories = ParseJsonNavigationResponse(content, source);
            }
            else if (content.TrimStart().StartsWith("<"))
            {
                _logger.LogDebug("Attempting to parse as XML for {Source}", source);
                // Parse as XML/YML if it's not JSON
                categories = ParseXmlNavigationResponse(content, source);
            }
            else
            {
                _logger.LogWarning("Unknown response format from {Source} API - Content: {Preview}",
                    source, content.Length > 100 ? content.Substring(0, 100) + "..." : content);
            }

            _logger.LogInformation("Parsed {CategoryCount} categories from {Source} API", categories.Count, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing navigation API response from {Source} - Content preview: {Preview}",
                source, content.Length > 100 ? content.Substring(0, 100) + "..." : content);
        }

        return categories;
    }

    /// <summary>
    /// Parses JSON response from navigation API
    /// </summary>
    private List<HierarchicalCategoryInfo> ParseJsonNavigationResponse(string jsonContent, string source)
    {
        var categories = new List<HierarchicalCategoryInfo>();

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        // Debug: Log the structure of the root element
        _logger.LogDebug("Root JSON structure for {Source}: Kind={Kind}, PropertyCount={PropertyCount}",
            source, root.ValueKind, root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().Count() : 0);

        if (root.ValueKind == JsonValueKind.Object)
        {
            _logger.LogDebug("Root object properties for {Source}: {Properties}",
                source, string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
        }

        // Look for various possible JSON structures
        if (root.ValueKind == JsonValueKind.Array)
        {
            categories.AddRange(ParseJsonCategoryArray(root, 0, null));
        }
        else if (root.TryGetProperty("categories", out var categoriesElement))
        {
            categories.AddRange(ParseJsonCategoryArray(categoriesElement, 0, null));
        }
        else if (root.TryGetProperty("navigation", out var navigationElement))
        {
            categories.AddRange(ParseJsonCategoryArray(navigationElement, 0, null));
        }
        else if (root.TryGetProperty("subNodes", out var subNodesElement))
        {
            // Kaspi uses "subNodes" for subcategories
            _logger.LogDebug("Found subNodes property for {Source}, parsing array", source);
            categories.AddRange(ParseJsonCategoryArray(subNodesElement, 0, null));
        }
        else
        {
            // Try to find any array properties that might contain categories
            foreach (var property in root.EnumerateObject())
            {
                _logger.LogDebug("Checking property {PropertyName} of type {PropertyType} for {Source}",
                    property.Name, property.Value.ValueKind, source);

                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    var parsed = ParseJsonCategoryArray(property.Value, 0, null);
                    if (parsed.Any())
                    {
                        categories.AddRange(parsed);
                        break;
                    }
                }
            }
        }

        return categories;
    }

    /// <summary>
    /// Recursively parses JSON category array
    /// </summary>
    private List<HierarchicalCategoryInfo> ParseJsonCategoryArray(
        JsonElement arrayElement,
        int level,
        string? parentSlug)
    {
        var categories = new List<HierarchicalCategoryInfo>();

        if (arrayElement.ValueKind != JsonValueKind.Array)
            return categories;

        foreach (var categoryElement in arrayElement.EnumerateArray())
        {
            if (categoryElement.ValueKind != JsonValueKind.Object)
                continue;

            var name = GetJsonString(categoryElement, "title", "name", "text", "label");
            var url = GetJsonString(categoryElement, "link", "url", "href", "path");

            // Debug: Log category properties for first few categories
            if (categories.Count < 3)
            {
                var props = string.Join(", ", categoryElement.EnumerateObject().Select(p => p.Name));
                _logger.LogDebug("Category {Index} properties: {Properties}", categories.Count, props);
                _logger.LogDebug("Category {Index} values: title='{Title}', link='{Link}'",
                    categories.Count,
                    GetJsonString(categoryElement, "title"),
                    GetJsonString(categoryElement, "link"));
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
                continue;

            // Skip non-category links
            if (!url.Contains("/shop/c/") && !url.Contains("/c/"))
                continue;

            var slug = ExtractSlugFromUrl(url);
            if (string.IsNullOrEmpty(slug))
                continue;

            // Convert relative URLs to absolute URLs
            var absoluteUrl = ConvertToAbsoluteUrl(url);

            var category = new HierarchicalCategoryInfo(name, slug, absoluteUrl, level, parentSlug);
            categories.Add(category);

            // Look for subcategories (Kaspi uses "subNodes")
            if (categoryElement.TryGetProperty("subNodes", out var subNodesElement))
            {
                var subcategories = ParseJsonCategoryArray(subNodesElement, level + 1, slug);
                categories.AddRange(subcategories);
            }
            // Fallback to other common property names
            else if (categoryElement.TryGetProperty("children", out var childrenElement) ||
                categoryElement.TryGetProperty("subcategories", out childrenElement) ||
                categoryElement.TryGetProperty("items", out childrenElement))
            {
                var subcategories = ParseJsonCategoryArray(childrenElement, level + 1, slug);
                categories.AddRange(subcategories);
            }
        }

        return categories;
    }

    /// <summary>
    /// Parses XML/YML response from navigation API
    /// </summary>
    private List<HierarchicalCategoryInfo> ParseXmlNavigationResponse(string xmlContent, string source)
    {
        var categories = new List<HierarchicalCategoryInfo>();

        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xmlContent);

            // Look for category nodes in XML
            var categoryNodes = doc.SelectNodes("//category | //item | //link");

            if (categoryNodes != null)
            {
                foreach (System.Xml.XmlNode node in categoryNodes)
                {
                    var name = node.SelectSingleNode("name | title | text")?.InnerText?.Trim();
                    var url = node.SelectSingleNode("url | href | link")?.InnerText?.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
                        continue;

                    // Skip non-category links
                    if (!url.Contains("/shop/c/") && !url.Contains("/c/"))
                        continue;

                    var slug = ExtractSlugFromUrl(url);
                    if (string.IsNullOrEmpty(slug))
                        continue;

                    // Convert relative URLs to absolute URLs
                    var absoluteUrl = ConvertToAbsoluteUrl(url);

                    var category = new HierarchicalCategoryInfo(name, slug, absoluteUrl, 0, null);
                    categories.Add(category);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing XML navigation response from {Source}", source);
        }

        return categories;
    }

    /// <summary>
    /// Helper to get string value from JSON element with multiple possible property names
    /// </summary>
    private string GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Extracts category slug from URL
    /// </summary>
    private string ExtractSlugFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri("https://kaspi.kz" + (url.StartsWith("/") ? "" : "/") + url);
            }

            var segments = uri.Segments;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].TrimEnd('/') == "c" && i + 1 < segments.Length)
                {
                    return segments[i + 1].TrimEnd('/');
                }
            }
        }
        catch (UriFormatException)
        {
            // Invalid URL format
        }

        return string.Empty;
    }

    /// <summary>
    /// Converts relative URLs to absolute URLs using the Kaspi base URL
    /// </summary>
    private string ConvertToAbsoluteUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        // If already absolute URL, return as-is
        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;

        // Convert relative URLs to absolute using Kaspi base URL
        const string kaspiBaseUrl = "https://kaspi.kz";
        
        if (url.StartsWith("/"))
        {
            return kaspiBaseUrl + url;
        }
        
        return kaspiBaseUrl + "/" + url;
    }

    /// <summary>
    /// Merges and deduplicates categories from multiple API sources
    /// </summary>
    private List<HierarchicalCategoryInfo> MergeAndDeduplicateCategories(
        params List<HierarchicalCategoryInfo>[] categorySets)
    {
        var categoryMap = new Dictionary<string, HierarchicalCategoryInfo>();

        foreach (var categorySet in categorySets)
        {
            foreach (var category in categorySet)
            {
                if (!categoryMap.ContainsKey(category.Slug))
                {
                    categoryMap[category.Slug] = category;
                }
                else
                {
                    // Merge additional data if needed
                    var existing = categoryMap[category.Slug];
                    if (existing.Level > category.Level)
                    {
                        // Keep the category with the lower level (higher in hierarchy)
                        categoryMap[category.Slug] = category;
                    }
                }
            }
        }

        return categoryMap.Values.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
    }

    /// <summary>
    /// Creates an anti-detection strategy optimized for navigation APIs
    /// </summary>
    private AntiDetectionStrategy CreateNavigationApiStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            SimulateBrowserFingerprint = true,
            AddReferrerPath = true,
            AddNavigationTiming = true,
            AddSessionCookies = true,
            CacheBusting = false, // Navigation data doesn't change often
            SimulateHumanTiming = true,
            BaseDelayMs = 300,
            JitterPercentage = 25
        };
    }

    /// <summary>
    /// Builds hierarchical relationships from flat category list
    /// </summary>
    private List<HierarchicalCategoryInfo> BuildHierarchy(List<HierarchicalCategoryInfo> flatCategories)
    {
        var categoryMap = new Dictionary<string, HierarchicalCategoryInfo>();
        var rootCategories = new List<HierarchicalCategoryInfo>();

        // First pass: create a map of all categories
        foreach (var category in flatCategories)
        {
            categoryMap[category.Slug] = category;
        }

        // Second pass: build parent-child relationships
        foreach (var category in flatCategories)
        {
            if (string.IsNullOrEmpty(category.ParentSlug))
            {
                // This is a root category
                rootCategories.Add(category);
            }
            else if (categoryMap.TryGetValue(category.ParentSlug, out var parent))
            {
                // This is a child category - add it to parent's subcategories
                var parentWithSubcategories = parent with
                {
                    Subcategories = parent.Subcategories.Concat(new[] { category }).ToList()
                };
                categoryMap[parent.Slug] = parentWithSubcategories;

                // Update in root categories if it's a root
                var rootIndex = rootCategories.FindIndex(c => c.Slug == parent.Slug);
                if (rootIndex >= 0)
                {
                    rootCategories[rootIndex] = parentWithSubcategories;
                }
            }
        }

        // Update the categoryMap references in case of multiple levels
        return BuildNestedHierarchy(categoryMap);
    }

    /// <summary>
    /// Recursively builds nested hierarchy ensuring all levels are properly connected
    /// </summary>
    private List<HierarchicalCategoryInfo> BuildNestedHierarchy(Dictionary<string, HierarchicalCategoryInfo> categoryMap)
    {
        var processedCategories = new HashSet<string>();
        var rootCategories = new List<HierarchicalCategoryInfo>();

        foreach (var kvp in categoryMap)
        {
            var category = kvp.Value;
            if (string.IsNullOrEmpty(category.ParentSlug))
            {
                var hierarchicalCategory = BuildCategoryWithChildren(category, categoryMap, processedCategories);
                rootCategories.Add(hierarchicalCategory);
            }
        }

        return rootCategories;
    }

    /// <summary>
    /// Recursively builds a category with all its children
    /// </summary>
    private HierarchicalCategoryInfo BuildCategoryWithChildren(
        HierarchicalCategoryInfo category,
        Dictionary<string, HierarchicalCategoryInfo> categoryMap,
        HashSet<string> processed)
    {
        if (processed.Contains(category.Slug))
            return category;

        processed.Add(category.Slug);

        // Find all direct children
        var children = categoryMap.Values
            .Where(c => c.ParentSlug == category.Slug)
            .Select(child => BuildCategoryWithChildren(child, categoryMap, processed))
            .ToList();

        return category with { Subcategories = children };
    }

}
