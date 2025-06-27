namespace ProductSearchEngine.Scraper.Configuration;

/// <summary>
/// Interface defining marketplace-specific configuration for web scrapers
/// </summary>
public interface IMarketplaceConfiguration
{
    /// <summary>
    /// The name of the marketplace (e.g., "Kaspi.kz")
    /// </summary>
    string MarketplaceName { get; }

    /// <summary>
    /// Base URL pattern for category listings
    /// </summary>
    string CategoryUrlPattern { get; }

    /// <summary>
    /// CSS selector for product links on category page
    /// </summary>
    string ProductLinkSelector { get; }

    /// <summary>
    /// CSS selector for the container of product listings
    /// </summary>
    string ProductContainerSelector { get; }

    /// <summary>
    /// CSS selector for next page button/link
    /// </summary>
    string NextPageSelector { get; }

    /// <summary>
    /// JavaScript expression to check if next page exists
    /// </summary>
    string HasNextPageScript { get; }

    /// <summary>
    /// JavaScript action to navigate to next page
    /// </summary>
    string NextPageClickScript { get; }

    /// <summary>
    /// CSS selector to wait for after page load on product details page
    /// </summary>
    string ProductDetailLoadSelector { get; }

    /// <summary>
    /// JavaScript to extract product information from product details page
    /// </summary>
    string ProductDataExtractionScript { get; }
}