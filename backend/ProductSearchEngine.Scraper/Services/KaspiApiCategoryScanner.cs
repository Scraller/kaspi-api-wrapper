using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Services;

namespace ProductSearchEngine.Scraper.Services;

/// <summary>
/// Enhanced category scanner that uses Kaspi's navigation APIs instead of HTML scraping
/// Based on the discovered API endpoints for better performance and reliability
/// </summary>
public class KaspiApiCategoryScanner
{
    private readonly KaspiNavigationApiClient _navigationClient;
    private readonly KaspiProductFilterApiClient _filterClient;
    private readonly CategoryValidationService _validationService;
    private readonly ILogger<KaspiApiCategoryScanner> _logger;

    public KaspiApiCategoryScanner(
        KaspiNavigationApiClient navigationClient,
        KaspiProductFilterApiClient filterClient,
        CategoryValidationService validationService,
        ILogger<KaspiApiCategoryScanner> logger)
    {
        _navigationClient = navigationClient ?? throw new ArgumentNullException(nameof(navigationClient));
        _filterClient = filterClient ?? throw new ArgumentNullException(nameof(filterClient));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the complete category hierarchy using the navigation APIs
    /// </summary>
    /// <param name="cityId">City ID (default: Almaty)</param>
    /// <param name="includeProductCounts">Whether to fetch product counts for each category</param>
    /// <returns>Complete category hierarchy with metadata</returns>
    public async Task<List<HierarchicalCategoryInfo>> GetCategoryHierarchyAsync(
        string cityId = "750000000",
        bool includeProductCounts = true)
    {
        try
        {
            _logger.LogInformation("Fetching category hierarchy using navigation APIs");

            // Get categories from navigation APIs
            var categories = await _navigationClient.GetCategoryHierarchyAsync(cityId, depth: 3);

            if (!categories.Any())
            {
                _logger.LogWarning("No categories found from navigation APIs");
                return new List<HierarchicalCategoryInfo>();
            }

            // Filter out invalid categories
            categories = FilterValidCategories(categories);

            // Optionally enhance with product counts
            if (includeProductCounts)
            {
                categories = await EnrichWithProductCountsAsync(categories, cityId);
            }

            _logger.LogInformation("Successfully retrieved {CategoryCount} categories with hierarchy", categories.Count);
            return categories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category hierarchy");
            throw;
        }
    }

    /// <summary>
    /// Gets subcategories for a specific category using both navigation and filter APIs
    /// </summary>
    /// <param name="categorySlug">Parent category slug</param>
    /// <param name="cityId">City ID</param>
    /// <returns>Subcategories of the specified category</returns>
    public async Task<List<HierarchicalCategoryInfo>> GetSubcategoriesAsync(
        string categorySlug,
        string cityId = "750000000")
    {
        try
        {
            _logger.LogInformation("Fetching subcategories for category: {Category}", categorySlug);

            // First, get all categories from navigation API
            var allCategories = await _navigationClient.GetCategoryHierarchyAsync(cityId, depth: 3);

            // Find the parent category
            var parentCategory = allCategories.FirstOrDefault(c =>
                c.Slug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));

            if (parentCategory == null)
            {
                _logger.LogWarning("Parent category {Category} not found", categorySlug);
                return new List<HierarchicalCategoryInfo>();
            }

            // Get direct subcategories
            var subcategories = allCategories.Where(c =>
                c.ParentSlug?.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) == true).ToList();

            // Also try to get additional subcategories from filter API
            var filterInfo = await _filterClient.GetCategoryFiltersAsync(categorySlug, cityId: cityId);

            if (filterInfo.AvailableCategories.Any())
            {
                // Create additional subcategories from filter API data
                var additionalSubcategories = CreateSubcategoriesFromFilterData(
                    filterInfo.AvailableCategories, parentCategory);

                // Merge with existing subcategories (avoid duplicates)
                subcategories = MergeSubcategories(subcategories, additionalSubcategories);
            }

            _logger.LogInformation("Found {SubcategoryCount} subcategories for {Category}",
                subcategories.Count, categorySlug);

            return subcategories.OrderBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategories for {Category}", categorySlug);
            return new List<HierarchicalCategoryInfo>();
        }
    }

    /// <summary>
    /// Searches for categories using text query and gets related filter information
    /// </summary>
    /// <param name="searchText">Search query</param>
    /// <param name="cityId">City ID</param>
    /// <returns>Categories and filters relevant to the search</returns>
    public async Task<CategorySearchResult> SearchCategoriesAsync(
        string searchText,
        string cityId = "750000000")
    {
        try
        {
            _logger.LogInformation("Searching categories for query: {Query}", searchText);

            var result = new CategorySearchResult
            {
                SearchText = searchText,
                Categories = new List<HierarchicalCategoryInfo>(),
                Filters = new CategoryFilterInfo()
            };

            // Get search filters which include available categories
            var filterInfo = await _filterClient.GetSearchFiltersAsync(searchText, cityId: cityId);
            result.Filters = filterInfo;

            // If we have available categories from search, get their full hierarchy info
            if (filterInfo.AvailableCategories.Any())
            {
                var allCategories = await _navigationClient.GetCategoryHierarchyAsync(cityId);

                result.Categories = allCategories.Where(c =>
                    filterInfo.AvailableCategories.Any(ac =>
                        ac.Equals(c.Name, StringComparison.OrdinalIgnoreCase) ||
                        ac.Equals(c.Slug, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            // Also search by text in category names
            if (!result.Categories.Any())
            {
                var allCategories = await _navigationClient.GetCategoryHierarchyAsync(cityId);
                result.Categories = allCategories.Where(c =>
                    c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Slug.Contains(searchText.Replace(" ", "%20"), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogInformation("Found {CategoryCount} categories for search query: {Query}",
                result.Categories.Count, searchText);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching categories for query: {Query}", searchText);
            return new CategorySearchResult { SearchText = searchText };
        }
    }

    /// <summary>
    /// Gets category with enhanced metadata including filters and product counts
    /// </summary>
    /// <param name="categorySlug">Category slug</param>
    /// <param name="cityId">City ID</param>
    /// <returns>Enhanced category information</returns>
    public async Task<EnhancedCategoryInfo?> GetCategoryWithMetadataAsync(
        string categorySlug,
        string cityId = "750000000")
    {
        try
        {
            _logger.LogInformation("Fetching enhanced metadata for category: {Category}", categorySlug);

            // Get category hierarchy
            var allCategories = await _navigationClient.GetCategoryHierarchyAsync(cityId);
            var category = allCategories.FirstOrDefault(c =>
                c.Slug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                _logger.LogWarning("Category {Category} not found", categorySlug);
                return null;
            }

            // Get filter information
            var filterInfo = await _filterClient.GetCategoryFiltersAsync(categorySlug, cityId: cityId);

            // Get subcategories
            var subcategories = allCategories.Where(c =>
                c.ParentSlug?.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) == true).ToList();

            return new EnhancedCategoryInfo
            {
                Category = category,
                Filters = filterInfo,
                Subcategories = subcategories,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced metadata for category: {Category}", categorySlug);
            return null;
        }
    }

    /// <summary>
    /// Filters categories using validation rules
    /// </summary>
    private List<HierarchicalCategoryInfo> FilterValidCategories(List<HierarchicalCategoryInfo> categories)
    {
        return categories.Where(category =>
            _validationService.IsValidCategoryLink(category.Url, category.Name)).ToList();
    }

    /// <summary>
    /// Enriches categories with product counts using filter API
    /// </summary>
    private async Task<List<HierarchicalCategoryInfo>> EnrichWithProductCountsAsync(
        List<HierarchicalCategoryInfo> categories,
        string cityId)
    {
        _logger.LogInformation("Enriching {CategoryCount} categories with product counts", categories.Count);

        var enrichedCategories = new List<HierarchicalCategoryInfo>();

        // Process categories in batches to avoid overwhelming the API
        const int batchSize = 5;
        var batches = categories.Chunk(batchSize).ToList();

        foreach (var batch in batches)
        {
            var tasks = batch.Select(async category =>
            {
                try
                {
                    var filterInfo = await _filterClient.GetCategoryFiltersAsync(category.Slug, cityId: cityId);

                    // Since we no longer track product counts, just keep the original category
                    var enrichedCategory = category;

                    return enrichedCategory;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get product count for category {Category}", category.Slug);
                    return category; // Return original if enrichment fails
                }
            });

            var batchResults = await Task.WhenAll(tasks);
            enrichedCategories.AddRange(batchResults);

            // Add delay between batches to be respectful to the API
            if (batches.IndexOf(batch) < batches.Count - 1)
            {
                await Task.Delay(1000);
            }
        }

        _logger.LogInformation("Successfully enriched categories with product counts");
        return enrichedCategories;
    }

    /// <summary>
    /// Creates subcategories from filter API data
    /// </summary>
    private List<HierarchicalCategoryInfo> CreateSubcategoriesFromFilterData(
        List<string> availableCategories,
        HierarchicalCategoryInfo parentCategory)
    {
        return availableCategories.Select(categoryName =>
        {
            var slug = categoryName.ToLowerInvariant().Replace(" ", "%20");
            var url = $"https://kaspi.kz/shop/c/{slug}/";

            return new HierarchicalCategoryInfo(
                categoryName,
                slug,
                url,
                parentCategory.Level + 1,
                parentCategory.Slug);
        }).ToList();
    }

    /// <summary>
    /// Merges subcategories from different sources, avoiding duplicates
    /// </summary>
    private List<HierarchicalCategoryInfo> MergeSubcategories(
        List<HierarchicalCategoryInfo> existing,
        List<HierarchicalCategoryInfo> additional)
    {
        var merged = new List<HierarchicalCategoryInfo>(existing);

        foreach (var additionalCategory in additional)
        {
            var existingCategory = existing.FirstOrDefault(e =>
                e.Slug.Equals(additionalCategory.Slug, StringComparison.OrdinalIgnoreCase) ||
                e.Name.Equals(additionalCategory.Name, StringComparison.OrdinalIgnoreCase));

            if (existingCategory == null)
            {
                merged.Add(additionalCategory);
            }
        }

        return merged;
    }
}

/// <summary>
/// Result of category search operation
/// </summary>
public class CategorySearchResult
{
    public string SearchText { get; set; } = string.Empty;
    public List<HierarchicalCategoryInfo> Categories { get; set; } = new();
    public CategoryFilterInfo Filters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enhanced category information with filters and subcategories
/// </summary>
public class EnhancedCategoryInfo
{
    public HierarchicalCategoryInfo Category { get; set; } = null!;
    public CategoryFilterInfo Filters { get; set; } = new();
    public List<HierarchicalCategoryInfo> Subcategories { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
