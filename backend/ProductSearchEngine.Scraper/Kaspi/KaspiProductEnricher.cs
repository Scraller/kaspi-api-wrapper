using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Enriches basic product information with additional details
/// </summary>
public class KaspiProductEnricher
{
    private readonly ILogger _logger;
    private readonly KaspiHttpClient _httpClient;
    private readonly KaspiJsonParser _jsonParser;
    private readonly KaspiHumanSimulator _humanSimulator;
    private readonly Random _random = new();

    // API constants
    private const string PRODUCT_DETAIL_URL = "https://kaspi.kz/yml/product-view/main/";

    public KaspiProductEnricher(
        ILogger logger,
        KaspiHttpClient httpClient,
        KaspiJsonParser jsonParser,
        KaspiHumanSimulator humanSimulator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
        _humanSimulator = humanSimulator ?? throw new ArgumentNullException(nameof(humanSimulator));
    }

    /// <summary>
    /// Enriches a list of products with additional details from product pages
    /// </summary>
    public async Task EnrichProductDetails(
        List<Product> products,
        AntiDetectionStrategy strategy,
        string cityId)
    {
        foreach (var product in products)
        {
            try
            {
                if (string.IsNullOrEmpty(product.ProductUrl))
                {
                    _logger.LogWarning("Product URL is empty for product ID: {ProductId}", product.Id);
                    continue;
                }

                var detailProduct = await ScrapeProductDetailsAsync(product.ProductUrl, strategy, cityId);
                if (detailProduct != null)
                {
                    // Update with additional details
                    product.Description = detailProduct.Description;
                    product.Brand = detailProduct.Brand;
                    product.Model = detailProduct.Model;
                    product.Specifications = detailProduct.Specifications;

                    // Prevent too frequent requests
                    await Task.Delay(_random.Next(500, 2000));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich product details for ID: {ProductId}", product.Id);
            }
        }
    }

    /// <summary>
    /// Scrapes detailed information for a single product
    /// </summary>
    public async Task<Product?> ScrapeProductDetailsAsync(
        string productUrl,
        AntiDetectionStrategy strategy,
        string cityId)
    {
        // Extract product ID from URL
        var productId = _jsonParser.ExtractProductIdFromUrl(productUrl);
        if (string.IsNullOrEmpty(productId))
        {
            _logger.LogWarning("Failed to extract product ID from URL: {Url}", productUrl);
            return null;
        }

        try
        {
            // Apply human-like timing
            await _humanSimulator.SimulateHumanTiming(strategy);

            // For product details, we use the product detail endpoint
            var detailsUrl = $"{PRODUCT_DETAIL_URL}{productId}?c={cityId}";

            // Send the HTTP request
            var (success, content) = await _httpClient.SendRequestAsync(detailsUrl, strategy, cityId);

            if (success && !string.IsNullOrEmpty(content))
            {
                // Parse the JSON response
                return _jsonParser.ParseProductDetails(content, productId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping product details for ID: {ProductId}", productId);
        }

        return null;
    }
}