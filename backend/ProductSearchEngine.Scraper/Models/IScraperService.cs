namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Interface for scraper services
/// </summary>
public interface IScraperService
{
    /// <summary>
    /// Scrape products by category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <param name="limit">Maximum number of products to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scraped products</returns>
    Task<List<Product>> ScrapeProductsByCategoryAsync(string categoryId, int limit = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search for products by query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of products to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of found products</returns>
    Task<List<Product>> SearchProductsAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get detailed product information
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details or null if not found</returns>
    Task<Product?> GetProductDetailsAsync(string productId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get offers for a specific product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="cityCode">Optional city code for regional offers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of offers for the product</returns>
    Task<List<Offer>> GetProductOffersAsync(string productId, string? cityCode = null, CancellationToken cancellationToken = default);
}
