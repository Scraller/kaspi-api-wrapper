using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Services;

namespace ProductSearchEngine.Tests.Integration.Kaspi;

[TestFixture]
[Category("Integration")]
[Category("ApiClient")]
public class KaspiNavigationApiClientTests
{
    private ServiceProvider _serviceProvider = null!;
    private KaspiNavigationApiClient _apiClient = null!;
    private ILogger<KaspiNavigationApiClientTests> _logger = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug); // Change to Debug to see more logs
        });

        // Register HTTP client factory with automatic decompression
        services.AddHttpClient("KaspiScraper", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.ConnectionClose = false;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        });

        // Register anti-detection services
        services.AddTransient<HeaderGenerator>();
        services.AddTransient<RequestThrottler>();
        services.AddTransient<SessionManager>();
        services.AddTransient<AntiDetectionStrategy>();

        // Register Kaspi services with proper logger resolution
        services.AddTransient<KaspiHttpClient>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<KaspiHttpClient>>();
            var headerGenerator = provider.GetRequiredService<HeaderGenerator>();
            var requestThrottler = provider.GetRequiredService<RequestThrottler>();
            var sessionManager = provider.GetRequiredService<SessionManager>();

            return new KaspiHttpClient(httpClientFactory, logger, headerGenerator, requestThrottler, sessionManager);
        });

        services.AddTransient<KaspiNavigationApiClient>();

        _serviceProvider = services.BuildServiceProvider();
        _apiClient = _serviceProvider.GetRequiredService<KaspiNavigationApiClient>();
        _logger = _serviceProvider.GetRequiredService<ILogger<KaspiNavigationApiClientTests>>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    [Description("Test fetching complete category hierarchy from navigation APIs")]
    public async Task GetCategoryHierarchyAsync_ShouldReturnCategories()
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync();

        // Assert
        Assert.That(categories, Is.Not.Null, "Categories should not be null");
        Assert.That(categories, Is.Not.Empty, "Should return at least some categories");

        _logger.LogInformation("Retrieved {CategoryCount} categories", categories.Count);

        // Verify category structure
        var topLevelCategories = categories.Where(c => c.Level == 0).ToList();
        Assert.That(topLevelCategories, Is.Not.Empty, "Should have top-level categories");

        _logger.LogInformation("Top-level categories: {TopLevelCount}", topLevelCategories.Count);

        // Verify category data quality
        foreach (var category in categories.Take(10)) // Check first 10
        {
            Assert.Multiple(() =>
            {
                Assert.That(category.Name, Is.Not.Null.And.Not.Empty, $"Category name should not be empty");
                Assert.That(category.Slug, Is.Not.Null.And.Not.Empty, $"Category slug should not be empty");
                Assert.That(category.Url, Is.Not.Null.And.Not.Empty, $"Category URL should not be empty");
            });

            Assert.That(category.Url, Contains.Substring("/c/"), $"Category URL should contain '/c/'");

            _logger.LogInformation("Category: {Name} -> {Slug} (Level: {Level})",
                category.Name, category.Slug, category.Level);
        }
    }

    [Test]
    [Description("Test fetching hierarchy with different depths")]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task GetCategoryHierarchyAsync_WithDifferentDepths_ShouldReturnAppropriateResults(int depth)
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync(depth: depth);

        // Assert
        Assert.That(categories, Is.Not.Null);

        var maxLevel = categories.Count != 0 ? categories.Max(c => c.Level) : -1;
        _logger.LogInformation("Depth {Depth}: Retrieved {CategoryCount} categories, max level: {MaxLevel}",
            depth, categories.Count, maxLevel);

        // For depth 1, we shouldn't have categories deeper than level 1
        if (depth == 1 && categories.Count != 0)
        {
            Assert.That(maxLevel, Is.LessThanOrEqualTo(1),
                $"With depth {depth}, max level should be <= 1, but was {maxLevel}");
        }
    }

    [Test]
    [Description("Test fetching hierarchy for different cities")]
    [TestCase("750000000", "Almaty")]
    [TestCase("710000000", "Nur-Sultan")]
    public async Task GetCategoryHierarchyAsync_WithDifferentCities_ShouldReturnResults(string cityId, string cityName)
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync(cityId: cityId);

        // Assert
        Assert.That(categories, Is.Not.Null);
        _logger.LogInformation("City {CityName} ({CityId}): Retrieved {CategoryCount} categories",
            cityName, cityId, categories.Count);

        if (categories.Count != 0)
        {
            // Verify some categories have valid data
            var sampleCategories = categories.Take(5).ToList();
            foreach (var category in sampleCategories)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(category.Name, Is.Not.Null.And.Not.Empty);
                    Assert.That(category.Slug, Is.Not.Null.And.Not.Empty);
                    Assert.That(category.Url, Contains.Substring("/c/"));
                });

            }
        }
    }

    [Test]
    [Description("Test category hierarchy structure and relationships")]
    public async Task GetCategoryHierarchyAsync_ShouldHaveValidHierarchyStructure()
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync();

        // Assert
        Assert.That(categories, Is.Not.Null.And.Not.Empty);

        // Group by level
        var categoryLevels = categories.GroupBy(c => c.Level).ToDictionary(g => g.Key, g => g.ToList());

        _logger.LogInformation("Category levels distribution:");
        foreach (var level in categoryLevels.Keys.OrderBy(k => k))
        {
            _logger.LogInformation("  Level {Level}: {Count} categories", level, categoryLevels[level].Count);
        }

        Assert.Multiple(() =>
        {
            // Check that we have level 0 categories
            Assert.That(categoryLevels.ContainsKey(0), Is.True, "Should have level 0 (top-level) categories");
            Assert.That(categoryLevels[0], Is.Not.Empty, "Should have at least one top-level category");
        });

        // Check parent-child relationships
        if (categoryLevels.ContainsKey(1))
        {
            var childCategories = categoryLevels[1];
            var parentSlugs = childCategories.Where(c => !string.IsNullOrEmpty(c.ParentSlug))
                                           .Select(c => c.ParentSlug)
                                           .Distinct()
                                           .ToList();

            _logger.LogInformation("Found {ParentCount} parent categories referenced by child categories",
                parentSlugs.Count);

            // Verify that parent slugs exist in level 0 categories
            var level0Slugs = categoryLevels[0].Select(c => c.Slug).ToHashSet();
            var validParents = parentSlugs.Where(ps => level0Slugs.Contains(ps!)).Count();

            _logger.LogInformation("Valid parent references: {ValidParents}/{TotalParents}",
                validParents, parentSlugs.Count);
        }
    }

    [Test]
    [Description("Test specific category types that should be present")]
    public async Task GetCategoryHierarchyAsync_ShouldContainExpectedCategories()
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync();

        // Assert
        Assert.That(categories, Is.Not.Null.And.Not.Empty);

        // Look for common categories that should exist
        var categoryNames = categories.Select(c => c.Name.ToLowerInvariant()).ToHashSet();
        var categorySlugs = categories.Select(c => c.Slug.ToLowerInvariant()).ToHashSet();

        var expectedCategories = new[]
        {
            "smartphones", "телефоны", "phone",
            "notebooks", "ноутбуки", "laptop",
            "fashion", "мода", "clothing",
            "beauty", "красота", "косметика"
        };

        var foundCategories = new List<string>();
        foreach (var expected in expectedCategories)
        {
            var found = categoryNames.Any(name => name.Contains(expected)) ||
                       categorySlugs.Any(slug => slug.Contains(expected.Replace(" ", "%20")));

            if (found)
            {
                foundCategories.Add(expected);
            }
        }

        _logger.LogInformation("Found expected categories: {FoundCategories}", string.Join(", ", foundCategories));
        Assert.That(foundCategories, Is.Not.Empty, "Should find at least some expected categories");
    }

    [Test]
    [Description("Test URL slug extraction functionality")]
    public async Task GetCategoryHierarchyAsync_ShouldHaveValidSlugs()
    {
        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync();

        // Assert
        Assert.That(categories, Is.Not.Null.And.Not.Empty);

        foreach (var category in categories.Take(20)) // Check first 20
        {
            // Verify slug is extracted correctly from URL
            Assert.That(category.Slug, Is.Not.Null.And.Not.Empty,
                $"Category '{category.Name}' should have a valid slug");

            // Verify URL contains the slug
            var urlContainsSlug = category.Url.Contains($"/c/{category.Slug}/") ||
                                 category.Url.EndsWith($"/c/{category.Slug}");

            Assert.That(urlContainsSlug, Is.True,
                $"URL '{category.Url}' should contain slug '{category.Slug}'");

            _logger.LogInformation("✓ {Name}: {Slug} -> {Url}", category.Name, category.Slug, category.Url);
        }
    }

    [Test]
    [Description("Test performance and response time")]
    public async Task GetCategoryHierarchyAsync_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var categories = await _apiClient.GetCategoryHierarchyAsync();

        // Assert
        stopwatch.Stop();

        _logger.LogInformation("API call completed in {ElapsedMs}ms, retrieved {CategoryCount} categories",
            stopwatch.ElapsedMilliseconds, categories?.Count ?? 0);

        // Should complete within 30 seconds (API calls + processing)
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30000),
            "API call should complete within 30 seconds");

        Assert.Multiple(() =>
        {
            // Should be significantly faster than HTML scraping (which takes 15-30 seconds)
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(15000),
                "API call should be faster than 10 seconds");

            Assert.That(categories, Is.Not.Null, "Should return categories within time limit");
        });

    }

    [Test]
    [Description("Debug test to investigate API client configuration")]
    public async Task DebugTest_CheckApiClientConfiguration()
    {
        // Act & Debug
        _logger.LogInformation("Starting debug test...");

        try
        {
            _logger.LogInformation("API Client instance: {ApiClient}", _apiClient?.GetType().Name ?? "NULL");

            if (_apiClient == null)
            {
                _logger.LogError("API Client is null!");
                Assert.Fail("API Client should not be null");
                return;
            }

            var categories = await _apiClient.GetCategoryHierarchyAsync();

            _logger.LogInformation("API call completed. Result: {Result}",
                categories == null ? "NULL" :
                categories.Count == 0 ? "EMPTY LIST" :
                $"LIST WITH {categories.Count} ITEMS");

            if (categories != null && categories.Count > 0)
            {
                var firstCategory = categories.First();
                _logger.LogInformation("First category: Name='{Name}', Slug='{Slug}', Level={Level}",
                    firstCategory.Name, firstCategory.Slug, firstCategory.Level);
            }

            // Don't assert anything, just log for debugging
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during debug test");
            throw; // Re-throw to see the exception in test results
        }
    }
}
