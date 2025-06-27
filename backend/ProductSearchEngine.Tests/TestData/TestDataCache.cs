using System.Text.Json;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Tests.TestData;

/// <summary>
/// Manages cached test data from real scraping operations for use in BDD tests
/// </summary>
public static class TestDataCache
{
    private static readonly string TestDataDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private static readonly string ProductCacheFile = Path.Combine(
        TestDataDirectory, "scraped-products.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    static TestDataCache()
    {
        // Ensure test data directory exists
        Directory.CreateDirectory(TestDataDirectory);
    }

    /// <summary>
    /// Stores scraped products to cache for later use in tests
    /// </summary>
    public static async Task StoreScrapedProductsAsync(IEnumerable<Product> products)
    {
        var productList = products.ToList();

        if (!productList.Any())
        {
            return;
        }

        var cacheData = new ScrapedProductCache
        {
            LastUpdated = DateTime.UtcNow,
            Products = productList.Select(p => new CachedProduct
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Brand = p.Brand,
                CurrentPrice = p.CurrentPrice,
                Currency = p.Currency,
                InStock = p.InStock,
                ProductUrl = p.ProductUrl,
                ImageUrl = p.ImageUrl,
                HasOffers = false, // TODO: Need to implement offer checking with new architecture
                OfferCount = 0     // TODO: Need to implement offer counting with new architecture
            }).ToList()
        };

        var json = JsonSerializer.Serialize(cacheData, JsonOptions);
        await File.WriteAllTextAsync(ProductCacheFile, json);
    }

    /// <summary>
    /// Retrieves cached products for use in tests
    /// </summary>
    public static async Task<IEnumerable<CachedProduct>> GetCachedProductsAsync()
    {
        if (!File.Exists(ProductCacheFile))
        {
            return Enumerable.Empty<CachedProduct>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(ProductCacheFile);
            var cacheData = JsonSerializer.Deserialize<ScrapedProductCache>(json, JsonOptions);

            // Return products cached within the last 24 hours
            if (cacheData != null && cacheData.LastUpdated > DateTime.UtcNow.AddHours(-24))
            {
                return cacheData.Products;
            }
        }
        catch (Exception)
        {
            // If there's an error reading the cache, return empty
        }

        return Enumerable.Empty<CachedProduct>();
    }

    /// <summary>
    /// Gets a random selection of cached products for test scenarios
    /// </summary>
    public static async Task<IEnumerable<CachedProduct>> GetRandomTestProductsAsync(int count = 3)
    {
        var cachedProducts = (await GetCachedProductsAsync()).ToList();

        if (!cachedProducts.Any())
        {
            // Return fallback test data if no cached products available
            return GetFallbackTestProducts();
        }

        // Filter products that should have offers available
        var productsWithOffers = cachedProducts
            .Where(p => p.InStock && !string.IsNullOrEmpty(p.Id))
            .ToList();

        if (productsWithOffers.Count <= count)
        {
            return productsWithOffers;
        }

        // Return random selection
        var random = new Random();
        return productsWithOffers
            .OrderBy(x => random.Next())
            .Take(count);
    }

    /// <summary>
    /// Fallback test products when no cached data is available
    /// </summary>
    private static IEnumerable<CachedProduct> GetFallbackTestProducts()
    {
        return new[]
        {
            new CachedProduct
            {
                Id = "102650292",
                Name = "Test Product 1",
                InStock = true,
                Category = "Test Category"
            },
            new CachedProduct
            {
                Id = "103790259",
                Name = "Test Product 2",
                InStock = true,
                Category = "Test Category"
            },
            new CachedProduct
            {
                Id = "102781958",
                Name = "Test Product 3",
                InStock = true,
                Category = "Test Category"
            }
        };
    }

    /// <summary>
    /// Checks if cached data is fresh (less than 24 hours old)
    /// </summary>
    public static async Task<bool> IsCacheValidAsync()
    {
        if (!File.Exists(ProductCacheFile))
        {
            return false;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ProductCacheFile);
            var cacheData = JsonSerializer.Deserialize<ScrapedProductCache>(json, JsonOptions);

            return cacheData?.LastUpdated > DateTime.UtcNow.AddHours(-24);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clears the cached test data
    /// </summary>
    public static void ClearCache()
    {
        if (File.Exists(ProductCacheFile))
        {
            File.Delete(ProductCacheFile);
        }
    }
}

/// <summary>
/// Represents the cached scraped product data
/// </summary>
public class ScrapedProductCache
{
    public DateTime LastUpdated { get; set; }
    public List<CachedProduct> Products { get; set; } = new();
}

/// <summary>
/// Simplified product information for test caching
/// </summary>
public class CachedProduct
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string Currency { get; set; } = "KZT";
    public bool InStock { get; set; }
    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }
    public bool HasOffers { get; set; }
    public int OfferCount { get; set; }
}
