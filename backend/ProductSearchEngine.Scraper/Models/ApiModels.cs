namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Represents filter information for a category or search query
/// </summary>
public class CategoryFilterInfo
{
    /// <summary>
    /// Category slug if this is a category filter
    /// </summary>
    public string? CategorySlug { get; set; }

    /// <summary>
    /// Search text if this is a search filter
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Available filter options (manufacturers, price ranges, etc.)
    /// </summary>
    public List<FilterOption> Filters { get; set; } = new();

    /// <summary>
    /// Available categories for cross-category search
    /// </summary>
    public List<string> AvailableCategories { get; set; } = new();

    /// <summary>
    /// Total number of products matching the base filter
    /// </summary>
    public int TotalProductCount { get; set; }

    /// <summary>
    /// Timestamp when this filter info was retrieved
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a filter option (e.g., manufacturer, price range)
/// </summary>
public class FilterOption
{
    /// <summary>
    /// Internal name used in API calls (e.g., "manufacturerName")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "Manufacturer")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Type of filter (e.g., "multi-select", "range", "single-select")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Available values for this filter
    /// </summary>
    public List<FilterValue> Options { get; set; } = new();
}

/// <summary>
/// Represents a specific filter value
/// </summary>
public class FilterValue
{
    /// <summary>
    /// Internal value used in API calls
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of products matching this filter value
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Whether this filter is currently selected
    /// </summary>
    public bool IsSelected { get; set; }
}

/// <summary>
/// Represents search results from the product results API
/// </summary>
public class ProductSearchResults
{
    /// <summary>
    /// The filter query used to get these results
    /// </summary>
    public string FilterQuery { get; set; } = string.Empty;

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Total number of pages available
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total number of products matching the filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Products on this page
    /// </summary>
    public List<ProductSummary> Products { get; set; } = new();

    /// <summary>
    /// Search query text if applicable
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Timestamp when results were retrieved
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a product summary from search results
/// </summary>
public class ProductSummary
{
    /// <summary>
    /// Product ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum price or current price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Main product image URL
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Product page URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Average rating
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Product category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether the product is in stock
    /// </summary>
    public bool InStock { get; set; } = true;

    /// <summary>
    /// Product brand/manufacturer
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Short description or key features
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
