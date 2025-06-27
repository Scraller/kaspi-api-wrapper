using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using Reqnroll;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper;
using ProductSearchEngine.Tests.Helpers;

namespace ProductSearchEngine.Tests.BDD.StepDefinitions.ProductScraping;

[Binding]
[NonParallelizable] // Prevent parallel execution due to shared state
public class KaspiProductScrapingStepDefinitions : TestBase
{
    private KaspiApiScraperEnhanced _scraper = null!;
    private string _category = string.Empty;
    private int _requestedProductCount;
    private IEnumerable<Product> _scrapedProducts = [];
    private List<Product> _firstSessionProducts = [];
    private readonly List<(int ProductCount, int CurrentPage)> _progressUpdates = [];
    private Stopwatch _scrapingStopwatch = new();
    private Exception? _scrapingException;

    /// <summary>
    /// Reset state between scenarios to avoid interference when running in parallel
    /// </summary>
    [BeforeScenario]
    public void ResetState()
    {
        _category = string.Empty;
        _requestedProductCount = 0;
        _scrapedProducts = [];
        _firstSessionProducts.Clear();
        _progressUpdates.Clear();
        _scrapingStopwatch.Reset();
        _scrapingException = null;
    }

    [Given(@"the scraper service is initialized")]
    public void GivenTheScraperServiceIsInitialized()
    {
        // Service provider should be automatically available via TestBase
        _scraper = GetScraperService();
        _scraper.Should().NotBeNull();
    }

    [Given(@"the scraper is configured for testing")]
    public void GivenTheScraperIsConfiguredForTesting()
    {
        // Set up event handlers for testing
        _scraper.ScrapingProgressUpdated += (sender, args) =>
        {
            _progressUpdates.Add((args.Status.ProductCount, args.Status.CurrentPage));
        };
    }

    [Given(@"I want to scrape ""([^""]*)"" category")]
    public void GivenIWantToScrapeCategory(string category)
    {
        _category = category;
        _category.Should().NotBeNullOrEmpty();
    }

    [Given(@"I request (\d+) products to be scraped")]
    public void GivenIRequestProductsToBeScraped(int productCount)
    {
        _requestedProductCount = productCount;
        _requestedProductCount.Should().BeGreaterThan(0);
    }

    [When(@"I request (\d+) products to be scraped")]
    public async Task WhenIRequestProductsToBeScraped(int productCount)
    {
        _requestedProductCount = productCount;
        await ExecuteScraping();
    }

    [Then(@"the scraping should complete successfully")]
    public void ThenTheScrapingShouldCompleteSuccessfully()
    {
        _scrapingException.Should().BeNull("Scraping should complete without exceptions");
        _scrapedProducts.Should().NotBeNull();
    }

    [Then(@"I should receive at least (\d+) products")]
    public void ThenIShouldReceiveAtLeastProducts(int minCount)
    {
        _scrapedProducts.Count().Should().BeGreaterOrEqualTo(minCount);
    }

    [Then(@"I should receive no more than (\d+) products")]
    public void ThenIShouldReceiveNoMoreThanProducts(int maxCount)
    {
        _scrapedProducts.Count().Should().BeLessOrEqualTo(maxCount);
    }

    [Then(@"I should receive (\d+) products")]
    public void ThenIShouldReceiveExactlyProducts(int expectedCount)
    {
        _scrapedProducts.Count().Should().Be(expectedCount);
    }

    [Then(@"all products should have valid data")]
    public void ThenAllProductsShouldHaveValidData()
    {
        foreach (var product in _scrapedProducts)
        {
            product.Id.Should().NotBeNullOrEmpty();
            product.Name.Should().NotBeNullOrEmpty();
            product.ProductUrl.Should().NotBeNullOrEmpty();
            product.ProductUrl.Should().StartWith("http");
        }
    }

    [Then(@"progress updates should be reported regularly")]
    public void ThenProgressUpdatesShouldBeReportedRegularly()
    {
        _progressUpdates.Should().NotBeEmpty("Progress updates should have been reported");
        _progressUpdates.Count.Should().BeGreaterThan(1, "Multiple progress updates should have been reported");
    }

    [Then(@"the product count should increase over time")]
    public void ThenTheProductCountShouldIncreaseOverTime()
    {
        _progressUpdates.Should().NotBeEmpty();

        var productCounts = _progressUpdates.Select(u => u.ProductCount).ToList();

        // Find the maximum product count reached
        var maxCount = productCounts.Max();
        maxCount.Should().BeGreaterThan(0, "Product count should increase during scraping");

        // Verify there's generally an increasing trend (allowing for some variation due to concurrency)
        var firstCount = productCounts.First();
        var lastCount = productCounts.Last();
        lastCount.Should().BeGreaterOrEqualTo(firstCount, "Product count should generally increase");
    }

    [Then(@"the current page should increase over time")]
    public void ThenTheCurrentPageShouldIncreaseOverTime()
    {
        _progressUpdates.Should().NotBeEmpty();

        var pages = _progressUpdates.Select(u => u.CurrentPage).ToList();
        var maxPage = pages.Max();
        maxPage.Should().BeGreaterThan(0, "Current page should advance during scraping");
    }

    [Then(@"no errors should be thrown")]
    public void ThenNoErrorsShouldBeThrown()
    {
        _scrapingException.Should().BeNull("No exceptions should have been thrown during scraping");
    }

    [Then(@"all products should have non-empty IDs")]
    public void ThenAllProductsShouldHaveNonEmptyIDs()
    {
        foreach (var product in _scrapedProducts)
        {
            product.Id.Should().NotBeNullOrEmpty($"Product ID should not be empty for product: {product.Name}");
        }
    }

    [Then(@"all products should have non-empty names")]
    public void ThenAllProductsShouldHaveNonEmptyNames()
    {
        foreach (var product in _scrapedProducts)
        {
            product.Name.Should().NotBeNullOrEmpty($"Product name should not be empty for product ID: {product.Id}");
        }
    }

    [Then(@"all products should have valid URLs")]
    public void ThenAllProductsShouldHaveValidURLs()
    {
        foreach (var product in _scrapedProducts)
        {
            product.ProductUrl.Should().NotBeNullOrEmpty($"Product URL should not be empty for product: {product.Name}");
            product.ProductUrl.Should().StartWith("http", $"Product URL should be valid for product: {product.Name}");
        }
    }

    [Then(@"products with prices should have valid currency information")]
    public void ThenProductsWithPricesShouldHaveValidCurrencyInformation()
    {
        foreach (var product in _scrapedProducts.Where(p => p.CurrentPrice > 0))
        {
            product.Currency.Should().NotBeNullOrEmpty($"Currency should be specified when price is available for product: {product.Name}");
        }
    }

    private async Task ExecuteScraping()
    {
        try
        {
            _scrapingStopwatch.Start();
            _scrapedProducts = await _scraper.ScrapeProductsAsync(_category, _requestedProductCount);
            _scrapingStopwatch.Stop();
        }
        catch (Exception ex)
        {
            _scrapingException = ex;
            _scrapingStopwatch.Stop();
        }
    }
}
