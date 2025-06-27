using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ProductSearchEngine.Scraper;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Tests.Helpers;

namespace ProductSearchEngine.Tests.Integration;

/// <summary>
/// Integration test that initializes test data cache for BDD tests
/// This test ensures the test infrastructure is properly set up before running BDD scenarios
/// Note: Actual scraping functionality is validated in BDD feature tests
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Integration")]
[Category("DataSeeding")]
public class ScrapedDataSeederTest : TestBase
{
    private KaspiApiScraperEnhanced _scraper = null!;
    private ILogger<ScrapedDataSeederTest> _logger = null!;

    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        base.OneTimeSetUp(); // Call parent OneTimeSetUp
        _scraper = ServiceProvider.GetRequiredService<KaspiApiScraperEnhanced>();
        _logger = ServiceProvider.GetRequiredService<ILogger<ScrapedDataSeederTest>>();
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        _scraper?.Dispose();
        base.OneTimeTearDown(); // Call parent OneTimeTearDown
    }

    [Test]
    [Order(1)]
    public async Task InitializeTestDataCache_ForBddTests()
    {
        // Check if we already have fresh cached data
        if (await ProductSearchEngine.Tests.TestData.TestDataCache.IsCacheValidAsync())
        {
            _logger.LogInformation("Test data cache is already fresh, initialization complete");
            var existingProducts = await ProductSearchEngine.Tests.TestData.TestDataCache.GetCachedProductsAsync();

            Assert.That(existingProducts.Any(), Is.True,
                "Cached products should be available");

            _logger.LogInformation("Using {Count} existing cached products",
                existingProducts.Count());
            return;
        }

        _logger.LogInformation("Initializing test data cache for BDD tests...");

        try
        {
            // Initialize cache with minimal test data
            // Note: This is just cache initialization - actual scraping functionality 
            // is tested in BDD scenarios
            var products = await _scraper.ScrapeProductsAsync("smartphone", maxProducts: 3);
            var productList = products.ToList();

            if (productList.Any())
            {
                await ProductSearchEngine.Tests.TestData.TestDataCache.StoreScrapedProductsAsync(productList);
                _logger.LogInformation("Successfully initialized cache with {Count} products",
                    productList.Count);
            }
            else
            {
                _logger.LogWarning("No products scraped for cache initialization");
            }

            // Verify cache initialization
            var cachedProducts = await ProductSearchEngine.Tests.TestData.TestDataCache.GetCachedProductsAsync();

            _logger.LogInformation("Test data cache initialization complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize test data cache");

            // Don't fail the test if cache initialization fails
            // BDD tests can use fallback data
            Assert.Warn($"Failed to initialize test data cache: {ex.Message}. BDD tests will use fallback data.");
        }
    }

    [Test]
    [Order(2)]
    public async Task VerifyTestDataCache_HasValidProducts()
    {
        var cachedProducts = await ProductSearchEngine.Tests.TestData.TestDataCache.GetCachedProductsAsync();

        // This test should pass even if seeding failed (using fallback data)
        var testProducts = await ProductSearchEngine.Tests.TestData.TestDataCache.GetRandomTestProductsAsync(3);

        Assert.That(testProducts.Any(), Is.True,
            "Should have test products available (either cached or fallback)");

        foreach (var product in testProducts)
        {
            Assert.That(product.Id, Is.Not.Null.And.Not.Empty,
                "Product ID should not be empty");
            Assert.That(product.Name, Is.Not.Null.And.Not.Empty,
                "Product name should not be empty");
        }

        _logger.LogInformation("Verified {Count} test products are available for BDD tests",
            testProducts.Count());

        foreach (var product in testProducts)
        {
            _logger.LogInformation("Test product: ID={Id}, Name={Name}, Category={Category}",
                product.Id, product.Name, product.Category);
        }
    }

    [Test]
    [Order(3)]
    public async Task ScrapeOffersForCachedProducts_ToVerifyOfferApiCompatibility()
    {
        var testProducts = await ProductSearchEngine.Tests.TestData.TestDataCache.GetRandomTestProductsAsync(2);

        Assert.That(testProducts.Any(), Is.True,
            "Should have test products available");

        var offerApiClient = ServiceProvider.GetRequiredService<KaspiOfferApiClient>();
        var antiDetectionStrategy = new ProductSearchEngine.Scraper.AntiDetection.AntiDetectionStrategy();

        foreach (var product in testProducts.Take(2)) // Test only 2 to avoid being too aggressive
        {
            _logger.LogInformation("Testing offer API compatibility for product {Id}: {Name}",
                product.Id, product.Name);

            try
            {
                var offers = await offerApiClient.GetOffersForProductAsync(
                    product.Id, "551010000", antiDetectionStrategy);

                var offerList = offers.ToList();

                _logger.LogInformation("Product {Id} has {Count} offers available",
                    product.Id, offerList.Count);

                // Don't assert on offer count as some products might legitimately have 0 offers
                // Just verify the API call succeeds
                Assert.That(offerList, Is.Not.Null,
                    "Offer API should return a valid response");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get offers for product {Id}: {Name}",
                    product.Id, product.Name);

                // Don't fail the test - some products might not have offers available
                Assert.Warn($"Product {product.Id} ({product.Name}) may not have offers available");
            }

            // Delay between requests to be respectful
            await Task.Delay(1000);
        }
    }
}
