using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using ProductSearchEngine.Scraper.Services;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Models;
using System.Reflection;

namespace ProductSearchEngine.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class KaspiNavigationApiClientUnitTests
{
    private KaspiNavigationApiClient _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        // Create a real instance with mocked dependencies to test the parsing logic
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger>();
        var headerGenerator = new HeaderGenerator();
        var requestThrottler = new RequestThrottler();
        var sessionManager = new SessionManager();

        var kaspiHttpClient = new KaspiHttpClient(
            mockHttpClientFactory.Object,
            mockLogger.Object,
            headerGenerator,
            requestThrottler,
            sessionManager);

        var mockApiLogger = new Mock<ILogger<KaspiNavigationApiClient>>();
        _apiClient = new KaspiNavigationApiClient(kaspiHttpClient, mockApiLogger.Object);
    }

    [Test]
    [Description("Test JSON response parsing with sample data")]
    public void ParseNavigationApiResponse_WithJsonResponse_ShouldParseCorrectly()
    {
        // Arrange
        var sampleJsonResponse = @"{
            ""categories"": [
                {
                    ""name"": ""Smartphones and Gadgets"",
                    ""url"": ""/shop/c/smartphones%20and%20gadgets/"",
                    ""children"": [
                        {
                            ""name"": ""Smartphones"",
                            ""url"": ""/shop/c/smartphones/""
                        },
                        {
                            ""name"": ""Tablets"",
                            ""url"": ""/shop/c/tablets/""
                        }
                    ]
                },
                {
                    ""name"": ""Fashion"",
                    ""url"": ""/shop/c/fashion/""
                }
            ]
        }";

        // Act - Use reflection to call the private parsing method
        var result = InvokeParseNavigationApiResponse(sampleJsonResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.GreaterThan(0), "Should parse categories from JSON");

        // Check top-level categories
        var topLevel = result.Where(c => c.Level == 0).ToList();
        Assert.That(topLevel.Count, Is.EqualTo(2), "Should have 2 top-level categories");

        var smartphonesGadgets = topLevel.FirstOrDefault(c => c.Name == "Smartphones and Gadgets");
        Assert.That(smartphonesGadgets, Is.Not.Null, "Should find 'Smartphones and Gadgets' category");
        Assert.That(smartphonesGadgets!.Slug, Is.EqualTo("smartphones%20and%20gadgets"));

        // Check subcategories
        var subCategories = result.Where(c => c.Level == 1).ToList();
        Assert.That(subCategories.Count, Is.EqualTo(2), "Should have 2 subcategories");

        var smartphones = subCategories.FirstOrDefault(c => c.Name == "Smartphones");
        Assert.That(smartphones, Is.Not.Null, "Should find 'Smartphones' subcategory");
        Assert.That(smartphones!.ParentSlug, Is.EqualTo("smartphones%20and%20gadgets"));
    }

    [Test]
    [Description("Test XML response parsing with sample data")]
    public void ParseNavigationApiResponse_WithXmlResponse_ShouldParseCorrectly()
    {
        // Arrange
        var sampleXmlResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <navigation>
            <category>
                <name>Electronics</name>
                <url>/shop/c/electronics/</url>
            </category>
            <category>
                <name>Fashion</name>
                <url>/shop/c/fashion/</url>
            </category>
        </navigation>";

        // Act - Use reflection to call the private parsing method
        var result = InvokeParseNavigationApiResponse(sampleXmlResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2), "Should parse 2 categories from XML");

        var electronics = result.FirstOrDefault(c => c.Name == "Electronics");
        Assert.That(electronics, Is.Not.Null, "Should find 'Electronics' category");
        Assert.That(electronics!.Slug, Is.EqualTo("electronics"));
        Assert.That(electronics.Url, Is.EqualTo("https://kaspi.kz/shop/c/electronics/"));

        var fashion = result.FirstOrDefault(c => c.Name == "Fashion");
        Assert.That(fashion, Is.Not.Null, "Should find 'Fashion' category");
        Assert.That(fashion!.Slug, Is.EqualTo("fashion"));
    }

    [Test]
    [Description("Test empty response handling")]
    public void ParseNavigationApiResponse_WithEmptyResponse_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyResponse = "";

        // Act
        var result = InvokeParseNavigationApiResponse(emptyResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0), "Should return empty list for empty response");
    }

    [Test]
    [Description("Test invalid JSON handling")]
    public void ParseNavigationApiResponse_WithInvalidJson_ShouldReturnEmptyList()
    {
        // Arrange
        var invalidJson = @"{ ""invalid"": json }";

        // Act
        var result = InvokeParseNavigationApiResponse(invalidJson, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0), "Should return empty list for invalid JSON");
    }

    [Test]
    [Description("Test URL slug extraction")]
    [TestCase("/shop/c/smartphones/", "smartphones")]
    [TestCase("/shop/c/smartphones%20and%20gadgets/", "smartphones%20and%20gadgets")]
    [TestCase("https://kaspi.kz/shop/c/fashion/", "fashion")]
    [TestCase("/c/electronics/", "electronics")]
    public void ExtractSlugFromUrl_ShouldExtractCorrectSlug(string url, string expectedSlug)
    {
        // Arrange - Create a JSON response with the test URL
        var jsonResponse = $@"{{""categories"": [{{""name"": ""Test Category"", ""url"": ""{url}""}}]}}";

        // Act
        var result = InvokeParseNavigationApiResponse(jsonResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1), "Should parse one category");

        var category = result.First();
        Assert.That(category.Slug, Is.EqualTo(expectedSlug),
            $"Slug extraction failed for URL: {url}");
    }

    [Test]
    [Description("Test JSON array response parsing")]
    public void ParseNavigationApiResponse_WithJsonArray_ShouldParseCorrectly()
    {
        // Arrange
        var jsonArrayResponse = @"[
            {
                ""name"": ""Electronics"",
                ""url"": ""/shop/c/electronics/""
            },
            {
                ""name"": ""Fashion"",
                ""url"": ""/shop/c/fashion/""
            }
        ]";

        // Act
        var result = InvokeParseNavigationApiResponse(jsonArrayResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2), "Should parse 2 categories from JSON array");

        var electronics = result.FirstOrDefault(c => c.Name == "Electronics");
        Assert.That(electronics, Is.Not.Null, "Should find 'Electronics' category");
        Assert.That(electronics!.Slug, Is.EqualTo("electronics"));
    }

    [Test]
    [Description("Test nested JSON categories")]
    public void ParseNavigationApiResponse_WithNestedCategories_ShouldParseHierarchy()
    {
        // Arrange
        var nestedJsonResponse = @"{
            ""categories"": [
                {
                    ""name"": ""Electronics"",
                    ""url"": ""/shop/c/electronics/"",
                    ""children"": [
                        {
                            ""name"": ""Smartphones"",
                            ""url"": ""/shop/c/smartphones/"",
                            ""children"": [
                                {
                                    ""name"": ""iPhone"",
                                    ""url"": ""/shop/c/iphones/""
                                }
                            ]
                        }
                    ]
                }
            ]
        }";

        // Act
        var result = InvokeParseNavigationApiResponse(nestedJsonResponse, "test");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3), "Should parse all 3 levels of categories");

        // Check hierarchy levels
        var levelCounts = result.GroupBy(c => c.Level).ToDictionary(g => g.Key, g => g.Count());
        Assert.That(levelCounts[0], Is.EqualTo(1), "Should have 1 top-level category");
        Assert.That(levelCounts[1], Is.EqualTo(1), "Should have 1 second-level category");
        Assert.That(levelCounts[2], Is.EqualTo(1), "Should have 1 third-level category");

        // Check parent-child relationships
        var electronics = result.First(c => c.Name == "Electronics");
        var smartphones = result.First(c => c.Name == "Smartphones");
        var iphone = result.First(c => c.Name == "iPhone");

        Assert.That(smartphones.ParentSlug, Is.EqualTo(electronics.Slug));
        Assert.That(iphone.ParentSlug, Is.EqualTo(smartphones.Slug));
    }

    /// <summary>
    /// Helper method to invoke the private ParseNavigationApiResponse method using reflection
    /// </summary>
    private List<HierarchicalCategoryInfo> InvokeParseNavigationApiResponse(string content, string source)
    {
        var method = typeof(KaspiNavigationApiClient).GetMethod(
            "ParseNavigationApiResponse",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.That(method, Is.Not.Null, "ParseNavigationApiResponse method should exist");

        var result = method!.Invoke(_apiClient, new object[] { content, source });

        return (List<HierarchicalCategoryInfo>)result!;
    }
}
