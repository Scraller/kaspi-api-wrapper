using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Services;

/// <summary>
/// Client for Kaspi Product Listing API
/// Based on discovered endpoint: /yml/product-view/pl/results
/// </summary>
public interface IKaspiProductListingApiClient
{
    /// <summary>
    /// Get products from category listing
    /// </summary>
    /// <param name="categoryCode">Category code or encoded query</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="cityCode">City code for regional results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product listing response</returns>
    Task<ProductListingResponse> GetCategoryProductsAsync(
        string categoryCode, 
        int page = 1, 
        string cityCode = "750000000",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search products using text query
    /// </summary>
    /// <param name="searchText">Search text</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="cityCode">City code for regional results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product listing response</returns>
    Task<ProductListingResponse> SearchProductsAsync(
        string searchText, 
        int page = 1, 
        string cityCode = "750000000",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for Kaspi Product Reviews API
/// Based on discovered endpoint: /yml/review-view/api/v1/reviews/product/{id}
/// </summary>
public interface IKaspiProductReviewsApiClient
{
    /// <summary>
    /// Get product reviews
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="filter">Review filter (COMMENT, RATING, etc.)</param>
    /// <param name="sort">Sort order (POPULARITY, DATE, RATING)</param>
    /// <param name="limit">Maximum number of reviews to return</param>
    /// <param name="withAgg">Include aggregated statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product reviews response</returns>
    Task<ProductReviewsResponse> GetProductReviewsAsync(
        string productId,
        string filter = "COMMENT",
        string sort = "POPULARITY", 
        int limit = 9,
        bool withAgg = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for Kaspi Product Filter API
/// Based on discovered endpoint: /yml/product-view/pl/filters
/// </summary>
public interface IKaspiProductFiltersApiClient
{
    /// <summary>
    /// Get available filters for products
    /// </summary>
    /// <param name="searchText">Search text (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="cityCode">City code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available filters response</returns>
    Task<object> GetProductFiltersAsync(
        string? searchText = null,
        int page = 1,
        string cityCode = "750000000",
        CancellationToken cancellationToken = default);
}
